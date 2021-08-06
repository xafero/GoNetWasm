using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GoNetWasm.Data;
using GoNetWasm.Internal;
using GoNetWasm.Runtime;
using NLog;
using Wasmtime;

namespace GoNetWasm
{
    public class Go : IDisposable
    {
        private readonly FileSystem _fs = new FileSystem();

        private readonly ILogger _log;
        private readonly UTF8Encoding _encoding;
        private readonly Dictionary<int, object> _scheduledTimeouts;
        private readonly Dictionary<int, double> _goRefCounts;
        private readonly Dictionary<int, object> _values;
        private readonly Stack<int> _idPool;
        private readonly Dictionary<object, int> _ids;

        private int _nextCallbackTimeoutId;
        private Engine _engine;
        private Module _module;
        private Linker _linker;
        private IStore _store;
        private Instance _instance;

        public EventData PendingEvent { get; set; }
        private bool? Exited { get; set; }

        public Go(ILogger log)
        {
            _log = log;
            _encoding = new UTF8Encoding();
            _scheduledTimeouts = new Dictionary<int, object>();
            _goRefCounts = new Dictionary<int, double>();
            _values = new Dictionary<int, object>();
            _idPool = new Stack<int>();
            _ids = new Dictionary<object, int>();
            _nextCallbackTimeoutId = 1;
            PendingEvent = null;
            Exited = null;
        }

        public void Create(Engine engine, Func<Engine, Module> setup)
        {
            _engine = engine;
            _module = setup(engine);
            _store = new Store(_engine);
            _linker = new Linker(_engine);
        }

        public void Instantiate()
        {
            _instance = _linker.Instantiate(_store, _module);
        }

        public void Dispose()
        {
            _linker?.Dispose();
            _module?.Dispose();
            _engine.Dispose();
        }

        private void Exit(int code)
        {
            if (code != 0)
                Console.Error.WriteLine($"exit code: {code}");
        }

        private Memory Mem => _instance.GetMemory(_store, "mem");
        private Span<byte> MemBuffer() => Mem.GetSpan(_store);

        #region Compatibility layer
        /// <summary>
        /// func clearTimeoutEvent(id int32)
        /// </summary>
        private void ClearTimeoutEvent(int sp)
        {
            sp >>= 0;
            var id = Mem.ReadInt32(_store, sp + 8);
            ClearTimeout(_scheduledTimeouts[id]);
            _scheduledTimeouts.Remove(id);
            _log.Trace("clearTimeoutEvent {id}", id);
        }

        /// <summary>
        /// func getRandomData(r []byte)
        /// </summary>
        private void GetRandomData(int sp)
        {
            sp >>= 0;
            var buf = LoadSlice(sp + 8);
            Crypto.GetRandomValues(buf);
            _log.Trace("getRandomData {buf}", buf.Length);
        }

        /// <summary>
        /// func finalizeRef(v ref)
        /// </summary>
        private void FinalizeRef(int sp)
        {
            sp >>= 0;
            var id = Mem.ReadInt32(_store, sp + 8);
            _goRefCounts[id]--;
            if (_goRefCounts[id] == 0)
            {
                var v = _values[id];
                _values[id] = null;
                _ids.Remove(v);
                _idPool.Push(id);
            }
            _log.Trace("finalizeRef {id}", id);
        }

        /// <summary>
        /// func nanotime1() int64
        /// </summary>
        private void NanoTime(int sp)
        {
            sp >>= 0;
            var longVal = ProcSystem.GetNanoTime() * 1000;
            SetInt64(sp + 8, longVal);
            _log.Trace("nanoTime {val}", longVal);
        }

        /// <summary>
        /// func walltime1() (sec int64, nsec int32)
        /// </summary>
        private void WallTime(int sp)
        {
            sp >>= 0;
            var msec = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var mVal = msec / 1000;
            SetInt64(sp + 8, mVal);
            var val = (msec % 1000) * 1000000;
            Mem.WriteInt32(_store, sp + 16, (int) val);
            _log.Trace("walltime {mVal} {val}", mVal, val);
        }

        /// <summary>
        /// func stringVal(value string) ref
        /// </summary>
        private void StringVal(int sp)
        {
            sp >>= 0;
            var txt = LoadString(sp + 8);
            StoreValue(sp + 24, txt);
            _log.Trace("stringVal '{val}'", txt);
        }

