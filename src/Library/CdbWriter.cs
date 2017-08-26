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
    /// CdbMake implements the database-creation side of
    /// D. J. Bernstein's constant database package.
    /// </summary>
    public sealed class CdbWriter : IDisposable
    {
        /// <summary>The <see cref="Stream"/> for the CDB file.</summary>
        private Stream fileStream;

        /// <summary>The list of hash pointers in the file, in their order in the constant database.</summary>
        private readonly List<CdbHashPointer> hashPointers;

        /// <summary>The number of entries in each hash table.</summary>
        private readonly int[] tableCount;

        /// <summary>The first entry in each table.</summary>
        private readonly int[] tableStart;


        /// <summary>The position of the current key in the constant database.</summary>
        private int currentKeyPos;

        /// <summary>
        /// Constructs a CdbMake object and begins the constant database creation process.
        /// </summary>
        /// <param name="filePath">The path to the constant database to create.</param>
        /// <exception cref="System.IO.IOException">If an error occurs creating the
        ///  constant database file.</exception>
        public CdbWriter(string filePath)
        {
            /* Initialize the class. */
            currentKeyPos = -1;
            hashPointers = new List<CdbHashPointer>();
            tableCount = new int[256];
            tableStart = new int[256];

            /* Clear the table counts. */
            for (int i = 0; i < 256; i++)
            {
                tableCount[i] = 0;
            }

            /* Open the temporary CDB file. */
            fileStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.ReadWrite);

            /* Seek to the end of the header. */
            currentKeyPos = 2048;
            fileStream.Seek(currentKeyPos, SeekOrigin.Begin);
        }

        /// <summary>
        /// Adds a key to the constant database.
        /// </summary>
        /// <param name="key">The key to add to the database.</param>
        /// <param name="data">The data associated with this key.</param>
        /// <exception cref="System.IO.IOException">If an error occurs adding the key
        ///  to the database.</exception>
        public void Add(byte[] key, byte[] data)
        {
            /* Write out the key length. */
            CdbUtils.WriteLittleEndianInt32(fileStream, key.Length);

            /* Write out the data length. */
            CdbUtils.WriteLittleEndianInt32(fileStream, data.Length);

            /* Write out the key. */
            fileStream.Write(key, 0, key.Length);

            /* Write out the data. */
            fileStream.Write(data, 0, data.Length);


            /* Add the hash pointer to our list. */
            int hash = CdbUtils.Hash(key);
            hashPointers.Add(new CdbHashPointer(hash, currentKeyPos));

            /* Add this item to the count. */
            tableCount[hash & 0xff]++;


            /* Update the file position pointer. */
            PosPlus(8);
            PosPlus(key.Length);
            PosPlus(data.Length);
        }

        /// <summary>
        /// Finalizes the constant database.
        /// </summary>
        /// <exception cref="System.IO.IOException">If an error occurs closing out the
        ///  database.</exception>
        public void Finish()
        {
            if (fileStream == null)
                return;

            /* Find the start of each hash table. */
            int curEntry = 0;
            for (int i = 0; i < 256; i++)
            {
                curEntry += tableCount[i];
                tableStart[i] = curEntry;
            }

            /* Create a new hash pointer list in order by hash table. */
            CdbHashPointer[] slotPointers = new CdbHashPointer[hashPointers.Count];
            foreach (CdbHashPointer hp in hashPointers)
            {
                slotPointers[--tableStart[hp.HashValue & 0xff]] = hp;
            }

            /* Write out each of the hash tables, building the slot table in
			 * the process. */
            byte[] slotTable = new byte[2048];
            for (int i = 0; i < 256; i++)
            {
                /* Get the length of the hashtable. */
                int len = tableCount[i]*2;

                /* Store the position of this table in the slot table. */
                slotTable[(i*8) + 0] = unchecked((byte) (currentKeyPos & 0xff));
                slotTable[(i*8) + 1] = unchecked((byte) (((int) ((uint) currentKeyPos >> 8)) & 0xff));
                slotTable[(i*8) + 2] = unchecked((byte) (((int) ((uint) currentKeyPos >> 16)) & 0xff));
                slotTable[(i*8) + 3] = unchecked((byte) (((int) ((uint) currentKeyPos >> 24)) & 0xff));
                slotTable[(i*8) + 4 + 0] = unchecked((byte) (len & 0xff));
                slotTable[(i*8) + 4 + 1] = unchecked((byte) (((int) ((uint) len >> 8)) & 0xff));
                slotTable[(i*8) + 4 + 2] = unchecked((byte) (((int) ((uint) len >> 16)) & 0xff));
                slotTable[(i*8) + 4 + 3] = unchecked((byte) (((int) ((uint) len >> 24)) & 0xff));

                /* Build the hash table. */
                int curSlotPointer = tableStart[i];
                CdbHashPointer[] hashTable = new CdbHashPointer[len];
                for (int u = 0; u < tableCount[i]; u++)
                {
                    /* Get the hash pointer. */
                    CdbHashPointer hp = slotPointers[curSlotPointer++];

                    /* Locate a free space in the hash table. */
                    int @where = ((int) ((uint) hp.HashValue >> 8))%len;
                    while (hashTable[@where] != null)
                    {
                        if (++@where == len)
                        {
                            @where = 0;
                        }
                    }

                    /* Store the hash pointer. */
                    hashTable[@where] = hp;
                }

                /* Write out the hash table. */
                for (int u = 0; u < len; u++)
                {
                    CdbHashPointer hp = hashTable[u];
                    if (hp != null)
                    {
                        CdbUtils.WriteLittleEndianInt32(fileStream, hashTable[u].HashValue);
                        CdbUtils.WriteLittleEndianInt32(fileStream, hashTable[u].EntryPos);
                    }
                    else
                    {
                        CdbUtils.WriteLittleEndianInt32(fileStream, 0);
                        CdbUtils.WriteLittleEndianInt32(fileStream, 0);
                    }
                    PosPlus(8);
                }
            }

            /* Seek back to the beginning of the file and write out the
			 * slot table. */
            fileStream.Seek(0, SeekOrigin.Begin);
            fileStream.Write(slotTable, 0, slotTable.Length);

            /* Close the file. */
            fileStream.Dispose();
            fileStream = null;
        }

        /// <summary>
        /// Advances the file pointer by <code>count</code> bytes, throwing
        /// an exception if doing so would cause the file to grow beyond
        /// 4 GB.
        /// </summary>
        /// <param name="count">The count of bytes to increase the file pointer by.</param>
        /// <exception cref="System.IO.IOException">If increasing the file pointer by
        ///  <code>count</code> bytes would cause the file to grow beyond
        ///  4 GB.</exception>
        private void PosPlus(int count)
        {
            int newpos = currentKeyPos + count;
            if (newpos < count)
            {
                throw new IOException("CDB file is too big.");
            }
            currentKeyPos = newpos;
        }


        /// <summary>
        /// Builds a CDB file from a CDB-format text file.
        /// </summary>
        /// <param name="dataFilePath">The CDB data file to read.</param>
        /// <param name="cdbFilePath">The CDB file to create.</param>
        /// <param name="tempFilePath">The temporary file to use when creating the
        ///  CDB file.</param>
        /// <exception cref="System.IO.IOException">if an error occurs rebuilding the
        ///  CDB file.</exception>
        public static void Make(string dataFilePath, string cdbFilePath, string tempFilePath)
        {
            Make(dataFilePath, cdbFilePath, tempFilePath, null);
        }

        /// <summary>
        /// Builds a CDB file from a CDB-format text file, excluding records
        /// with data matching keys in `ignoreCdb'.
        /// </summary>
        /// <param name="dataFilePath">The CDB data file to read.</param>
        /// <param name="cdbFilePath">The CDB file to create.</param>
        /// <param name="tempFilePath">The temporary file to use when creating the
        ///  CDB file.</param>
        /// <param name="ignoreCdb">If the data for an entry matches a key in this
        ///  CDB file, the entry will not be added to the new file.</param>
        /// <exception cref="System.IO.IOException">if an error occurs rebuilding the
        ///  CDB file.</exception>
        public static void Make(string dataFilePath, string cdbFilePath, string tempFilePath, CdbReader ignoreCdb)
        {
            /* Open the data file. */
            using (FileStream fileStream = new FileStream(dataFilePath, FileMode.CreateNew, FileAccess.ReadWrite))
            {
                /* Build the database. */
                Make(fileStream, cdbFilePath, tempFilePath, ignoreCdb);
            }
        }

        /// <summary>
        /// Builds a CDB file from a CDB-format <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="cdbFilepath">The CDB file to create. </param>
        /// <param name="tempFilepath">The temporary file to use when creating the
        ///  CDB file.</param>
        /// <exception cref="System.IO.IOException">if an error occurs rebuilding the
        ///  CDB file.</exception>
        public static void Make(Stream stream, string cdbFilepath, string tempFilepath)
        {
            Make(stream, cdbFilepath, tempFilepath, null);
        }

        /// <summary>
        /// Builds a CDB file from a CDB-format <see cref="Stream"/>, excluding
        /// records with data matching keys in `ignoreCdb'.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to read from.</param>
        /// <param name="cdbFilepath">The CDB file to create.</param>
        /// <param name="tempFilepath">The temporary file to use when creating the
        ///  CDB file.</param>
        /// <param name="ignoreCdb">If the data for an entry matches a key in this
        ///  CDB file, the entry will not be added to the new file.</param>
        /// <exception cref="System.IO.IOException">if an error occurs rebuilding the
        ///  CDB file.</exception>
        public static void Make(Stream stream, string cdbFilepath, string tempFilepath, CdbReader ignoreCdb)
        {
            /* Create the CdbMake object. */
            using (CdbWriter cdbMake = new CdbWriter(tempFilepath))
            {
                /* Process the data file. */
                int ch;
                while (true)
                {
                    /* Read and process a byte. */
                    ch = stream.ReadByte();
                    if (ch == -1)
                    {
                        break;
                    }
                    if (ch == '\n')
                    {
                        break;
                    }
                    if (ch != '+')
                    {
                        throw new ArgumentException("input file not in correct format");
                    }

                    /* Get the key length. */
                    int klen = 0;
                    for (;;)
                    {
                        ch = stream.ReadByte();
                        if (ch == ',')
                        {
                            break;
                        }
                        if ((ch < '0') || (ch > '9'))
                        {
                            throw new ArgumentException("input file not in correct format");
                        }
                        if (klen > 429496720)
                        {
                            throw new ArgumentException("key length is too big");
                        }
                        klen = klen*10 + (ch - '0');
                    }

                    /* Get the data length. */
                    int dlen = 0;
                    for (;;)
                    {
                        ch = stream.ReadByte();
                        if (ch == ':')
                        {
                            break;
                        }
                        if ((ch < '0') || (ch > '9'))
                        {
                            throw new ArgumentException("input file not in correct format");
                        }
                        if (dlen > 429496720)
                        {
                            throw new ArgumentException("data length is too big");
                        }
                        dlen = dlen*10 + (ch - '0');
                    }

                    /* Read in the key. */
                    byte[] key = new byte[klen];
                    for (int i = 0; i < klen; i++)
                    {
                        /* Read the character. */
                        ch = stream.ReadByte();
                        if (ch == -1)
                        {
                            throw new ArgumentException("input file is truncated");
                        }

                        /* Store the character. */
                        key[i] = unchecked((byte) (ch & 0xff));
                    }

                    /* Read key/data separator characters. */
                    ch = stream.ReadByte();
                    if (ch != '-')
                    {
                        throw new ArgumentException("input file not in correct format");
                    }

                    ch = stream.ReadByte();
                    if (ch != '>')
                    {
                        throw new ArgumentException("input file not in correct format");
                    }

                    /* Read in the data. */
                    byte[] data = new byte[dlen];
                    for (int i = 0; i < dlen; i++)
                    {
                        /* Read the character. */
                        ch = stream.ReadByte();
                        if (ch == -1)
                        {
                            throw new ArgumentException("input file is truncated");
                        }

                        /* Store the character. */
                        data[i] = unchecked((byte) (ch & 0xff));
                    }

                    /* Add the key/data pair to the database if it is not in
				 * ignoreCdb. */
                    if ((ignoreCdb == null) || (ignoreCdb.Find(data) == null))
                    {
                        cdbMake.Add(key, data);
                    }

                    /* Read the terminating LF. */
                    ch = stream.ReadByte();
                    if (ch != '\n')
                    {
                        throw new ArgumentException("input file not in correct format");
                    }
                }
            }

            /* Rename the data file. */
            File.Move(tempFilepath, cdbFilepath);
        }

        public void Dispose()
        {
            Finish();
        }
    }
}