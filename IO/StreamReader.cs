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
    public class BinaryStreamReader
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
        public long Available => Stream?.CanSeek ?? false ? AvailableInBuffer + Stream.Length : AvailableInBuffer;

        protected Stream Stream { get; private set; } = null;
        protected readonly int BufferUpdateThreshold = 0;
        protected byte[] Buffer { get; private set; }
        protected char[] DecodeBuffer { get; private set; }
        protected int AvailableInBuffer => head - tail;
        protected int head = 0;
        protected int tail = 0;

        private long position = 0;
        private static readonly Regex lineRegex = new Regex(@"^(.*?)\r?(?:\n|$)");

        public BinaryStreamReader(Stream stream, int bufferSize = 0x4000) : this(stream, Encoding.Default, bufferSize) { }
        public BinaryStreamReader(Stream stream, Encoding encoding, int bufferSize = 0x4000)
        {
            Stream = stream;
            Encoding = encoding;
            BufferUpdateThreshold = bufferSize;
            Buffer = new byte[bufferSize * 2];
            DecodeBuffer = new char[bufferSize];
        }
        public BinaryStreamReader(IEnumerable<byte> bytes) : this(bytes, Encoding.Default) { }
        public BinaryStreamReader(IEnumerable<byte> bytes, Encoding encoding)
        {
            Encoding = encoding;
            Buffer = bytes.ToArray();
            DecodeBuffer = new char[Buffer.Length];
        }
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
        /// <param name="count">The number of bytes to retrieve.</param>
        /// <returns>An array containing the requested bytes.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Count is negative or larger than the buffered data.</exception>
        /// <exception cref="EndOfStreamException">The stream does not contain the requested number of bytes.</exception>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public byte[] PeekBytes(int count)
        {
            ThrowIfDisposed();
            Validation.ThrowIfNegative(count, nameof(count));

            if(count > AvailableInBuffer)
            {
                // If the buffer doesn't contain the requested count, it may be because we're at the end of the stream.
                if (AvailableInBuffer < BufferUpdateThreshold)
                {
                    throw new EndOfStreamException();
                }
                // Or it could be that the buffer is too small to handle that much data.
                throw new ArgumentOutOfRangeException("Requested more data than is buffered.");
            }

            // Otherwise, copy the data from the buffer.
            byte[] result = new byte[count];
            Array.Copy(Buffer, tail, result, 0, count);
            return result;
        }
        /// <summary>Returns the next byte available in the buffer without advancing the position.</summary>
        /// <returns>The next byte in the buffer.</returns>
        /// <exception cref="EndOfStreamException">The stream does not contain the requested number of bytes.</exception>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public byte PeekByte()
        {
            ThrowIfDisposed();

            if (AvailableInBuffer >= 1)
            {
                return Buffer[tail];
            }

            throw new EndOfStreamException();
        }
        public sbyte PeekSbyte()
        {
            ThrowIfDisposed();

            if (AvailableInBuffer >= 1)
            {
                return (sbyte)Buffer[tail];
            }

            throw new EndOfStreamException();
        }
        public short PeekShort(bool byteSwap)
        {
            ThrowIfDisposed();

            if (AvailableInBuffer >= sizeof(short))
            {
                return Buffer.GetShort(tail, byteSwap);
            }

            throw new EndOfStreamException();
        }
        public ushort PeekUshort(bool byteSwap)
        {
            ThrowIfDisposed();

            if (AvailableInBuffer >= sizeof(ushort))
            {
                return Buffer.GetUshort(tail, byteSwap);
            }

            throw new EndOfStreamException();
        }
        public int PeekInt(bool byteSwap)
        {
            ThrowIfDisposed();

            if (AvailableInBuffer >= sizeof(int))
            {
                return Buffer.GetInt(tail, byteSwap);
            }

            throw new EndOfStreamException();
        }
        public uint PeekUint(bool byteSwap)
        {
            ThrowIfDisposed();

            if (AvailableInBuffer >= sizeof(uint))
            {
                return Buffer.GetUint(tail, byteSwap);
            }

            throw new EndOfStreamException();
        }
        public long PeekLong(bool byteSwap)
        {
            ThrowIfDisposed();

            if (AvailableInBuffer >= sizeof(long))
            {
                return Buffer.GetLong(tail, byteSwap);
            }

            throw new EndOfStreamException();
        }
        public ulong PeekUlong(bool byteSwap)
        {
            ThrowIfDisposed();

            if (AvailableInBuffer >= sizeof(ulong))
            {
                return Buffer.GetUlong(tail, byteSwap);
            }

            throw new EndOfStreamException();
        }
        public float PeekFloat(bool byteSwap)
        {
            ThrowIfDisposed();

            if (AvailableInBuffer >= sizeof(float))
            {
                return Buffer.GetShort(tail, byteSwap);
            }

            throw new EndOfStreamException();
        }
        public double PeekDouble(bool byteSwap)
        {
            ThrowIfDisposed();

            if (AvailableInBuffer >= sizeof(double))
            {
                return Buffer.GetShort(tail, byteSwap);
            }

            throw new EndOfStreamException();
        }
        /// <summary>Returns the next string available in the buffer without advancing the position.</summary>
        /// <returns>The string at the start of the buffer.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Count is negative or larger than the buffered data.</exception>
        /// <exception cref="EndOfStreamException">The stream does not contain the requested number of bytes.</exception>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
        public string PeekString(int charCount, Encoding encoding = null)
        {
            ThrowIfDisposed();
            Validation.ThrowIfNegative(charCount, nameof(charCount));

            DecodeAtLeast(charCount, encoding);
            return new string(DecodeBuffer, 0, charCount);

            throw new EndOfStreamException();
        }
        /// <summary>Reads up to the given number of bytes and copies them to the given array.</summary>
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

            if (offset + count > buffer.Length)
            {
                throw new ArgumentException("Cannot fit the requested number of bytes into the remaining space of the buffer.");
            }
            Validation.ThrowIfNegative(offset, nameof(offset));
            Validation.ThrowIfNegative(count, nameof(count));

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
        public byte[] ReadBytes(int count)
        {
            ThrowIfDisposed();

            if (Stream == null && AvailableInBuffer < count)
            {
                throw new EndOfStreamException();
            }

            byte[] result = new byte[count];
            int bytesRead = Read(result, 0, count);
            if(bytesRead < count)
            {
                throw new EndOfStreamException();
            }


            if (AvailableInBuffer >= count)
            {
                result = Buffer.GetBytes(tail, count, byteSwap);
                Skip(count);
                return result;
            }
            result = default;
            return false;
        }
        public byte ReadByte()
        {
            byte result = PeekByte();
            Skip(1);
            return result;
        }
        public sbyte ReadSbyte()
        {
            sbyte result = PeekSbyte();
            Skip(1);
            return result;
        }
        public short ReadShort(bool byteSwap)
        {
            short result = PeekShort(byteSwap);
            Skip(sizeof(short));
            return result;
        }
        public ushort ReadUshort(bool byteSwap)
        {
            ushort result = PeekUshort(byteSwap);
            Skip(sizeof(ushort));
            return result;
        }
        public int ReadInt(bool byteSwap)
        {
            int result = PeekInt(byteSwap);
            Skip(sizeof(int));
            return result;
        }
        public uint ReadUint(bool byteSwap)
        {
            uint result = PeekUint(byteSwap);
            Skip(sizeof(uint));
            return result;
        }
        public long ReadLong(bool byteSwap)
        {
            long result = PeekLong(byteSwap);
            Skip(sizeof(long));
            return result;
        }
        public ulong ReadUlong(bool byteSwap)
        {
            ulong result = PeekUlong(byteSwap);
            Skip(sizeof(ulong));
            return result;
        }
        public float ReadFloat(bool byteSwap)
        {
            float result = PeekShort(byteSwap);
            Skip(sizeof(float));
            return result;
        }
        public double ReadDouble(bool byteSwap)
        {
            double result = PeekDouble(byteSwap);
            Skip(sizeof(double));
            return result;
        }
        public string ReadString(int charCount, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding;
            string result = PeekString(charCount, encoding);
            Skip(encoding.GetByteCount(result));
            return result;
        }
        public bool TryReadLine(out string result, Encoding encoding = null) => TryReadRegex(lineRegex, out result, encoding);
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
        public bool TryReadString(int charCount, out string result, Encoding encoding = null)
        {
            if (TryDecode(charCount, encoding))
            {
                result = new string(DecodeBuffer, 0, charCount);
                return true;
            }
            result = null;
            return false;
        }
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
        public bool TryReadRegex(Regex regex, out Match match, Encoding encoding = null)
        {
            if (Buffer != null)
            {
                encoding = encoding ?? Encoding;
                int decodeHead = Decode(encoding);
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

        private int Decode(Encoding encoding)
        {
            ThrowIfDisposed();

            encoding = encoding ?? Encoding;
            int charCount = 0;
            int bytesToRead = Math.Min(Encoding.GetMaxByteCount(DecodeBuffer.Length), AvailableInBuffer);
            int bufferTail = tail;
            int decodeHead = 0;

            // Perform incremental decoding to get the maximum number of chars.
            while (decodeHead < DecodeBuffer.Length && bytesToRead > 0)
            {
                try
                {
                    bytesToRead = Math.Min(bytesToRead, Encoding.GetMaxByteCount(charCount));
                    decodeHead += Encoding.GetChars(Buffer, bufferTail, bytesToRead, DecodeBuffer, decodeHead);
                    bufferTail += bytesToRead;
                    bytesToRead = (int)Math.Ceiling(bytesToRead * 0.5f);
                }
                catch (DecoderFallbackException) { }
            }

            return charCount;
        }
        private void DecodeAtLeast(int minCharCount, Encoding encoding)
        {
            ThrowIfDisposed();

            // We cannot decode more than the decode buffer's size.
            if (minCharCount > DecodeBuffer.Length)
            {
                throw new ArgumentOutOfRangeException($"Decode buffer is too small ({DecodeBuffer.Length}) to decode the requested string length ({minCharCount}).")
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
            }

            return decodeHead >= minCharCount;
        }
        private void ResetBuffer()
        {
            ThrowIfDisposed();

            if (Stream != null)
            {
                tail = 0;
                head = Stream.Read(Buffer, 0, Buffer.Length);
            }
        }
        private void Swap(byte[] bytes) => Swap(bytes, 0, bytes.Length - 1);
        private void Swap(byte[] bytes, int firstIndex, int lastIndex)
        {
            for(; firstIndex < lastIndex; ++firstIndex, --lastIndex)
            {
                (bytes[firstIndex], bytes[lastIndex]) = (bytes[lastIndex], bytes[firstIndex]);
            }
        }
        private void ThrowIfDisposed()
        {
            if(Buffer == null)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }
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

        private string DecodeUntilException(Encoding encoding, byte[] bytes, int index, int count)
        {
            Decoder decoder = encoding.GetDecoder();
            int charCount = encoding.GetMaxCharCount(count);
            char[] chars = new char[charCount];
            int bytesUsed = 0;
            int charsUsed = 0;
            bool completed = false;
            while (bytesUsed < count)
            {
                try
                {
                    decoder.Convert(bytes, index + bytesUsed, count - bytesUsed, chars, charsUsed, charCount - charsUsed, false, out int bytesDecoded, out int charsDecoded, out completed);
                    bytesUsed += bytesDecoded;
                    charsUsed += charsDecoded;
                }
                catch (DecoderFallbackException)
                {
                    break;
                }
            }
            return new string(chars, 0, charsUsed);
        }
    }




    public class StreamReader : IDisposable
    {
        public long Index { get; private set; } = 0;
        public long Line { get; set; } = 1;
        public bool HasNext => BufferIndex < Buffer.Length;
        public int Available => Buffer.Length - BufferIndex;

        protected int BufferSize { get; private set; } = 0x4000;
        protected readonly Stream Stream;

        private int BufferIndex = 0;
        private byte[] Buffer;
        private readonly bool LeaveOpen;

        public StreamReader(IEnumerable<byte> data)
        {
            Buffer = data.ToArray();
            BufferSize = Buffer.Length;
        }
        public StreamReader(Stream stream, int bufferSize = 0x4000, bool leaveOpen = false)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("Cannot read from null stream.", nameof(stream));
            }
            if (!stream.CanRead)
            {
                throw new ArgumentException("Cannot read from target stream.", nameof(stream));
            }
            if (bufferSize < 64)
            {
                throw new ArgumentOutOfRangeException("Buffer size cannot be less than 64 characters.", nameof(bufferSize));
            }
            Buffer = new byte[bufferSize];
            Stream = stream;
            BufferSize = bufferSize;
            LeaveOpen = leaveOpen;
            UpdateBuffer();
        }
        public static RegexReader ReadFromFile(string path, int bufferSize = 0x4000)
        {
            FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read);
            return new RegexReader(stream, bufferSize, false);
        }
        public static RegexReader ReadFromFile(string path, Encoding encoding, int bufferSize = 0x4000)
        {
            FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read);
            return new RegexReader(stream, encoding, bufferSize, false);
        }
        public void Dispose()
        {
            Buffer = null;
            DecodeBuffer = null;
            if (Stream != null && !LeaveOpen)
            {
                Stream.Dispose();
            }
        }
        /// <summary>Returns the next character in the stream.</summary>
        /// <exception cref="EndOfStreamException">The end of the stream has already been reached.</exception>
        public char PeekChar()
        {
            UpdateBuffer();
            if (HasNext)
            {
                return Buffer[BufferIndex];
            }
            else
            {
                throw new EndOfStreamException("Tried to read past the end of the stream.");
            }
        }
        /// <summary>Attempts to return the next character in the stream.</summary>
        public bool TryPeekChar(out char c)
        {
            UpdateBuffer();
            if (HasNext)
            {
                c = Buffer[BufferIndex];
                return true;
            }
            else
            {
                c = (char)0;
                return false;
            }
        }
        /// <summary>Reads a single character.</summary>
        /// <exception cref="EndOfStreamException">The end of the stream has already been reached.</exception>
        public char ReadChar()
        {
            UpdateBuffer();
            if (HasNext)
            {
                char c = Buffer[BufferIndex];
                MoveIndex(1, $"{c}");
                return c;
            }
            else
            {
                throw new EndOfStreamException("Tried to read past the end of the stream.");
            }
        }
        /// <summary>Attempts to read a single character.</summary>
        public bool TryReadChar(out char c)
        {
            UpdateBuffer();
            if (HasNext)
            {
                c = Buffer[BufferIndex];
                MoveIndex(1, $"{c}");
                return true;
            }
            else
            {
                c = (char)0;
                return false;
            }
        }
        /// <summary>Reads all text </summary>
        /// <param name="count"></param>
        /// <param name="text"></param>
        /// <param name="limitToBuffer"></param>
        /// <returns></returns>
        public int Read(int count, out string text, bool limitToBuffer = true)
        {
            UpdateBuffer();
            if (Stream == null || limitToBuffer)
            {
                text = Buffer.Substring(BufferIndex, Math.Min(count, Buffer.Length - BufferIndex));
                MoveIndex(text.Length, text);
            }
            else
            {
                StringBuilder builder = new StringBuilder();
                count = (int)Math.Min(count, Stream.Length - Index, builder.MaxCapacity);
                while (count > 0)
                {

                }
                text = builder.ToString();
            }
            return text.Length;
        }
        public void Skip(int skipCount)
        {
            while (skipCount > 0)
            {
                int bufferMargin = Buffer.Length - BufferIndex;
                if (skipCount >= bufferMargin)
                {
                    Index += bufferMargin;
                    BufferIndex = 0;
                    UpdateBuffer();
                    skipCount -= bufferMargin;
                }
                else
                {
                    Index += skipCount;
                    BufferIndex += skipCount;
                    UpdateBuffer();
                    skipCount = 0;
                }
            }
        }
        public string FormatPosition(bool index = true, bool line = false) => index ? (line ? $"Index: {Index}, Line: {Line}" : $"Index: {Index}") : $"Line: {Line}";
        /// <summary>Attempts to read the stream until the start of the given pattern is encountered.</summary>
        public bool TryReadUntil(Regex endPattern, out string text, out Match endMatch, bool limitToBuffer = true)
        {
            text = null;
            endMatch = null;
            if (Stream == null || limitToBuffer)
            {
                if (TryFindInBuffer(endPattern, out endMatch))
                {
                    Read(endMatch.Index - BufferIndex, out text, true);
                }
            }
            else
            {
                StringBuilder builder = new StringBuilder();
                while (Index < Stream.Length)
                {
                    if (TryFindInBuffer(endPattern, out endMatch, BufferIndex))
                    {
                        builder.Append(Buffer, BufferIndex, endMatch.Index);
                        break;
                    }
                    else
                    {
                        Advance(BufferSize - BufferIndex, out string skippedText);
                        builder.Append(skippedText);
                    }
                }
                text = builder.ToString();
            }
            return text != null;
        }
        /// <summary>Attempts to read the stream until the end of the given pattern is encountered.</summary>
        public bool TryReadUntil(Regex endPattern, out string text, bool limitToBuffer = true) => TryReadUntil(endPattern, out text, limitToBuffer);
        /// <summary>Attempts to read the contents of a scope defined by the given delimiter pattern.</summary>
        /// <param name="delimiterPattern">A regex pattern containing patterns for both the 'start' and 'end' delimiters as so-named groups.</param>
        public bool TryReadScope(Regex delimiterPattern, out Match startMatch, out string content, out Match endMatch, bool limitToBuffer = true)
        {
            content = null;
            endMatch = null;
            return TryFindInBuffer(delimiterPattern, out startMatch, BufferIndex)
                && startMatch.Groups["start"].Success
                && TryFinishScope(delimiterPattern, startMatch.Index + startMatch.Length, out content, out endMatch, limitToBuffer);
        }
        public bool TryFinishScope(Regex delimiterPattern, out string content, out Match endMatch, bool limitToBuffer = true) => TryFinishScope(delimiterPattern, BufferIndex, out content, out endMatch, limitToBuffer);
        /// <summary>Skips up to the given number of characters.</summary>
        public int Advance(int count, bool limitToBuffer = true) => Advance(count, out _, limitToBuffer);
        /// <summary>Skips up to the given number of characters and returns the text that was skipped.</summary>
        public int Advance(int count, out string skippedText, bool limitToBuffer = true)
        {
            if (Stream == null || limitToBuffer)
            {
                count = System.Math.Min(count, Buffer.Length - BufferIndex);
                skippedText = Buffer.Substring(BufferIndex, count);
                MoveIndex(count, skippedText);
            }
            else
            {
                StringBuilder builder = new StringBuilder();
                count = (int)Math.Min(Math.Min(count, Stream.Length - Index), builder.MaxCapacity);
                while (builder.Length < count)
                {
                    int subCount = Math.Min(count, Buffer.Length - BufferIndex);
                    skippedText = Buffer.Substring(BufferIndex, count);
                    MoveIndex(subCount);
                    builder.Append(skippedText);
                    UpdateBuffer();
                }
                skippedText = builder.ToString();
            }
            return count;
        }

        /// <summary>Increments the general and buffer indices and counts newlines traversed.</summary>
        private void MoveIndex(int count, string traversedText = null)
        {
            Line += (traversedText ?? Buffer.Substring(BufferIndex, count)).Count(c => c == '\n');
            BufferIndex += count;
            Index += count;
        }
        /// <summary>Reads bytes from the BaseStream into DecodeBuffer, then decodes those bytes into RegexBuffer.</summary>
        private void UpdateBuffer()
        {
            while (DecodeBuffer != null && Stream.Position < Stream.Length && Buffer.Length - BufferIndex < BufferSize)
            {
                Stream.Read(DecodeBuffer, 0, DecodeBuffer.Length);
                Buffer = (BufferIndex > 0 ? Buffer.Substring(BufferIndex) : Buffer) + Encoding.GetString(DecodeBuffer);
            }
        }
        /// <summary>Attempts to find a matching close delimiter for the currently opened scope.</summary>
        /// <param name="delimiterPattern"></param>
        /// <param name="startIndex"></param>
        /// <param name="content"></param>
        /// <param name="endMatch"></param>
        /// <param name="limitToBuffer"></param>
        private bool TryFinishScope(Regex delimiterPattern, int startIndex, out string content, out Match endMatch, bool limitToBuffer = true)
        {
            content = null;
            endMatch = null;
            int endIndex = startIndex;
            int depth = 1;
            if (Stream == null || limitToBuffer)
            {
                while (depth > 0)
                {
                    if (TryFindInBuffer(delimiterPattern, out endMatch, endIndex))
                    {
                        if (endMatch.Groups["start"].Success)
                        {
                            ++depth;
                        }
                        else if (endMatch.Groups["end"].Success)
                        {
                            --depth;
                        }
                        endIndex = endMatch.Index;
                    }
                    else
                    {
                        return false;
                    }
                }
                content = Buffer.Substring(startIndex, endIndex - startIndex);
            }
            else
            {
                StringBuilder builder = new StringBuilder();
                MoveIndex(startIndex - BufferIndex);
                while (depth > 0)
                {
                    if (TryReadUntil(delimiterPattern, out string text, out endMatch, false))
                    {
                        builder.Append(text);
                        if (endMatch.Groups["start"].Success)
                        {
                            ++depth;
                        }
                        else if (endMatch.Groups["end"].Success)
                        {
                            --depth;
                        }
                        if (depth > 0)
                        {
                            builder.Append(endMatch.Value);
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                content = builder.ToString();
            }
            return true;
        }
    }
}