        /// <summary>
        /// func valueInvoke(v ref, args []ref) (ref, bool)
        /// </summary>
        private void ValueInvoke(int sp)
        {
            sp >>= 0;
            try
            {
                var v = LoadValue(sp + 8);
                var args = LoadSliceOfValues(sp + 16);
                var result = Reflect.Apply(v, JsUndefined.S, args);
                sp = GetSp() >> 0;
                StoreValue(sp + 40, result);
                Mem.WriteByte(_store, sp + 48, 1);
                _log.Trace("valueInvoke {v} {args} {result}", v, args, result);
            }
            catch (Exception err)
            {
                StoreValue(sp + 40, err);
                Mem.WriteByte(_store, sp + 48, 0);
            }
        }

        /// <summary>
        /// func valueNew(v ref, args []ref) (ref, bool)
        /// </summary>
        private void ValueNew(int sp)
        {
            sp >>= 0;
            try
            {
                var v = LoadValue(sp + 8);
                var args = LoadSliceOfValues(sp + 16);
                var result = Reflect.Construct(v, args);
                sp = GetSp() >> 0;
                StoreValue(sp + 40, result);
                Mem.WriteByte(_store, sp + 48, 1);
                _log.Trace("valueNew {v} {args} {result}", v, args, result);
            }
            catch (Exception err)
            {
                StoreValue(sp + 40, err);
                Mem.WriteByte(_store, sp + 48, 0);
            }
        }

        /// <summary>
        /// func valueLength(v ref) int
        /// </summary>
        private void ValueLength(int sp)
        {
            sp >>= 0;
            var val = LoadValue(sp + 8);
            var num = ((ICollection) val).Count;
            SetInt64(sp + 16, num);
            _log.Trace("valueLength {val} {num}", val, num);
        }

        /// <summary>
        /// valuePrepareString(v ref) (ref, int)
        /// </summary>
        private void ValuePrepareString(int sp)
        {
            sp >>= 0;
            var val = LoadValue(sp + 8);
            var txt = (string) val;
            var str = _encoding.GetBytes(txt);
            StoreValue(sp + 16, str);
            SetInt64(sp + 24, str.Length);
        }

        private void SetInt64(int addr, long v)
        {
            Mem.WriteInt32(_store, addr + 0, (int) v);
            Mem.WriteInt32(_store, addr + 4, (int) Math.Floor(v / 4294967296.0));
        }

        private int GetSp()
        {
            var getter = _instance.GetFunction(_store, "getsp");
            var res = getter?.Invoke(_store);
            return (int?) res ?? -1;
        }

        /// <summary>
        /// valueLoadString(v ref, b []byte)
        /// </summary>
        private void ValueLoadString(int sp)
        {
            sp >>= 0;
            var str = (ICollection<byte>) LoadValue(sp + 8);
            var bytes = LoadSlice(sp + 16);
            bytes.Refill(str);
        }

        private long GetInt64(int addr)
        {
            var low = Mem.ReadInt32(_store, addr + 0);
            var high = Mem.ReadInt32(_store, addr + 4);
            return low + high * 4294967296;
        }

        private object SetTimeout(Action action, long interval)
        {
            throw new InvalidOperationException(nameof(SetTimeout));
        }

        private object LoadValue(long addr)
        {
            var f = Mem.ReadDouble(_store, (int) addr);

            if (f == 0)
                return JsUndefined.S;

            if (!double.IsNaN(f))
                return f;

            var id = Mem.ReadInt32(_store, (int) addr);
            return _values[id];
        }

        private object[] LoadSliceOfValues(int addr)
        {
            var array = GetInt64(addr + 0);
            var len = GetInt64(addr + 8);
            var a = new object[len];
            for (var i = 0; i < len; i++)
                a[i] = LoadValue(array + i * 8);
            return a;
        }

        /// <summary>
        /// func copyBytesToGo(dst []byte, src ref) (int, bool)
        /// </summary>
        private void CopyBytesToGo(int sp)
        {
            sp >>= 0;
            var dst = LoadSlice(sp + 8);
            var src = LoadValue(sp + 32);
            if (!(src is byte[] || src is ICollection<byte>))
            {
                Mem.WriteByte(_store, sp + 48, 0);
                return;
            }
            var srcArray = (ICollection<byte>) src;
            var toCopy = srcArray.Skip(0).Take(dst.Length).ToArray();
            dst.Refill(toCopy);
            SetInt64(sp + 40, toCopy.Length);
            Mem.WriteByte(_store, sp + 48, 1);
            _log.Trace("copyBytesToGo {src} {dst} {toCopy}", srcArray.Count, dst.Length, toCopy.Length);
        }

