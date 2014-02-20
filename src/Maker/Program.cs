using System;
using System.Data.ConstantDatabase;
using System.IO;

namespace ConstantDatabase.Maker
{
    class Program
    {
        static void Main(string[] args)
        {
            /* Display a usage message if we didn't get the correct number
			 * of arguments. */
			if (args.Length < 2)
			{
                Console.WriteLine("CDB Maker: usage: ConstantDatabase.Maker.exe cdb_file temp_file [ignoreCdb]");
				return;
			}
			/* Decode our arguments. */
			string cdbFile = args[0];
			string tempFile = args[1];

			/* Load the ignoreCdb if requested. */
			CdbReader ignoreCdb = null;
			if (args.Length > 3)
			{
				try
				{
					ignoreCdb = new CdbReader(args[2]);
				}
				catch (IOException ioException)
				{
					Console.WriteLine("Couldn't load `ignore' CDB file: " + ioException);
				}
			}

			/* Create the CDB file. */
			try
			{
				CdbWriter.Make(Console.OpenStandardInput(), cdbFile, tempFile, ignoreCdb);
			}
			catch (IOException ioException)
			{
				Console.WriteLine("Couldn't create CDB file: " + ioException);
			}

        }
    }
}
