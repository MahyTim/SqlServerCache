using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace SQLServerCache.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var p = new Persons();
            for (int i = 0; i < 5000; i++)
            {
                p.Items.Add(new Person() { Age = 10, Name = "Frans" });
            }

            var databaseName = "TestCaching";
            var serverName = @"(localdb)\v11.0";

            using (var connection = new SqlConnection($"Data Source={serverName};Initial Catalog={databaseName};Integrated Security=true"))
            using (var client = new CachingClient(connection))
            {
                connection.Execute($"DELETE FROM {databaseName}.[Cache].[CacheItem]");
                connection.Execute($"DELETE FROM {databaseName}.[Cache].[CacheItemMetaData]");

                connection.Open();
                int count = 1000;
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
                    var found = client.TryGet<Persons>("test" + i);
                }
                Console.WriteLine("Read/Item: " + ms.ElapsedMilliseconds / count);
                var notFound = client.TryGet<Person>("notfound");
                Debug.Assert(notFound == null);

            }
        }
    }
}