        /// <summary>
        /// func copyBytesToJS(dst ref, src []byte) (int, bool)
        /// </summary>
        private void CopyBytesToJs(int sp)
        {
            sp >>= 0;
            var dst = LoadValue(sp + 8);
            var src = LoadSlice(sp + 16);
            if (!(dst is byte[] || dst is ICollection<byte>))
            {
                Mem.WriteByte(_store, sp + 48, 0);
                return;
            }
            var dstArray = (ICollection<byte>) dst;
            var toCopy = src.Slice(0, dstArray.Count);
            dstArray.Refill(toCopy);
            SetInt64(sp + 40, toCopy.Length);
            Mem.WriteByte(_store, sp + 48, 1);
            _log.Trace("copyBytesToJS {src} {dst} {toCopy}", src.Length, dstArray.Count, toCopy.Length);
        }

        private string LoadString(int addr)
        {
            var saddr = GetInt64(addr + 0);
            var len = GetInt64(addr + 8);
            var bytes = MemBuffer().ToArray();
            return _encoding.GetString(bytes, (int) saddr, (int) len);
        }

        public Func<object[], object> MakeFuncWrapper(double id)
        {
            var go = this;

            object FuncWrap(params object[] args)
            {
                var @event = new EventData {Id = id, This = null, Args = args};
                go.PendingEvent = @event;
                go.Resume();
                return @event.Result;
            }

            return FuncWrap;
        }

        /// <summary>
        /// func valueGet(v ref, p string) ref
        /// </summary>
        private void ValueGet(int sp)
        {
            sp >>= 0;
            var @ref = LoadValue(sp + 8);
            var str = LoadString(sp + 16);
            var result = Reflect.Get(@ref, str);
            sp = GetSp() >> 0;
            StoreValue(sp + 32, result);
            _log.Trace("valueGet {ref} {str} {result}", @ref, str, result);
        }

        /// <summary>
        /// func valueSet(v ref, p string, x ref)
        /// </summary>
        private void ValueSet(int sp)
        {
            sp >>= 0;
            var obj = LoadValue(sp + 8);
            var name = LoadString(sp + 16);
            var val = LoadValue(sp + 32);
            Reflect.Set(obj, name, val);
            _log.Trace("valueSet {obj} {name} {val}", obj, name, val);
        }

        /// <summary>
        /// func valueDelete(v ref, p string)
        /// </summary>
        private void ValueDelete(int sp)
        {
            sp >>= 0;
            var obj = LoadValue(sp + 8);
            var name = LoadString(sp + 16);
            Reflect.DeleteProperty(obj, name);
            _log.Trace("valueDelete {obj} {name}", obj, name);
        }

        /// <summary>
        /// func valueIndex(v ref, i int) ref
        /// </summary>
        private void ValueIndex(int sp)
        {
            sp >>= 0;
            var obj = LoadValue(sp + 8);
            var index = GetInt64(sp + 16);
            var item = Reflect.Get(obj, index);
            StoreValue(sp + 24, item);
            _log.Trace("valueIndex {obj} {index} {item}", obj, index, item);
        }

        private void StoreValue(int addr, object v)
        {
            const int nanHead = 0x7FF80000;

            double vd;
            if (v.IsNumber() && (v + "") != "0" && (vd = double.Parse(v + "")) != 0)
            {
                if (double.IsNaN(vd))
                {
                    Mem.WriteInt32(_store, addr + 4, nanHead);
                    Mem.WriteInt32(_store, addr, 0);
                    return;
                }
                Mem.WriteDouble(_store, addr, vd);
                return;
            }

            if (v.IsJsUndefined())
            {
                Mem.WriteDouble(_store, addr, 0);
                return;
            }

            if (!_ids.TryGetValue(v, out var id))
            {
                if (!_idPool.TryPop(out id))
                    id = _values.Count;
                _values[id] = v;
                _goRefCounts[id] = 0;
                _ids[v] = id;
            }
            _goRefCounts[id]++;

            var typeFlag = 0;
            switch (v)
            {
                case Delegate _:
                    typeFlag = 4;
                    break;
                case "symbol":
                    typeFlag = 3;
                    break;
                case string _:
                    typeFlag = 2;
                    break;
                case JsNull _:
                    typeFlag = 0;
                    break;
                case bool _:
                    typeFlag = 0;
                    break;
                case { }:
                    typeFlag = 1;
                    break;
            }

            Mem.WriteInt32(_store, addr + 4, nanHead | typeFlag);
            Mem.WriteInt32(_store, addr, id);
            _log.Trace("storeValue {typeFlag} {id} {addr}", typeFlag, id, addr);
        }

