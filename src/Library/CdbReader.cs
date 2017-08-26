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

using System.Collections.Generic;
using System.IO;

namespace System.Data.ConstantDatabase
{
    /// <summary>
    /// Cdb implements a .NET interface to D. J. Bernstein's CDB database.
    /// </summary>
    public class CdbReader : IDisposable
    {
        /// <summary>The <see cref="Stream"/> of the CDB file.</summary>
        private Stream fileStream;

        /// <summary>
        /// The slot pointers, cached here for efficiency as we do not have
        /// mmap() to do it for us.  These entries are paired as (pos, len)
        /// tuples. 
        /// </summary>
        private readonly int[] slotTable;

        /// <summary>The number of hash slots searched under this key.</summary>
        private int numberOfHashSlotsSearched;

        /// <summary>
        /// The hash value for the current key.</summary>
        private int currentKeyHashValue;

        /// <summary>The number of hash slots in the hash table for the current key.</summary>
        private int numberOfHashSlots;

        /// <summary>The position of the hash table for the current key.</summary>
        private int currentKeyHashTablePos;

        /// <summary>The position of the current key in the slot.</summary>
        private int currentKeyPos;

        /// <summary>
        /// Creates an instance of the Cdb class and loads the given CDB file.
        /// </summary>
        /// <param name="filepath"> The path to the CDB file to open.</param>
        /// <exception cref="System.IO.IOException"> if the CDB file could not be
        ///  opened.</exception>
        public CdbReader(string filepath)
        {
            /* Open the CDB file. */
            fileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read);

            /* Read and parse the slot table.  We do not throw an exception
			 * if this fails; the file might empty, which is not an error. */
            try
            {
                /* Read the table. */
                byte[] table = new byte[2048];
                fileStream.Read(table, 0, table.Length);

                /* Create and parse the table. */
                slotTable = new int[256*2];

                int offset = 0;
                for (int i = 0; i < 256; i++)
                {
                    int pos = table[offset++] & 0xff | ((table[offset++] & 0xff) << 8) |
                              ((table[offset++] & 0xff) << 16) | ((table[offset++] & 0xff) << 24);

                    int len = table[offset++] & 0xff | ((table[offset++] & 0xff) << 8) |
                              ((table[offset++] & 0xff) << 16) | ((table[offset++] & 0xff) << 24);

                    slotTable[i << 1] = pos;
                    slotTable[(i << 1) + 1] = len;
                }
            }
            catch (IOException)
            {
                slotTable = null;
            }
        }

        /// <summary>
        /// Release the associated stream.
        /// </summary>
        public void Dispose()
        {
            if (fileStream != null)
            {
                fileStream.Dispose();
                fileStream = null;
            }
        }

        /// <summary>
        /// Closes the CDB database.
        /// </summary>
        public void Close()
        {
            Dispose();
        }

        /// <summary>
        /// Prepares the class to search for the given key.
        /// </summary>
        /// <param name="key"> The key to search for. </param>
        public void FindStart(byte[] key)
        {
            numberOfHashSlotsSearched = 0;
        }

        /// <summary>
        /// Finds the first record stored under the given key.
        /// </summary>
        /// <param name="key"> The key to search for. </param>
        /// <returns> The record store under the given key, or
        ///  <code>null</code> if no record with that key could be found. </returns>
        public byte[] Find(byte[] key)
        {
            lock (this)
            {
                FindStart(key);
                return FindNext(key);
            }
        }

