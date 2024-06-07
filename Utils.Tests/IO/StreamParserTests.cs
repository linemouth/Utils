using System.Text;
using Utils;

namespace Utils.Tests
{
    [TestFixture]
    public class StreamParserTests
    {
        private class OverrideStream(Stream stream, bool canSeek = true, bool canRead = true, bool canWrite = true, long? length = null) : Stream
        {
            public override bool CanSeek
            {
                get => Stream.CanSeek && canSeek;
            }
            public override bool CanRead => Stream.CanRead && canRead;
            public override bool CanWrite => Stream.CanWrite && canWrite;
            public override long Length => length ?? Stream.Length;
            public override long Position { get => Stream.Position; set => Stream.Position = value; }

            private readonly Stream Stream = stream;
            private bool canSeek = canSeek;
            private bool canRead = canRead;
            private bool canWrite = canWrite;
            private long? length = length;

            public override void Flush() => Stream.Flush();
            public override int Read(byte[] buffer, int offset, int count) => Stream.Read(buffer, offset, count);
            public override long Seek(long offset, SeekOrigin origin) => Stream.Seek(offset, origin);
            public override void SetLength(long value) => Stream.SetLength(value);
            public override void Write(byte[] buffer, int offset, int count) => Stream.Write(buffer, offset, count);
            public void SetCanSeek(bool canSeek) => this.canSeek = canSeek;
            public void SetCanRead(bool canRead) => this.canRead = canRead;
            public void SetCanWrite(bool canWrite) => this.canWrite = canWrite;
            public void SetLength(long? length) => this.length = length;
        }

        public static readonly byte[] EmptyTestData = Array.Empty<byte>();
        public static readonly byte[] StringTestData = Encoding.UTF8.GetBytes("Hello, 世界!");
        public static readonly byte[] BinaryTestData = ByteArrayExtensions.ToBytes(-1.0d, false);
        public static readonly byte[] MixedTestData = StringTestData.Concat(BinaryTestData).ToArray();

        #region Properties
        [Test]
        public void Position_SetterGetter()
        {
            using OverrideStream stream = new OverrideStream(new MemoryStream(StringTestData), false);
            using StreamParser reader = new StreamParser(stream, 2);

            // Demonstrate that the stream is initialized to position == 0.
            Assert.That(reader.Position, Is.Zero);

            // Demonstrate seeking to a position within the buffer.
            reader.Position = 1;
            Assert.That(reader.Position, Is.EqualTo(1));

            // Demonstrate seeking to a position reachable by reading the stream.
            reader.Position = 12;
            Assert.That(reader.Position, Is.EqualTo(12));

            // Demonstrate failure to seek to a position already dropped from the buffer.
            Assert.Throws<ArgumentOutOfRangeException>(() => reader.Position = 0);

            // Demonstrate that, despite failure, the position has not changed.
            Assert.That(reader.Position, Is.EqualTo(12));

            // Demonstrate failure to seek beyond the end of the stream.
            Assert.Throws<EndOfStreamException>(() => reader.Position = 1000);

            // Demonstrate that, when seeking beyond the end of the stream, the position is the length of the stream.
            Assert.That(reader.Position, Is.EqualTo(14));

            // Demonstrate that, when the stream is seekable, backwards seeking is supported.
            stream.SetCanSeek(true);
            reader.Position = 0;
            Assert.That(reader.Position, Is.Zero);
        }
        [Test]
        public void Available_Getter()
        {
            using OverrideStream stream = new OverrideStream(new MemoryStream(StringTestData), false);
            using StreamParser reader = new StreamParser(stream, 2);

            // Demonstrate that we can't read the stream length.
            Assert.That(reader.Available, Is.Null);

            // Demonstrate reading the length of the stream.
            stream.SetCanSeek(true);
            Assert.That(reader.Available, Is.EqualTo(StringTestData.Length));

            // Demonstrate end of stream.
            reader.Position = StringTestData.Length;
            Assert.That(reader.Available, Is.Zero);

        }
        [Test]
        public void AvailableInBuffer_Getter()
        {
            using OverrideStream stream = new OverrideStream(new MemoryStream(StringTestData), false);
            using StreamParser reader = new StreamParser(stream, 2);

            // Demonstrate that we can't read the stream length.
            Assert.That(reader.AvailableInBuffer, Is.EqualTo(2 + 8));

            // Demonstrate end of stream.
            reader.Position = StringTestData.Length;
            Assert.That(reader.AvailableInBuffer, Is.Zero);

        }
        #endregion

