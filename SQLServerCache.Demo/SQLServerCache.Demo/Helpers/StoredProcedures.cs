using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace SQLServerCache.Demo.Helpers
{
    internal static class StoredProcedures
    {
        public static CacheItemMetaData GetCacheItemMetaDataOrNull(string key, SqlConnection connection)
        {
            var item = connection.Query<CacheItemMetaData>("Cache.GetCacheItemMetaData", new {key }, commandType: CommandType.StoredProcedure).FirstOrDefault();
            return item;
        }

        public static CacheItemMetaData AddOrRenewCacheItem(string key, int expireAfterMinutes, SqlConnection connection)
        {
            var item = connection.Query<CacheItemMetaData>("Cache.AddOrRenewCacheItem", new {key, expireAfterMinutes }, commandType: CommandType.StoredProcedure).FirstOrDefault();
            return item;
        }
    }
}
