# LibImageQuant.Net

[![CI](https://github.com/JohSand/LibImageQuant.Net/actions/workflows/ci.yml/badge.svg)](https://github.com/JohSand/LibImageQuant.Net/actions/workflows/ci.yml)

.NET bindings for [libimagequant](https://github.com/ImageOptim/libimagequant) (the color
quantization engine behind [pngquant](https://pngquant.org/)) and
[zopfli](https://github.com/google/zopfli) (Google's exhaustive-search DEFLATE compressor),
plus a small managed PNG encoder/decoder to tie them together. Point it at an RGB(A) image and
get back a palettized PNG that's substantially smaller than what most encoders produce, with no
external process to shell out to - everything runs in-process via P/Invoke.

Targets **.NET 10**. Native binaries are currently built for **win-x64** and **linux-x64**.

## Packages

| Package | What it does |
|---|---|
| `LibImageQuant.Net.Core` | P/Invoke bindings to libimagequant. `Quantizer` + `QuantizationResult`: turns raw RGBA pixels into a palette + indexed pixel data. |
| `LibImageQuant.Net.Codec` | A minimal managed PNG reader/writer. `Decoder` reads PNG into pixels; `Coder` writes a `QuantizationResult` back out as an indexed-color PNG. |
| `Zopfli.Net` | P/Invoke bindings to zopfli. `ZopfliStream` is a drop-in `Stream` compressor that produces smaller DEFLATE output than `System.IO.Compression.ZLibStream`, at the cost of being much slower. |

These aren't on NuGet yet - see [Building from source](#building-from-source) below, or
[Status](#status).

## Quick start

Decode a PNG, quantize it down to a palette, and write it back out - using Zopfli for the best
compression and an adaptive filter search on top:

```csharp
using LibImageQuant.Net.Codec;
using LibImageQuant.Net.Core;
using Zopfli.Net;
using Decoder = LibImageQuant.Net.Codec.Decoder;

var sourceBytes = File.ReadAllBytes("photo.png");
using var source = Decoder.ReadPng(sourceBytes);

IProvideImages provider = source.ColorType switch
{
    ColorType.RGBA => new ManagedProvider<ARGBFiller>(source),
    ColorType.RGB => new ManagedProvider<RGBFiller>(source),
    _ => throw new NotSupportedException("Source is already palettized"),
};

using var quantizer = new Quantizer
{
    MaxColors = 128,
    DitheringLevel = 0.6f,
};
using var result = quantizer.Quantize(provider, source.Width, source.Height);

var coder = new Coder(
    source.Width, source.Height,
    compressorStream: s => new ZopfliStream(s, leaveOpen: true),
    filterStrategy: PngFilterStrategy.Adaptive);

byte[] outputPng = coder.CreateBytes(result);
File.WriteAllBytes("photo-quantized.png", outputPng);
```

A few things worth knowing about the pieces above:

- **`compressorStream`** defaults to the built-in `ZLibStream` if you don't pass one. Swap in
  `ZopfliStream` (shown above) when output size matters more than encode time - it's
  meaningfully slower but reliably produces smaller files (see
  [benchmarks](benchmarks/LibImageQuant.Net.Benchmarks)).
- **`filterStrategy`** controls PNG scanline filtering and defaults to `PngFilterStrategy.None`.
  `Coder` only ever writes indexed-color (palette) PNGs, and per the PNG spec's own guidance,
  filtering indexed pixel data essentially never helps - `Adaptive` (and the other non-`None`
  strategies) are there for completeness and for inputs where it's worth double-checking, not
  because they're expected to beat `None` on typical output from this library.
- **`IProvideImages`** decouples quantization from any particular pixel layout - `ManagedProvider`
  adapts `Decoder`'s output, but you can implement it directly (e.g. to quantize pixels you
  already have in memory from somewhere other than a decoded PNG). If you already have
  contiguous RGBA bytes, `Quantizer.Quantize(ReadOnlySpan<byte> imageBytes, int width, int height)`
  skips the adapter entirely.

## Building from source

The managed side is a normal .NET solution:

```bash
dotnet build LibImageQuant.Net.sln
dotnet test LibImageQuant.Net.sln
```

But `LibImageQuant.Net.Core` and `Zopfli.Net` P/Invoke into native libraries that have to be
built first, from the `native/libimagequant` and `native/zopfli` submodules:

```bash
git submodule update --init --recursive
```

`libimagequant` v4+ is a Rust crate, built into a C-ABI shared library via
[cargo-c](https://github.com/lu-zero/cargo-c):

```bash
cargo install cargo-c
```

`zopfli` is built via CMake (see `native/CMakeLists.txt`, which also builds `zopflibridge` - a
tiny shim so managed code can free `ZopfliCompress`'s output through zopfli's own allocator).

With those in place:

```bash
# linux-x64
./native/build.sh

# win-x64
pwsh ./native/build.ps1
```

Each script stages its output under `native/runtimes/<rid>/native/` and the `Core`/`Zopfli.Net`
projects pick it up automatically from there - both for local `dotnet build`/`dotnet test` on
the matching platform, and for `dotnet pack`. `.github/workflows/ci.yml` runs this same sequence
on every push for both platforms.

## Status

Actively maintained, but pre-1.0 and not yet published to NuGet - the release pipeline
(`.github/workflows/release.yml`) is wired up end-to-end (build native → pack → push), it just
needs a `NUGET_API_KEY` configured against a `nuget-publish` GitHub environment before a tagged
push will actually publish anything. Until then, build from source as above.

## License

[GPL-3.0-only](LICENSE), inherited from libimagequant.
