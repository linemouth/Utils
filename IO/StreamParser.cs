using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Math = System.Math;

namespace Utils
{
    /// <summary>Represents a utility class for reading binary data from a stream.</summary>
    /// <remarks>This class provides methods for reading various data types from a binary stream. It buffers the data read from the stream to improve performance.</remarks>
    public class StreamParser
    {
        #region Properties & Fields
        /// <summary>The default encoding to use when reading text from the stream.</summary>
        public Encoding Encoding { get; set; }
        /// <summary>If true, Dispose() will leave the source stream open.</summary>
        public bool LeaveOpen { get; set; } = false;
        /// <summary>Returns the current position in the stream.</summary>
        /// <exception cref="IndexOutOfRangeException">Thrown if the specified position lies outside the buffer and cannot be reached.</exception>
        public long Position
        {
            get => position;
            set
            {
                // If the stream supports seeking, simply use that to reach the specified position.
                if (Stream?.CanSeek ?? false)
                {
                    Stream.Position = position = value;
                    ResetBuffer();
                }

                long delta = value - position;

                // Try to seek to an earlier position which still exists in the buffer.
                if (delta < 0)
                {
                    if (-delta < tail)
                    {
                        throw new NotSupportedException("Unable to seek to a position before the current buffer.");
                    }
                    tail += (int)delta;
                    position = value;
                }

                // Advance to a later position.
                if (delta > 0)
                {
                    Skip(delta);
                }
            }
        }
        /// <summary>Returns the total number of bytes remaining to be read. If the stream does not support reading the length, the number of buffered bytes available is returned instead.</summary>
        public long Available => (Stream != null && Stream.CanSeek && Stream.CanRead) ? AvailableInBuffer + Stream.Length : AvailableInBuffer;

        protected Stream Stream { get; private set; } = null;
        protected readonly int BufferUpdateThreshold = 0;
        protected byte[] Buffer { get; private set; }
        protected char[] DecodeBuffer { get; private set; }
        protected int AvailableInBuffer => head - tail;
        protected int head = 0;
        protected int tail = 0;

        private long position = 0;
        private static readonly Regex lineRegex = new Regex(@"^(.*?)\r?(?:\n|$)");
        #endregion