        /// <summary>
        /// Finds the next record stored under the given key.
        /// </summary>
        /// <param name="key"> The key to search for. </param>
        /// <returns> The next record store under the given key, or
        ///  <code>null</code> if no record with that key could be found. </returns>
        public byte[] FindNext(byte[] key)
        {
            lock (this)
            {
                /* There are no keys if we could not read the slot table. */
                if (slotTable == null)
                {
                    return null;
                }

                /* Locate the hash entry if we have not yet done so. */
                if (numberOfHashSlotsSearched == 0)
                {
                    /* Get the hash value for the key. */
                    int u = CdbUtils.Hash(key);

                    /* Unpack the information for this record. */
                    int slot = u & 255;
                    numberOfHashSlots = slotTable[(slot << 1) + 1];
                    if (numberOfHashSlots == 0)
                    {
                        return null;
                    }
                    currentKeyHashTablePos = slotTable[slot << 1];

                    /* Store the hash value. */
                    currentKeyHashValue = u;

                    /* Locate the slot containing this key. */
                    u = (int) ((uint) u >> 8);
                    u %= numberOfHashSlots;
                    u <<= 3;
                    currentKeyPos = currentKeyHashTablePos + u;
                }

                /* Search all of the hash slots for this key. */
                try
                {
                    while (numberOfHashSlotsSearched < numberOfHashSlots)
                    {
                        /* Read the entry for this key from the hash slot. */
                        fileStream.Seek(currentKeyPos, SeekOrigin.Begin);

                        int h = fileStream.ReadByte() | (fileStream.ReadByte() << 8) | (fileStream.ReadByte() << 16) |
                                (fileStream.ReadByte() << 24);

                        int pos = fileStream.ReadByte() | (fileStream.ReadByte() << 8) | (fileStream.ReadByte() << 16) |
                                  (fileStream.ReadByte() << 24);
                        if (pos == 0)
                        {
                            return null;
                        }

                        /* Advance the loop count and key position.  Wrap the
						 * key position around to the beginning of the hash slot
						 * if we are at the end of the table. */
                        numberOfHashSlotsSearched += 1;

                        currentKeyPos += 8;
                        if (currentKeyPos == (currentKeyHashTablePos + (numberOfHashSlots << 3)))
                        {
                            currentKeyPos = currentKeyHashTablePos;
                        }

                        /* Ignore this entry if the hash values do not match. */
                        if (h != currentKeyHashValue)
                        {
                            continue;
                        }

                        /* Get the length of the key and data in this hash slot
						 * entry. */
                        fileStream.Seek(pos, SeekOrigin.Begin);

                        int klen = fileStream.ReadByte() | (fileStream.ReadByte() << 8) | (fileStream.ReadByte() << 16) |
                                   (fileStream.ReadByte() << 24);
                        if (klen != key.Length)
                        {
                            continue;
                        }

                        int dlen = fileStream.ReadByte() | (fileStream.ReadByte() << 8) | (fileStream.ReadByte() << 16) |
                                   (fileStream.ReadByte() << 24);

                        /* Read the key stored in this entry and compare it to
						 * the key we were given. */
                        bool match = true;
                        byte[] k = new byte[klen];
                        fileStream.Read(k, 0, k.Length);
                        for (int i = 0; i < k.Length; i++)
                        {
                            if (k[i] != key[i])
                            {
                                match = false;
                                break;
                            }
                        }

                        /* No match; check the next slot. */
                        if (!match)
                        {
                            continue;
                        }

                        /* The keys match, return the data. */
                        byte[] d = new byte[dlen];
                        fileStream.Read(d, 0, d.Length);
                        return d;
                    }
                }
                catch (IOException)
                {
                    return null;
                }

                /* No more data values for this key. */
                return null;
            }
        }

        /// <summary>
        /// Returns an <see cref="IEnumerator{CdbEntry}"/> containing a <see cref="CdbEntry"/> for each entry in
        /// the constant database.
        /// </summary>
        /// <param name="fileName">The CDB file name to read. </param>
        /// <returns> An <see cref="IEnumerator{CdbEntry}"/> containing a <see cref="CdbEntry"/> for each entry in
        ///  the constant database.</returns>
        /// <exception cref="System.IO.IOException">if an error occurs reading the
        ///  constant database.</exception>
        public static IEnumerator<CdbEntry> Entries(string fileName)
        {
            /* Open the data file. */
            FileStream @in = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            /* Read the end-of-data value. */
            int eod = (@in.ReadByte() & 0xff) | ((@in.ReadByte() & 0xff) << 8) | ((@in.ReadByte() & 0xff) << 16) |
                      ((@in.ReadByte() & 0xff) << 24);

            /* Skip the rest of the hashtable. */
            @in.Seek(2048 - 4, SeekOrigin.Current);

            /* Return the Enumeration. */
            return new CdbEntryEnumerator(@in, eod);
        }
    }
}