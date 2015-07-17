using System;
using System.Data.SqlClient;
using Microsoft.Hadoop.Avro;
using SQLServerCache.Demo.Helpers;

namespace SQLServerCache.Demo
{
    class CachingClient : IDisposable
    {
        private readonly SqlConnection _connection;
        private readonly AvroSerializerSettings _settings;

        public CachingClient(SqlConnection connection)
        {
            _connection = connection;
            _settings = new AvroSerializerSettings() { Resolver = new AvroDataContractResolver(), GenerateDeserializer = true, GenerateSerializer = true, UseCache = true };
        }

        public void Dispose()
        {
        }

        public void Store<T>(string key, T person)
        {
            using (var outputStream = new BlobStreamWriter(_connection, "Cache", key))
            {
                using (var encoder = new BufferedBinaryEncoder(outputStream))
                {
                    AvroSerializer.Create<T>(_settings).Serialize(encoder, person);
                }
            }
        }

        public T TryGet<T>(string key) where T : class
        {
            using (var outputStream = new BlobStreamReader(_connection, "Cache", key))
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