        #region Constructors
        /// <summary>Initializes a new instance of the <see cref="StreamParser"/> class.</summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="bufferSize">The size of the buffer used to read data from the stream.</param>
        public StreamParser(Stream stream, int bufferSize = 0x4000) : this(stream, Encoding.Default, bufferSize) { }
        /// <summary>Initializes a new instance of the <see cref="StreamParser"/> class.</summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="encoding">The encoding to use when reading text from the stream.</param>
        /// <param name="bufferSize">The size of the buffer used to read data from the stream.</param>
        public StreamParser(Stream stream, Encoding encoding, int bufferSize = 0x4000)
        {
            if(bufferSize < 1)
            {
                throw new ArgumentOutOfRangeException("Buffer size cannot be less than 1 byte.");
            }

            Stream = stream;
            Encoding = encoding;
            BufferUpdateThreshold = bufferSize;
            Buffer = new byte[bufferSize * 2];
            DecodeBuffer = new char[bufferSize];
        }
        /// <summary>Initializes a new instance of the <see cref="StreamParser"/> class.</summary>
        /// <param name="bytes">The byte array to read from.</param>
        public StreamParser(IEnumerable<byte> bytes) : this(bytes, Encoding.Default) { }
        /// <summary>Initializes a new instance of the <see cref="StreamParser"/> class.</summary>
        /// <param name="bytes">The byte array to read from.</param>
        /// <param name="encoding">The encoding to use when reading text from the stream.</param>
        public StreamParser(IEnumerable<byte> bytes, Encoding encoding)
        {
            Encoding = encoding;
            Buffer = bytes.ToArray();
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
        public short PeekShort(bool byteSwap)
        {
            ValidateStream(sizeof(short));
            return Buffer.GetShort(tail, byteSwap);
        }
        /// <summary>Reads a 16-bit unsigned integer from the buffer without advancing the position.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public ushort PeekUshort(bool byteSwap)
        {
            ValidateStream(sizeof(ushort));
            return Buffer.GetUshort(tail, byteSwap);
        }
        /// <summary>Reads a 32-bit signed integer from the buffer without advancing the position.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public int PeekInt(bool byteSwap)
        {
            ValidateStream(sizeof(int));
            return Buffer.GetInt(tail, byteSwap);
        }
        /// <summary>Reads a 32-bit unsigned integer from the buffer without advancing the position.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public uint PeekUint(bool byteSwap)
        {
            ValidateStream(sizeof(uint));
            return Buffer.GetUint(tail, byteSwap);
        }
        /// <summary>Reads a 64-bit signed integer from the buffer without advancing the position.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public long PeekLong(bool byteSwap)
        {
            ValidateStream(sizeof(long));
            return Buffer.GetLong(tail, byteSwap);
        }
        /// <summary>Reads a 64-bit unsigned integer from the buffer without advancing the position.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public ulong PeekUlong(bool byteSwap)
        {
            ValidateStream(sizeof(ulong));
            return Buffer.GetUlong(tail, byteSwap);
        }
        /// <summary>Reads a single-precision floating point number from the buffer without advancing the position.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public float PeekFloat(bool byteSwap)
        {
            ValidateStream(sizeof(float));
            return Buffer.GetFloat(tail, byteSwap);
        }
        /// <summary>Reads a double-precision floating point number from the buffer without advancing the position.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public double PeekDouble(bool byteSwap)
        {
            ValidateStream(sizeof(double));
            return Buffer.GetDouble(tail, byteSwap);
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
            ThrowIfDisposed();
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

            if(Buffer != null)
            {
                encoding = encoding ?? Encoding;
                int bufferTail = tail;
                int decodeHead = 0;
                expectedLength = Math.Min(DecodeBuffer.Length, expectedLength);
                int bytesToRead = Math.Min(encoding.GetMaxByteCount(expectedLength), AvailableInBuffer);
                int growIncrement = expectedLength >> 4; // Default grow increment is 25% of the original guess.
                Match match;

                while(decodeHead < DecodeBuffer.Length)
                {
                    try
                    {
                        // Decode the next set of characters.
                        int charsDecoded = encoding.GetChars(Buffer, bufferTail, bytesToRead, DecodeBuffer, decodeHead);

                        // Check for a fallback character.
                        if(encoding.DecoderFallback is DecoderReplacementFallback replacementFallback)
                        {
                            int fallbackCharIndex = Array.IndexOf(DecodeBuffer, replacementFallback.DefaultString[0], decodeHead, charsDecoded);
                            if(fallbackCharIndex >= 0)
                            {
                                // Fallback found; try to match up to the start of the fallback string.
                                if(regex.TryMatch(new string(DecodeBuffer, 0, decodeHead), out match))
                                {
                                    return match;
                                }

                                // Otherwise, we throw. Calculate the index of the invalid byte sequence from the tail.
                                int fallbackByteIndex = encoding.GetByteCount(DecodeBuffer, decodeHead, fallbackCharIndex) + bufferTail;
                                throw new DecoderFallbackException($"Invalid byte sequence found at index {fallbackByteIndex}");
                            }
                        }

                        // Advance indices.
                        bufferTail += bytesToRead;
                        decodeHead += charsDecoded;

                        // Attempt a match on the current DecodeBuffer contents.
                        if(regex.TryMatch(new string(DecodeBuffer, 0, decodeHead), out match))
                        {
                            return match;
                        }

                        // Prepare to decode more bytes, growing with each attempt.
                        bytesToRead = Math.Min(AvailableInBuffer - bytesToRead, growIncrement);
                        growIncrement = Math.Min(growIncrement <<= 2, 2000);
                    }
                    catch(DecoderFallbackException e)
                    {
                        // We encountered an invalid byte sequence. We can only hope to use the characters up to that point.
                        Regex decoderFallbackRegex = new Regex(@"at index (\d+)");
                        Match fallbackMatch = decoderFallbackRegex.Match(e.Message);
                        if(fallbackMatch.Success && int.TryParse(fallbackMatch.Groups[1].Value, out int index))
                        {
                            // If the index is known, we can calculate the end of the DecodeBuffer.
                            decodeHead += encoding.GetCharCount(Buffer, bufferTail, index);
                            if(regex.TryMatch(new string(DecodeBuffer, 0, decodeHead), out match))
                            {
                                return match;
                            }
                        }

                        // Otherwise, we don't have more valid data to decode.
                        throw e;
                    }
                    catch(ArgumentException e)
                    {
                        // We ran out of room in the DecodeBuffer.
                        if(e.Message.StartsWith("The output char buffer is too small to contain the decoded characters"))
                        {
                            // We might as well check the whole buffer while we're here.
                            decodeHead = DecodeBuffer.Length;
                            if(regex.TryMatch(new string(DecodeBuffer, 0, decodeHead), out match))
                            {
                                return match;
                            }
                            break;
                        }

                        // Otherwise, we don't have enough space to decode more data.
                        break;
                    }
                }
            }
            throw new EndOfStreamException("Could not match regular expression within buffer.");
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
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
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
            ThrowIfDisposed();
            Validation.ThrowIfNegative(count, nameof(count));

            byte[] result = new byte[count];
            int actualCount = Read(result, 0, count);
            if(actualCount != count)
            {
                throw new EndOfStreamException();
            }
            return result;
        }
        /// <summary>Reads an unsigned byte from the buffer, advancing the position by 1 byte.</summary>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public byte ReadByte()
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
        public short ReadShort(bool byteSwap)
        {
            short result = PeekShort(byteSwap);
            Skip(sizeof(short));
            return result;
        }
        /// <summary>Reads a 16-bit unsigned integer from the buffer, advancing the position by 2 bytes.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public ushort ReadUshort(bool byteSwap)
        {
            ushort result = PeekUshort(byteSwap);
            Skip(sizeof(ushort));
            return result;
        }
        /// <summary>Reads a 32-bit signed integer from the buffer, advancing the position by 4 bytes.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public int ReadInt(bool byteSwap)
        {
            int result = PeekInt(byteSwap);
            Skip(sizeof(int));
            return result;
        }
        /// <summary>Reads a 32-bit unsigned integer from the buffer, advancing the position by 4 bytes.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public uint ReadUint(bool byteSwap)
        {
            uint result = PeekUint(byteSwap);
            Skip(sizeof(uint));
            return result;
        }
        /// <summary>Reads a 64-bit signed integer from the buffer, advancing the position by 8 bytes.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public long ReadLong(bool byteSwap)
        {
            long result = PeekLong(byteSwap);
            Skip(sizeof(long));
            return result;
        }
        /// <summary>Reads a 64-bit unsigned integer from the buffer, advancing the position by 8 bytes.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public ulong ReadUlong(bool byteSwap)
        {
            ulong result = PeekUlong(byteSwap);
            Skip(sizeof(ulong));
            return result;
        }
        /// <summary>Reads a single-precision floating point number from the buffer, advancing the position by 4 bytes.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public float ReadFloat(bool byteSwap)
        {
            float result = PeekShort(byteSwap);
            Skip(sizeof(float));
            return result;
        }
        /// <summary>Reads a double-precision floating point number from the buffer, advancing the position by 8 bytes.</summary>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>The value read from the buffer.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        public double ReadDouble(bool byteSwap)
        {
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
                Skip(count);
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
        public bool TryPeekShort(out short result, bool byteSwap)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(short))
            {
                result = Buffer.GetShort(tail, byteSwap);
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a 16-bit unsigned integer from the buffer without advancing the position.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryPeekUshort(out ushort result, bool byteSwap)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(ushort))
            {
                result = Buffer.GetUshort(tail, byteSwap);
                Skip(sizeof(ushort));
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a 32-bit signed integer from the buffer without advancing the position.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryPeekInt(out int result, bool byteSwap)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(int))
            {
                result = Buffer.GetInt(tail, byteSwap);
                Skip(sizeof(int));
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a 32-bit unsigned integer from the buffer without advancing the position.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryPeekUint(out uint result, bool byteSwap)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(uint))
            {
                result = Buffer.GetUint(tail, byteSwap);
                Skip(sizeof(uint));
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a 64-bit signed integer from the buffer without advancing the position.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryPeekLong(out long result, bool byteSwap)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(long))
            {
                result = Buffer.GetLong(tail, byteSwap);
                Skip(sizeof(long));
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a 64-bit unsigned integer from the buffer without advancing the position.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryPeekUlong(out ulong result, bool byteSwap)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(ulong))
            {
                result = Buffer.GetUlong(tail, byteSwap);
                Skip(sizeof(ulong));
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a single-precision floating-point number from the buffer without advancing the position.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryPeekFloat(out float result, bool byteSwap)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(float))
            {
                result = Buffer.GetShort(tail, byteSwap);
                Skip(sizeof(float));
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a double-precision floating-point number from the buffer without advancing the position.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryPeekDouble(out double result, bool byteSwap)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(double))
            {
                result = Buffer.GetShort(tail, byteSwap);
                Skip(sizeof(double));
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
            int count = TryDecode(charCount, encoding);
            if (count >= charCount)
            {
                result = new string(DecodeBuffer, 0, count);
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
                int growIncrement = expectedLength >> 4; // Default grow increment is 25% of the original guess.

                while (decodeHead < DecodeBuffer.Length)
                {
                    try
                    {
                        // Decode the next set of characters.
                        int charsDecoded = encoding.GetChars(Buffer, bufferTail, bytesToRead, DecodeBuffer, decodeHead);
                        bufferTail += bytesToRead;

                        // Check for a fallback character
                        if (encoding.DecoderFallback is DecoderReplacementFallback replacementFallback)
                        {
                            int fallbackIndex = Array.IndexOf(DecodeBuffer, replacementFallback.DefaultString[0], decodeHead, charsDecoded);
                            if (fallbackIndex >= 0)
                            {
                                // Fallback found; try to match up to the start of the fallback string
                                return regex.TryMatch(new string(DecodeBuffer, 0, fallbackIndex), out match);
                            }
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
                        growIncrement = Math.Min(growIncrement <<= 2, 2000);
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
            if(Buffer != null && count <= AvailableInBuffer)
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
        public bool TryReadShort(out short result, bool byteSwap)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(short))
            {
                result = Buffer.GetShort(tail, byteSwap);
                Skip(sizeof(short));
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a 16-bit unsigned integer from the buffer. If successful, the position is advanced by 2 bytes.</summary>
        /// <param name="result">Returns the resulting value on success. Otherwise, the value should be discarded.</param>
        /// <param name="byteSwap">Set to true to perform byte-swapping.</param>
        /// <returns>true if the value was successfully read; otherwise, false.</returns>
        public bool TryReadUshort(out ushort result, bool byteSwap)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(ushort))
            {
                result = Buffer.GetUshort(tail, byteSwap);
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
        public bool TryReadInt(out int result, bool byteSwap)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(int))
            {
                result = Buffer.GetInt(tail, byteSwap);
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
        public bool TryReadUint(out uint result, bool byteSwap)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(uint))
            {
                result = Buffer.GetUint(tail, byteSwap);
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
        public bool TryReadLong(out long result, bool byteSwap)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(long))
            {
                result = Buffer.GetLong(tail, byteSwap);
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
        public bool TryReadUlong(out ulong result, bool byteSwap)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(ulong))
            {
                result = Buffer.GetUlong(tail, byteSwap);
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
        public bool TryReadFloat(out float result, bool byteSwap)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(float))
            {
                result = Buffer.GetShort(tail, byteSwap);
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
        public bool TryReadDouble(out double result, bool byteSwap)
        {
            if (Buffer != null && AvailableInBuffer >= sizeof(double))
            {
                result = Buffer.GetShort(tail, byteSwap);
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
            if(TryPeekString(charCount, out result, encoding))
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
            if(TryReadRegex(regex, expectedLength, out Match match, encoding))
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

        #region Other Methods
        /// <summary>Releases all resources used by the <see cref="StreamParser"/> object.</summary>
        public void Dispose()
        {
            Buffer = null;
            DecodeBuffer = null;
            if (!LeaveOpen)
            {
                Stream?.Dispose();
            }
            Stream = null;
        }
        /// <summary>Skips a specified number of bytes in the stream without advancing the position.</summary>
        /// <param name="byteCount">The number of bytes to skip.</param>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public void Skip(long byteCount)
        {
            ThrowIfDisposed();

            if (byteCount >= AvailableInBuffer)
            {
                // Consume all bytes in the buffer.
                byteCount -= AvailableInBuffer;
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
                        }
                    }
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
            }

            UpdateBuffer();
        }

        /// <summary>Attempts to decode characters from the stream using the specified encoding, up to the specified maximum number of characters.</summary>
        /// <param name="maxCharCount">The maximum number of characters to decode.</param>
        /// <param name="encoding">The encoding to use to interpret the buffer. If not provided, the stream's encoding will be used.</param>
        /// <returns>The number of characters actually decoded.</returns>
        private int TryDecode(int maxCharCount, Encoding encoding = null)
        {
            ThrowIfDisposed();

            encoding = encoding ?? Encoding;
            maxCharCount = Math.Min(DecodeBuffer.Length, maxCharCount);
            int bytesToRead = Math.Min(encoding.GetMaxByteCount(maxCharCount), AvailableInBuffer);
            int bufferTail = tail;
            int decodeHead = 0;

            // Perform incremental decoding to get the maximum number of chars.
            while (decodeHead < DecodeBuffer.Length && bytesToRead > 0)
            {
                try
                {
                    decodeHead += encoding.GetChars(Buffer, bufferTail, bytesToRead, DecodeBuffer, decodeHead);
                    bufferTail += bytesToRead;
                    bytesToRead = (bytesToRead + 1) >> 1;
                }
                catch (DecoderFallbackException) { }
            }

            return decodeHead;
        }
        /// <summary>Decodes characters from the stream using the specified encoding until at least the specified minimum number of characters are decoded.</summary>
        /// <param name="minCharCount">The minimum number of characters to decode.</param>
        /// <param name="encoding">The encoding to use to interpret the buffer. If not provided, the stream's encoding will be used.</param>
        private void Decode(int charCount, Encoding encoding)
        {
            ValidateStream();

            // We cannot decode more than the decode buffer's size.
            if (charCount > DecodeBuffer.Length)
            {
                throw new ArgumentOutOfRangeException($"Decode buffer is too small ({DecodeBuffer.Length} bytes) to decode the requested string length ({charCount} bytes).");
            }

            encoding = encoding ?? Encoding;
            int bytesToRead = Math.Min(encoding.GetMaxByteCount(charCount), AvailableInBuffer);
            int bufferTail = tail;
            int decodeHead = 0;

            // Perform incremental decoding to get at least the minimum char count.
            while (decodeHead < charCount && bytesToRead > 0)
            {
                try
                {
                    decodeHead += encoding.GetChars(Buffer, bufferTail, bytesToRead, DecodeBuffer, decodeHead);
                    bufferTail += bytesToRead;
                }
                catch (DecoderFallbackException) { }
                bytesToRead = (int)Math.Ceiling(bytesToRead * 0.5f);

                // If we failed to decode enough characters, and we don't have enough margin to reach our target, rethrow DecoderFallbackException.
                if (decodeHead + bytesToRead < charCount)
                {
                    throw new DecoderFallbackException();
                }

                // If we don't have enough bytes available to decode the remaining chars, throw EndOfStreamException.
                if (bytesToRead > AvailableInBuffer)
                {
                    throw new EndOfStreamException();
                }
            }
        }
        /// <summary>Resets the buffer to the current position in the stream.</summary>
        private void ResetBuffer()
        {
            ThrowIfDisposed();

            if (Stream != null)
            {
                tail = 0;
                head = Stream.Read(Buffer, 0, Buffer.Length);
            }
        }
        /// <summary>Ensures that the stream is not disposed.</summary>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        private void ThrowIfDisposed()
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
        /// <exception cref="EndOfStreamException">The end of the stream will be encountered within the given number of bytes.</exception>
        private void ValidateStream(int requestedCount = 1)
        {
            ThrowIfDisposed();

            // The count is negative.
            if (requestedCount < 0)
            {
                throw new ArgumentOutOfRangeException($"Cannot request a negative number of bytes.");
            }

            // The buffer is too small to handle that much data.
            if (requestedCount > BufferUpdateThreshold)
            {
                throw new ArgumentOutOfRangeException($"Requested ${requestedCount} bytes, which is larger than the buffer size (${BufferUpdateThreshold} bytes).");
            }

            // The buffer doesn't contain the requested number of bytes.
            if (requestedCount > AvailableInBuffer)
            {
                throw new EndOfStreamException($"Requested ${requestedCount} bytes, but only ${AvailableInBuffer} bytes are available.");
            }
        }
        /// <summary>Updates the buffer by reading more data from the stream if necessary.</summary>
        private void UpdateBuffer()
        {
            ThrowIfDisposed();

            if (Stream == null || AvailableInBuffer >= BufferUpdateThreshold)
            {
                return;
            }

            if (Stream != null && AvailableInBuffer < BufferUpdateThreshold)
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