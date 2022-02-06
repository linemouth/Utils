using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Utils
{
    public class RegexReader : IDisposable
    {
        public readonly Stream BaseStream;
        public long Index { get; private set; } = 0;
        public long Line { get; private set; } = 1;
        public bool HasNext => BufferIndex < RegexBuffer.Length;

        private int BufferIndex = 0;
        private int BufferSize = 0x4000;
        private byte[] DecodeBuffer = null;
        private string RegexBuffer = "";
        private readonly Encoding Encoding;
        private readonly bool LeaveOpen;

        /// <summary>Initializes a new instance of the RegexReader class for the specified stream.</summary>
        /// <param name="stream">The stream to be read.</param>
        /// <param name="bufferSize">The minimum size of the decode and pattern buffers. This should be at least as large as the largest anticipated match.</param>
        /// <param name="leaveOpen">true to leave the stream open after the RegexReader object is disposed; otherwise, false.</param>
        /// <exception cref="ArgumentException">stream does not support reading.</exception>
        /// <exception cref="ArgumentNullException">stream is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">bufferSize is too small or negative.</exception>
        public RegexReader(string text) : this(text, Encoding.UTF8) { }
        /// <summary>Initializes a new instance of the RegexReader class for the specified stream with the given encoding.</summary>
        /// <param name="stream">The stream to be read.</param>
        /// <param name="encoding">The character encoding to use.</param>
        /// <param name="bufferSize">The minimum size of the decode and pattern buffers. This should be at least as large as the largest anticipated match.</param>
        /// <param name="leaveOpen">true to leave the stream open after the RegexReader object is disposed; otherwise, false.</param>
        /// <exception cref="ArgumentException">stream does not support reading.</exception>
        /// <exception cref="ArgumentNullException">stream is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">bufferSize is too small or negative.</exception>
        public RegexReader(string text, Encoding encoding)
        {
            RegexBuffer = text;
            BufferSize = RegexBuffer.Length;
        }
        /// <summary>Initializes a new instance of the RegexReader class for the specified stream.</summary>
        /// <param name="stream">The stream to be read.</param>
        /// <param name="bufferSize">The minimum size of the decode and pattern buffers. This should be at least as large as the largest anticipated match.</param>
        /// <param name="leaveOpen">true to leave the stream open after the RegexReader object is disposed; otherwise, false.</param>
        /// <exception cref="ArgumentException">stream does not support reading.</exception>
        /// <exception cref="ArgumentNullException">stream is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">bufferSize is too small or negative.</exception>
        public RegexReader(Stream stream, int bufferSize = 0x4000, bool leaveOpen = false) : this(stream, Encoding.UTF8, bufferSize, leaveOpen) { }
        /// <summary>Initializes a new instance of the RegexReader class for the specified stream with the given encoding.</summary>
        /// <param name="stream">The stream to be read.</param>
        /// <param name="encoding">The character encoding to use.</param>
        /// <param name="bufferSize">The minimum size of the decode and pattern buffers. This should be at least as large as the largest anticipated match.</param>
        /// <param name="leaveOpen">true to leave the stream open after the RegexReader object is disposed; otherwise, false.</param>
        /// <exception cref="ArgumentException">stream does not support reading.</exception>
        /// <exception cref="ArgumentNullException">stream is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">bufferSize is too small or negative.</exception>
        public RegexReader(Stream stream, Encoding encoding, int bufferSize = 0x4000, bool leaveOpen = false)
        {
            if(stream == null)
            {
                throw new ArgumentNullException("Cannot read from null stream.", "stream");
            }
            if(!stream.CanRead)
            {
                throw new ArgumentException("Cannot read from target stream.", "stream");
            }
            if(bufferSize < 64)
            {
                throw new ArgumentOutOfRangeException("Buffer size cannot be less than 64 characters.", "bufferSize");
            }
            BaseStream = stream;
            Encoding = encoding;
            BufferSize = bufferSize;
            DecodeBuffer = new byte[BufferSize * 2];
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
            RegexBuffer = null;
            DecodeBuffer = null;
            if(BaseStream != null && !LeaveOpen)
            {
                BaseStream.Dispose();
            }
        }
        /// <summary>Returns the next character in the stream.</summary>
        /// <exception cref="EndOfStreamException">The end of the stream has already been reached.</exception>
        public char PeekChar()
        {
            UpdateBuffer();
            if(HasNext)
            {
                return RegexBuffer[BufferIndex];
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
            if(HasNext)
            {
                c = RegexBuffer[BufferIndex];
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
            if(HasNext)
            {
                char c = RegexBuffer[BufferIndex];
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
            if(HasNext)
            {
                c = RegexBuffer[BufferIndex];
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
            if(BaseStream == null || limitToBuffer)
            {
                text = RegexBuffer.Substring(BufferIndex, Math.Min(count, RegexBuffer.Length - BufferIndex));
                MoveIndex(text.Length, text);
            }
            else
            {
                StringBuilder builder = new StringBuilder();
                count = (int)Math.Min(count, BaseStream.Length - Index, builder.MaxCapacity);
                while(count > 0)
                {

                }
                text = builder.ToString();
            }
            return text.Length;
        }
        public void Skip(int skipCount)
        {
            while(skipCount > 0)
            {
                int bufferMargin = BufferSize - BufferIndex;
                if(skipCount >= bufferMargin)
                {
                    Line += RegexBuffer.Substring(BufferIndex).Count(c => c == '\n');
                    Index += bufferMargin;
                    BufferIndex = 0;
                    UpdateBuffer();
                    skipCount -= bufferMargin;
                }
                else
                {
                    Line += RegexBuffer.Substring(BufferIndex, skipCount).Count(c => c == '\n');
                    Index += skipCount;
                    BufferIndex += skipCount;
                    UpdateBuffer();
                    skipCount = 0;
                }
            }
        }
        public bool TryReadRegex(Regex pattern, out Match match, bool limitToBuffer = true)
        {
            if(BaseStream == null || limitToBuffer)
            {
                if(TryFindInBuffer(pattern, out match, BufferIndex))
                {
                    Advance(match.Index + match.Length - BufferIndex);
                }
            }
            else
            {
                while(true)
                {
                    if(TryFindInBuffer(pattern, out match, BufferIndex))
                    {
                        Advance(match.Index + match.Length - BufferIndex);
                        break;
                    }
                    else
                    {
                        Advance(BufferSize - BufferIndex);
                    }
                }
            }
            return match.Success;
        }
        public bool TryPeekRegex(Regex pattern, out Match match) => TryFindInBuffer(pattern, out match);
        public string FormatPosition(bool index = true, bool line = false) => index ? (line ? $"Index: {Index}, Line: {Line}" : $"Index: {Index}") : $"Line: {Line}";
        /// <summary>Attempts to read the stream until the start of the given pattern is encountered.</summary>
        public bool TryReadUntil(Regex endPattern, out string text, out Match endMatch, bool limitToBuffer = true)
        {
            text = null;
            endMatch = null;
            if(BaseStream == null || limitToBuffer)
            {
                if(TryFindInBuffer(endPattern, out endMatch))
                {
                    Read(endMatch.Index - BufferIndex, out text, true);
                }
            }
            else
            {
                StringBuilder builder = new StringBuilder();
                while(Index < BaseStream.Length)
                {
                    if(TryFindInBuffer(endPattern, out endMatch, BufferIndex))
                    {
                        builder.Append(RegexBuffer, BufferIndex, endMatch.Index);
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
            if(BaseStream == null || limitToBuffer)
            {
                count = System.Math.Min(count, RegexBuffer.Length - BufferIndex);
                skippedText = RegexBuffer.Substring(BufferIndex, count);
                MoveIndex(count, skippedText);
            }
            else
            {
                StringBuilder builder = new StringBuilder();
                count = (int)Math.Min(count, BaseStream.Length - Index, builder.MaxCapacity);
                while(builder.Length < count)
                {
                    int subCount = Math.Min(count, RegexBuffer.Length - BufferIndex);
                    skippedText = RegexBuffer.Substring(BufferIndex, count);
                    MoveIndex(subCount);
                    builder.Append(skippedText);
                    UpdateBuffer();
                }
                skippedText = builder.ToString();
            }
            return count;
        }
        /// <summary>Returns the current line.</summary>
        public string GetCurrentLine()
        {
            int startIndex = GetLineStartIndex();
            int endIndex = GetLineEndIndex();
            return RegexBuffer.Substring(startIndex, endIndex - startIndex);
        }
        /// <summary>Returns the current index relative to the start of the current line.</summary>
        public int GetPositionInLine() => BufferIndex - GetLineStartIndex();

        /// <summary>Attempts to locate a Regex match within the currently loaded buffer, starting at the given index within the RegexBuffer.</summary>
        protected bool TryFindInBuffer(Regex pattern, out Match match, int startIndex)
        {
            if(startIndex < 0)
            {
                startIndex = BufferIndex;
            }
            match = pattern.Match(RegexBuffer, startIndex);
            return match.Success;
        }
        /// <summary>Attempts to locate a Regex match within the currently loaded buffer, starting at the current index.</summary>
        protected bool TryFindInBuffer(Regex pattern, out Match match) => TryFindInBuffer(pattern, out match, BufferIndex);
        /// <summary>Returns the index of the start of the current line.</summary>
        protected int GetLineStartIndex()
        {
            int startIndex = BufferIndex;
            while(startIndex > 0 && !RegexBuffer[startIndex - 1].IsNewline())
            {
                --startIndex;
            }
            return startIndex;
        }
        /// <summary>Returns the index of the end of the current line.</summary>
        protected int GetLineEndIndex()
        {
            int endIndex = BufferIndex;
            while(endIndex < 0 && !RegexBuffer[endIndex].IsNewline())
            {
                --endIndex;
            }
            return endIndex;
        }

        /// <summary>Increments the general and buffer indices and counts newlines traversed.</summary>
        private void MoveIndex(int count, string traversedText = null)
        {
            Line += (traversedText ?? RegexBuffer.Substring(BufferIndex, count)).Count(c => c == '\n');
            BufferIndex += count;
            Index += count;
        }
        /// <summary>Reads bytes from the BaseStream into DecodeBuffer, then decodes those bytes into RegexBuffer.</summary>
        private void UpdateBuffer()
        {
            while(DecodeBuffer != null && BaseStream.Position < BaseStream.Length && RegexBuffer.Length - BufferIndex < BufferSize)
            {
                BaseStream.Read(DecodeBuffer, 0, DecodeBuffer.Length);
                RegexBuffer = (BufferIndex > 0 ? RegexBuffer.Substring(BufferIndex) : RegexBuffer) + Encoding.GetString(DecodeBuffer);
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
            if(BaseStream == null || limitToBuffer)
            {
                while(depth > 0)
                {
                    if(TryFindInBuffer(delimiterPattern, out endMatch, endIndex))
                    {
                        if(endMatch.Groups["start"].Success)
                        {
                            ++depth;
                        }
                        else if(endMatch.Groups["end"].Success)
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
                content = RegexBuffer.Substring(startIndex, endIndex - startIndex);
            }
            else
            {
                StringBuilder builder = new StringBuilder();
                MoveIndex(startIndex - BufferIndex);
                while(depth > 0)
                {
                    if(TryReadUntil(delimiterPattern, out string text, out endMatch, false))
                    {
                        builder.Append(text);
                        if(endMatch.Groups["start"].Success)
                        {
                            ++depth;
                        }
                        else if(endMatch.Groups["end"].Success)
                        {
                            --depth;
                        }
                        if(depth > 0)
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
