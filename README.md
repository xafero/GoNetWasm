# GoNetWasm
A .NET project for running Go(lang)'s WebAssembly code

## Compile your Go
* cd Examples/Go/Hello
* GOOS=js GOARCH=wasm go build -o main.wasm

## Run it in .NET
* WasmExec ./main.wasm

## Acknowledgements
* Google for the language
* Go's community in general
* Bytecode Alliance for Wasmtime