        /// <summary>
        /// valueSetIndex(v ref, i int, x ref)
        /// </summary>
        private void ValueSetIndex(int sp)
        {
            sp >>= 0;
            var obj = LoadValue(sp + 8);
            var index = GetInt64(sp + 16);
            var val = LoadValue(sp + 24);
            Reflect.Set(obj, index, val);
        }

        /// <summary>
        /// func valueCall(v ref, m string, args []ref) (ref, bool)
        /// </summary>
        private void ValueCall(int sp)
        {
            sp >>= 0;
            try
            {
                var v = LoadValue(sp + 8);
                var name = LoadString(sp + 16);
                var m = Reflect.Get(v, name);
                var args = LoadSliceOfValues(sp + 32);
                var result = Reflect.Apply(m, v, args);
                sp = GetSp() >> 0;
                StoreValue(sp + 56, result);
                Mem.WriteByte(_store, sp + 64, 1);
                _log.Trace("valueCall {v} {name} {m} {args} {result}", v, name, m, args, result);
            }
            catch (Exception err)
            {
                StoreValue(sp + 56, err);
                Mem.WriteByte(_store, sp + 64, 0);
            }
        }

        private Span<byte> LoadSlice(int addr)
        {
            var array = GetInt64(addr + 0);
            var len = GetInt64(addr + 8);
            var bytes = MemBuffer();
            return bytes.Slice((int) array, (int) len);
        }

        /// <summary>
        /// func wasmExit(code int32)
        /// </summary>
        private void WasmExit(int sp)
        {
            sp >>= 0;
            var code = Mem.ReadInt32(_store, sp + 8);
            Exited = true;
            _instance = null;
            _values.Clear();
            _goRefCounts.Clear();
            _ids.Clear();
            _idPool.Clear();
            Exit(code);
            _log.Trace("wasmExit {code}", code);
        }

        /// <summary>
        /// func wasmWrite(fd uintptr, p unsafe.Pointer, n int32)
        /// </summary>
        private void WasmWrite(int sp)
        {
            sp >>= 0;
            var fd = GetInt64(sp + 8);
            var p = GetInt64(sp + 16);
            var n = Mem.ReadInt32(_store, sp + 24);
            var slice = MemBuffer().Slice((int) p, n);
            _fs.WriteSync(fd, slice.ToArray());
            _log.Trace("wasmWrite {fd} {p} {n} {slice}", fd, p, n, slice.Length);
        }

        private void Debug(int value)
        {
            Console.WriteLine(value);
        }

        private void ClearTimeout(object o)
        {
            throw new NotImplementedException(nameof(ClearTimeout));
        }

        /// <summary>
        /// func resetMemoryDataView()
        /// </summary>
        private void ResetMemoryDataView(int sp)
        {
            sp >>= 0;
            _log.Trace("resetMemoryDataView");
        }

        /// <summary>
        /// func valueInstanceOf(v ref, t ref) bool
        /// </summary>
        private void ValueInstanceOf(int sp)
        {
            sp >>= 0;
            var @ref = LoadValue(sp + 8);
            var tr = LoadValue(sp + 16);
            _log.Trace("valueInstanceOf {ref} {tr}", @ref, tr);
        }

        /// <summary>
        /// func scheduleTimeoutEvent(delay int64) int32
        /// </summary>
        private void ScheduleTimeoutEvent(int sp)
        {
            sp >>= 0;
            var id = _nextCallbackTimeoutId;
            _nextCallbackTimeoutId++;
            _scheduledTimeouts[id] = SetTimeout(() =>
                {
                    Resume();
                    while (_scheduledTimeouts.ContainsKey(id))
                    {
                        // for some reason Go failed to register the timeout event, log and try again
                        // (temporary workaround for https://github.com/golang/go/issues/28975)
                        Console.Error.WriteLine("scheduleTimeoutEvent: missed timeout event");
                        Resume();
                    }
                },
                // setTimeout has been seen to fire up to 1 millisecond early
                GetInt64(sp + 8) + 1
            );
            Mem.WriteInt32(_store, sp + 16, id);
            _log.Trace("scheduleTimeoutEvent {id}", id);
        }

        private void Resume()
        {
            if (Exited == true)
            {
                throw new InvalidOperationException("Go program has already exited");
            }
            var resume = _instance.GetFunction(_store, "resume");
            resume?.Invoke(_store);
            if (Exited == true)
            {
            }
        }

