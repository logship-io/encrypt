namespace encrypt.Utilities
{
    internal class ReadExceptOffsetStream : Stream
    {
        private readonly Stream baseStream;
        private readonly int remainingOffset;

        private readonly byte[] outputBuffer;

        private readonly LinkedList<ReadOnlyMemory<byte>> buffers = new LinkedList<ReadOnlyMemory<byte>>();

        public ReadExceptOffsetStream(Stream baseStream, int offset)
        {
            this.baseStream = baseStream;
            this.remainingOffset = offset;

            this.outputBuffer = new byte[offset];
        }

        public byte[] OffsetBuffer => this.outputBuffer;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => this.baseStream.Length - remainingOffset;

        public override long Position
        {
            get => this.baseStream.Position;
            set => throw new NotSupportedException("Setting position is not supported in ReadExceptOffsetStream.");
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var readableBytes = this.buffers.Sum(b => b.Length) - this.remainingOffset;
            while (readableBytes <= 0)
            {
                // First lets see if we can read more
                var newBuffer = new byte[1024];
                int bytesRead = this.baseStream.Read(newBuffer, 0, newBuffer.Length);

                if (bytesRead == 0)
                {
                    break;
                }

                this.buffers.AddLast(new ReadOnlyMemory<byte>(newBuffer, 0, bytesRead));
                readableBytes += bytesRead;
            }

            if (readableBytes <= 0)
            {
                // We need to copy everything we have to the destination buffer
                var destinationIndex = 0;
                foreach (var bufferSegment in this.buffers)
                {
                    for (var i = 0; i < bufferSegment.Length; i++)
                    {
                        this.outputBuffer[destinationIndex++] = bufferSegment.Span[i];
                    }
                }

                return 0;
            }
            else
            {
                // We copy to the output buffer
                var outputLength = Math.Min(readableBytes, count);
                var currentNode = this.buffers.First;
                var outputIndex = 0;
                while (outputIndex < outputLength)
                {
                    var i = 0;
                    for (; i < currentNode!.Value.Length && outputIndex < outputLength; i++)
                    {
                        buffer[outputIndex++] = currentNode.Value.Span[i];
                    }
                    if (i == currentNode.Value.Length)
                    {
                        this.buffers.RemoveFirst();
                        currentNode = this.buffers.First;
                    }
                    else
                    {
                        currentNode.Value = currentNode.Value.Slice(i);
                        break;
                    }
                }

                return outputLength;
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

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            this.baseStream.Dispose();
            base.Dispose(disposing);
        }
    }
}
