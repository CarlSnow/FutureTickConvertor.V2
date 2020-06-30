using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfTest.MqData.Min
{
    public class DateTimeToOffsetMap
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
        private readonly FileHeader fileHeader;
        private static readonly DateTime DefaultKey = default(DateTime);
        private readonly DateTime[] keys = new DateTime[BucketSize];
        private readonly OffsetCount[] values = new OffsetCount[BucketSize];

        public DateTimeToOffsetMap(FileHeader fileHeader)
        {
            this.fileHeader = fileHeader;
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
            while (keys[index] != DefaultKey)
            {
                if (keys[index] == key)
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
                var old = values[index];
                values[index] = new OffsetCount(old.Offset, old.Count + 1);
            }
            else
            {
                keys[index] = key;
                values[index] = new OffsetCount(offset, 1);
                fileHeader.MarkAsDirty(true);
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
            for (var i = 0; i < keys.Length; ++i)
            {
                if (keys[idx] != DefaultKey)
                {
                    if (key == keys[idx])
                    {
                        index = idx;
                        break;
                    }

                    if (key < keys[idx])
                    {
                        if (tmpkey == DefaultKey || tmpkey > keys[idx])
                        {
                            tmpkey = keys[idx];
                            index = idx;
                        }
                    }
                }

                ++idx;
                if (idx == keys.Length)
                    idx = 0;
            }

            if (index == -1)
            {
                throw new ArgumentException("invalid date time");
            }

            var value = values[index];
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
            for (var i = 0; i < keys.Length; ++i)
            {
                if (keys[idx] != DefaultKey)
                {
                    if (key == keys[idx])
                    {
                        index = idx;
                        break;
                    }

                    if (key > keys[idx])
                    {
                        if (tmpkey == DefaultKey || tmpkey < keys[idx])
                        {
                            tmpkey = keys[idx];
                            index = idx;
                        }
                    }
                }

                ++idx;
                if (idx == keys.Length)
                    idx = 0;
            }

            if (index == -1)
            {
                throw new ArgumentException("invalid date time");
            }

            var value = values[index];
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
            while (keys[index] != DefaultKey)
            {
                if (keys[index] == key)
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
                var value = values[index];
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
                writer.Write(keys[i].Ticks);
            }

            for (var i = 0; i < BucketSize; i++)
            {
                var value = values[i];
                writer.Write(value.Offset);
                writer.Write(value.Count);
            }
        }

        public void Read(BinaryReader reader)
        {
            for (var i = 0; i < BucketSize; i++)
            {
                keys[i] = new DateTime(reader.ReadInt64());
            }

            for (var i = 0; i < BucketSize; i++)
            {
                values[i] = new OffsetCount(reader.ReadInt64(), reader.ReadInt32());
            }
        }

        private static int HashKey(DateTime time)
        {
            return time.Day;
        }
    }
}
