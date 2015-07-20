using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Runtime.Caching;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Hadoop.Avro;
using SQLServerCache.Demo.Helpers;
using SQLServerCache.Demo.TestModel;

namespace SQLServerCache.Demo
{
    public interface ILocalBuffer
    {
        void AfterStore<T>(CacheItemMetaData metadata, T item) where T : class;
        T BeforeTryGet<T>(CacheItemMetaData metadata) where T : class;
        void Evict(CacheItemMetaData metadata);
    }

    internal class InMemoryLocalBuffer : ILocalBuffer
    {
        private static readonly MemoryCache Internal = new MemoryCache("InMemoryLocalBuffer");
        private static readonly ReaderWriterLockSlim CacheLock = new ReaderWriterLockSlim();

        public void AfterStore<T>(CacheItemMetaData metadata, T item) where T : class
        {
            CacheLock.EnterWriteLock();
            try
            {
                Internal.Add($"{metadata.InternalId}-{metadata.UpdatedTimestamp.Ticks}", item, new CacheItemPolicy());
            }
            finally
            {
                CacheLock.ExitWriteLock();
            }
        }

        public T BeforeTryGet<T>(CacheItemMetaData metadata) where T : class
        {
            CacheLock.EnterReadLock();
            try
            {
                return Internal.Get($"{metadata.InternalId}-{metadata.UpdatedTimestamp.Ticks}") as T;
            }
            finally
            {
                CacheLock.ExitReadLock();
            }
        }

        public void Evict(CacheItemMetaData metadata)
        {
            CacheLock.EnterWriteLock();
            try
            {
                Internal.Remove($"{metadata.InternalId}-{metadata.UpdatedTimestamp.Ticks}");
            }
            finally
            {
                CacheLock.ExitWriteLock();
            }
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

        public void Store<T>(string key, T obj,int expireAfterMinutes) where T : class
        {
            var item = StoredProcedures.AddOrRenewCacheItem(key, expireAfterMinutes, _connection);

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
            if (item == null)
                return null;

            if (item.IsExpired)
            {
                _localBuffer.Evict(item);
                return null;
            }

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