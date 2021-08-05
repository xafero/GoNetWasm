package main

import (
	"fmt"
	"os"
	"runtime"
)

func main() {
	fmt.Printf("OS   = %s\n", runtime.GOOS)
	fmt.Printf("ARCH = %s\n", runtime.GOARCH)
	fmt.Printf("ROOT = %s\n", runtime.GOROOT())
	pwd, _ := os.Getwd()
	fmt.Printf("DIR  = %s\n", pwd)
}