        public void Run()
        {
            var env = new Dictionary<string, object>();
            var argv = new[] {"js"};

            if (_instance == null)
            {
                throw new InvalidOperationException("Go.run: WebAssembly.Instance expected");
            }

            var global = new Globals();
            var @null = JsNull.S;

            // JS values that Go currently has references to, indexed by reference id
            _values.Refill(new object[]
            {
                double.NaN, 0, @null, true, false, global, this
            });

            // number of references that Go has to a JS value, indexed by reference id
            _goRefCounts.Refill(Enumerable.Repeat(double.PositiveInfinity, _values.Count));

            // mapping from JS values to reference ids
            _ids.Refill(new (object, int)[]
            {
                (0, 1),
                (@null, 2),
                (true, 3),
                (false, 4),
                (global, 5),
                (this, 6)
            });

            // unused ids that have been garbage collected
            _idPool.Clear();

            // whether the Go program has exited
            Exited = false;

            // Pass command line arguments and environment variables to WebAssembly by writing them to the linear memory
            var offset = 4096;

            var strPtr = new Func<string, int>(str =>
            {
                var ptr = offset;
                var bytes = _encoding.GetBytes(str + "\0");
                var slice = MemBuffer().Slice(offset, bytes.Length);
                slice.Refill(bytes);
                offset += bytes.Length;
                if (offset % 8 != 0)
                {
                    offset += 8 - (offset % 8);
                }
                return ptr;
            });

            var argc = argv.Length;

            var argvPtrs = new List<int>();
            Array.ForEach(argv, arg => argvPtrs.Add(strPtr(arg)));
            argvPtrs.Add(0);

            var keys = env.Keys.OrderBy(k => k).ToArray();
            Array.ForEach(keys, key => argvPtrs.Add(strPtr($"{key}={env[key]}")));
            argvPtrs.Add(0);

            var localArgv = offset;
            Array.ForEach(argvPtrs.ToArray(), ptr =>
            {
                Mem.WriteInt32(_store, offset, ptr);
                Mem.WriteInt32(_store, offset + 4, 0);
                offset += 8;
            });

            var run = _instance.GetFunction(_store, "run");
            run?.Invoke(_store, argc, localArgv);

            if (Exited == true)
            {
            }
        }

        /// <summary>
        /// Define all methods needed by Golang
        /// </summary>
        public void ImportObject()
        {
            DefineMethod("debug", Debug);
            DefineMethod("runtime.resetMemoryDataView", ResetMemoryDataView);
            DefineMethod("runtime.wasmExit", WasmExit);
            DefineMethod("runtime.wasmWrite", WasmWrite);
            DefineMethod("runtime.nanotime1", NanoTime);
            DefineMethod("runtime.walltime1", WallTime);
            DefineMethod("runtime.scheduleTimeoutEvent", ScheduleTimeoutEvent);
            DefineMethod("runtime.clearTimeoutEvent", ClearTimeoutEvent);
            DefineMethod("runtime.getRandomData", GetRandomData);
            DefineMethod("syscall/js.finalizeRef", FinalizeRef);
            DefineMethod("syscall/js.stringVal", StringVal);
            DefineMethod("syscall/js.valueGet", ValueGet);
            DefineMethod("syscall/js.valueSet", ValueSet);
            DefineMethod("syscall/js.valueDelete", ValueDelete);
            DefineMethod("syscall/js.valueIndex", ValueIndex);
            DefineMethod("syscall/js.valueSetIndex", ValueSetIndex);
            DefineMethod("syscall/js.valueCall", ValueCall);
            DefineMethod("syscall/js.valueInvoke", ValueInvoke);
            DefineMethod("syscall/js.valueNew", ValueNew);
            DefineMethod("syscall/js.valueLength", ValueLength);
            DefineMethod("syscall/js.valuePrepareString", ValuePrepareString);
            DefineMethod("syscall/js.valueLoadString", ValueLoadString);
            DefineMethod("syscall/js.valueInstanceOf", ValueInstanceOf);
            DefineMethod("syscall/js.copyBytesToGo", CopyBytesToGo);
            DefineMethod("syscall/js.copyBytesToJS", CopyBytesToJs);
        }

        private void DefineMethod(string name, Action<int> callback)
        {
            const string module = "go";
            var func = Function.FromCallback(_store, callback);
            _linker.Define(module, name, func);
        }
        #endregion

        public override string ToString() => nameof(Go);
    }
}