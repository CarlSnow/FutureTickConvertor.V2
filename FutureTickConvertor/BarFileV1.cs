using System;
using Ats.Core;

namespace FutureTickConvertor
{
    /// <summary>
    /// Version 1.0 implementation of BarStream.
    /// </summary>
    public class BarFileV1 : BarStream
    {
        private const string FileVersion = "MQ 1.0";
         
        /// <summary>
        /// Open a new bar file.
        /// </summary>
        /// <param name="filePath"></param>
        public BarFileV1(string filePath)
            : base(filePath, null)
        {
        }

        /// <summary>
        /// Create a new bar file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="market"></param>
        /// <param name="barType"></param>
        /// <param name="period"></param>
        public BarFileV1(string filePath, EnumMarket market, EnumBarType barType = EnumBarType.∑÷÷”, int period = 1)
            : base(filePath, new FileHeader
            {
                FileVersion = FileVersion,
                Period = period,
                Market = market,
                BarType = barType
            })
        {
        }

        protected override void ValidateHeader(FileHeader fileHeader)
        {
            if (fileHeader.FileVersion != FileVersion)
            {
                throw new InvalidOperationException("invalid file version");
            }
        }
    }
}