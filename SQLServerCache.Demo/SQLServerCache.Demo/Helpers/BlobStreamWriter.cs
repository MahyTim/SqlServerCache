using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Dapper;

namespace SQLServerCache.Demo.Helpers
{
    class BlobStreamWriter : Stream
    {
        private readonly SqlCommand _appendChunkCommand;

        private readonly SqlParameter _dataParameter;
        private readonly SqlParameter _lengthParameter;
        private readonly SqlParameter _indexParameter;
        private bool _disposed;


        public BlobStreamWriter(
            SqlConnection connection,
            string schemaName,
            CacheItemMetaData item)
        {
            _appendChunkCommand = new SqlCommand($@"UPDATE [{schemaName}].[CacheItem] SET [Content].WRITE(@chunk, @index, @len) WHERE [InternalId] = @internalId", connection);
            _appendChunkCommand.Parameters.AddWithValue("@internalId", item.InternalId);
            _lengthParameter = _appendChunkCommand.Parameters.Add("len", SqlDbType.Int);
            _indexParameter = _appendChunkCommand.Parameters.Add("index", SqlDbType.Int);
            _dataParameter = new SqlParameter("@chunk", SqlDbType.VarBinary, -1);
            _appendChunkCommand.Parameters.Add(_dataParameter);
        }
        public override void Flush()
        {
        }

        ~BlobStreamWriter()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (false == _disposed)
            {
                _appendChunkCommand.Dispose();
                _disposed = true;
            }
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
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int index, int count)
        {
            byte[] bytesToWrite = buffer;
            if (index != 0 || count != buffer.Length)
            {
                bytesToWrite = new MemoryStream(buffer, index, count).ToArray();
            }
            _dataParameter.Value = bytesToWrite;
            _lengthParameter.Value = count;
            _indexParameter.Value = Position;
            _appendChunkCommand.ExecuteNonQuery();
            Position += count;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => 0;
        public override long Position { get; set; }
    }
}