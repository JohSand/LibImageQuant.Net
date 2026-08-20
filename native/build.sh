#!/bin/bash

# Builds the native libraries for the current platform and lays them out
# under runtimes/<rid>/native/, matching the NuGet native-asset convention
# consumed by src/LibImageQuant.Net.Core and src/Zopfli.Net.

set -eo pipefail
SCRIPT_DIR=$(cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd)

RID=linux-x64
OUT_DIR="$SCRIPT_DIR/runtimes/$RID/native"
mkdir -p "$OUT_DIR"

cd "$SCRIPT_DIR/libimagequant"
./configure --prefix=/usr
make libimagequant.so
cp libimagequant.so "$OUT_DIR/libimagequant.so"
cd "$SCRIPT_DIR"

cd "$SCRIPT_DIR/zopfli"
make libzopfli
cp libzopfli.so.1.0.3 "$OUT_DIR/libzopfli.so"
cd "$SCRIPT_DIR"