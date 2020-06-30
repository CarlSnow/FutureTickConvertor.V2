using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Ats.Core;

namespace FutureTickConvertor
{
    /// <summary>
    /// Base class to abstract away core bar stream processing logic.
    /// </summary>
    public abstract class BarStream : IDisposable
    {
        private const int BarLength = 88;
        private const int SimpleDayBarCountThreshold = 18;
        private const string FileExtension = ".min";
        private readonly FileStream _fileStream;
        private readonly FileHeader _fileHeader;
        private bool _isHeaderRead;
        private readonly bool _createNew;

        protected BarStream(string filePath, FileHeader fileHeader)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("filePath");
            }

            var ext = Path.GetExtension(filePath).ToLower();
            if (ext != FileExtension)
            {
                throw new ArgumentException("invalid file extension");
            }

            _createNew = !File.Exists(filePath);

            FilePath = filePath;
            _fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

            this._fileHeader = fileHeader;

            if (!_createNew)
            {
                if (this._fileHeader == null)
                {
                    this._fileHeader = new FileHeader();
                }
                
                using (var reader = CreateReader())
                {
                    ReadHeader(reader);
                }
            }
        }

        public string FilePath { get; private set; }

        public DateTime BeginTime
        {
            get { return _fileHeader.BeginTime; }
        }

        public DateTime EndTime
        {
            get { return _fileHeader.EndTime; }
        }

        public int BarCount
        {
            get { return _fileHeader.BarCount; }
        }

        public EnumBarType BarType
        {
            get { return _fileHeader.BarType; }
        }

        public EnumMarket Market
        {
            get { return _fileHeader.Market; }
        }

        public int Period
        {
            get { return _fileHeader.Period; }
        }

        /// <summary>
        /// Write the bar series into bar stream file.
        /// </summary>
        /// <param name="barSeries"></param>
        public void Write(BarSeries barSeries)
        {
            Write(barSeries, 0, barSeries.Count);
        }

        /// <summary>
        /// Write the bar series into bar stream file.
        /// </summary>
        /// <param name="barSeries"></param>
        /// <param name="start">start index of the series</param>
        /// <param name="length">the length of bar to be written.</param>
        public void Write(BarSeries barSeries, int start, int length)
        {
            WriteInternal(barSeries, start, length, _fileHeader.FileHeaderSize);
        }

        /// <summary>
        /// Read bars given the specified trading day.
        /// </summary>
        /// <param name="barSeries">The bar series.</param>
        /// <param name="tradingDay">trading day provided following exchange datetime spec.</param>
        /// <returns></returns>
        public bool ReadTradingDay(BarSeries barSeries, DateTime tradingDay)
        {
            if (barSeries == null)
            {
                throw new ArgumentNullException("barSeries");
            }

            if (tradingDay == default(DateTime))
            {
                throw new ArgumentException("tradingDay");
            }

            var readCount = 0;
            tradingDay = new DateTime(tradingDay.Year, tradingDay.Month, tradingDay.Day);
            using (var reader = CreateReader())
            {
                ReadHeader(reader);
                if (tradingDay < _fileHeader.BeginTradingDay || tradingDay > _fileHeader.EndTradingDay)
                {
                    return false;
                }

                long startOffset;
                int count;
                if (_fileHeader.TradingDayIndices.TryGet(tradingDay, out startOffset, out count))
                {
                    reader.BaseStream.Position = startOffset;
                    while (count > 0)
                    {
                        barSeries.Add(ReadBar(reader));
                        count--;
                        readCount++;
                    }
                }

                return readCount > 0;
            }
        }

        /// <summary>
        /// Read bars given the specified trading day interval [startTradingDay, endTradingDay].
        /// </summary>
        /// <param name="barSeries">The bar series.</param>
        /// <param name="startTradingDay">The start trading day.</param>
        /// <param name="endTradingDay">The end trading day.</param>
        /// <returns></returns>
        public bool ReadTradingDays(BarSeries barSeries, DateTime startTradingDay, DateTime endTradingDay)
        {
            if (barSeries == null)
            {
                throw new ArgumentNullException("barSeries");
            }

            if (startTradingDay == default(DateTime))
            {
                throw new ArgumentException("from");
            }

            if (endTradingDay == default(DateTime))
            {
                throw new ArgumentException("to");
            }

            startTradingDay = new DateTime(startTradingDay.Year, startTradingDay.Month, startTradingDay.Day);
            endTradingDay = new DateTime(endTradingDay.Year, endTradingDay.Month, endTradingDay.Day);

            if (startTradingDay > endTradingDay)
            {
                throw new ArgumentException("endTradingDay must be larger than startTradingDay");
            }

            if (startTradingDay == endTradingDay)
            {
                return ReadTradingDay(barSeries, startTradingDay);
            }

            var readCount = 0;
            using (var reader = CreateReader())
            {
                ReadHeader(reader);
                if (endTradingDay < _fileHeader.BeginTradingDay && startTradingDay > _fileHeader.EndTradingDay)
                {
                    return false;
                }

                if (startTradingDay < _fileHeader.BeginTradingDay)
                {
                    startTradingDay = _fileHeader.BeginTradingDay;
                }

                if (endTradingDay > _fileHeader.EndTradingDay)
                {
                    endTradingDay = _fileHeader.EndTradingDay;
                }

                long startOffset, endOffset;
                int startCount, endCount;
                if (_fileHeader.TradingDayIndices.GetTradingDayOffsets(startTradingDay, endTradingDay, out startOffset, out endOffset, out startCount, out endCount))
                {
                    endOffset += BarLength * endCount;
                    reader.BaseStream.Position = startOffset;
                    while (reader.BaseStream.Position < endOffset)
                    {
                        barSeries.Add(ReadBar(reader));
                        readCount++;
                    }
                }
            }

            return readCount > 0;
        }

        /// <summary>
        /// Read bar stream into bar series starting by given date.
        /// </summary>
        /// <param name="barSeries">The bar series.</param>
        /// <param name="from">The start date.</param>
        /// <returns></returns>
        public bool ReadFrom(BarSeries barSeries, DateTime from)
        {
            if (barSeries == null)
            {
                throw new ArgumentNullException("barSeries");
            }

            if (from == default(DateTime))
            {
                throw new ArgumentException("from");
            }

            var readCount = 0;
            using (var reader = CreateReader())
            {
                ReadHeader(reader);
                if (from > _fileHeader.EndTime)
                {
                    return false;
                }

                if (from > _fileHeader.BeginTime)
                {
                    long startDayOffset;
                    int startCount;
                    _fileHeader.NaturalDayIndices.GetStartOffset(from, out startDayOffset, out startCount);

                    long startOffset;
                    var startBar = ReadStartBarOffset(reader, from, startDayOffset, startCount, out startOffset);

                    if (startOffset < 0)
                    {
                        return false;
                    }

                    if (startBar != null)
                    {
                        readCount++;
                        barSeries.Add(startBar);
                    }

                    reader.BaseStream.Position = startOffset;
                }

                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    readCount++;
                    barSeries.Add(ReadBar(reader));
                }
            }

            return readCount > 0;
        }

        /// <summary>
        /// Read bar stream into bar series starting from the start of stream and ending by given the date.
        /// </summary>
        /// <param name="barSeries">The bar series</param>
        /// <param name="to">the end date.</param>
        /// <returns></returns>
        public bool ReadTo(BarSeries barSeries, DateTime to)
        {
            if (barSeries == null)
            {
                throw new ArgumentNullException("barSeries");
            }

            if (to == default(DateTime))
            {
                throw new ArgumentException("to");
            }

            var readCount = 0;
            using (var reader = CreateReader())
            {
                ReadHeader(reader);

                if (to < _fileHeader.BeginTime)
                {
                    return false;
                }

                long endOffset;
                Bar endBar = null;
                if (to >= _fileHeader.EndTime)
                {
                    endOffset = reader.BaseStream.Length - 1;
                }
                else
                {
                    long endDayOffset;
                    int endCount;
                    _fileHeader.NaturalDayIndices.GetEndOffset(to, out endDayOffset, out endCount);
                    endBar = ReadEndBarOffset(reader, to, endDayOffset, endCount, out endOffset);
                    if (endOffset < 0)
                    {
                        return false;
                    }
                }


                reader.BaseStream.Position = _fileHeader.FileHeaderSize;
                while (reader.BaseStream.Position <= endOffset)
                {
                    readCount++;
                    barSeries.Add(ReadBar(reader));
                }

                if (endBar != null)
                {
                    readCount++;
                    barSeries.Add(endBar);
                }
            }

            return readCount > 0;
        }

        /// <summary>
        /// Read all bars in the stream into bar series.
        /// </summary>
        /// <param name="barSeries">The bar series.</param>
        /// <returns></returns>
        public bool ReadAll(BarSeries barSeries)
        {
            var readCount = 0;
            using (var reader = CreateReader())
            {
                ReadHeader(reader);
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    readCount++;
                    barSeries.Add(ReadBar(reader));
                }
            }

            return readCount > 0;
        }

        /// <summary>
        /// Read bar stream into bar series.
        /// </summary>
        /// <param name="barSeries">The bar series.</param>
        /// <param name="from">The start time.</param>
        /// <param name="to">The end time.</param>
        /// <returns></returns>
        public bool Read(BarSeries barSeries, DateTime from, DateTime to)
        {
            if (barSeries == null)
            {
                throw new ArgumentNullException("barSeries");
            }

            if (from == default(DateTime))
            {
                throw new ArgumentException("from");
            }

            if (to == default(DateTime))
            {
                throw new ArgumentException("to");
            }

            if (to < from)
            {
                throw new ArgumentException("to must be larger than from");
            }

            if (from == to)
            {
                return false;
            }

            var readCount = 0;

            using (var reader = CreateReader())
            {
                ReadHeader(reader);
                if (from > _fileHeader.EndTime || to <= _fileHeader.BeginTime)
                {
                    return false;
                }

                if (from < _fileHeader.BeginTime)
                {
                    from = _fileHeader.BeginTime;
                }

                if (to > _fileHeader.EndTime)
                {
                    to = _fileHeader.EndTime;
                }

                long startDayOffset;
                int startCount;
                _fileHeader.NaturalDayIndices.GetStartOffset(from, out startDayOffset, out startCount);

                long endDayOffset;
                int endCount;
                if (from.Day == to.Day && from.Month == to.Month)
                {
                    if (startCount <= SimpleDayBarCountThreshold)
                    {
                        reader.BaseStream.Position = startDayOffset;
                        while (startCount > 0)
                        {
                            var bar = ReadBarByInterval(reader, from, to);
                            if (bar != null)
                            {
                                barSeries.Add(bar);
                                readCount++;
                            }

                            startCount--;
                        }

                        return readCount > 0;
                    }

                    endDayOffset = startDayOffset;
                    endCount = startCount;
                }
                else
                {
                    _fileHeader.NaturalDayIndices.GetEndOffset(to, out endDayOffset, out endCount);
                }

                if (startDayOffset < 0 || endDayOffset < 0 || startDayOffset > endDayOffset)
                {
                    return false;
                }

                long startOffset;
                long endOffset;
                var startBar = ReadStartBarOffset(reader, from, startDayOffset, startCount, out startOffset);
                var endBar = ReadEndBarOffset(reader, to, endDayOffset, endCount, out endOffset);
                if (startOffset >= 0 && endOffset >= 0 && startOffset < endOffset)
                {
                    if (startBar != null)
                    {
                        barSeries.Add(startBar);
                        readCount++;
                    }

                    reader.BaseStream.Position = startOffset;
                    while (reader.BaseStream.Position <= endOffset)
                    {
                        barSeries.Add(ReadBar(reader));
                        readCount++;
                    }

                    if (endBar != null)
                    {
                        barSeries.Add(endBar);
                        readCount++;
                    }
                }
            }

            return readCount > 0;
        }

        /// <summary>
        /// Append the bar series into bar stream file.
        /// </summary>
        /// <param name="barSeries">The bar series</param>
        public void Append(BarSeries barSeries)
        {
            Append(barSeries, 0, barSeries.Count);
        }

        /// <summary>
        /// Append the bar series into bar stream file.
        /// </summary>
        /// <param name="barSeries">The bar series</param>
        /// <param name="start">start index of the series</param>
        /// <param name="length">the length of bar to be appended.</param>
        public void Append(BarSeries barSeries, int start, int length)
        {
            var offset = Math.Max(_fileStream.Length, _fileHeader.FileHeaderSize);
            WriteInternal(barSeries, start, length, offset);
        }

        /// <summary>
        /// Close the bar stream.
        /// </summary>
        public void Close()
        {
            _fileStream.Close();
        }

        /// <summary>
        /// Flush the bar stream.
        /// </summary>
        public void Flush()
        {
            _fileStream.Flush();
        }

        /// <summary>
        /// Asynchronously flush the stream.
        /// </summary>
        /// <returns></returns>
        public Task FlushAsync()
        {
            return _fileStream.FlushAsync();
        }

        /// <summary>
        /// Dispose the bar stream.
        /// </summary>
        public void Dispose()
        {
            _fileStream.Dispose();
        }

        protected abstract void ValidateHeader(FileHeader fileHeader);

        private BinaryReader CreateReader()
        {
            return new BinaryReader(_fileStream, new UTF8Encoding(), true);
        }

        private BinaryWriter CreateWriter()
        {
            return new BinaryWriter(_fileStream, new UTF8Encoding(), true);
        }

        protected virtual void ReadRemainingBar(BinaryReader reader, Bar bar)
        {
            bar.Open = reader.ReadDouble();
            bar.Close = reader.ReadDouble();
            bar.High = reader.ReadDouble();
            bar.Low = reader.ReadDouble();
            bar.PreClose = reader.ReadDouble();
            bar.Volume = reader.ReadDouble();
            bar.Turnover = reader.ReadDouble();
            bar.OpenInterest = reader.ReadDouble();
        }

        protected virtual void WriteBar(BinaryWriter writer, Bar bar)
        {
            var tradingDay = new DateTime(bar.TradingDate.Year, bar.TradingDate.Month, bar.TradingDate.Day);
            if (_fileHeader.BeginTradingDay == default(DateTime) || tradingDay < _fileHeader.BeginTradingDay)
            {
                _fileHeader.BeginTradingDay = tradingDay;
                _fileHeader.MarkAsDirty(true);
            }

            if (_fileHeader.EndTradingDay == default(DateTime) || tradingDay > _fileHeader.EndTradingDay)
            {
                _fileHeader.EndTradingDay = tradingDay;
                _fileHeader.MarkAsDirty(true);
            }

            var offset = writer.BaseStream.Position;
            _fileHeader.NaturalDayIndices.Add(bar.BeginTime, offset);
            _fileHeader.TradingDayIndices.Add(bar.TradingDate, offset);
            writer.Write(bar.BeginTime.Ticks);
            writer.Write(bar.EndTime.Ticks);
            writer.Write(bar.TradingDate.Ticks);
            writer.Write(bar.Open);
            writer.Write(bar.Close);
            writer.Write(bar.High);
            writer.Write(bar.Low);
            writer.Write(bar.PreClose);
            writer.Write(bar.Volume);
            writer.Write(bar.Turnover);
            writer.Write(bar.OpenInterest);
        }

        private void WriteInternal(BarSeries barSeries, int start, int length, long offset)
        {
            if (barSeries == null)
            {
                throw new ArgumentNullException("barSeries");
            }

            if (length < 0)
            {
                throw new ArgumentException("length");
            }

            if (start < 0 || start + length > barSeries.Count)
            {
                throw new ArgumentException("start");
            }

            UpdateFileHeader(barSeries, start, length);

            using (var writer = CreateWriter())
            {
                writer.BaseStream.Position = offset;
                WriteBars(writer, barSeries, start, length);
                _fileHeader.Write(writer);
            }
        }

        private Bar ReadStartBarOffset(BinaryReader reader, DateTime date, long offset, int count, out long startOffset)
        {
            reader.BaseStream.Position = offset;
            var current = new DateTime(reader.ReadInt64());
            if (current >= date)
            {
                var bar = ReadBar(reader, current);
                startOffset = offset + BarLength;
                return bar;
            }

            var low = 0;
            var high = count - 1;
            if (count % 2 == 0)
            {
                low = 1;
            }

            while (low <= high)
            {
                var median = low + (high - low >> 1);
                startOffset = offset + median * BarLength;
                reader.BaseStream.Position = startOffset;
                current = new DateTime(reader.ReadInt64());
                if (current == date)
                {
                    var bar = ReadBar(reader, current);
                    startOffset += BarLength;
                    return bar;
                }

                if (high == low || median == 0)
                {
                    while (current < date)
                    {
                        startOffset += BarLength;
                        reader.BaseStream.Position = startOffset;
                        current = new DateTime(reader.ReadInt64());
                    }

                    var bar = ReadBar(reader, current);
                    startOffset += BarLength;
                    return bar;
                }

                if (current < date)
                {
                    low = median + 1;
                }
                else
                {
                    high = median - 1;
                }
            }

            startOffset = -1;
            return null;
        }

        private Bar ReadEndBarOffset(BinaryReader reader, DateTime date, long offset, int count, out long endOffset)
        {
            reader.BaseStream.Position = offset + (count - 1) * BarLength;
            var start = new DateTime(reader.ReadInt64());
            var current = new DateTime(reader.ReadInt64());
            if (current <= date)
            {
                var bar = ReadBar(reader, start, current);
                endOffset = offset + (count - 2) * BarLength;
                return bar;
            }

            var low = 0;
            var high = count - 1;
            if (count % 2 == 0)
            {
                high = count - 2;
            }

            while (low <= high)
            {
                var median = low + (high - low >> 1);
                endOffset = offset + median * BarLength;
                reader.BaseStream.Position = endOffset;
                start = new DateTime(reader.ReadInt64());
                current = new DateTime(reader.ReadInt64());
                if (current == date)
                {
                    var bar = ReadBar(reader, start, current);
                    endOffset -= BarLength;
                    return bar;
                }

                if (low == high || median == 0)
                {
                    while (current > date)
                    {
                        endOffset -= BarLength;
                        reader.BaseStream.Position = endOffset;
                        start = new DateTime(reader.ReadInt64());
                        current = new DateTime(reader.ReadInt64());
                    }

                    var bar = ReadBar(reader, start, current);
                    endOffset -= BarLength;
                    return bar;
                }

                if (current < date)
                {
                    low = median + 1;
                }
                else
                {
                    high = median - 1;
                }
            }

            endOffset = -1;
            return null;
        }

        private Bar ReadBarByInterval(BinaryReader reader, DateTime from, DateTime to)
        {
            var begin = new DateTime(reader.ReadInt64());
            var end = new DateTime(reader.ReadInt64());
            if (from <= begin && end <= to)
            {
                var bar = new Bar
                {
                    BeginTime = begin,
                    EndTime = end,
                    TradingDate = new DateTime(reader.ReadInt64()),
                    IsCompleted = true
                };

                ReadRemainingBar(reader, bar);
                return bar;
            }

            reader.BaseStream.Position += BarLength - 2 * 8;
            return null;
        }

        private Bar ReadBar(BinaryReader reader, DateTime beginTime)
        {
            var bar = new Bar
            {
                BeginTime = beginTime,
                EndTime = new DateTime(reader.ReadInt64()),
                TradingDate = new DateTime(reader.ReadInt64()),
                IsCompleted = true
            };

            ReadRemainingBar(reader, bar);
            return bar;
        }

        private Bar ReadBar(BinaryReader reader, DateTime beginTime, DateTime endTime)
        {
            var bar = new Bar
            {
                BeginTime = beginTime,
                EndTime = endTime,
                TradingDate = new DateTime(reader.ReadInt64()),
                IsCompleted = true
            };

            ReadRemainingBar(reader, bar);
            return bar;
        }

        private Bar ReadBar(BinaryReader reader)
        {
            var bar = new Bar
            {
                BeginTime = new DateTime(reader.ReadInt64()),
                EndTime = new DateTime(reader.ReadInt64()),
                TradingDate = new DateTime(reader.ReadInt64()),
                IsCompleted = true
            };

            ReadRemainingBar(reader, bar);
            return bar;
        }

        private void WriteBars(BinaryWriter writer, BarSeries barSeries, int start, int length)
        {
            for (var i = start; i < length; i++)
            {
                var bar = barSeries[i];
                WriteBar(writer, bar);
            }
        }

        private void UpdateFileHeader(BarSeries barSeries, int start, int length)
        {
            var startBar = barSeries[start];
            var endBar = barSeries[start + length - 1];
            if (_fileHeader.BeginTime == default(DateTime) || startBar.BeginTime < _fileHeader.BeginTime)
            {
                _fileHeader.BeginTime = startBar.BeginTime;
                _fileHeader.MarkAsDirty(true);
            }

            if (_fileHeader.EndTime == default(DateTime) || endBar.EndTime > _fileHeader.EndTime)
            {
                _fileHeader.EndTime = endBar.EndTime;
                _fileHeader.MarkAsDirty(true);
            }

            _fileHeader.BarCount += length;
        }

        private void ReadHeader(BinaryReader reader)
        {
            try
            {
                if (!_isHeaderRead)
                {
                    _fileHeader.Read(reader);
                    if (_createNew)
                    {
                        ValidateHeader(_fileHeader);
                    }

                    _isHeaderRead = false;
                }
                else
                {
                    reader.BaseStream.Position = _fileHeader.FileHeaderSize;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Invalid file header", ex);
            }
        }

        protected class FileHeader
        {
            private const int FileVersionSize = 8;
            private const int FILE_HEADER_SIZE = 64;
            private const int FileHeaderTotalSize = 64 + 32 * 20 + 32 * 20;

            private bool _isDirty;
            public string FileVersion { get; set; }

            public EnumMarket Market { get; set; }

            public EnumBarType BarType { get; set; }

            public DateTime BeginTime { get; set; }

            public DateTime EndTime { get; set; }

            public DateTime BeginTradingDay { get; set; }

            public DateTime EndTradingDay { get; set; }

            public int BarCount { get; set; }

            public int Period { get; set; }

            public DateTimeToOffsetMap TradingDayIndices { get; set; }

            public DateTimeToOffsetMap NaturalDayIndices { get; set; }

            public int FileHeaderSize
            {
                get { return FileHeaderTotalSize; }
            }

            public FileHeader()
            {
                TradingDayIndices = new DateTimeToOffsetMap(this);
                NaturalDayIndices = new DateTimeToOffsetMap(this);
            }

            public void MarkAsDirty(bool value)
            {
                _isDirty = value;
            }

            public virtual void Write(BinaryWriter writer)
            {
                if (_isDirty)
                {
                    writer.BaseStream.Seek(0, SeekOrigin.Begin);
                    writer.Write(FileVersion);
                    writer.BaseStream.Position = FileVersionSize;
                    writer.Write((int)Market);
                    writer.Write((int)BarType);
                    writer.Write(Period);
                    writer.Write(BeginTime.Ticks);
                    writer.Write(EndTime.Ticks);
                    writer.Write(BeginTradingDay.Ticks);
                    writer.Write(EndTradingDay.Ticks);
                    writer.Write(BarCount);
                    writer.BaseStream.Position = FILE_HEADER_SIZE;
                    TradingDayIndices.Write(writer);
                    NaturalDayIndices.Write(writer);
                    MarkAsDirty(false);
                }
            }

            public virtual void Read(BinaryReader reader)
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                FileVersion = reader.ReadString();
                reader.BaseStream.Position = FileVersionSize;
                Market = (EnumMarket)reader.ReadInt32();
                BarType = (EnumBarType)reader.ReadInt32();
                Period = reader.ReadInt32();
                BeginTime = new DateTime(reader.ReadInt64());
                EndTime = new DateTime(reader.ReadInt64());
                BeginTradingDay = new DateTime(reader.ReadInt64());
                EndTradingDay = new DateTime(reader.ReadInt64());
                BarCount = reader.ReadInt32();
                reader.BaseStream.Position = FILE_HEADER_SIZE;
                TradingDayIndices.Read(reader);
                NaturalDayIndices.Read(reader);
                MarkAsDirty(false);
            }
        }

        protected class DateTimeToOffsetMap
        {
            public struct OffsetCount
            {
                public long Offset { get; private set; }
                public int Count { get; private set; }

                public OffsetCount(long offset, int count)
                    : this()
                {
                    Offset = offset;
                    Count = count;
                }

                public override string ToString()
                {
                    return string.Format("Offset:{0}, Count:{1}", Offset, Count);
                }
            }

            private const int BucketSize = 32;
            private const int Mask = BucketSize - 1;
            private readonly FileHeader _fileHeader;
            private static readonly DateTime DefaultKey = default(DateTime);
            private readonly DateTime[] _keys = new DateTime[BucketSize];
            private readonly OffsetCount[] _values = new OffsetCount[BucketSize];

            public DateTimeToOffsetMap(FileHeader fileHeader)
            {
                this._fileHeader = fileHeader;
            }

            public bool Add(DateTime dateTime, long offset)
            {
                if (dateTime == DefaultKey)
                {
                    throw new ArgumentException("invalid date time");
                }

                var key = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day);
                var hash = HashKey(key);
                var index = hash & Mask;
                var found = false;
                var oldIndex = index;
                while (_keys[index] != DefaultKey)
                {
                    if (_keys[index] == key)
                    {
                        found = true;
                        break;
                    }

                    hash++;
                    index = hash & Mask;
                    if (oldIndex == index)
                    {
                        throw new InvalidOperationException("Unexpected collision is detected in DateTimeToOffsetMap.");
                    }
                }

                if (found)
                {
                    var old = _values[index];
                    _values[index] = new OffsetCount(old.Offset, old.Count + 1);
                }
                else
                {
                    _keys[index] = key;
                    _values[index] = new OffsetCount(offset, 1);
                    _fileHeader.MarkAsDirty(true);
                }

                return !found;
            }

            public bool GetTradingDayOffsets(DateTime from, DateTime to, out long startOffset, out long endOffset, out int startCount, out int endCount)
            {
                var start = from;
                var end = to;
                while (!TryGet(start, out startOffset, out startCount))
                {
                    start = start.AddDays(1);
                    if (start > to)
                    {
                        startOffset = -1;
                        endOffset = -1;
                        endCount = 0;
                        return false;
                    }
                }

                while (!TryGet(end, out endOffset, out endCount))
                {
                    end = end.AddDays(-1);
                    if (end < from)
                    {
                        startOffset = -1;
                        endOffset = -1;
                        return false;
                    }
                }

                return startOffset <= endOffset;
            }

            public void GetStartOffset(DateTime key, out long offset, out int count)
            {
                if (key == DefaultKey)
                {
                    throw new ArgumentException("invalid date time");
                }

                // bug:DJB-585
                //key = new DateTime(key.Year, key.Month, key.Day);
                //var hash = HashKey(key);
                //var index = hash & Mask;
                //if (keys[index] != key)
                //{
                //    while (index < BucketSize)
                //    {
                //        if (keys[++index] != DefaultKey)
                //        {
                //            break;
                //        }
                //    }
                //}

                key = new DateTime(key.Year, key.Month, key.Day);
                var tmpkey = DefaultKey;
                var index = -1;

                var hash = HashKey(key);
                var idx = hash & Mask;
                for (var i = 0; i < _keys.Length; ++i)
                {
                    if (_keys[idx] != DefaultKey)
                    {
                        if (key == _keys[idx])
                        {
                            index = idx;
                            break;
                        }

                        if (key < _keys[idx])
                        {
                            if (tmpkey == DefaultKey || tmpkey > _keys[idx])
                            {
                                tmpkey = _keys[idx];
                                index = idx;
                            }
                        }
                    }

                    ++idx;
                    if (idx == _keys.Length)
                        idx = 0;
                }

                if (index == -1)
                {
                    throw new ArgumentException("invalid date time");
                }

                var value = _values[index];
                offset = value.Offset;
                count = value.Count;
            }

            public void GetEndOffset(DateTime key, out long offset, out int count)
            {
                if (key == DefaultKey)
                {
                    throw new ArgumentException("invalid date time");
                }

                // bug:DJB-585
                //key = new DateTime(key.Year, key.Month, key.Day);
                //var hash = HashKey(key);
                //var index = hash & Mask;
                //if (keys[index] != key)
                //{
                //    while (index >= 0)
                //    {
                //        if (keys[index] != DefaultKey)
                //        {
                //            break;
                //        }

                //        --index;
                //    }
                //}

                key = new DateTime(key.Year, key.Month, key.Day);
                var tmpkey = DefaultKey;
                var index = -1;

                var hash = HashKey(key);
                var idx = hash & Mask;
                for (var i = 0; i < _keys.Length; ++i)
                {
                    if (_keys[idx] != DefaultKey)
                    {
                        if (key == _keys[idx])
                        {
                            index = idx;
                            break;
                        }

                        if (key > _keys[idx])
                        {
                            if (tmpkey == DefaultKey || tmpkey < _keys[idx])
                            {
                                tmpkey = _keys[idx];
                                index = idx;
                            }
                        }
                    }

                    ++idx;
                    if (idx == _keys.Length)
                        idx = 0;
                }

                if (index == -1)
                {
                    throw new ArgumentException("invalid date time");
                }

                var value = _values[index];
                offset = value.Offset;
                count = value.Count;
            }

            public bool TryGet(DateTime key, out long offset, out int count)
            {
                if (key == DefaultKey)
                {
                    throw new ArgumentException("invalid date time");
                }

                key = new DateTime(key.Year, key.Month, key.Day);
                var hash = HashKey(key);
                var index = hash & Mask;
                var found = false;
                var oldIndex = index;
                while (_keys[index] != DefaultKey)
                {
                    if (_keys[index] == key)
                    {
                        found = true;
                        break;
                    }

                    hash++;
                    index = hash & Mask;
                    if (oldIndex == index)
                    {
                        throw new InvalidOperationException("Unexpected collision is detected in DateTimeToOffsetMap.");
                    }
                }

                if (found)
                {
                    var value = _values[index];
                    offset = value.Offset;
                    count = value.Count;
                }
                else
                {
                    offset = -1;
                    count = 0;
                }

                return found;
            }

            public void Write(BinaryWriter writer)
            {
                for (var i = 0; i < BucketSize; i++)
                {
                    writer.Write(_keys[i].Ticks);
                }

                for (var i = 0; i < BucketSize; i++)
                {
                    var value = _values[i];
                    writer.Write(value.Offset);
                    writer.Write(value.Count);
                }
            }

            public void Read(BinaryReader reader)
            {
                for (var i = 0; i < BucketSize; i++)
                {
                    _keys[i] = new DateTime(reader.ReadInt64());
                }

                for (var i = 0; i < BucketSize; i++)
                {
                    _values[i] = new OffsetCount(reader.ReadInt64(), reader.ReadInt32());
                }
            }

            private static int HashKey(DateTime time)
            {
                return time.Day;
            }
        }
    }
}
