using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using SQLServerCache.Demo.TestModel;

namespace SQLServerCache.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var p = new Recipes();
            for (int i = 0; i < 1000; i++)
            {
                p.Items.Add(new Compound()
                {
                    Code = Guid.NewGuid().ToString(),
                    Description = Guid.NewGuid().ToString(),
                    CompositionRows = new List<CompositionRow>(
                        Enumerable.Range(1, 25).Select(z => new CompositionRow()
                        {
                            Code = z.ToString(),
                            Amount = 10
                        }))
                });
            }

            var databaseName = "TestCaching";
            var serverName = @"(localdb)\v11.0";

            string connectionString = $"Data Source={serverName};Initial Catalog={databaseName};Integrated Security=true";
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Execute($"DELETE FROM {databaseName}.[Cache].[CacheItem]");
                connection.Execute($"DELETE FROM {databaseName}.[Cache].[CacheItemMetaData]");
            }

            DoStuff(connectionString, p);
            DoStuff(connectionString, p);
            DoStuff(connectionString, p);

            var tasks = new List<Task>();

            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Factory.StartNew(() => DoStuff(connectionString, p)));
            }
            Task.WaitAll(tasks.ToArray());
        }

        private static void DoStuff(string connectionString, Recipes p)
        {
            int count = 500;
            var keys = new List<string>(GetIds(500));
            using (var connection = new SqlConnection(connectionString))
            using (var client = new CachingClient(connection))
            {
                connection.Open();
                var ms = Stopwatch.StartNew();
                for (int i = 0; i < count; i++)
                {
                    client.Store(keys[i], p, 1);
                }
                ms.Stop();
                Console.WriteLine("Insert/Item: " + ms.ElapsedMilliseconds / count);
                ms.Reset();
                ms.Start();
                for (int i = 0; i < count; i++)
                {
                    var found = client.TryGet<Recipes>(keys[i]);
                }
                Console.WriteLine("Read: " + ms.ElapsedMilliseconds);
                Console.WriteLine("Read/Item: " + ms.ElapsedMilliseconds / count);
                var notFound = client.TryGet<Recipe>("notfound");
                Debug.Assert(notFound == null);
            }
        }

        private static IEnumerable<string> GetIds(int count)
        {
            for (int i = 0; i < count / 2; i++)
            {
                yield return "key" + i;
                yield return Guid.NewGuid().ToString();
            }
        }
    }
}
