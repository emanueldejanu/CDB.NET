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

namespace System.Data.ConstantDatabase
{
    /// <summary>
    /// CdbElement represents a single entry in a constant database.
    /// </summary>
    public sealed class CdbEntry
    {
        /// <summary>The key value for this entry.</summary>
        private readonly byte[] key;

        /// <summary>The data value for this entry.</summary>
        private readonly byte[] data;

        /// <summary>
        /// Initializes a new instance of the <see cref="CdbEntry"/> class.
        /// </summary>
        /// <param name="key">The key for the new entry.</param>
        /// <param name="data">The data for the new entru.</param>
        public CdbEntry(byte[] key, byte[] data)
        {
            this.key = key;
            this.data = data;
        }


        /// <summary>
        /// Returns this entry's key.
        /// </summary>
        /// <returns>This entry's key.</returns>
        public byte[] Key
        {
            get { return key; }
        }

        /// <summary>
        /// Returns this entry's data.
        /// </summary>
        /// <returns>This entry's data.</returns>
        public byte[] Data
        {
            get { return data; }
        }
    }
}