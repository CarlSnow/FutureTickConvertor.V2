using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ats.Core;

namespace WpfTest.MqData.Min
{
    public class FileHeader
    {
        private const int FILE_VERSION_SIZE = 8;
        private const int FILE_HEADER_SIZE = 64;
        private const int FILE_HEADER_TOTAL_SIZE = 64 + 32 * 20 + 32 * 20;

        private bool isDirty;
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
            get { return FILE_HEADER_TOTAL_SIZE; }
        }

        public FileHeader()
        {
            TradingDayIndices = new DateTimeToOffsetMap(this);
            NaturalDayIndices = new DateTimeToOffsetMap(this);
        }

        public void MarkAsDirty(bool value)
        {
            isDirty = value;
        }

        public virtual void Write(BinaryWriter writer)
        {
            if (isDirty)
            {
                writer.BaseStream.Seek(0, SeekOrigin.Begin);
                writer.Write(FileVersion);
                writer.BaseStream.Position = FILE_VERSION_SIZE;
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
            reader.BaseStream.Position = FILE_VERSION_SIZE;
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
}
