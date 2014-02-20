/*
 * Conversion to .NET by Emanuel Dejanu
 * Copyright (c) 2000-2014, Michael Alyn Miller <malyn@strangeGizmo.com>
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 *
 * 1. Redistributions of source code must retain the above copyright
 *    notice unmodified, this list of conditions, and the following
 *    disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. Neither the name of Michael Alyn Miller nor the names of the
 *    contributors to this software may be used to endorse or promote
 *    products derived from this software without specific prior written
 *    permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR AND CONTRIBUTORS ``AS IS'' AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 * OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
 */

using System.IO;

namespace System.Data.ConstantDatabase
{
    internal static class CdbUtils
    {
        /// <summary>
        /// Computes and returns the hash value for the given key.
        /// </summary>
        /// <param name="key"> The key to compute the hash value for.</param>
        /// <returns>The hash value of <code>key</code>.</returns>
        public static int Hash(byte[] key)
        {
            /* Initialize the hash value. */
            long h = 5381;

            /* Add each byte to the hash value. */
            for (int i = 0; i < key.Length; i++)
            {
                //			h = ((h << 5) + h) ^ key[i];
                long l = h << 5;
                h += (l & 0x00000000ffffffffL);
                h = (h & 0x00000000ffffffffL);

                int k = key[i];
                k = (k + 0x100) & 0xff;

                h = h ^ k;
            }

            /* Return the hash value. */
            return unchecked((int) (h & 0x00000000ffffffffL));
        }

        /// <summary>
        /// Reads a little-endian integer from specified stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>The read 32 bit integer</returns>
        public static int ReadLittleEndianInt32(Stream stream)
        {
            return (stream.ReadByte() & 0xff) | ((stream.ReadByte() & 0xff) << 8) | ((stream.ReadByte() & 0xff) << 16) |
                   ((stream.ReadByte() & 0xff) << 24);
        }

        /// <summary>
        /// Writes an integer in little-endian format to the specified stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="value">The integer to write to the stream.</param>
        public static void WriteLittleEndianInt32(Stream stream, int value)
        {
            stream.WriteByte(unchecked((byte)(value & 0xff)));
            stream.WriteByte(unchecked((byte)(((int)((uint)value >> 8)) & 0xff)));
            stream.WriteByte(unchecked((byte)(((int)((uint)value >> 16)) & 0xff)));
            stream.WriteByte(unchecked((byte)(((int)((uint)value >> 24)) & 0xff)));
        }
    }
}
