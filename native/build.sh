#!/bin/bash

# Builds the native libraries for the current platform and lays them out
# under runtimes/<rid>/native/, matching the NuGet native-asset convention
# consumed by src/LibImageQuant.Net.Core and src/Zopfli.Net.

set -eo pipefail
SCRIPT_DIR=$(cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd)

RID=linux-x64
RUST_TARGET=x86_64-unknown-linux-gnu
OUT_DIR="$SCRIPT_DIR/runtimes/$RID/native"
mkdir -p "$OUT_DIR"

# libimagequant v4+ is a Rust crate; cargo-c (https://github.com/lu-zero/cargo-c) builds its
# C-ABI-compatible shared library. Install it once with:
#   cargo install cargo-c
cd "$SCRIPT_DIR/libimagequant/imagequant-sys"
cargo cbuild --release
# cargo-c builds into ../target (the imagequant-sys/imagequant workspace root), matching
# upstream's own Makefile convention (--target-dir=../target), not imagequant-sys/target.
cp "../target/$RUST_TARGET/release/libimagequant.so" "$OUT_DIR/libimagequant.so"
cd "$SCRIPT_DIR"

cd "$SCRIPT_DIR/zopfli"
make libzopfli
cp libzopfli.so.1.0.3 "$OUT_DIR/libzopfli.so"
cd "$SCRIPT_DIR"

# See zopfli_free_shim.c: lets managed code free ZopfliCompress's output
# buffer through zopfli's own allocator. On Linux this is unambiguous either
# way (one process-wide glibc heap), so a plain standalone build is fine.
gcc -shared -fPIC -O3 -o "$OUT_DIR/libzopflibridge.so" "$SCRIPT_DIR/zopfli_free_shim.c"