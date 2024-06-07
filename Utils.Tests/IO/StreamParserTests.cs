using System.Text;

namespace Utils.Tests.IO
{
    [TestFixture]
    public class StreamParserTests
    {
        public static readonly byte[] EmptyTestData = Array.Empty<byte>();
        public static readonly byte[] StringTestData = Encoding.UTF8.GetBytes("Hello, 世界!");
        public static readonly byte[] BinaryTestData = ByteArrayExtensions.ToBytes(-1.0d, false);
        public static readonly byte[] MixedTestData = StringTestData.Concat(BinaryTestData).ToArray();

        #region Properties
        [Test]
        public void Position_SetterGetter_Success()
        {
            using (MemoryStream stream = new MemoryStream(StringTestData))
            {
                StreamParser reader = new StreamParser(stream);
                Assert.That(reader.Position, Is.Zero);
                reader.Position = 5;
                Assert.That(reader.Position, Is.EqualTo(5));
            }
        }
        [Test]
        public void Available_StreamAvailable_Success()
        {
            using (MemoryStream stream = new MemoryStream(StringTestData))
            {
                StreamParser reader = new StreamParser(stream);
                Assert.That(reader.Available, Is.EqualTo(StringTestData.Length));
                reader.Position = StringTestData.Length;
                Assert.That(reader.Available, Is.Zero);
            }
        }
        #endregion

        #region Constructors
        [Test]
        public void Constructor_StreamAndEncoding_Success()
        {
            using (MemoryStream stream = new MemoryStream(StringTestData))
            {
                StreamParser reader = new StreamParser(stream, Encoding.UTF8);
                Assert.Multiple(() =>
                {
                    Assert.That(reader.Encoding, Is.EqualTo(Encoding.UTF8));
                    Assert.That(reader.LeaveOpen, Is.False);
                });
            }
        }
        [Test]
        public void Constructor_StreamEncodingAndBufferSize_Success()
        {
            using (MemoryStream stream = new MemoryStream(StringTestData))
            {
                StreamParser reader = new StreamParser(stream, Encoding.UTF8, 64);
                Assert.Multiple(() =>
                {
                    Assert.That(reader.Encoding, Is.EqualTo(Encoding.UTF8));
                    Assert.That(reader.LeaveOpen, Is.False);
                });
            }
        }
        [Test]
        public void Constructor_ByteArrayAndEncoding_Success()
        {
            StreamParser reader = new StreamParser(StringTestData, Encoding.UTF8);
            Assert.That(reader.Encoding, Is.EqualTo(Encoding.UTF8));
        }
        #endregion

        #region Methods
        [Test]
        public void Dispose_ClosesStream()
        {
            MemoryStream stream = new MemoryStream(StringTestData);
            StreamParser reader = new StreamParser(stream, Encoding.UTF8);
            reader.Dispose();
            Assert.Throws<ObjectDisposedException>(() => stream.ReadByte());
        }
        #endregion

        #region Peek Methods
        [Test]
        public void PeekBytes_WhenStreamNotEmpty_ReturnsRequestedBytes()
        {
            using (MemoryStream stream = new MemoryStream(BinaryTestData))
            {
                StreamParser reader = new StreamParser(stream);
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
        }

        [Test]
        public void PeekBytes_WhenStreamEmpty_ThrowsEndOfStreamException()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                StreamParser reader = new StreamParser(stream);
                int count = 5;
                Assert.Throws<EndOfStreamException>(() => reader.PeekBytes(count));
            }
        }
        [Test]
        public void PeekBytes_WhenRequestedCountGreaterThanData_Throw()
        {
            using (MemoryStream stream = new MemoryStream(BinaryTestData))
            {
                StreamParser reader = new StreamParser(stream);
                Assert.Throws<EndOfStreamException>(() => reader.PeekBytes(10));
            }
        }
        [Test]
        public void PeekBytes_WhenRequestedCountGreaterThanBuffer_Throw()
        {
            using (MemoryStream stream = new MemoryStream(BinaryTestData))
            {
                StreamParser reader = new StreamParser(stream, 2);
                Assert.Throws<ArgumentOutOfRangeException>(() => reader.PeekBytes(4));
            }
        }
        [Test]
        public void PeekBytes_Success()
        {
            StreamParser reader = new StreamParser(BinaryTestData, Encoding.UTF8);
            reader.Position = StringTestData.Length;
            Assert.That(reader.Available, Is.Zero);
        }
        [Test]
        public void PeekByte_WhenStreamNotEmpty_ReturnsFirstByte()
        {
            using MemoryStream stream = new MemoryStream(StringTestData);
            StreamParser reader = new StreamParser(stream);
            byte peekedByte = reader.PeekByte();
            Assert.That(peekedByte, Is.EqualTo(StringTestData[0]));
        }
        [Test]
        public void PeekByte_WhenStreamEmpty_ThrowsEndOfStreamException()
        {
            using MemoryStream stream = new MemoryStream();
            StreamParser reader = new StreamParser(stream);
            Assert.Throws<EndOfStreamException>(() => reader.PeekByte());
        }
        [Test]
        public void PeekByte_MultipleCalls_DoNotAdvancePosition()
        {
            using MemoryStream stream = new MemoryStream(StringTestData);
            StreamParser reader = new StreamParser(stream);
            reader.PeekByte();
            reader.PeekByte();
            reader.PeekByte();
            var peekedByte = reader.PeekByte();
            Assert.That(peekedByte, Is.EqualTo(StringTestData[0])); // Position should not have been advanced
        }
        #endregion
    }
}