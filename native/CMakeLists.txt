# Umbrella project used only by build.ps1 (Windows). Building zopflibridge
# alongside zopfli's own libzopfli target, in the same cmake configure/build
# invocation, means both get CMake's same default runtime-library settings -
# so zopflibridge shares zopfli's CRT allocator and can safely free memory
# zopfli allocated. See zopfli_free_shim.c for why that matters.
cmake_minimum_required(VERSION 3.15)
project(LibImageQuantNetNative)

add_subdirectory(zopfli)

add_library(zopflibridge SHARED zopfli_free_shim.c)
