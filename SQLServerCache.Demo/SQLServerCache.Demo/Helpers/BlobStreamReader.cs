using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace SQLServerCache.Demo.Helpers
{
    class BlobStreamReader : Stream
    {
        private readonly SqlCommand _command;
        private readonly SqlDataReader _reader;
        private bool _disposed;

        public BlobStreamReader(SqlConnection connection, string schemaName, CacheItemMetaData item)
        {
            _command = new SqlCommand($"SELECT TOP 1 i.Content FROM {schemaName}.[CacheItem] i WHERE i.[InternalId] = @internalId", connection);
            _command.Parameters.AddWithValue("internalId", item.InternalId);
            _reader = _command.ExecuteReader(CommandBehavior.SequentialAccess);
            _reader.Read();
        }

        ~BlobStreamReader()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (false == _disposed)
            {
                _reader.Dispose();
                _command.Dispose();
                _disposed = true;
            }
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (false == _reader.HasRows || _reader.IsDBNull(0))
            {
                return 0;
            }
            _reader.GetBytes(0, Position, buffer, offset, count);
            Position += count;
            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length { get; }
        public override long Position { get; set; }
    }
}