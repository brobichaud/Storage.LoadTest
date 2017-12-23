using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace StorageLoad
{
	public class DocumentDB
	{
		private static DocumentClient _client;
		private static DocumentCollection _collection;
		private const string _databaseId = "dev";
		private const string _collectionId = "api-telemetry";
		private const string _endpointUrl = "https://nonlivemadrasuse.documents.azure.com:443/";
		private const string _authKey = "ilVqti9PWexICJW4yyJ6glV9Nw5zqEBdqiem74iDlYTZNuRsVa7d8CNNE7x8p7j57LGCckfs+vQyuEP+QMPYfg==";

		static DocumentDB()
		{
			var policy = new ConnectionPolicy()
			{
				ConnectionMode = ConnectionMode.Gateway,
				ConnectionProtocol = Protocol.Https
			};

			// note: should be wrapped in using
			_client = new DocumentClient(new Uri(_endpointUrl), _authKey, policy);
			var database = RetrieveOrCreateDatabaseAsync(_databaseId).Result;
			_collection = RetrieveOrCreateCollectionAsync(database.SelfLink, _collectionId).Result;
		}

		public static void ClearDb()
		{
			RetrieveOrCreateDatabaseAsync(_databaseId, true).Wait();
		}

		public static async Task BlastService(int instance, int durationSec)
		{
			try
			{
				var timer = Stopwatch.StartNew();

				int loop = 1;
				var logData = new List<Logging.LogData>();

				while (timer.Elapsed < TimeSpan.FromSeconds(durationSec))
				{
					var sw = Stopwatch.StartNew();
					var doc = await CreateDocumentAsync(_collection.SelfLink);
					sw.Stop();

					if (doc == null)
					{
						var data = new Logging.LogData() { Thread = instance, Iteration = loop++, ElapsedMs = 99999 };
						logData.Add(data);
						Console.WriteLine("Thread: {0:d2}, Iteration: {1:d4}, Table: Error, Elapsed: {2}ms", instance, data.Iteration, data.ElapsedMs);
					}
					else
					{
						var data = new Logging.LogData() { Thread = instance, Iteration = loop++, ElapsedMs = sw.ElapsedMilliseconds };
						logData.Add(data);
						Console.WriteLine("Thread: {0:d2}, Iteration: {1:d4}, DocId: {2}, Elapsed: {3}ms", data.Thread, data.Iteration, doc.Id, data.ElapsedMs);
					}
				}

				Logging.AddLogData(logData);
				Console.WriteLine("Thread: {0:d2} exiting, duration elapsed", instance);

				//// Use DocumentDB SQL to query the documents within the Game _collection
				//var game1 = _client.CreateDocumentQuery(_collection.SelfLink, "SELECT * FROM Games g WHERE g.gameId = '1'").ToArray().FirstOrDefault();
				//if (game1 != null)
				//	Console.WriteLine("Game with Id '1': {0}", game1);
			}
			catch (DocumentClientException de)
			{
				Exception baseException = de.GetBaseException();
				Console.WriteLine("Status code {0} error occurred: {1}, Message: {2}", de.StatusCode, de.Message, baseException.Message);
			}
			catch (Exception e)
			{
				Exception baseException = e.GetBaseException();
				Console.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
			}
		}

		private static async Task<Document> CreateDocumentAsync(string collection)
		{
			var rnd = new Random(DateTime.UtcNow.Millisecond);

			// create a dynamic object
			dynamic doc = new
			{
				occurred = DateTime.UtcNow,
				callerIp = "10.123." + rnd.Next(0, 255) + ".1",
				verb = "POST",
				method = "/service/read",
				queryParams = "?var1=value&var2=another",
				userAgent = "DDIM v0.23",
				userName = "joe@gmail.com",
				elapsedMs = rnd.Next(100, 5000),
				responseHttpStatus = 200,
				responseHttpErrorBody = "",
			};

			return await _client.CreateDocumentAsync(collection, doc);

			//// Create a dynamic object
			//dynamic dynamicGame2 = new
			//{
			//	gameId = "2",
			//	name = "Flappy Parrot in Wonderland",
			//	releaseDate = new DateTime(2014, 7, 10),
			//	categories = new string[] { "mobile", "completely free", "arcade", "2D" },
			//	played = true,
			//	scores = new[]
			//		 {
			//			  new {
			//					playerName = "KevinTheGreat",
			//					score = 300
			//			  }
			//		 },
			//	levels = new[] 
			//		 {
			//			  new {
			//					title = "Stage 1",
			//					parrots = 3,
			//					rocks = 5,
			//					ghosts = 1
			//			  },
			//			  new {
			//					title = "Stage 2",
			//					parrots = 5,
			//					rocks = 7,
			//					ghosts = 2
			//			  }
			//		 }
			//};

			//var document2 = await _client.CreateDocumentAsync(_collection, dynamicGame2);
		}

		private static async Task<Database> RetrieveOrCreateDatabaseAsync(string id, bool clearDb = false)
		{
			// Try to retrieve the database (Microsoft.Azure.Documents.Database) whose Id is equal to _databaseId            
			var database = _client.CreateDatabaseQuery().Where(db => db.Id == id).AsEnumerable().FirstOrDefault();

			// wipe any existing data
			if (clearDb && (database != null))
			{
				await _client.DeleteDatabaseAsync(database.SelfLink);
				database = null;
			}

			// If the previous call didn't return a Database then create it
			if (database == null)
			{
				database = await _client.CreateDatabaseAsync(new Database {Id = _databaseId});
				//Console.WriteLine("Created Database: {0}, selfLink: {1}", database.Id, database.SelfLink);
			}
			else
			{
				//Console.WriteLine("Existing Database: {0}, selfLink: {1}", database.Id, database.SelfLink);
			}

			return database;
		}

		private static async Task<DocumentCollection> RetrieveOrCreateCollectionAsync(string databaseSelfLink, string id)
		{
			// Try to retrieve the _collection (Microsoft.Azure.Documents.DocumentCollection) whose Id is equal to _collectionId
			var collection = _client.CreateDocumentCollectionQuery(databaseSelfLink).Where(c => c.Id == id).ToArray().FirstOrDefault();

			// If the previous call didn't return a Collection then create it
			if (collection == null)
			{
				collection = await _client.CreateDocumentCollectionAsync(databaseSelfLink, new DocumentCollection { Id = id });
				//Console.WriteLine("Created Collection: {0}, selfLink: {1}", id, _collection.SelfLink);
			}
			else
			{
				//Console.WriteLine("Existing Collection: {0}, selfLink: {1}", id, _collection.SelfLink);
			}

			return collection;
		}
	}
}
