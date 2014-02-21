using System;
using System.Collections.Generic;
using System.Data.ConstantDatabase;
using System.IO;
using System.Text;

namespace ConstantDatabase.Dumper
{
    class Program
    {
        static void Main(string[] args)
        {
            /* Display a usage message if we didn't get the correct number
             * of arguments. */
            if (args.Length != 1)
            {
                Console.WriteLine("CDB Dumper: usage: ConstantDatabase.Dumper.exe file");
                return;
            }

            /* Decode our arguments. */
            string cdbFile = args[0];

            /* Dump the CDB file. */

            byte[] newLine = Encoding.ASCII.GetBytes(Environment.NewLine);
            byte[] keyValueSeparator = { (byte)'-', (byte)'>' };
            Stream outStream = Console.OpenStandardOutput();
            try
            {
                IEnumerator<CdbEntry> iter = CdbReader.Entries(cdbFile);
                while (iter.MoveNext())
                {
                    CdbEntry entry = iter.Current;

                    if (entry == null)
                        continue;

                    byte[] key = entry.Key;
                    byte[] data = entry.Data;

                    byte[] buffer = Encoding.ASCII.GetBytes("+" + key.Length + "," + data.Length + ":");
                    outStream.Write(buffer, 0, buffer.Length);
                    outStream.Write(key, 0, key.Length);
                    outStream.Write(keyValueSeparator, 0, keyValueSeparator.Length);
                    outStream.Write(data, 0, data.Length);
                    outStream.Write(newLine, 0, newLine.Length);
                }

                outStream.Write(newLine, 0, newLine.Length);
            }
            catch (IOException ioException)
            {
                Console.WriteLine("Couldn't dump CDB file: " + ioException);
            }

        }
    }
}
