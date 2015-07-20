using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Runtime.Caching;
using System.Runtime.InteropServices;
using Microsoft.Hadoop.Avro;
using SQLServerCache.Demo.Helpers;
using SQLServerCache.Demo.TestModel;

namespace SQLServerCache.Demo
{
    public interface ILocalBuffer
    {
        void AfterStore<T>(CacheItemMetaData metadata, T item) where T : class;
        T BeforeTryGet<T>(CacheItemMetaData metadata) where T : class;
    }

    internal class InMemoryLocalBuffer : ILocalBuffer
    {
        private static readonly MemoryCache Internal = new MemoryCache("InMemoryLocalBuffer");

        public void AfterStore<T>(CacheItemMetaData metadata, T item) where T : class
        {
            Internal.Add($"{metadata.InternalId}-{metadata.UpdatedTimestamp.Ticks}", item, new CacheItemPolicy());
        }

        public T BeforeTryGet<T>(CacheItemMetaData metadata) where T : class
        {
            return Internal.Get($"{metadata.InternalId}-{metadata.UpdatedTimestamp.Ticks}") as T;
        }
    }

    public class CachingClient : IDisposable
    {
        private readonly SqlConnection _connection;
        private readonly AvroSerializerSettings _settings;
        private readonly ILocalBuffer _localBuffer;

        public CachingClient(SqlConnection connection, ILocalBuffer localBuffer = null)
        {
            _connection = connection;
            _localBuffer = localBuffer ?? new InMemoryLocalBuffer();
            _settings = new AvroSerializerSettings()
            {
                Resolver = new AvroDataContractResolver(),
                GenerateDeserializer = true,
                GenerateSerializer = true,
                UseCache = true,
                KnownTypes = new List<Type>(typeof(Recipe).Assembly.GetTypes())
            };
        }

        public void Dispose()
        {
        }

        public void Store<T>(string key, T obj) where T : class
        {
            var item = StoredProcedures.AddOrRenewCacheItem(key, 5, _connection);

            using (var outputStream = new BlobStreamWriter(_connection, "Cache", item))
            {
                using (var encoder = new BufferedBinaryEncoder(outputStream))
                {
                    AvroSerializer.Create<T>(_settings).Serialize(encoder, obj);
                }
            }
            _localBuffer.AfterStore<T>(item, obj);
        }

        public T TryGet<T>(string key) where T : class
        {
            var item = StoredProcedures.GetCacheItemMetaDataOrNull(key, _connection);
            if (item == null || item.IsExpired)
                return null;

            var local = _localBuffer.BeforeTryGet<T>(item);
            if (local != null)
                return local;

            using (var outputStream = new BlobStreamReader(_connection, "Cache", item))
            {
                using (var encoder = new BinaryDecoder(outputStream))
                {
                    try
                    {
                        return AvroSerializer.Create<T>(_settings).Deserialize(encoder) as T;
                    }
                    catch (Exception)
                    {
                        return default(T);
                    }
                }
            }
        }
    }
}