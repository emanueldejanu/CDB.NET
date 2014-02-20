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

using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace System.Data.ConstantDatabase
{
    /// <summary>
    /// CDB entries enumerator
    /// </summary>
    internal class CdbEntryEnumerator : IEnumerator<CdbEntry>
    {
        private readonly Stream cdbStream;
        private readonly int endOfData;

        /* Current data pointer. */
        private int pos = 2048;

        /// <summary>
        /// Initializes a new instance of the <see cref="CdbEntryEnumerator"/> class.
        /// </summary>
        /// <param name="cdbStream">The CDB stream.</param>
        /// <param name="endOfData">The end of data.</param>
        public CdbEntryEnumerator(Stream cdbStream, int endOfData)
        {
            this.cdbStream = cdbStream;
            this.endOfData = endOfData;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            cdbStream.Close();
        }

        /* Returns the next data element in the CDB file. */
        private CdbEntry NextElement()
        {
            try
            {
                /* Read the key and value lengths. */
                int klen = CdbUtils.ReadLittleEndianInt32(cdbStream);
                pos += 4;
                int dlen = CdbUtils.ReadLittleEndianInt32(cdbStream);
                pos += 4;

                /* Read the key. */
                byte[] key = new byte[klen];
                for (int off = 0; off < klen; ) // below
                {
                    int count = cdbStream.Read(key, off, klen - off);
                    if (count == -1)
                    {
                        throw new ArgumentException("invalid cdb format");
                    }
                    off += count;
                }
                pos += klen;

                /* Read the data. */
                byte[] data = new byte[dlen];
                for (int off = 0; off < dlen; ) // below
                {
                    int count = cdbStream.Read(data, off, dlen - off);
                    if (count == -1)
                    {
                        throw new ArgumentException("invalid cdb format");
                    }
                    off += count;
                }
                pos += dlen;

                /* Return a CdbElement with the key and data. */
                return new CdbEntry(key, data);
            }
            catch (IOException)
            {
                throw new ArgumentException("invalid cdb format");
            }
        }

        /// <summary>
        /// Advances the enumerator to the next element of the CDB.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next entry; false if the enumerator has passed the end of the CDB.
        /// </returns>
        public bool MoveNext()
        {
            if (pos >= endOfData)
                return false;

            Current = NextElement();

            return true;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the CDB.
        /// </summary>
        public void Reset()
        {
            pos = 2048;
        }

        /// <summary>
        /// Gets the entry in the CDB at the current position of the enumerator.
        /// </summary>
        /// <returns>The entry in the CDB at the current position of the enumerator.</returns>
        public CdbEntry Current { get; private set; }

        /// <summary>
        /// Gets the entry in the CDB at the current position of the enumerator.
        /// </summary>
        /// <returns>The entry in the CDB at the current position of the enumerator.</returns>
        object IEnumerator.Current
        {
            get { return Current; }
        }
    }
}