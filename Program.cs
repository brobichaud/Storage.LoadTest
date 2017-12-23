using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StorageLoad
{
	class Program
	{
		public static int Main(string[] args)
		{
			if (args.Length < 3)
			{
				Console.WriteLine("Argument error. Specify ThreadCount, Duration, StorageProvider");
				Console.WriteLine("  Eg: StorageLoad 5 10 documentdb");
				return 1;
			}

			int threadCount = Convert.ToInt16(args[0]);
			int durationSec = Convert.ToInt16(args[1]);
			string store = args[2].ToLower();

			Console.WriteLine("Thread Count: {0}, Duration Seconds: {1}, Storage Provider: {2}", threadCount, durationSec, store);

			//DocumentDB.ClearDb();

			// spin up some threads to do the work
			var tasks = new List<Task>();
			for (int loop = 1; loop <= threadCount; loop++)
			{
				int loop1 = loop;
				if (store == "documentdb")
					tasks.Add(Task.Run(async () => { await DocumentDB.BlastService(loop1, durationSec); }));
			}

			Task.WaitAll(tasks.ToArray());
			Logging.WriteLogData("LoadData." + store + ".csv", threadCount);
			return 0;
		}
	}
}
