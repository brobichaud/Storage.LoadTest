using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StorageLoad
{
	internal class Logging
	{
		private static readonly List<LogData> _logData = new List<LogData>();
		private static readonly List<object> _dataLists = new List<object>();

		public static void AddLogData(IEnumerable<LogData> data)
		{
			_dataLists.Add(data);
		}

		public static void WriteLogData(string file, int threadCount)
		{
			CombineLogData();
			File.Delete(file);
			File.AppendAllText(file, "Iteration");

			int loop = 0;
			while (++loop <= threadCount)
				File.AppendAllText(file, string.Format(",Thread {0:d2}", loop));

			var sorted = _logData.OrderBy(x => x.Iteration).ThenBy(x => x.Thread);
			foreach (var item in sorted)
			{
				if (item.Thread == 1)
					File.AppendAllText(file, string.Format("\n{0},{1}", item.Iteration, item.ElapsedMs));
				else
					File.AppendAllText(file, string.Format(",{0}", item.ElapsedMs));
			}
		}

		private static void CombineLogData()
		{
			int shortestList = (from List<LogData> item in _dataLists select item.Count).Concat(new[] { int.MaxValue }).Min();
			foreach (List<LogData> item in _dataLists)
			{
				if (item.Count > shortestList)
					item.RemoveRange(shortestList, item.Count - shortestList);
				_logData.AddRange(item);
			}
		}

		public class LogData
		{
			public int Thread { get; set; }
			public int Iteration { get; set; }
			public long ElapsedMs { get; set; }
		}
	}
}
