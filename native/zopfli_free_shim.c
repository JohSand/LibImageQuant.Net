/*
 * ZopfliCompress() (native/zopfli/src/zopfli/zopfli.h) allocates its output
 * buffer with the C runtime's malloc/realloc and documents that the caller
 * must free it. Freeing it from managed code with Marshal.FreeHGlobal is
 * only safe by coincidence: on Linux, .NET's AllocHGlobal/FreeHGlobal call
 * the same process-wide glibc malloc/free that libzopfli.so also uses. On
 * Windows, Marshal.FreeHGlobal calls LocalFree (a Win32 heap API), which is
 * a different heap than the CRT malloc used inside zopfli.dll - freeing
 * CRT-allocated memory that way is undefined behavior.
 *
 * This tiny library is built alongside zopfli with matching CRT settings
 * (see native/CMakeLists.txt for Windows, native/build.sh for Linux) so it
 * shares zopfli's allocator and can free its output buffer correctly on
 * both platforms. See src/Zopfli.Net/Zopfli.cs.
 */
#include <stdlib.h>

void ZopfliNetFree(void* ptr) {
    free(ptr);
}
