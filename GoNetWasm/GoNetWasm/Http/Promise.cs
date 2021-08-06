using System;
using System.Threading.Tasks;

namespace GoNetWasm.Http
{
    internal class Promise<T> : IDisposable
    {
        private readonly IDisposable _handle;
        private readonly Task<T> _task;
        private readonly Func<T, object> _wrapper;

        public Promise(IDisposable handle, Task<T> task, Func<T, object> wrapper)
        {
            _handle = handle;
            _task = task;
            _wrapper = wrapper;
        }

        public void Then(Func<object[], object> onSuccess, Func<object[], object> onFailure)
        {
            try
            {
                var result = _task.GetAwaiter().GetResult();
                var wrap = _wrapper(result);
                onSuccess(new[] {wrap});
            }
            catch (Exception e)
            {
                var err = new Error(e.Message)
                {
                    Code = e.GetType().Name.ToUpperInvariant()
                };
                onFailure(new object[] {err});
            }
        }

        public void Dispose()
        {
            _handle?.Dispose();
            _task?.Dispose();
        }

        public override string ToString() => nameof(Promise<T>);
    }
}