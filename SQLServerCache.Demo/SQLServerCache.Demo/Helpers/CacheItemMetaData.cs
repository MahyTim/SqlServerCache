using System;

namespace SQLServerCache.Demo.Helpers
{
    public class CacheItemMetaData
    {
        public long InternalId { get; set; }
        public DateTime UpdatedTimestamp { get; set; }
        public bool IsExpired { get; set; }
    }
}