        #region Constructors
        [Test]
        public void Constructor_StreamAndEncoding_Success()
        {
            using MemoryStream stream = new MemoryStream(StringTestData);
            using StreamParser reader = new StreamParser(stream, Encoding.UTF8);
            Assert.Multiple(() =>
            {
                Assert.That(reader.Encoding, Is.EqualTo(Encoding.UTF8));
                Assert.That(reader.LeaveOpen, Is.False);
            });
        }
        [Test]
        public void Constructor_StreamEncodingAndBufferSize_Success()
        {
            using MemoryStream stream = new MemoryStream(StringTestData);
            using StreamParser reader = new StreamParser(stream, Encoding.UTF8, 64);
            Assert.Multiple(() =>
            {
                Assert.That(reader.Encoding, Is.EqualTo(Encoding.UTF8));
                Assert.That(reader.LeaveOpen, Is.False);
            });
        }
        [Test]
        public void Constructor_ByteArrayAndEncoding_Success()
        {
            using StreamParser reader = new StreamParser(StringTestData, Encoding.UTF8);
            Assert.That(reader.Encoding, Is.EqualTo(Encoding.UTF8));
        }
        #endregion

        #region Methods
        [Test]
        public void Dispose_ClosesStream()
        {
            MemoryStream stream = new MemoryStream(StringTestData);
            using StreamParser reader = new StreamParser(stream, Encoding.UTF8);
            reader.Dispose();
            Assert.Throws<ObjectDisposedException>(() => stream.ReadByte());
        }
        #endregion

        #region Peek Methods
        [Test]
        public void PeekBytes_WhenStreamNotEmpty_ReturnsRequestedBytes()
        {
            using MemoryStream stream = new MemoryStream(BinaryTestData);
            using StreamParser reader = new StreamParser(stream);
            int count = 5;
            byte[] peekedBytes = reader.PeekBytes(count);
            Assert.Multiple(() =>
            {
                Assert.That(peekedBytes, Has.Length.EqualTo(count));
                Assert.That(peekedBytes[0], Is.EqualTo(BinaryTestData[0]));
                Assert.That(peekedBytes[1], Is.EqualTo(BinaryTestData[1]));
                Assert.That(peekedBytes[2], Is.EqualTo(BinaryTestData[2]));
                Assert.That(peekedBytes[3], Is.EqualTo(BinaryTestData[3]));
                Assert.That(peekedBytes[4], Is.EqualTo(BinaryTestData[4]));
            });
        }

        [Test]
        public void PeekBytes_WhenStreamEmpty_ThrowsEndOfStreamException()
        {
            using MemoryStream stream = new MemoryStream();
            using StreamParser reader = new StreamParser(stream);
            int count = 5;
            Assert.Throws<EndOfStreamException>(() => reader.PeekBytes(count));
        }
        [Test]
        public void PeekBytes_WhenRequestedCountGreaterThanData_Throw()
        {
            using MemoryStream stream = new MemoryStream(BinaryTestData);
            using StreamParser reader = new StreamParser(stream);
            Assert.Throws<EndOfStreamException>(() => reader.PeekBytes(10));
        }
        [Test]
        public void PeekBytes_WhenRequestedCountGreaterThanBuffer_Throw()
        {
            using MemoryStream stream = new MemoryStream(BinaryTestData);
            using StreamParser reader = new StreamParser(stream, 2);
            Assert.Throws<ArgumentOutOfRangeException>(() => reader.PeekBytes(BinaryTestData.Length));
        }
        [Test]
        public void PeekBytes_Success()
        {
            using StreamParser reader = new StreamParser(BinaryTestData, Encoding.UTF8);

            Assert.That(reader.Position, Is.EqualTo(0));
            for (int i = 0; i < BinaryTestData.Length; ++i)
            {
                reader.Position = i;
                Assert.That(reader.Position, Is.EqualTo(i));
                Assert.That(reader.PeekByte(), Is.EqualTo(BinaryTestData[i]));
                Assert.That(reader.PeekByte(), Is.EqualTo(BinaryTestData[i]));
                Assert.That(reader.Position, Is.EqualTo(i));
            }
        }
        [Test]
        public void PeekByte_WhenStreamNotEmpty_ReturnsFirstByte()
        {
            using MemoryStream stream = new MemoryStream(StringTestData);
            using StreamParser reader = new StreamParser(stream);
            byte peekedByte = reader.PeekByte();
            Assert.That(peekedByte, Is.EqualTo(StringTestData[0]));
        }
        [Test]
        public void PeekByte_WhenStreamEmpty_ThrowsEndOfStreamException()
        {
            using MemoryStream stream = new MemoryStream();
            using StreamParser reader = new StreamParser(stream);
            Assert.Throws<EndOfStreamException>(() => reader.PeekByte());
        }
        [Test]
        public void PeekByte_MultipleCalls_DoNotAdvancePosition()
        {
            using MemoryStream stream = new MemoryStream(StringTestData);
            using StreamParser reader = new StreamParser(stream);
            reader.PeekByte();
            reader.PeekByte();
            reader.PeekByte();
            var peekedByte = reader.PeekByte();
            Assert.That(peekedByte, Is.EqualTo(StringTestData[0])); // Position should not have been advanced
        }
        #endregion
    }
}