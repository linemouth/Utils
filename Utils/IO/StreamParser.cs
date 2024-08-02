using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Utils
{
    /// <summary>Represents a utility class for reading binary data from a stream.</summary>
    /// <remarks>This class provides methods for reading various data types from a binary stream. It buffers the data read from the stream to improve performance.</remarks>
    public class StreamParser : Stream, IDisposable
    {
        #region Properties & Fields
        /// <summary>The default encoding to use when reading text from the stream.</summary>
        public Encoding Encoding { get; set; }
        /// <summary>The default byte-swapping behavior to use when reading multi-byte types from the stream.</summary>
        public bool ByteSwap { get; set; }
        /// <summary>If true, Dispose() will leave the source stream open.</summary>
        public bool LeaveOpen { get; set; } = false;
        /// <summary>Indicates whether the stream supports seeking.</summary>
        /// <returns>true if the stream supports seeking; otherwise, false.</returns>
        public override bool CanSeek => Stream?.CanSeek ?? true;
        /// <summary>Indicates whether the stream supports reading.</summary>
        /// <returns>true always: StreamParser is read-only.</returns>
        public override bool CanRead => throw new NotImplementedException();
        /// <summary>Indicates whether the stream supports writing.</summary>
        /// <returns>false always: StreamParser is read-only.</returns>
        public override bool CanWrite => false;
        /// <summary>Returns the current position in the stream.</summary>
        /// <exception cref="IndexOutOfRangeException">Thrown if the specified position lies outside the buffer and cannot be reached.</exception>
        public override long Position
        {
            get => position;
            set
            {
                // If the stream supports seeking, simply use that to reach the specified position.
                if (Stream?.CanSeek ?? false)
                {
                    if (value > Stream.Length)
                    {
                        Stream.Position = position = Stream.Length;
                        ResetBuffer();
                        throw new EndOfStreamException();
                    }
                    else
                    {
                        Stream.Position = position = value;
                        ResetBuffer();
                    }
                }
                else
                {
                    long delta = value - position;

                    if (delta < 0)
                    {
                        // Try to seek to an earlier position which still exists in the buffer.
                        if (tail + delta < 0)
                        {
                            throw new ArgumentOutOfRangeException("Unable to seek to a position before the current buffer.");
                        }
                        tail += (int)delta;
                        position = value;
                    }
                    else if (delta > 0)
                    {
                        // Advance to a later position.
                        Skip(delta);
                    }
                }
            }
        }
        /// <summary>Gets the length in bytes of the stream.</summary>
        /// <returns>A long value representing the length of the stream in bytes.</returns>
        /// <exception cref="NotSupportedException">The base stream does not support seeking.</exception>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public override long Length
        {
            get
            {
                ValidateStream();

                return Stream?.Length ?? Buffer.Length;
            }
        }
        /// <summary>Returns the total number of bytes remaining to be read if the stream's length can be determined. Otherwise, returns null, and AvailableInBuffer should be used instead.</summary>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public long? Available => Stream?.CanSeek ?? false ? Stream?.Length - position : null;
        /// <summary>Returns the number of bytes currently remaining in the buffer.</summary>
        public int AvailableInBuffer => head - tail;
        /// <summary>Indicates that there is no more data to be read from the buffer or stream.</summary>
        public bool EoS => AvailableInBuffer == 0;
        /// <summary>Returns up to the next 64 bytes available in the buffer.</summary>
        public string NextBytes => $"[{string.Join(" ", Buffer.Skip(tail).Take(Math.Min(AvailableInBuffer, 64)).Select(b => b.ToString("X2")))}]";
        /// <summary>Returns up to the next 64 bytes available in the buffer, decoded as a string.</summary>
        public string NextChars
        {
            get
            {
                using (Stream stream = new MemoryStream(Buffer.GetBytes(tail, Math.Min(AvailableInBuffer, Encoding.GetMaxByteCount(64)))))
                using (StreamReader reader = new StreamReader(stream))
                {
                    char[] chars = new char[64];
                    int count = reader.ReadBlock(chars, 0, Math.Min(AvailableInBuffer, 64));
                    return new string(chars, 0, count);
                }
            }
        }

        protected Stream Stream { get; private set; } = null;
        protected int MaximumBufferSize => (Stream == null && Buffer != null) ? Buffer.Length : MinimumBufferSize + Math.Clamp(MinimumBufferSize, 8, 0x100000); // Clamp buffer max size from +8 bytes to +1MB.
        protected byte[] Buffer { get; private set; }
        protected char[] DecodeBuffer { get; private set; }
        protected readonly int MinimumBufferSize = 0;
        protected int head = 0;
        protected int tail = 0;

        private long position = 0;
        private Stack<long> positionStack = new Stack<long>();
        private static readonly Regex lineRegex = new Regex(@"^(.*?)\r?(?:\n|$)");
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance of the <see cref="StreamParser"/> class.</summary>
        /// <remarks>The parser will assume the stream contains little-endian fields and UTF-8 text.</remarks>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="bufferSize">The size of the buffer used to read data from the stream.</param>
        public StreamParser(Stream stream, int bufferSize = 0x4000) : this(stream, Encoding.Default, bufferSize) { }
        /// <summary>Initializes a new instance of the <see cref="StreamParser"/> class.</summary>
        /// <remarks>The parser will assume the stream contains little-endian fields.</remarks>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="encoding">The encoding to use when reading text from the stream.</param>
        /// <param name="bufferSize">The size of the buffer used to read data from the stream.</param>
        public StreamParser(Stream stream, Encoding encoding, int bufferSize = 0x4000) : this(stream, encoding, ByteArrayExtensions.MustSwap(ByteArrayExtensions.Endian.Little), bufferSize) { }
        /// <summary>Initializes a new instance of the <see cref="StreamParser"/> class.</summary>
        /// <remarks>The parser will assume the stream contains UTF-8 text.</remarks>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="byteSwap">Whether or not to endian-swap when reading multi-byte fields from the stream.</param>
        /// <param name="bufferSize">The size of the buffer used to read data from the stream.</param>
        public StreamParser(Stream stream, bool byteSwap, int bufferSize = 0x4000) : this(stream, Encoding.Default, byteSwap, bufferSize) { }
        /// <summary>Initializes a new instance of the <see cref="StreamParser"/> class.</summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="encoding">The encoding to use when reading text from the stream.</param>
        /// <param name="byteSwap">Whether or not to endian-swap when reading multi-byte fields from the stream.</param>
        /// <param name="bufferSize">The size of the buffer used to read data from the stream.</param>
        public StreamParser(Stream stream, Encoding encoding, bool byteSwap, int bufferSize = 0x4000)
        {
            if (bufferSize < 1)
            {
                throw new ArgumentOutOfRangeException("Buffer size cannot be less than 1 byte.");
            }

            Stream = stream;
            Encoding = encoding;
            ByteSwap = byteSwap;
            MinimumBufferSize = bufferSize;
            Buffer = new byte[MaximumBufferSize];
            DecodeBuffer = new char[bufferSize];
            ResetBuffer();
        }
        /// <summary>Initializes a new instance of the <see cref="StreamParser"/> class.</summary>
        /// <remarks>The parser will assume the buffer contains little-endian fields and UTF-8 text.</remarks>
        /// <param name="bytes">The byte array to read from.</param>
        public StreamParser(IEnumerable<byte> bytes) : this(bytes, Encoding.Default) { }
        /// <summary>Initializes a new instance of the <see cref="StreamParser"/> class.</summary>
        /// <remarks>The parser will assume the buffer contains little-endian fields.</remarks>
        /// <param name="bytes">The byte array to read from.</param>
        /// <param name="encoding">The encoding to use when reading text from the stream.</param>
        public StreamParser(IEnumerable<byte> bytes, Encoding encoding) : this(bytes, encoding, ByteArrayExtensions.MustSwap(ByteArrayExtensions.Endian.Little)) { }
        /// <summary>Initializes a new instance of the <see cref="StreamParser"/> class.</summary>
        /// <remarks>The parser will assume the buffer contains UTF-8 text.</remarks>
        /// <param name="bytes">The byte array to read from.</param>
        /// <param name="byteSwap">Whether or not to endian-swap when reading multi-byte fields from the stream.</param>
        public StreamParser(IEnumerable<byte> bytes, bool byteSwap) : this(bytes, Encoding.Default, byteSwap) { }
        /// <summary>Initializes a new instance of the <see cref="StreamParser"/> class.</summary>
        /// <param name="bytes">The byte array to read from.</param>
        /// <param name="encoding">The encoding to use when reading text from the stream.</param>
        /// <param name="byteSwap">Whether or not to endian-swap when reading multi-byte fields from the stream.</param>
        public StreamParser(IEnumerable<byte> bytes, Encoding encoding, bool byteSwap)
        {
            Encoding = encoding;
            ByteSwap = byteSwap;
            Buffer = bytes.ToArray();
            MinimumBufferSize = Buffer.Length;
            head = Buffer.Length;
            DecodeBuffer = new char[Buffer.Length];
        }
        #endregion

        #region Peek Methods
        /// <summary>Reads the specified number of bytes from the buffer without advancing the position.</summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>An array containing the requested bytes.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The specified count is negative.</exception>
        /// <exception cref="EndOfStreamException">The requested number of bytes is not available in the buffer.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public byte[] PeekBytes(int count)
        {
            ValidateStream(count);
            byte[] result = new byte[count];
            Array.Copy(Buffer, tail, result, 0, count);
            return result;
        }
        /// <summary>Reads an unsigned byte from the buffer without advancing the position.</summary>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public byte PeekByte()
        {
            ValidateStream(sizeof(byte));
            return Buffer[tail];
        }
        /// <summary>Reads a signed byte from the buffer without advancing the position.</summary>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public sbyte PeekSbyte()
        {
            ValidateStream(sizeof(sbyte));
            return (sbyte)Buffer[tail];
        }
        /// <summary>Reads a 16-bit signed integer from the buffer without advancing the position.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public short PeekShort(bool? byteSwap = null)
        {
            ValidateStream(sizeof(short));
            return Buffer.GetShort(tail, byteSwap ?? ByteSwap);
        }
        /// <summary>Reads a 16-bit unsigned integer from the buffer without advancing the position.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public ushort PeekUshort(bool? byteSwap = null)
        {
            ValidateStream(sizeof(ushort));
            return Buffer.GetUshort(tail, byteSwap ?? ByteSwap);
        }
        /// <summary>Reads a 32-bit signed integer from the buffer without advancing the position.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public int PeekInt(bool? byteSwap = null)
        {
            ValidateStream(sizeof(int));
            return Buffer.GetInt(tail, byteSwap ?? ByteSwap);
        }
        /// <summary>Reads a 32-bit unsigned integer from the buffer without advancing the position.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public uint PeekUint(bool? byteSwap = null)
        {
            ValidateStream(sizeof(uint));
            return Buffer.GetUint(tail, byteSwap ?? ByteSwap);
        }
        /// <summary>Reads a 64-bit signed integer from the buffer without advancing the position.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public long PeekLong(bool? byteSwap = null)
        {
            ValidateStream(sizeof(long));
            return Buffer.GetLong(tail, byteSwap ?? ByteSwap);
        }
        /// <summary>Reads a 64-bit unsigned integer from the buffer without advancing the position.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public ulong PeekUlong(bool? byteSwap = null)
        {
            ValidateStream(sizeof(ulong));
            return Buffer.GetUlong(tail, byteSwap ?? ByteSwap);
        }
        /// <summary>Reads a single-precision floating point number from the buffer without advancing the position.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public float PeekFloat(bool? byteSwap = null)
        {
            ValidateStream(sizeof(float));
            return Buffer.GetFloat(tail, byteSwap ?? ByteSwap);
        }
        /// <summary>Reads a double-precision floating point number from the buffer without advancing the position.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public double PeekDouble(bool? byteSwap = null)
        {
            ValidateStream(sizeof(double));
            return Buffer.GetDouble(tail, byteSwap ?? ByteSwap);
        }
        /// <summary>Decodes a string of a given length from the buffer without advancing the position.</summary>
        /// <param name="charCount">The number of characters to read from the stream.</param>
        /// <param name="encoding">The encoding to use to interpret the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>The string decoded from the buffer.</returns>
        /// <exception cref="DecoderFallbackException">The requested number of characters could not be decoded from the buffer.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached before the specified number of characters could be decoded.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public string PeekString(int charCount, Encoding encoding = null)
        {
            ValidateStream();
            Validation.ThrowIfNegative(charCount, nameof(charCount));

            Decode(charCount, encoding);
            return new string(DecodeBuffer, 0, charCount);
        }
        /// <summary>Reads a string matching a regular expression from the buffer without advancing the position.</summary>
        /// <param name="regex">The regular expression to match. If a capture group is provided, the first capture group will be extracted as the resulting string.</param>
        /// <param name="encoding">The encoding to use to interpret the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>The string matched by the regular expression.</returns>
        /// <exception cref="DecoderFallbackException">A match could not be found before decoding invalid data.</exception>
        /// <exception cref="EndOfStreamException">A match could not be found within the decode buffer.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public string PeekString(Regex regex, Encoding encoding = null) => PeekString(regex, regex.ToString().Length, encoding);
        /// <summary>Reads a string matching a regular expression from the buffer without advancing the position.</summary>
        /// <param name="regex">The regular expression to match. If a capture group is provided, the first capture group will be extracted as the resulting string.</param>
        /// <param name="expectedLength">A rough guess of the index where the capture will end. This is used to improve decoding performance.</param>
        /// <param name="encoding">The encoding to use to interpret the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>The string matched by the regular expression.</returns>
        /// <exception cref="DecoderFallbackException">A match could not be found before decoding invalid data.</exception>
        /// <exception cref="EndOfStreamException">A match could not be found within the decode buffer.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public string PeekString(Regex regex, int expectedLength, Encoding encoding = null)
        {
            Match match = PeekRegex(regex, expectedLength, encoding);
            return match.Groups[1].Success ? match.Groups[1].Value : match.Groups[0].Value;
        }
        /// <summary>Matches a regular expression pattern from the buffer without advancing the position.</summary>
        /// <param name="regex">The regular expression pattern to match.</param>
        /// <param name="encoding">The encoding to use when decoding the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>The resulting match.</returns>
        /// <exception cref="DecoderFallbackException">A match could not be found before decoding invalid data.</exception>
        /// <exception cref="EndOfStreamException">A match could not be found within the decode buffer.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public Match PeekRegex(Regex regex, Encoding encoding = null) => PeekRegex(regex, regex.ToString().Length, encoding);
        /// <summary>Matches a regular expression pattern from the buffer without advancing the position.</summary>
        /// <param name="regex">The regular expression pattern to match.</param>
        /// <param name="expectedLength">A rough guess of the index where the capture will end, if successful. This is used to improve decoding performance.</param>
        /// <param name="encoding">The encoding to use when decoding the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>The resulting match.</returns>
        /// <exception cref="DecoderFallbackException">A match could not be found before decoding invalid data.</exception>
        /// <exception cref="EndOfStreamException">A match could not be found within the decode buffer.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public Match PeekRegex(Regex regex, int expectedLength, Encoding encoding = null)
        {
            ValidateStream();

            encoding = encoding ?? Encoding;
            using (MemoryStream stream = new MemoryStream(Buffer, tail, AvailableInBuffer))
            using (StreamReader reader = new StreamReader(stream, encoding))
            {
                string decodedString = reader.ReadToEnd();
                if (regex.TryMatch(decodedString, out Match match))
                {
                    return match;
                }
                throw new EndOfStreamException("Could not match regular expression within buffer.");
            }
        }
        /// <summary>Reads a line of characters from the buffer without advancing the position.</summary>
        /// <param name="encoding">The encoding to use when decoding the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>The resulting line.</returns>
        /// <exception cref="DecoderFallbackException">A match could not be found before decoding invalid data.</exception>
        /// <exception cref="EndOfStreamException">A match could not be found within the decode buffer.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public string PeekLine(Encoding encoding = null) => PeekString(lineRegex, encoding);
        #endregion

        #region Read Methods
        /// <summary>Reads up to the given number of bytes from the buffer and/or stream and copies them to the provided buffer, advancing the position by the number of bytes read.</summary>
        /// <param name="buffer">The destination array to which the data will be copied.</param>
        /// <param name="offset">The starting position in the destination array at which to start copying data.</param>
        /// <param name="count">The maximum number of bytes to be read from the stream.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
        /// <exception cref="ArgumentException">The sum of offset and count is larger than the buffer length.</exception>
        /// <exception cref="ArgumentNullException">The buffer is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Offset or count is negative.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        /// <exception cref="NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateStream();
            Validation.ThrowIfNegative(offset, nameof(offset));
            Validation.ThrowIfNegative(count, nameof(count));
            if (offset + count > buffer.Length)
            {
                throw new ArgumentException("Cannot fit the requested number of bytes into the remaining space of the buffer.");
            }

            if (count <= AvailableInBuffer)
            {
                // Copy everything we need from the buffer.
                Array.Copy(Buffer, tail, buffer, offset, count);
                Skip(count);
                return count;
            }
            else
            {
                // Copy everything in the buffer.
                Array.Copy(Buffer, tail, buffer, offset, AvailableInBuffer);
                int bytesCopied = AvailableInBuffer;

                // Read the remaining bytes directly from the stream.
                bytesCopied += Stream.Read(buffer, offset + bytesCopied, count - bytesCopied);

                // Restore the buffer at the current position.
                position += bytesCopied;
                ResetBuffer();

                return bytesCopied;
            }
        }
        /// <summary>Reads the specified number of bytes from the buffer and/or stream, advancing the position by the number of bytes read.</summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>An array containing the requested bytes.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The specified count is negative.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached before the specified number of bytes could be read.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public byte[] ReadBytes(int count)
        {
            ValidateStream();
            Validation.ThrowIfNegative(count, nameof(count));

            byte[] result = new byte[count];
            int actualCount = Read(result, 0, count);
            if (actualCount != count)
            {
                throw new EndOfStreamException();
            }
            return result;
        }
        /// <summary>Reads an unsigned byte from the buffer, advancing the position by 1 byte.</summary>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public new byte ReadByte()
        {
            byte result = PeekByte();
            Skip(1);
            return result;
        }
        /// <summary>Reads a signed byte from the buffer, advancing the position by 1 byte.</summary>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public sbyte ReadSbyte()
        {
            sbyte result = PeekSbyte();
            Skip(1);
            return result;
        }
        /// <summary>Reads a 16-bit signed integer from the buffer, advancing the position by 2 bytes.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public short ReadShort(bool? byteSwap = null)
        {
            byteSwap = byteSwap ?? ByteSwap;
            short result = PeekShort(byteSwap);
            Skip(sizeof(short));
            return result;
        }
        /// <summary>Reads a 16-bit unsigned integer from the buffer, advancing the position by 2 bytes.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public ushort ReadUshort(bool? byteSwap = null)
        {
            byteSwap = byteSwap ?? ByteSwap;
            ushort result = PeekUshort(byteSwap);
            Skip(sizeof(ushort));
            return result;
        }
        /// <summary>Reads a 32-bit signed integer from the buffer, advancing the position by 4 bytes.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public int ReadInt(bool? byteSwap = null)
        {
            byteSwap = byteSwap ?? ByteSwap;
            int result = PeekInt(byteSwap);
            Skip(sizeof(int));
            return result;
        }
        /// <summary>Reads a 32-bit unsigned integer from the buffer, advancing the position by 4 bytes.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public uint ReadUint(bool? byteSwap = null)
        {
            byteSwap = byteSwap ?? ByteSwap;
            uint result = PeekUint(byteSwap);
            Skip(sizeof(uint));
            return result;
        }
        /// <summary>Reads a 64-bit signed integer from the buffer, advancing the position by 8 bytes.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public long ReadLong(bool? byteSwap = null)
        {
            byteSwap = byteSwap ?? ByteSwap;
            long result = PeekLong(byteSwap);
            Skip(sizeof(long));
            return result;
        }
        /// <summary>Reads a 64-bit unsigned integer from the buffer, advancing the position by 8 bytes.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public ulong ReadUlong(bool? byteSwap = null)
        {
            byteSwap = byteSwap ?? ByteSwap;
            ulong result = PeekUlong(byteSwap);
            Skip(sizeof(ulong));
            return result;
        }
        /// <summary>Reads a single-precision floating point number from the buffer, advancing the position by 4 bytes.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public float ReadFloat(bool? byteSwap = null)
        {
            byteSwap = byteSwap ?? ByteSwap;
            float result = PeekShort(byteSwap);
            Skip(sizeof(float));
            return result;
        }
        /// <summary>Reads a double-precision floating point number from the buffer, advancing the position by 8 bytes.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public double ReadDouble(bool? byteSwap = null)
        {
            byteSwap = byteSwap ?? ByteSwap;
            double result = PeekDouble(byteSwap);
            Skip(sizeof(double));
            return result;
        }
        /// <summary>Decodes a string of a given length from the buffer, advancing the position by the number of bytes decoded.</summary>
        /// <param name="charCount">The number of characters to read from the stream.</param>
        /// <param name="encoding">The encoding to use to interpret the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>The string decoded from the buffer.</returns>
        /// <exception cref="DecoderFallbackException">The requested number of characters could not be decoded from the buffer.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached before the specified number of characters could be decoded.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public string ReadString(int charCount, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding;
            string result = PeekString(charCount, encoding);
            Skip(encoding.GetByteCount(result));
            return result;
        }
        /// <summary>Reads a string matching a regular expression from the buffer, advancing the position to the end of the capture.</summary>
        /// <param name="regex">The regular expression to match. If a capture group is provided, the first capture group will be extracted as the resulting string.</param>
        /// <param name="encoding">The encoding to use to interpret the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>The string matched by the regular expression.</returns>
        /// <exception cref="DecoderFallbackException">A match could not be found before decoding invalid data.</exception>
        /// <exception cref="EndOfStreamException">A match could not be found within the decode buffer.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public string ReadString(Regex regex, Encoding encoding = null) => ReadString(regex, regex.ToString().Length, encoding);
        /// <summary>Reads a string matching a regular expression from the buffer, advancing the position to the end of the capture.</summary>
        /// <param name="regex">The regular expression to match. If a capture group is provided, the first capture group will be extracted as the resulting string.</param>
        /// <param name="expectedLength">A rough guess of the index where the capture will end. This is used to improve decoding performance.</param>
        /// <param name="encoding">The encoding to use to interpret the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>The string matched by the regular expression.</returns>
        /// <exception cref="DecoderFallbackException">A match could not be found before decoding invalid data.</exception>
        /// <exception cref="EndOfStreamException">A match could not be found within the decode buffer.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public string ReadString(Regex regex, int expectedLength, Encoding encoding = null)
        {
            Match match = ReadRegex(regex, expectedLength, encoding);
            return match.Groups[1].Success ? match.Groups[1].Value : match.Groups[0].Value;
        }
        /// <summary>Matches a regular expression pattern from the buffer, advancing the position to the end of the capture.</summary>
        /// <param name="regex">The regular expression pattern to match.</param>
        /// <param name="encoding">The encoding to use when decoding the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>The resulting match.</returns>
        /// <exception cref="DecoderFallbackException">A match could not be found before decoding invalid data.</exception>
        /// <exception cref="EndOfStreamException">A match could not be found within the decode buffer.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public Match ReadRegex(Regex regex, Encoding encoding = null) => ReadRegex(regex, regex.ToString().Length, encoding);
        /// <summary>Matches a regular expression pattern from the buffer, advancing the position to the end of the capture.</summary>
        /// <param name="regex">The regular expression pattern to match.</param>
        /// <param name="expectedLength">A rough guess of the index where the capture will end, if successful. This is used to improve decoding performance.</param>
        /// <param name="encoding">The encoding to use when decoding the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>The resulting match.</returns>
        /// <exception cref="DecoderFallbackException">A match could not be found before decoding invalid data.</exception>
        /// <exception cref="EndOfStreamException">A match could not be found within the decode buffer.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public Match ReadRegex(Regex regex, int expectedLength, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding;
            Match match = PeekRegex(regex, expectedLength, encoding);
            Skip(encoding.GetByteCount(DecodeBuffer, 0, match.Index + match.Length));
            return match;
        }
        /// <summary>Reads a line of characters from the buffer, advancing the position to after the following newline.</summary>
        /// <param name="encoding">The encoding to use when decoding the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>The resulting line.</returns>
        /// <exception cref="DecoderFallbackException">A match could not be found before decoding invalid data.</exception>
        /// <exception cref="EndOfStreamException">A match could not be found within the decode buffer.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public string ReadLine(Encoding encoding = null) => ReadString(lineRegex, encoding);
        #endregion

        #region TryPeek Methods
        /// <summary>Attempts to read the specified number of bytes from the buffer without advancing the position.</summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <param name="result">Returns an array containing the requested bytes on success. Otherwise, the value should be discarded.</param>
        /// <returns>true if the specified number of bytes was successfully read; otherwise, false.</returns>
        public bool TryPeekBytes(int count, out byte[] result)
        {
            if (Buffer != null && AvailableInBuffer >= count)
            {
                result = Buffer.GetBytes(tail, count);
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read an unsigned byte from the buffer without advancing the position.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryPeekByte(out byte result)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(byte))
            {
                result = Buffer[tail];
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a signed byte from the buffer without advancing the position.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryPeekSbyte(out sbyte result)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(sbyte))
            {
                result = (sbyte)Buffer[tail];
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a 16-bit signed integer from the buffer without advancing the position.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryPeekShort(out short result, bool? byteSwap = null)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(short))
            {
                result = Buffer.GetShort(tail, byteSwap ?? ByteSwap);
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a 16-bit unsigned integer from the buffer without advancing the position.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryPeekUshort(out ushort result, bool? byteSwap = null)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(ushort))
            {
                result = Buffer.GetUshort(tail, byteSwap ?? ByteSwap);
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a 32-bit signed integer from the buffer without advancing the position.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryPeekInt(out int result, bool? byteSwap = null)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(int))
            {
                result = Buffer.GetInt(tail, byteSwap ?? ByteSwap);
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a 32-bit unsigned integer from the buffer without advancing the position.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryPeekUint(out uint result, bool? byteSwap = null)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(uint))
            {
                result = Buffer.GetUint(tail, byteSwap ?? ByteSwap);
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a 64-bit signed integer from the buffer without advancing the position.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryPeekLong(out long result, bool? byteSwap = null)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(long))
            {
                result = Buffer.GetLong(tail, byteSwap ?? ByteSwap);
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a 64-bit unsigned integer from the buffer without advancing the position.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryPeekUlong(out ulong result, bool? byteSwap = null)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(ulong))
            {
                result = Buffer.GetUlong(tail, byteSwap ?? ByteSwap);
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a single-precision floating-point number from the buffer without advancing the position.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryPeekFloat(out float result, bool? byteSwap = null)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(float))
            {
                result = Buffer.GetShort(tail, byteSwap ?? ByteSwap);
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a double-precision floating-point number from the buffer without advancing the position.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryPeekDouble(out double result, bool? byteSwap = null)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(double))
            {
                result = Buffer.GetShort(tail, byteSwap ?? ByteSwap);
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to decode a string of characters from the buffer without advancing the position.</summary>
        /// <param name="charCount">The number of characters to decode.</param>
        /// <param name="result">Returns the resulting string on success. Otherwise, the value should be discarded.</param>
        /// <param name="encoding">The encoding to use when decoding the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>true if the string was successfully decoded; otherwise false.</returns>
        public bool TryPeekString(int charCount, out string result, Encoding encoding = null)
        {
            if (TryDecode(charCount, encoding))
            {
                result = new string(DecodeBuffer, 0, charCount);
                return true;
            }
            result = null;
            return false;
        }
        /// <summary>Attempts to decode a string matching a regular expression pattern from the buffer without advancing the position.</summary>
        /// <remarks>If a capture group is specified, the resulting string will be the contents of the first capture group. Otherwise, it will be the entire capture (group[0]).</remarks>
        /// <param name="regex">The regular expression to match. If a capture group is provided, the first capture group will be extracted as the resulting string.</param>
        /// <param name="result">Returns the resulting string on success. Otherwise, the value should be discarded.</param>
        /// <param name="encoding">The encoding to use when decoding the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>true if a matching string was successfully captured; otherwise, false.</returns>
        public bool TryPeekString(Regex regex, out string result, Encoding encoding = null) => TryPeekString(regex, regex.ToString().Length, out result, encoding);
        /// <summary>Attempts to decode a string matching a regular expression pattern from the buffer without advancing the position.</summary>
        /// <remarks>If a capture group is specified, the resulting string will be the contents of the first capture group. Otherwise, it will be the entire capture (group[0]).</remarks>
        /// <param name="regex">The regular expression to match. If a capture group is provided, the first capture group will be extracted as the resulting string.</param>
        /// <param name="expectedLength">A rough guess of the index where the capture will end, if successful. This is used to improve decoding performance.</param>
        /// <param name="result">Returns the resulting string on success. Otherwise, the value should be discarded.</param>
        /// <param name="encoding">The encoding to use when decoding the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>true if a matching string was successfully captured; otherwise, false.</returns>
        public bool TryPeekString(Regex regex, int expectedLength, out string result, Encoding encoding = null)
        {
            if (TryPeekRegex(regex, expectedLength, out Match match, encoding))
            {
                result = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[0].Value;
                return true;
            }
            result = null;
            return false;
        }
        /// <summary>Attempts to match a regular expression pattern from the buffer without advancing the position.</summary>
        /// <param name="regex">The regular expression pattern to match.</param>
        /// <param name="match">Returns the resulting match on success. Otherwise, the value should be discarded.</param>
        /// <param name="encoding">The encoding to use when decoding the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>true if a match was successfully captured; otherwise, false.</returns>
        public bool TryPeekRegex(Regex regex, out Match match, Encoding encoding = null) => TryPeekRegex(regex, regex.ToString().Length, out match, encoding);
        /// <summary>Attempts to match a regular expression pattern from the buffer without advancing the position.</summary>
        /// <param name="regex">The regular expression pattern to match.</param>
        /// <param name="expectedLength">A rough guess of the index where the capture will end, if successful. This is used to improve decoding performance.</param>
        /// <param name="match">Returns the resulting match on success. Otherwise, the value should be discarded.</param>
        /// <param name="encoding">The encoding to use when decoding the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>true if a match was successfully captured; otherwise, false.</returns>
        public bool TryPeekRegex(Regex regex, int expectedLength, out Match match, Encoding encoding = null)
        {
            if (Buffer != null)
            {
                encoding = encoding ?? Encoding;
                int bufferTail = tail;
                int decodeHead = 0;
                expectedLength = Math.Min(DecodeBuffer.Length, expectedLength);
                int bytesToRead = Math.Min(encoding.GetMaxByteCount(expectedLength), AvailableInBuffer);
                int growIncrement = Math.Clamp(expectedLength / 4, 64, 0x8000); // Default grow increment is 25% of the original guess, clamped to the range [64B,32kB].

                while (decodeHead < DecodeBuffer.Length)
                {
                    try
                    {
                        // Decode the next set of characters.
                        int charsDecoded = encoding.GetChars(Buffer, bufferTail, bytesToRead, DecodeBuffer, decodeHead);
                        bufferTail += bytesToRead;

                        // Check for a fallback character.
                        if (encoding.DecoderFallback is DecoderReplacementFallback replacementFallback)
                        {
                            int fallbackIndex = Array.IndexOf(DecodeBuffer, replacementFallback.DefaultString[0], decodeHead, charsDecoded);
                            if (fallbackIndex >= 0)
                            {
                                // Fallback found; try to match up to the start of the fallback string
                                return regex.TryMatch(new string(DecodeBuffer, 0, fallbackIndex), out match);
                            }
                        }

                        // Check for end of stream.
                        int eosIndex = Array.IndexOf(DecodeBuffer, '\0', decodeHead, charsDecoded);
                        if(eosIndex >= 0)
                        {
                            decodeHead += eosIndex;

                            // EoS found; try to match up to the start of the fallback string
                            return regex.TryMatch(new string(DecodeBuffer, 0, decodeHead), out match);
                        }

                        // Adcance the head of the DecodeBuffer.
                        decodeHead += charsDecoded;

                        // Attempt a match on the current DecodeBuffer contents.
                        if (regex.TryMatch(new string(DecodeBuffer, 0, decodeHead), out match))
                        {
                            return true;
                        }

                        // Prepare to decode more bytes, growing with each attempt.
                        bytesToRead = Math.Min(AvailableInBuffer - bytesToRead, growIncrement);

                        // Next time, we'll grow by an increment in the range [4kB,32kB].
                        growIncrement = Math.Clamp(growIncrement * 4, 0x1000, 0x8000);
                    }
                    catch (DecoderFallbackException e)
                    {
                        // We encountered an invalid byte sequence. We can only hope to use the characters up to that point.
                        Regex decoderFallbackRegex = new Regex(@"at index (\d+)");
                        Match fallbackMatch = decoderFallbackRegex.Match(e.Message);
                        if (fallbackMatch.Success && int.TryParse(fallbackMatch.Groups[1].Value, out int index))
                        {
                            // If the index is known, we can calculate the end of the DecodeBuffer.
                            decodeHead += encoding.GetCharCount(Buffer, bufferTail, index);
                            return regex.TryMatch(new string(DecodeBuffer, 0, decodeHead), out match);
                        }

                        // Otherwise, we don't have more valid data to decode.
                        break;
                    }
                    catch (ArgumentException e)
                    {
                        // We ran out of room in the DecodeBuffer.
                        if (e.Message.StartsWith("The output char buffer is too small to contain the decoded characters"))
                        {
                            // We might as well check the whole buffer while we're here.
                            decodeHead = DecodeBuffer.Length;
                            return regex.TryMatch(new string(DecodeBuffer, 0, decodeHead), out match);
                        }

                        // Otherwise, we don't have enough space to decode more data.
                        break;
                    }
                }
            }
            match = null;
            return false;
        }
        /// <summary>Attempts to read a line of characters from the buffer without advancing the position.</summary>
        /// <param name="line">Returns the resulting line on success. Otherwise, the value should be discarded.</param>
        /// <param name="encoding">The encoding to use when decoding the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>true if a line was successfully read; otherwise, false.</returns>
        public bool TryPeekLine(out string line, Encoding encoding = null) => TryPeekString(lineRegex, out line, encoding);
        #endregion

        #region TryRead Methods
        /// <summary>Attempts to read the specified number of bytes from the buffer. If successful, the position is advanced by the number of bytes read.</summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <param name="result">Returns an array containing the requested bytes on success. Otherwise, the value should be discarded.</param>
        /// <returns>true if the specified number of bytes was successfully read; otherwise, false.</returns>
        public bool TryReadBytes(int count, out byte[] result)
        {
            if (Buffer != null && count <= AvailableInBuffer)
            {
                try
                {
                    result = new byte[count];
                    Array.Copy(Buffer, tail, result, 0, count);
                    Skip(count);
                    return true;
                }
                catch { }
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read an unsigned byte from the buffer. If successful, the position is advanced by 1 byte.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryReadByte(out byte result)
        {
            if (Buffer != null && AvailableInBuffer >= 1)
            {
                result = Buffer[tail];
                Skip(1);
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a signed byte from the buffer. If successful, the position is advanced by 1 byte.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryReadSbyte(out sbyte result)
        {
            if (Buffer != null && AvailableInBuffer >= 1)
            {
                result = (sbyte)Buffer[tail];
                Skip(1);
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a 16-bit signed integer from the buffer. If successful, the position is advanced by 2 bytes.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryReadShort(out short result, bool? byteSwap = null)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(short))
            {
                result = Buffer.GetShort(tail, byteSwap ?? ByteSwap);
                Skip(sizeof(short));
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read the specified 16-bit unsigned integer from the buffer. If successful, the position is advanced by 2 bytes.</summary>
        /// <param name="value">Specifies the value which must be read in order to be successful.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryReadUshort(ushort value, bool? byteSwap = null)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(ushort) && Buffer.GetUshort(tail, byteSwap ?? ByteSwap) == value)
            {
                Skip(sizeof(ushort));
                return true;
            }
            return false;
        }
        /// <summary>Attempts to read a 16-bit unsigned integer from the buffer. If successful, the position is advanced by 2 bytes.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryReadUshort(out ushort result, bool? byteSwap = null)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(ushort))
            {
                result = Buffer.GetUshort(tail, byteSwap ?? ByteSwap);
                Skip(sizeof(ushort));
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a 32-bit signed integer from the buffer. If successful, the position is advanced by 4 bytes.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryReadInt(out int result, bool? byteSwap = null)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(int))
            {
                result = Buffer.GetInt(tail, byteSwap ?? ByteSwap);
                Skip(sizeof(int));
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a 32-bit unsigned integer from the buffer. If successful, the position is advanced by 4 bytes.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryReadUint(out uint result, bool? byteSwap = null)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(uint))
            {
                result = Buffer.GetUint(tail, byteSwap ?? ByteSwap);
                Skip(sizeof(uint));
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a 64-bit signed integer from the buffer. If successful, the position is advanced by 8 bytes.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryReadLong(out long result, bool? byteSwap = null)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(long))
            {
                result = Buffer.GetLong(tail, byteSwap ?? ByteSwap);
                Skip(sizeof(long));
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a 64-bit unsigned integer from the buffer. If successful, the position is advanced by 8 bytes.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryReadUlong(out ulong result, bool? byteSwap = null)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(ulong))
            {
                result = Buffer.GetUlong(tail, byteSwap ?? ByteSwap);
                Skip(sizeof(ulong));
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a single-precision floating-point number from the buffer. If successful, the position is advanced by 4 bytes.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryReadFloat(out float result, bool? byteSwap = null)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(float))
            {
                result = Buffer.GetShort(tail, byteSwap ?? ByteSwap);
                Skip(sizeof(float));
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a double-precision floating-point number from the buffer. If successful, the position is advanced by 8 bytes.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryReadDouble(out double result, bool? byteSwap = null)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(double))
            {
                result = Buffer.GetShort(tail, byteSwap ?? ByteSwap);
                Skip(sizeof(double));
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to decode a string of characters from the buffer. If successful, the position is advanced by the number of bytes that were decoded.</summary>
        /// <param name="charCount">The number of characters to decode.</param>
        /// <param name="result">Returns the resulting string on success. Otherwise, the value should be discarded.</param>
        /// <param name="encoding">The encoding to use when decoding the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>true if the string was successfully decoded; otherwise false.</returns>
        public bool TryReadString(int charCount, out string result, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            if (TryPeekString(charCount, out result, encoding))
            {
                Skip(encoding.GetByteCount(result));
                return true;
            }
            result = null;
            return false;
        }
        /// <summary>Attempts to decode a string matching a regular expression pattern from the buffer. If successful, the position is advanced to the end of the capture.</summary>
        /// <remarks>If a capture group is specified, the resulting string will be the contents of the first capture group. Otherwise, it will be the entire capture (group[0]).</remarks>
        /// <param name="regex">The regular expression to match. If a capture group is provided, the first capture group will be extracted as the resulting string.</param>
        /// <param name="result">Returns the resulting string on success. Otherwise, the value should be discarded.</param>
        /// <param name="encoding">The encoding to use when decoding the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>true if a matching string was successfully captured; otherwise, false.</returns>
        public bool TryReadString(Regex regex, out string result, Encoding encoding = null) => TryReadString(regex, regex.ToString().Length, out result, encoding);
        /// <summary>Attempts to decode a string matching a regular expression pattern from the buffer. If successful, the position is advanced to the end of the capture.</summary>
        /// <remarks>If a capture group is specified, the resulting string will be the contents of the first capture group. Otherwise, it will be the entire capture (group[0]).</remarks>
        /// <param name="regex">The regular expression to match. If a capture group is provided, the first capture group will be extracted as the resulting string.</param>
        /// <param name="expectedLength">A rough guess of the index where the capture will end, if successful. This is used to improve decoding performance.</param>
        /// <param name="result">Returns the resulting string on success. Otherwise, the value should be discarded.</param>
        /// <param name="encoding">The encoding to use when decoding the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>true if a matching string was successfully captured; otherwise, false.</returns>
        public bool TryReadString(Regex regex, int expectedLength, out string result, Encoding encoding = null)
        {
            if (TryReadRegex(regex, expectedLength, out Match match, encoding))
            {
                result = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[0].Value;
                // Skip() is performed in TryReadRegex().
                return true;
            }
            result = null;
            return false;
        }
        /// <summary>Attempts to match a regular expression pattern from the buffer. If successful, the position is advanced to the end of the capture.</summary>
        /// <param name="regex">The regular expression pattern to match.</param>
        /// <param name="match">Returns the resulting match on success. Otherwise, the value should be discarded.</param>
        /// <param name="encoding">The encoding to use when decoding the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>true if a match was successfully captured; otherwise, false.</returns>
        public bool TryReadRegex(Regex regex, out Match match, Encoding encoding = null) => TryReadRegex(regex, regex.ToString().Length, out match, encoding);
        /// <summary>Attempts to match a regular expression pattern from the buffer. If successful, the position is advanced to the end of the capture.</summary>
        /// <param name="regex">The regular expression pattern to match.</param>
        /// <param name="expectedLength">A rough guess of the index where the capture will end, if successful. This is used to improve decoding performance.</param>
        /// <param name="match">Returns the resulting match on success. Otherwise, the value should be discarded.</param>
        /// <param name="encoding">The encoding to use when decoding the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>true if a match was successfully captured; otherwise, false.</returns>
        public bool TryReadRegex(Regex regex, int expectedLength, out Match match, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding;
            if (TryPeekRegex(regex, expectedLength, out match, encoding))
            {
                Skip(encoding.GetByteCount(DecodeBuffer, 0, match.Index + match.Length));
                return true;
            }
            return false;
        }
        /// <summary>Attempts to read a line of characters from the buffer. If successful, the position is advanced to after the following newline.</summary>
        /// <param name="line">Returns the resulting line on success. Otherwise, the value should be discarded.</param>
        /// <param name="encoding">The encoding to use when decoding the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>true if a line was successfully read; otherwise, false.</returns>
        public bool TryReadLine(out string line, Encoding encoding = null) => TryReadString(lineRegex, out line, encoding);
        #endregion

        #region Assert Methods
        /// <summary>Reads an exact 16-bit unsigned integer from the start of the buffer, advancing the position by 2 bytes.</summary>
        /// <param name="valueToMatch">The exact ushort which must be read for the function to succeed.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public void AssertUshort(ushort valueToMatch, bool? byteSwap = null)
        {
            byteSwap = byteSwap ?? ByteSwap;
            if (valueToMatch != PeekUshort(byteSwap))
            {
                throw new InvalidDataException($"Failed to match ushort: '{valueToMatch}'");
            }
            Skip(sizeof(ushort));
        }
        /// <summary>Decodes an exact string from the buffer, advancing the position to the end of the match.</summary>
        /// <param name="textToMatch">The exact string which must be decoded for the function to succeed.</param>
        /// <param name="encoding">The encoding to use to interpret the buffer. If not provided, the stream's encoding will be used.</param>
        /// <exception cref="DecoderFallbackException">The requested number of characters could not be decoded from the buffer.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached before the specified number of characters could be decoded.</exception>
        /// <exception cref="InvalidDataException">The given string could not be decoded from the start of the buffer.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public void AssertString(string textToMatch, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding;
            string result = PeekString(textToMatch.Length, encoding);
            if (textToMatch != result)
            {
                throw new InvalidDataException($"Failed to match string: '{textToMatch}'");
            }
            Skip(encoding.GetByteCount(result));
        }
        #endregion

        #region TryAssert Methods
        /// <summary>Attempts to read an exact 16-bit unsigned integer from the start of the buffer, advancing the position by 2 bytes on success.</summary>
        /// <param name="valueToMatch">The exact ushort which must be read for the function to succeed.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the exact ushort was read from the start of the buffer; otherwise false.</returns>
        public bool TryAssertUshort(ushort valueToMatch, bool? byteSwap = null)
        {
            byteSwap = byteSwap ?? ByteSwap;
            if (TryPeekUshort(out ushort result, byteSwap) && result == valueToMatch)
            {
                Skip(sizeof(ushort));
                return true;
            }
            return false;
        }
        /// <summary>Attempts to decode an exact string from the start of the buffer, advancing the position to the end of the match on success.</summary>
        /// <param name="textToMatch">The exact string which must be decoded for the function to succeed.</param>
        /// <param name="encoding">The encoding to use to interpret the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>true if the exact string was read from the start of the buffer; otherwise false.</returns>
        public bool TryAssertString(string textToMatch, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding;
            if(TryPeekString(textToMatch.Length, out string result, encoding) && result == textToMatch)
            {
                Skip(encoding.GetByteCount(result));
                return true;
            }
            return false;
        }
        #endregion

        #region Other Methods
        /// <summary>Releases all resources used by the <see cref="StreamParser"/> object.</summary>
        public new void Dispose()
        {
            Buffer = null;
            DecodeBuffer = null;
            if (!LeaveOpen)
            {
                // Destroy the stream.
                Stream?.Dispose();
            }
            else if (Stream.CanSeek)
            {
                // Restore the stream to the current position.
                Stream.Position = Position;
            }
            Stream = null;
        }
        public override void Flush() => Stream?.Flush();
        public override long Seek(long offset, SeekOrigin origin)
        {
            ValidateStream();

            if(Stream != null)
            {
                position = Stream.Seek(offset, origin);
                ResetBuffer();
                return position;
            }
            
            switch (origin)
            {
                default:
                case SeekOrigin.Begin:
                    Position = offset;
                    return Position;
                case SeekOrigin.End:
                    Position = Buffer.Length + offset;
                    return Position;
                case SeekOrigin.Current:
                    Position += offset;
                    return Position;
            }
        }
        public override void SetLength(long value) => throw new InvalidOperationException("Tried to change the length of a read-only stream.");
        /// <summary>Skips a specified number of bytes in the stream without advancing the position.</summary>
        /// <param name="byteCount">The number of bytes to skip.</param>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public void Skip(long byteCount)
        {
            ValidateStream();

            if (byteCount >= AvailableInBuffer)
            {
                // Consume all bytes in the buffer.
                byteCount -= AvailableInBuffer;
                position += AvailableInBuffer;
                tail = head = 0;

                // Advance the stream
                if (Stream != null)
                {
                    if (Stream.CanSeek)
                    {
                        // If the stream supports seeking, this is easy.
                        Stream.Seek(byteCount, SeekOrigin.Current);
                    }
                    else
                    {
                        // Otherwise, we must incrementally read bytes to discard.
                        while (byteCount > 0)
                        {
                            int bytesRead = Stream.Read(Buffer, 0, (int)Math.Min(byteCount, Buffer.Length));
                            if (bytesRead == 0)
                            {
                                throw new EndOfStreamException();
                            }
                            byteCount -= bytesRead;
                            position += bytesRead;
                        }
                    }
                    ResetBuffer();
                }
                else
                {
                    // If there is no stream, and all bytes have been consumed, we've reached the end.
                    throw new EndOfStreamException();
                }
            }
            else
            {
                // Consume bytes in the buffer.
                tail += (int)byteCount;
                position += byteCount;
            }

            UpdateBuffer();
        }
        public override void Write(byte[] buffer, int offset, int count) => throw new InvalidOperationException("Tried to write to a read-only stream.");
        public void PushPosition() => positionStack.Push(Position);
        public void PopPosition() => Position = positionStack.Pop();

        /// <summary>Attempts to decode characters from the stream using the specified encoding, up to the specified maximum number of characters.</summary>
        /// <param name="charCount">The maximum number of characters to decode.</param>
        /// <param name="encoding">The encoding to use to interpret the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>The number of characters actually decoded.</returns>
        private bool TryDecode(int charCount, Encoding encoding = null)
        {
            if (Buffer != null && charCount >= 0 && charCount <= DecodeBuffer.Length)
            {
                encoding = encoding ?? Encoding;
                int bytesToRead = Math.Min(encoding.GetMaxByteCount(charCount), AvailableInBuffer);

                try
                {
                    // Decode the next set of characters.
                    int charsDecoded = encoding.GetChars(Buffer, tail, bytesToRead, DecodeBuffer, 0);

                    // Check for a fallback character.
                    if (encoding.DecoderFallback is DecoderReplacementFallback replacementFallback)
                    {
                        int fallbackCharIndex = Array.IndexOf(DecodeBuffer, replacementFallback.DefaultString[0], 0, charsDecoded);
                        if(fallbackCharIndex >= 0)
                        {
                            charsDecoded = fallbackCharIndex;
                        }
                    }

                    // If the requested number of characters was decoded, return successfully.
                    if (charsDecoded >= charCount)
                    {
                        return true;
                    }
                }
                catch (DecoderFallbackException e)
                {
                    // We encountered an invalid byte sequence. Parhaps it came after the requested number of characters.
                    Regex decoderFallbackRegex = new Regex(@"at index (\d+)");
                    Match fallbackMatch = decoderFallbackRegex.Match(e.Message);
                    if (fallbackMatch.Success && int.TryParse(fallbackMatch.Groups[1].Value, out int index))
                    {
                        // If the index is known, we can calculate the end of the DecodeBuffer.
                        int charsDecoded = encoding.GetCharCount(Buffer, tail, index);

                        // If the requested number of characters was decoded, return successfully.
                        if (charsDecoded >= charCount)
                        {
                            return true;
                        }
                    }

                    // Otherwise, fail.
                }
                catch (ArgumentException e)
                {
                    // We ran out of room in the DecodeBuffer.
                    if (e.Message.StartsWith("The output char buffer is too small to contain the decoded characters"))
                    {
                        // Making some assumptions about the decoder, we will assume the whole DecodeBuffer was filled.
                        return true;
                    }

                    // Otherwise, this is an unfamiliar error. Fail.
                }
            }
            return false;
        }
        /// <summary>Decodes characters from the stream using the specified encoding until at least the specified minimum number of characters are decoded.</summary>
        /// <param name="minCharCount">The minimum number of characters to decode.</param>
        /// <param name="encoding">The encoding to use to interpret the buffer. If not provided, the stream's encoding will be used.</param>
        private void Decode(int charCount, Encoding encoding)
        {
            ValidateStream();
            Validation.ThrowIfNegative(charCount, nameof(charCount));

            if (charCount > DecodeBuffer.Length)
            {
                throw new ArgumentOutOfRangeException($"Cannot decode more characters than will fit in the decode buffer ({DecodeBuffer.Length}).");
            }

            encoding = encoding ?? Encoding;
            int bytesToRead = Math.Min(encoding.GetMaxByteCount(charCount), AvailableInBuffer);

            try
            {
                // Decode the next set of characters.
                int charsDecoded = encoding.GetChars(Buffer, tail, bytesToRead, DecodeBuffer, 0);

                // Check for a fallback character.
                if (encoding.DecoderFallback is DecoderReplacementFallback replacementFallback)
                {
                    int fallbackCharIndex = Array.IndexOf(DecodeBuffer, replacementFallback.DefaultString[0], 0, charsDecoded);
                    if (fallbackCharIndex >= 0 && fallbackCharIndex < charCount)
                    {
                        // If there was an invalid character within the requested number of characters, throw DecoderFallbackException.
                        // Calculate the index of the invalid byte sequence from the tail.
                        int fallbackByteIndex = encoding.GetByteCount(DecodeBuffer, 0, fallbackCharIndex) + tail;
                        throw new DecoderFallbackException($"Invalid byte sequence found at index {fallbackByteIndex}");
                    }
                }

                // If the requested number of characters was decoded, return successfully.
                if (charsDecoded >= charCount)
                {
                    return;
                }
            }
            catch (DecoderFallbackException e)
            {
                // We encountered an invalid byte sequence. Parhaps it came after the requested number of characters.
                Regex decoderFallbackRegex = new Regex(@"at index (\d+)");
                Match fallbackMatch = decoderFallbackRegex.Match(e.Message);
                if (fallbackMatch.Success && int.TryParse(fallbackMatch.Groups[1].Value, out int index))
                {
                    // If the index is known, we can calculate the end of the DecodeBuffer.
                    int charsDecoded = encoding.GetCharCount(Buffer, tail, index);

                    // If the requested number of characters was decoded, return successfully.
                    if (charsDecoded >= charCount)
                    {
                        return;
                    }
                }

                // Otherwise, rethrow DecoderFallbackException.
                throw e;
            }
            catch (ArgumentException e)
            {
                // We ran out of room in the DecodeBuffer.
                if (e.Message.StartsWith("The output char buffer is too small to contain the decoded characters"))
                {
                    // Making some assumptions about the decoder, we will assume the whole DecodeBuffer was filled.
                    return;
                }

                // Otherwise, this is an unfamiliar error. Rethrow ArgumentException.
                throw e;
            }
            throw new EndOfStreamException($"Could not decode {charCount}-character string within buffer.");
        }
        /// <summary>Resets the buffer to the current position in the stream.</summary>
        private void ResetBuffer()
        {
            ValidateStream();

            if (Stream != null)
            {
                tail = 0;
                head = Stream.Read(Buffer, 0, Buffer.Length);
            }
        }
        /// <summary>Ensures that the stream is not disposed.</summary>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        private void ValidateStream()
        {
            // The instance has been disposed.
            if (Buffer == null)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
        /// <summary>Ensures that the stream is not disposed and that the requested number of bytes can be read from the buffer.</summary>
        /// <param name="requestedCount">The number of bytes requested.</param>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException">More bytes were requested than can be read from the buffer.</exception>
        private void ValidateStream(int requestedCount)
        {
            ValidateStream();

            // The count is negative.
            if (requestedCount < 0)
            {
                throw new ArgumentOutOfRangeException($"Cannot request a negative number of bytes.");
            }

            // The buffer is too small to handle that much data.
            if (requestedCount > MinimumBufferSize)
            {
                throw new ArgumentOutOfRangeException($"Requested {requestedCount} bytes, which is larger than the buffer size ({MinimumBufferSize} bytes).");
            }

            // If the stream is null, throw if the buffer doesn't contain the requested number of bytes.
            // Otherwise, throw if the stream's length can be determined and the requested number of bytes will overrun the end.
            if (Stream == null ? requestedCount > AvailableInBuffer : requestedCount > Available)
            {
                throw new EndOfStreamException($"Requested {requestedCount} bytes, but only {Available} bytes are available.");
            }
        }
        /// <summary>Updates the buffer by reading more data from the stream if necessary.</summary>
        private void UpdateBuffer()
        {
            ValidateStream();

            if (Stream != null && AvailableInBuffer < MinimumBufferSize)
            {
                Array.Copy(Buffer, tail, Buffer, 0, AvailableInBuffer);
                head -= tail;
                tail = 0;
                head += Stream.Read(Buffer, head, AvailableInBuffer);
            }
        }
        #endregion
    }
}