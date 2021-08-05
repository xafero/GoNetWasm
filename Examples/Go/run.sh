#!/bin/sh
echo " ::: Run natively..."
go run main.go
echo " ::: Compiling to WASM..."
GOOS=js GOARCH=wasm go build -o main.wasm
echo " ::: Run with dotnet..."
../../../GoNetWasm/WasmExec/bin/Debug/netcoreapp3.1/WasmExec ./main.wasm
echo " ::: Done."
