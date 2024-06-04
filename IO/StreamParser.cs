using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Utils
{
    /// <summary>Represents a utility class for reading binary data from a stream.</summary>
    /// <remarks>This class provides methods for reading various data types from a binary stream. It buffers the data read from the stream to improve performance.</remarks>
    public class StreamParser
    {
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
                    if(-delta < tail)
                    {
                        throw new NotSupportedException("Unable to seek to a position before the current buffer.");
                    }
                    tail += (int)delta;
                    position = value;
                }

                // Advance to a later position.
                if(delta > 0)
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

        /// <summary>Initializes a new instance of the<see cref="StreamParser"/> class with the specified stream and default encoding.</summary>
        /// <param name = "stream" > The stream to read from.</param>
        /// <param name = "bufferSize" > The size of the buffer used to read data from the stream.The default value is 0x4000 bytes.</param>
        public StreamParser(Stream stream, int bufferSize = 0x4000) : this(stream, Encoding.Default, bufferSize) { }
        /// <summary>Initializes a new instance of the<see cref="StreamParser"/> class with the specified stream and encoding.</summary>
        /// <param name = "stream" > The stream to read from.</param>
        /// <param name = "encoding" > The encoding to use when reading text from the stream.</param>
        /// <param name = "bufferSize" > The size of the buffer used to read data from the stream.The default value is 0x4000 bytes.</param>
        public StreamParser(Stream stream, Encoding encoding, int bufferSize = 0x4000)
        {
            Stream = stream;
            Encoding = encoding;
            BufferUpdateThreshold = bufferSize;
            Buffer = new byte[bufferSize * 2];
            DecodeBuffer = new char[bufferSize];
        }
        /// <summary>Initializes a new instance of the<see cref="StreamParser"/> class with the specified byte array and default encoding.</summary>
        /// <param name = "bytes" > The byte array to read from.</param>
        public StreamParser(IEnumerable<byte> bytes) : this(bytes, Encoding.Default) { }
        /// <summary>Initializes a new instance of the<see cref="StreamParser"/> class with the specified byte array and encoding.</summary>
        /// <param name = "bytes" > The byte array to read from.</param>
        /// <param name = "encoding" > The encoding to use when reading text from the stream.</param>
        public StreamParser(IEnumerable<byte> bytes, Encoding encoding)
        {
            Encoding = encoding;
            Buffer = bytes.ToArray();
            DecodeBuffer = new char[Buffer.Length];
        }
        /// <summary>Releases all resources used by the<see cref="StreamParser"/> object.</summary>
        public void Dispose()
        {
            if(Buffer != null)
            {
                Buffer = null;
                DecodeBuffer = null;
                if(!LeaveOpen)
                {
                    Stream?.Dispose();
                }
                Stream = null;
            }
        }
        /// <summary>Returns the next several bytes in the stream without advancing the position.</summary>
        /// <param name = "count" > The number of bytes to retrieve.</param>
        /// <returns>An array containing the requested bytes.</returns>
        /// <exception cref = "ArgumentOutOfRangeException" >Thrown if the count is negative or larger than the buffered data.</exception>
        /// <exception cref = "EndOfStreamException" >Thrown if the stream does not contain the requested number of bytes.</exception>
        /// <exception cref = "ObjectDisposedException" >Thrown if methods were called after the stream was closed.</exception>
        public byte[] PeekBytes(int count)
        {
            Validation.ThrowIfNegative(count, nameof(count));
            ValidateStream(count);

            // Copy the data from the buffer.
            byte[] result = new byte[count];
            Array.Copy(Buffer, tail, result, 0, count);
            return result;
        }
        /// <summary>Returns the next byte available in the buffer without advancing the position.</summary>
        /// <returns>The next byte in the buffer.</returns>
        /// <exception cref = "EndOfStreamException" >Thrown if the stream does not contain the requested number of bytes.</exception>
        /// <exception cref = "ObjectDisposedException" >Thrown if methods were called after the stream was closed.</exception>
        public byte PeekByte()
        {
            ValidateStream(sizeof(byte));
            return Buffer[tail];
        }
        /// <summary>Returns the next signed byte available in the buffer without advancing the position.</summary>
        /// <returns>The next signed byte in the buffer.</returns>
        /// <exception cref = "EndOfStreamException" >Thrown if the stream does not contain the requested number of bytes.</exception>
        /// <exception cref = "ObjectDisposedException" >Thrown if methods were called after the stream was closed.</exception>
        public sbyte PeekSbyte()
        {
            ValidateStream(sizeof(sbyte));
            return (sbyte)Buffer[tail];
        }
        /// <summary>Returns the next short available in the buffer without advancing the position.</summary>
        /// <param name = "byteSwap" > Indicates whether to byte-swap the result.</param>
        /// <returns>The next short in the buffer.</returns>
        /// <exception cref = "EndOfStreamException" >Thrown if the stream does not contain the requested number of bytes.</exception>
        /// <exception cref = "ObjectDisposedException" >Thrown if methods were called after the stream was closed.</exception>
        public short PeekShort(bool byteSwap)
        {
            ValidateStream(sizeof(short));
            return Buffer.GetShort(tail, byteSwap);
        }
        /// <summary>Returns the next unsigned short available in the buffer without advancing the position.</summary>
        /// <param name = "byteSwap" > Indicates whether to byte-swap the result.</param>
        /// <returns>The next unsigned short in the buffer.</returns>
        /// <exception cref = "EndOfStreamException" >Thrown if the stream does not contain the requested number of bytes.</exception>
        /// <exception cref = "ObjectDisposedException" >Thrown if methods were called after the stream was closed.</exception>
        public ushort PeekUshort(bool byteSwap)
        {
            ValidateStream(sizeof(ushort));
            return Buffer.GetUshort(tail, byteSwap);
        }
        /// <summary>Returns the next integer available in the buffer without advancing the position.</summary>
        /// <param name = "byteSwap" > Indicates whether to byte-swap the result.</param>
        /// <returns>The next integer in the buffer.</returns>
        /// <exception cref = "EndOfStreamException" >Thrown if the stream does not contain the requested number of bytes.</exception>
        /// <exception cref = "ObjectDisposedException" >Thrown if methods were called after the stream was closed.</exception>
        public int PeekInt(bool byteSwap)
        {
            ValidateStream(sizeof(int));
            return Buffer.GetInt(tail, byteSwap);
        }
        /// <summary>Returns the next unsigned integer available in the buffer without advancing the position.</summary>
        /// <param name = "byteSwap" > Indicates whether to byte-swap the result.</param>
        /// <returns>The next unsigned integer in the buffer.</returns>
        /// <exception cref = "EndOfStreamException" >Thrown if the stream does not contain the requested number of bytes.</exception>
        /// <exception cref = "ObjectDisposedException" >Thrown if methods were called after the stream was closed.</exception>
        public uint PeekUint(bool byteSwap)
        {
            ValidateStream(sizeof(uint));
            return Buffer.GetUint(tail, byteSwap);
        }
        /// <summary>Returns the next long available in the buffer without advancing the position.</summary>
        /// <param name = "byteSwap" > Indicates whether to byte-swap the result.</param>
        /// <returns>The next long in the buffer.</returns>
        /// <exception cref = "EndOfStreamException" >Thrown if the stream does not contain the requested number of bytes.</exception>
        /// <exception cref = "ObjectDisposedException" >Thrown if methods were called after the stream was closed.</exception>
        public long PeekLong(bool byteSwap)
        {
            ValidateStream(sizeof(long));
            return Buffer.GetLong(tail, byteSwap);
        }
        /// <summary>Returns the next unsigned long available in the buffer without advancing the position.</summary>
        /// <param name = "byteSwap" > Indicates whether to byte-swap the result.</param>
        /// <returns>The next unsigned long in the buffer.</returns>
        /// <exception cref = "EndOfStreamException" >Thrown if the stream does not contain the requested number of bytes.</exception>
        /// <exception cref = "ObjectDisposedException" >Thrown if methods were called after the stream was closed.</exception>
        public ulong PeekUlong(bool byteSwap)
        {
            ValidateStream(sizeof(ulong));
            return Buffer.GetUlong(tail, byteSwap);
        }
        /// <summary>Returns the next float available in the buffer without advancing the position.</summary>
        /// <param name = "byteSwap" > Indicates whether to byte-swap the result.</param>
        /// <returns>The next next single-precision floating-point number in the buffer.</returns>
        /// <exception cref = "EndOfStreamException" >Thrown if the stream does not contain the requested number of bytes.</exception>
        /// <exception cref = "ObjectDisposedException" >Thrown if methods were called after the stream was closed.</exception>
        public float PeekFloat(bool byteSwap)
        {
            ValidateStream(sizeof(float));
            return Buffer.GetFloat(tail, byteSwap);
        }
        /// <summary>Returns the next double available in the buffer without advancing the position.</summary>
        /// <param name = "byteSwap" > Indicates whether to byte-swap the result.</param>
        /// <returns>The next next double-precision floating-point number in the buffer.</returns>
        /// <exception cref = "EndOfStreamException" >Thrown if the stream does not contain the requested number of bytes.</exception>
        /// <exception cref = "ObjectDisposedException" >Thrown if methods were called after the stream was closed.</exception>
        public double PeekDouble(bool byteSwap)
        {
            ValidateStream(sizeof(double));
            return Buffer.GetDouble(tail, byteSwap);
        }
        /// <summary>Returns the next string available in the buffer without advancing the position.</summary>
        /// <returns>The string at the start of the buffer.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Count is negative or larger than the buffered data.</exception>
        /// <exception cref="EndOfStreamException">The stream does not contain the requested number of bytes.</exception>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <exception cref="DecoderFallbackException">The requested number of characters could not be decoded from the binary stream.</exception>
        public string PeekString(int charCount, Encoding encoding = null)
        {
            ThrowIfDisposed();
            Validation.ThrowIfNegative(charCount, nameof(charCount));

            DecodeAtLeast(charCount, encoding);
            return new string(DecodeBuffer, 0, charCount);
        }
        /// <summary>Reads up to the given number of bytes from the stream and copies them to the provided buffer.</summary>
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
        /// <summary>Reads the specified number of bytes from the stream and returns them as an array.</summary>
        /// <param name="count">The number of bytes to read from the stream.</param>
        /// <returns>An array containing the bytes read from the stream.</returns>
        /// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The specified count is negative.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached before the specified number of bytes could be read.</exception>
        public byte[] ReadBytes(int count)
        {
            ValidateStream(count);
            Validation.ThrowIfNegative(count, nameof(count));

            byte[] result = new byte[count];
            Read(result, 0, count);
            return result;
        }
        /// <summary>Reads a byte from the stream and advances the position by one byte.</summary>
        /// <returns>The byte read from the stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        public byte ReadByte()
        {
            byte result = PeekByte();
            Skip(1);
            return result;
        }
        /// <summary>Reads a signed byte from the stream and advances the position by one byte.</summary>
        /// <returns>The signed byte read from the stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        public sbyte ReadSbyte()
        {
            sbyte result = PeekSbyte();
            Skip(1);
            return result;
        }
        /// <summary>Reads a 16-bit signed integer from the stream and advances the position by two bytes.</summary>
        /// <param name="byteSwap">Indicates whether the bytes should be swapped based on the endianness of the system.</param>
        /// <returns>The 16-bit signed integer read from the stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        public short ReadShort(bool byteSwap)
        {
            short result = PeekShort(byteSwap);
            Skip(sizeof(short));
            return result;
        }
        /// <summary>Reads a 16-bit unsigned integer from the stream and advances the position by two bytes.</summary>
        /// <param name="byteSwap">Indicates whether the bytes should be swapped based on the endianness of the system.</param>
        /// <returns>The 16-bit unsigned integer read from the stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        public ushort ReadUshort(bool byteSwap)
        {
            ushort result = PeekUshort(byteSwap);
            Skip(sizeof(ushort));
            return result;
        }
        /// <summary>Reads a 32-bit signed integer from the stream and advances the position by four bytes.</summary>
        /// <param name="byteSwap">Indicates whether the bytes should be swapped based on the endianness of the system.</param>
        /// <returns>The 32-bit signed integer read from the stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        public int ReadInt(bool byteSwap)
        {
            int result = PeekInt(byteSwap);
            Skip(sizeof(int));
            return result;
        }
        /// <summary>Reads a 32-bit unsigned integer from the stream and advances the position by four bytes.</summary>
        /// <param name="byteSwap">Indicates whether the bytes should be swapped based on the endianness of the system.</param>
        /// <returns>The 32-bit unsigned integer read from the stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        public uint ReadUint(bool byteSwap)
        {
            uint result = PeekUint(byteSwap);
            Skip(sizeof(uint));
            return result;
        }
        /// <summary>Reads a 64-bit signed integer from the stream and advances the position by eight bytes.</summary>
        /// <param name="byteSwap">Indicates whether the bytes should be swapped based on the endianness of the system.</param>
        /// <returns>The 64-bit signed integer read from the stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        public long ReadLong(bool byteSwap)
        {
            long result = PeekLong(byteSwap);
            Skip(sizeof(long));
            return result;
        }
        /// <summary>Reads a 64-bit unsigned integer from the stream and advances the position by eight bytes.</summary>
        /// <param name="byteSwap">Indicates whether the bytes should be swapped based on the endianness of the system.</param>
        /// <returns>The 64-bit unsigned integer read from the stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        public ulong ReadUlong(bool byteSwap)
        {
            ulong result = PeekUlong(byteSwap);
            Skip(sizeof(ulong));
            return result;
        }
        /// <summary>Reads a single-precision floating point number from the stream and advances the position by four bytes.</summary>
        /// <param name="byteSwap">Indicates whether the bytes should be swapped based on the endianness of the system.</param>
        /// <returns>The single-precision floating point number read from the stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        public float ReadFloat(bool byteSwap)
        {
            float result = PeekShort(byteSwap);
            Skip(sizeof(float));
            return result;
        }
        /// <summary>Reads a double-precision floating point number from the stream and advances the position by eight bytes.</summary>
        /// <param name="byteSwap">Indicates whether the bytes should be swapped based on the endianness of the system.</param>
        /// <returns>The double-precision floating point number read from the stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached.</exception>
        public double ReadDouble(bool byteSwap)
        {
            double result = PeekDouble(byteSwap);
            Skip(sizeof(double));
            return result;
        }
        /// <summary>Decodes a string from the stream, based on the specified character count and encoding, and advances the position by the corresponding number of bytes.</summary>
        /// <param name="charCount">The number of characters to read from the stream.</param>
        /// <param name="encoding">The encoding to use for decoding. If null, the default encoding of the stream is used.</param>
        /// <returns>The decoded string read from the stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream has been reached before the specified number of characters could be read.</exception>
        public string ReadString(int charCount, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding;
            string result = PeekString(charCount, encoding);
            Skip(encoding.GetByteCount(result));
            return result;
        }
        /// <summary>Attempts to read a line of characters from the current stream and advances the position within the stream according to the Encoding used and the specific character being read. </summary>
        /// <param name="result">When this method returns, contains the next line from the input stream, or an empty string if the end of the input stream is reached.</param>
        /// <param name="encoding">The encoding to use. If null, the Encoding property will be used.</param>
        /// <returns>true if a line was successfully read; otherwise, false.</returns>
        public bool TryReadLine(out string result, Encoding encoding = null) => TryReadRegex(lineRegex, out result, encoding);
        /// <summary>Attempts to read the specified number of bytes from the current stream and advances the position within the stream by the number of bytes read.</summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <param name="result">When this method returns, contains the specified number of bytes read from the input stream, or null if the end of the input stream is reached.</param>
        /// <param name="byteSwap">true to perform byte swapping; otherwise, false.</param>
        /// <returns>true if the specified number of bytes was successfully read; otherwise, false.</returns>
        public bool TryReadBytes(int count, out byte[] result, bool byteSwap)
        {
            if (Buffer != null && AvailableInBuffer >= count)
            {
                result = Buffer.GetBytes(tail, count, byteSwap);
                Skip(count);
                return true;
            }
            result = default;
            return false;
        }
        /// <summary>Attempts to read a byte from the current stream and advances the position within the stream by one byte, or returns false if the end of the stream is reached.</summary>
        /// <param name="result">When this method returns, contains the byte read from the input stream, or default if the end of the input stream is reached.</param>
        /// <returns>true if a byte was successfully read; otherwise, false.</returns>
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
        /// <summary>Attempts to read a signed byte from the current stream and advances the position within the stream by one byte, or returns false if the end of the stream is reached.</summary>
        /// <param name="result">When this method returns, contains the signed byte read from the input stream, or default if the end of the input stream is reached.</param>
        /// <returns>true if a signed byte was successfully read; otherwise, false.</returns>
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
        /// <summary>Attempts to read a 16-bit signed integer from the current stream and advances the position within the stream by two bytes, or returns false if the end of the stream is reached.</summary>
        /// <param name="result">When this method returns, contains the 16-bit signed integer read from the input stream, or default if the end of the input stream is reached.</param>
        /// <param name="byteSwap">true to perform byte swapping; otherwise, false.</param>
        /// <returns>true if a 16-bit signed integer was successfully read; otherwise, false.</returns>
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
        /// <summary>Attempts to read a 16-bit unsigned integer from the current stream and advances the position within the stream by two bytes, or returns false if the end of the stream is reached.</summary>
        /// <param name="result">When this method returns, contains the 16-bit unsigned integer read from the input stream, or default if the end of the input stream is reached.</param>
        /// <param name="byteSwap">true to perform byte swapping; otherwise, false.</param>
        /// <returns>true if a 16-bit unsigned integer was successfully read; otherwise, false.</returns>
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
        /// <summary>Attempts to read a 32-bit signed integer from the current stream and advances the position within the stream by four bytes, or returns false if the end of the stream is reached.</summary>
        /// <param name="result">When this method returns, contains the 32-bit signed integer read from the input stream, or default if the end of the input stream is reached.</param>
        /// <param name="byteSwap">true to perform byte swapping; otherwise, false.</param>
        /// <returns>true if a 32-bit signed integer was successfully read; otherwise, false.</returns>
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
        /// <summary>Attempts to read a 32-bit unsigned integer from the current stream and advances the position within the stream by four bytes, or returns false if the end of the stream is reached.</summary>
        /// <param name="result">When this method returns, contains the 32-bit unsigned integer read from the input stream, or default if the end of the input stream is reached.</param>
        /// <param name="byteSwap">true to perform byte swapping; otherwise, false.</param>
        /// <returns>true if a 32-bit unsigned integer was successfully read; otherwise, false.</returns>
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
        /// <summary>Attempts to read a 64-bit signed integer from the current stream and advances the position within the stream by eight bytes, or returns false if the end of the stream is reached.</summary>
        /// <param name="result">When this method returns, contains the 64-bit signed integer read from the input stream, or default if the end of the input stream is reached.</param>
        /// <param name="byteSwap">true to perform byte swapping; otherwise, false.</param>
        /// <returns>true if a 64-bit signed integer was successfully read; otherwise, false.</returns>
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
        /// <summary>Attempts to read a 64-bit unsigned integer from the current stream and advances the position within the stream by eight bytes, or returns false if the end of the stream is reached.</summary>
        /// <param name="result">When this method returns, contains the 64-bit unsigned integer read from the input stream, or default if the end of the input stream is reached.</param>
        /// <param name="byteSwap">true to perform byte swapping; otherwise, false.</param>
        /// <returns>true if a 64-bit unsigned integer was successfully read; otherwise, false.</returns>
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
        /// <summary>Attempts to read a single-precision floating-point number from the current stream and advances the position within the stream by four bytes, or returns false if the end of the stream is reached.</summary>
        /// <param name="result">When this method returns, contains the single-precision floating-point number read from the input stream, or default if the end of the input stream is reached.</param>
        /// <param name="byteSwap">true to perform byte swapping; otherwise, false.</param>
        /// <returns>true if a single-precision floating-point number was successfully read; otherwise, false.</returns>
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
        /// <summary>Attempts to read a double-precision floating-point number from the stream, optionally performing byte swapping.</summary>
        /// <param name="result">When this method returns, contains the double-precision floating-point number read from the stream, if the operation succeeded, or zero if the operation failed. The value is unspecified if the operation failed.</param>
        /// <param name="byteSwap">True to perform byte swapping; otherwise, false.</param>
        /// <returns>True if a double-precision floating-point number was successfully read from the stream; otherwise, false.</returns>
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
        /// <summary>Decodes the binary stream as a string, up to the given number of characters.</summary>
        /// <param name="maxCharCount">The number of characters to request. The actual number read may be less.</param>
        /// <param name="result">The decoded string.</param>
        /// <param name="encoding">The encoding to use by the decoding operation.</param>
        /// <returns>The number of characters actually decoded. This may be less than the number requested.</returns>
        public int TryReadString(int maxCharCount, out string result, Encoding encoding = null)
        {
            int count = TryDecode(maxCharCount, encoding);
            result = new string(DecodeBuffer, 0, count);
            return count;
        }
        /// <summary>Attempts to read a string from the stream that matches the specified regular expression, using the specified encoding.</summary>
        /// <param name="regex">The regular expression to match.</param>
        /// <param name="result">When this method returns, contains the string that matches the regular expression, if the operation succeeded, or null if the operation failed. The value is unspecified if the operation failed.</param>
        /// <param name="encoding">The encoding to use for decoding the string. If null, the default encoding is used.</param>
        /// <returns>True if a string matching the regular expression was successfully read from the stream; otherwise, false.</returns>
        public bool TryReadRegex(Regex regex, out string result, Encoding encoding = null)
        {
            if (TryReadRegex(regex, out Match match, encoding))
            {
                result = match.Groups[1].Value;
                return true;
            }
            result = null;
            return false;
        }
        /// <summary>Attempts to match a regular expression pattern in the decoded string from the stream, using the specified encoding.</summary>
        /// <param name="regex">The regular expression pattern to match.</param>
        /// <param name="match">When this method returns, contains the first substring that matches the regular expression pattern, if the operation succeeded, or null if the operation failed. The value is unspecified if the operation failed.</param>
        /// <param name="encoding">The encoding to use for decoding the string. If null, the default encoding is used.</param>
        /// <returns>True if a match for the regular expression pattern was found in the decoded string; otherwise, false.</returns>
        public bool TryReadRegex(Regex regex, out Match match, Encoding encoding = null)
        {
            if (Buffer != null)
            {
                encoding = encoding ?? Encoding;
                int decodeHead = TryDecode(encoding);
                string decodeBuffer = new string(DecodeBuffer, 0, decodeHead);
                match = regex.Match(decodeBuffer);
                if(match.Success)
                {
                    Skip(encoding.GetByteCount(match.Groups[0].Value));
                    return true;
                }
            }
            match = null;
            return false;
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

        /// <summary>Attempts to decode characters from the stream using the specified encoding.</summary>
        /// <param name="encoding">The encoding to use for decoding characters. If null, the default encoding is used.</param>
        /// <returns>The number of characters actually decoded.</returns>
        private int TryDecode(Encoding encoding = null) => TryDecode(int.MaxValue, encoding);
        /// <summary>Attempts to decode characters from the stream using the specified encoding, up to the specified maximum number of characters.</summary>
        /// <param name="maxCharCount">The maximum number of characters to decode.</param>
        /// <param name="encoding">The encoding to use for decoding characters. If null, the default encoding is used.</param>
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
        /// <param name="encoding">The encoding to use for decoding characters. If null, the default encoding is used.</param>
        private void DecodeAtLeast(int minCharCount, Encoding encoding)
        {
            ValidateStream(minCharCount);

            // We cannot decode more than the decode buffer's size.
            if (minCharCount > DecodeBuffer.Length)
            {
                throw new ArgumentOutOfRangeException($"Decode buffer is too small ({DecodeBuffer.Length} bytes) to decode the requested string length ({minCharCount} bytes).");
            }

            encoding = encoding ?? Encoding;
            int bytesToRead = Math.Min(encoding.GetMaxByteCount(minCharCount), AvailableInBuffer);
            int bufferTail = tail;
            int decodeHead = 0;

            // Perform incremental decoding to get at least the minimum char count.
            while (decodeHead < minCharCount && bytesToRead > 0)
            {
                try
                {
                    decodeHead += encoding.GetChars(Buffer, bufferTail, bytesToRead, DecodeBuffer, decodeHead);
                    bufferTail += bytesToRead;
                }
                catch (DecoderFallbackException) { }
                bytesToRead = (int)Math.Ceiling(bytesToRead * 0.5f);
                
                // If we failed to decode enough characters, and we don't have enough margin to reach our target, rethrow DecoderFallbackException.
                if (decodeHead + bytesToRead < minCharCount)
                {
                    throw new DecoderFallbackException();
                }

                // If we don't have enough bytes available to decode the remaining chars, throw EndOfStreamException.
                if(bytesToRead > AvailableInBuffer)
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
    }
}