using System;
using System.Collections.Generic;
using System.IO;
using System.Data.ConstantDatabase;
using System.Text;

namespace Cdb.DemoConsole
{
	class MainClass
	{
		private static string cdbFilePath = "demo.cdb";
		private static KeyValuePair<string, string>[] data = new KeyValuePair<string, string> []
		{
			new KeyValuePair<string, string>("key1", "value1"),
			new KeyValuePair<string, string>("key2", "value2.1"),
			new KeyValuePair<string, string>("key3", "value3"),
			new KeyValuePair<string, string>("key2", "value2.2"),
			new KeyValuePair<string, string>("key4", "value4"),
			new KeyValuePair<string, string>("key2", "value2.3"),
			new KeyValuePair<string, string>("key5", "value5")
		};

		public static void Main(string[] args)
		{
			Console.WriteLine("This is a demo for using the CDB.NET library.");
			Console.WriteLine("===============================================================================");

			RemoveDatabaseFileIfItExists();
			CreateDatabase();

			Console.WriteLine();
			RetrieveKeyWithFindNext("key2");
			
			Console.WriteLine();
			RetrieveKeyWithFind("key2");
			
			Console.WriteLine();
			RetrieveKeyWithFindMultipleTimes("key2");
		}
		
		private static void CreateDatabase()
		{
			Console.WriteLine(string.Format("Creating the '{0}' database file.", cdbFilePath));

			using (CdbWriter cdbWriter = new CdbWriter(cdbFilePath))
			{
				foreach (KeyValuePair<string, string> item in data)
				{
					Console.WriteLine(string.Format("Adding item '{0}' to the database. Value = {1}", item.Key, item.Value));

					byte[] key = EncodeKey(item.Key);
					byte[] value = EncodeValue(item.Value);
					cdbWriter.Add(key, value);
				}
				
				cdbWriter.Finish();
			}
		}
		
		private static void RemoveDatabaseFileIfItExists()
		{
			if (!File.Exists(cdbFilePath))
				return;
			
			Console.WriteLine(string.Format("Deleting the existent '{0}' database file.", cdbFilePath));
			File.Delete(cdbFilePath);
		}
		
		private static void RetrieveKeyWithFindNext(string keyAsString)
		{
			Console.WriteLine(string.Format("Retrieveing '{0}' using FindStart() and FindNext() methods:", keyAsString));

			using (CdbReader cdbReader = new CdbReader(cdbFilePath))
			{
				byte[] key = EncodeKey(keyAsString);
				byte[] value = null;

				cdbReader.FindStart(key);

				do
				{
					value = cdbReader.FindNext(key);

					DisplayValue(value);
				}
				while (value != null);
			}
		}

		private static void RetrieveKeyWithFind(string keyAsString)
		{
			Console.WriteLine(string.Format("Retrieveing '{0}' using Find() method:", keyAsString));

			using (CdbReader cdbReader = new CdbReader(cdbFilePath))
			{
				byte[] key = EncodeKey(keyAsString);
				byte[] value = null;
				
				value = cdbReader.Find(key);

				DisplayValue(value);
			}
		}
		
		private static void RetrieveKeyWithFindMultipleTimes(string keyAsString)
		{
			Console.WriteLine(string.Format("Retrieveing '{0}' using Find() method multiple times in a row:", keyAsString));

			using (CdbReader cdbReader = new CdbReader(cdbFilePath))
			{
				byte[] key = EncodeKey(keyAsString);
				byte[] value = null;
				
				value = cdbReader.Find(key);
				DisplayValue(value);

				value = cdbReader.Find(key);
				DisplayValue(value);

				value = cdbReader.Find(key);
				DisplayValue(value);
			}
		}

		private static void DisplayValue(byte[] value)
		{
			string valueAsString = DecodeValue(value);

			if (value != null)
				Console.WriteLine(string.Format("Value found: {0}", valueAsString));
			else
				Console.WriteLine("No value found.");
		}

		private static byte[] EncodeKey(string key)
		{
			return Encoding.UTF8.GetBytes(key);
		}
		
		private static byte[] EncodeValue(string value)
		{
			return Encoding.UTF8.GetBytes(value);
		}
		
		private static string DecodeValue(byte[] value)
		{
			if (value == null)
				return null;

			return Encoding.UTF8.GetString(value);
		}
	}
}
