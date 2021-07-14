using System;
using System.IO;
using System.IO.Compression;
using System.Reflection.Emit;
using System.Reflection;

namespace LibImageQuant.Net.Codec
{
    public static class DeflateStreamHelpers
    {
        public static readonly Func<Stream, CompressionMode, bool, int, long, DeflateStream> ZlibStream = CreateDelegate();

        private static Func<Stream, CompressionMode, bool, int, long, DeflateStream> CreateDelegate()
        {
            var method = new DynamicMethod("CreateDeflateStream", typeof(DeflateStream), new Type[] { typeof(Stream), typeof(CompressionMode), typeof(bool), typeof(int), typeof(long) }, restrictedSkipVisibility: false);
            ILGenerator il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_3);
            il.Emit(OpCodes.Ldarg, 4);
            il.Emit(OpCodes.Newobj, typeof(DeflateStream).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Stream), typeof(CompressionMode), typeof(bool), typeof(int), typeof(long) }, null)!);
            il.Emit(OpCodes.Ret);
            return (Func<Stream, CompressionMode, bool, int, long, DeflateStream>)method.CreateDelegate(typeof(Func<Stream, CompressionMode, bool, int, long, DeflateStream>));
        }
    }
}
