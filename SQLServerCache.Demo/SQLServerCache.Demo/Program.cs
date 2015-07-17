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

            using (var connection = new SqlConnection($"Data Source={serverName};Initial Catalog={databaseName};Integrated Security=true"))
            using (var client = new CachingClient(connection))
            {
                connection.Execute($"DELETE FROM {databaseName}.[Cache].[CacheItem]");
                connection.Execute($"DELETE FROM {databaseName}.[Cache].[CacheItemMetaData]");

                connection.Open();
                int count = 500;
                var ms = Stopwatch.StartNew();
                for (int i = 0; i < count; i++)
                {
                    client.Store("test" + i, p);
                }
                ms.Stop();
                Console.WriteLine("Insert/Item: " + ms.ElapsedMilliseconds / count);
                ms.Reset();
                ms.Start();
                for (int i = 0; i < count; i++)
                {
                    var found = client.TryGet<Recipes>("test" + i);
                }
                Console.WriteLine("Read/Item: " + ms.ElapsedMilliseconds / count);
                var notFound = client.TryGet<Recipe>("notfound");
                Debug.Assert(notFound == null);

            }
        }
    }
}
