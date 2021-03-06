#!/bin/bash


set -eo pipefail
SCRIPT_DIR=$(cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd)

mkdir -p Release
cd libimagequant
./configure --prefix=/usr
make libimagequant.so
cp libimagequant.so ../Release/libimagequant.so
cd ..


cd zopfli
make libzopfli
cp libzopfli.so.1.0.3 ../Release/libzopfli.so
cd ..