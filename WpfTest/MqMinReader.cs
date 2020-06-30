using Ats.Core;
using System;
using System.IO;
using System.Text;
using WpfTest.MqData.Min;

namespace WpfTest
{
    public class MqMinReader
    {
        private readonly FileStream fileStream;
        private readonly FileHeader fileHeader;
        private const string FILE_EXTENSION = ".min";
        private readonly bool createNew;
        public string FilePath { get; private set; }
        private bool isHeaderRead;

        public MqMinReader(string filePath)
        {
            FileHeader fileHeader = new FileHeader()
            {
                FileVersion = "MQ 1.0",
                Period = 1,
                Market = EnumMarket.期货,
                BarType = EnumBarType.分钟
            };

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("filePath");
            }

            var ext = Path.GetExtension(filePath).ToLower();
            if (ext != FILE_EXTENSION)
            {
                throw new ArgumentException("invalid file extension");
            }

            createNew = !File.Exists(filePath);

            FilePath = filePath;
            fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

            this.fileHeader = fileHeader;

            if (!createNew)
            {
                if (this.fileHeader == null)
                {
                    this.fileHeader = new FileHeader();
                }

                using (var reader = CreateReader())
                {
                    ReadHeader(reader);
                }
            }
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
        private BinaryReader CreateReader()
        {
            return new BinaryReader(fileStream, new UTF8Encoding(), true);
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

        private void ReadHeader(BinaryReader reader)
        {
            try
            {
                if (!isHeaderRead)
                {
                    fileHeader.Read(reader);
                    if (createNew)
                    {
                        //ValidateHeader(fileHeader);
                    }

                    isHeaderRead = false;
                }
                else
                {
                    reader.BaseStream.Position = fileHeader.FileHeaderSize;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Invalid file header", ex);
            }
        }
    }
}
