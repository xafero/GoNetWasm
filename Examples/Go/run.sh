#!/bin/sh
GOOS=js GOARCH=wasm go build -o main.wasm
../../../GoNetWasm/WasmExec/bin/Debug/netcoreapp3.1/WasmExec ./main.wasm
