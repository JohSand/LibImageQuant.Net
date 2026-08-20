[CmdletBinding()]
Param(
    [Parameter(Position=0,Mandatory=$false,ValueFromRemainingArguments=$true)]
    [string[]]$BuildArguments
)

Write-Output "PowerShell $($PSVersionTable.PSEdition) version $($PSVersionTable.PSVersion)"

Set-StrictMode -Version 2.0; $ErrorActionPreference = "Stop"; $ConfirmPreference = "None"; trap { Write-Error $_ -ErrorAction Continue; exit 1 }
$PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent
Set-Location $PSScriptRoot

# Builds the native libraries for win-x64 and lays them out under
# runtimes/win-x64/native/, matching the NuGet native-asset convention
# consumed by src/LibImageQuant.Net.Core and src/Zopfli.Net.

$Rid = "win-x64"
$OutDir = Join-Path $PSScriptRoot "runtimes\$Rid\native"
New-Item -Force -Path $OutDir -ItemType Directory | Out-Null

# imagequant
#
# libimagequant v4+ is a Rust crate; cargo-c (https://github.com/lu-zero/cargo-c) builds its
# C-ABI-compatible shared library. Install it once with:
#   cargo install cargo-c

$RustTarget = "x86_64-pc-windows-msvc"

cd libimagequant/imagequant-sys

cargo cbuild --release --target $RustTarget

# cargo-c builds into ../target (the imagequant-sys/imagequant workspace root), matching
# upstream's own Makefile convention (--target-dir=../target), not imagequant-sys/target.
Copy-Item -Force -Path "../target/$RustTarget/release/imagequant.dll" -Destination $OutDir

cd ../..


# zopfli + zopflibridge
#
# Built together from native/CMakeLists.txt (an umbrella project that
# add_subdirectory()s zopfli and adds a zopflibridge target alongside it)
# rather than from zopfli's own CMakeLists.txt directly, so both targets get
# the same default CRT/runtime-library settings in one cmake invocation. See
# zopfli_free_shim.c for why zopflibridge needs to match zopfli's allocator.

new-item -Force -Name build-zopfli -ItemType directory | Out-Null

cd build-zopfli

# zopfli/CMakeLists.txt (Google's, not ours) declares cmake_minimum_required(VERSION 2.8.11);
# modern CMake dropped support for configuring anything below 3.5 outright, refusing to even
# parse it otherwise. CMAKE_POLICY_VERSION_MINIMUM is the documented escape hatch for old
# projects like this one that still work fine, they just haven't updated that line in years.
# Quoted as one token: unquoted, PowerShell passes "-DX=3.5" to the native cmake.exe split into
# two arguments at the decimal point ("-DX=3" and ".5"), which cmake then rejects/misreads.
cmake -A x64 "-DCMAKE_POLICY_VERSION_MINIMUM=3.5" -DCMAKE_WINDOWS_EXPORT_ALL_SYMBOLS=TRUE -DBUILD_SHARED_LIBS=TRUE ..
cmake --build . --config Release
# libzopfli is defined inside zopfli/CMakeLists.txt, reached via our add_subdirectory(zopfli) -
# the Visual Studio generator nests each subdirectory's own targets under a matching build-tree
# folder, so its output lands at zopfli\Release\zopfli.dll, not flat under Release\ like
# zopflibridge (defined directly in our own top-level native/CMakeLists.txt).
Copy-Item -Force -Path "zopfli\Release\zopfli.dll" -Destination $OutDir
Copy-Item -Force -Path "Release\zopflibridge.dll" -Destination $OutDir
cd ..