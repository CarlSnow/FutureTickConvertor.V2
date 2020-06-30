using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ats.Core;
using log4net;

namespace FutureTickConvertor
{
    /// <summary>
    /// 期货Tick文件
    /// </summary>
    public sealed class FutureTickFileNew 
    {
        #region 变量

        /// <summary>
        /// 文件头长度
        /// </summary>
        const int CFileheaderLen = 44;
        const int COrigdayLen = 8 * 4;
        const int CTickOffset = CFileheaderLen + COrigdayLen;

        /// <summary>
        /// 每条tick开始到盘口的字节数
        /// </summary>
        const int CPosTicklen = 32;
        const int CFileFlag = (((int)'K') << 24) + (((int)'C') << 16) + (((int)'I') << 8) + (((int)'T'));

        DateTime _tradingDay = default(DateTime);
        List<DateTime> _origDays = new List<DateTime>(4);
        List<int> _origTickOffset = new List<int>(4);

        double _preClosePrice = 0;
        double _preSettlementPrice = 0;
        double _upLimit = 0;
        double _downLimit = 0;
        double _openPrice = 0;
        double _preInterest = 0;

        string _tickFileName = "";

        int _version = 0;
        int _origdays = 1;
        int _origdayoffset = 0;
        int _tickoffset = 0;
        int _quotecount = 0;
        int _tickcount = 0;
        double _multiUnit = 1000;

        string _futureId = "";
        string _exCode = "";
        bool _firstRead = true;
        bool _bValid = false;

        bool _bNewVersion = true;

        EnumMarket _market = EnumMarket.期货;

        ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        #region 属性

        /// <summary>
        /// 交易日
        /// </summary>
        public DateTime TradingDay
        {
            get { return _tradingDay; }
        }

        /// <summary>
        /// 昨天收盘价
        /// </summary>
        public double PreClosePrice
        {
            get { return _preClosePrice; }
        }

        /// <summary>
        /// 涨停价
        /// </summary>
        public double UpLimit
        {
            get { return _upLimit; }
            set { _upLimit = value; }
        }

        /// <summary>
        /// 跌停价
        /// </summary>
        public double DownLimit
        {
            get { return _downLimit; }
            set { _downLimit = value; }
        }

        public bool IsValid
        {
            get { return _bValid; }
        }

        public int TickCount
        {
            get { return _tickcount; }
        }

        #endregion

        #region 数据文件读取

        public FutureTickFileNew()
        {

        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="strFile"></param>
        /// <param name="futureId"></param>
        /// <param name="exCode">exCode是交易所代码</param>
        public void Init(EnumMarket market, string strFile, string futureId, string exCode )
        {
            _tickFileName = strFile;
            _futureId = futureId;
            _exCode = exCode;
            _firstRead = true;
            _bValid = false;
            _tradingDay = default(DateTime);
            _market = market;
        }

        public bool Read(List<Tick> ticklist, DateTime fromtime, DateTime totime)
        {
            if (fromtime == default(DateTime) && totime == default(DateTime))
                return Read(ticklist, 0, int.MaxValue);

            if (totime == default(DateTime))
                totime = DateTime.MaxValue;

            FileStream stream = null;
            try
            {
                if (!File.Exists(_tickFileName))
                {
                    _firstRead = true;
                    _bValid = false;

                    _logger.Error("读取期货tick数据失败(" + _tickFileName + "),文件不存在");

                    return false;
                }

                stream = new FileStream(_tickFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024);
                BinaryReader reader = new BinaryReader(stream);

                if (_firstRead)
                {
                    if (ReadHeader(reader))
                    {
                        stream.Seek(_origdayoffset, SeekOrigin.Begin);
                        ReadOrigDays(reader);
                    }
                    else
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        ReadOldHeader(reader);
                    }

                    _firstRead = false;
                }

                if (_bNewVersion)
                {
                    long pos = _tickoffset;
                    stream.Seek(pos, SeekOrigin.Begin);

                    switch (_quotecount)
                    {
                        case 1: ReadTicks1(ticklist, stream, reader, fromtime, totime); break;
                        case 5: ReadTicks5(ticklist, stream, reader, fromtime, totime); break;
                        default: throw new Exception("不支持" + _quotecount.ToString() + "档盘口");
                    }
                }
                else
                {
                    long pos = CFileheaderlenOld;
                    stream.Seek(pos, SeekOrigin.Begin);

                    switch (_quotecount)
                    {
                        case 1: ReadOldTicks1(ticklist, stream, reader, fromtime, totime); break;
                        case 5: ReadOldTicks5(ticklist, stream, reader, fromtime, totime); break;
                        default: throw new Exception("不支持" + _quotecount.ToString() + "档盘口");
                    }
                }

                stream.Close();

                _bValid = true;

                return true;
            }
            catch (Exception ex)
            {
                _firstRead = true;
                _bValid = false;

                if (stream != null)
                    stream.Close();

                _logger.Error("读取期货tick数据失败(" + _tickFileName + ")", ex);
            }

            return false;
        }

        /// <summary>
        /// 读取Tick
        /// </summary>
        /// <param name="ticklist">从外面传入</param>
        /// <param name="offset">从文件的第offset个Tick读取 默认0</param>
        /// <param name="count">读取多少个 默认int.MaxValue</param>
        /// <returns></returns>
        public bool Read(List<Tick> ticklist, int offset, int count)
        {
            FileStream stream = null;
            try
            {
                if (!File.Exists(_tickFileName))
                {
                    _firstRead = true;
                    _bValid = false;

                    _logger.Error("读取期货tick数据失败(" + _tickFileName + "),文件不存在");

                    return false;
                }

                stream = new FileStream(_tickFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024);
                BinaryReader reader = new BinaryReader(stream);

                if (_firstRead)
                {
                    if (ReadHeader(reader))
                    {
                        stream.Seek(_origdayoffset, SeekOrigin.Begin);
                        ReadOrigDays(reader);
                    }
                    else
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        ReadOldHeader(reader);
                    }

                    _firstRead = false;
                }

                if (_bNewVersion)
                {
                    long pos = _tickoffset + offset * (CPosTicklen + _quotecount * 2 * 8);
                    stream.Seek(pos, SeekOrigin.Begin);

                    switch (_quotecount)
                    {
                        case 1: ReadTicks1(ticklist, reader, offset, count); break;
                        case 5: ReadTicks5(ticklist, reader, offset, count); break;
                        default: throw new Exception("不支持" + _quotecount.ToString() + "档盘口");
                    }
                }
                else
                {
                    long pos = CFileheaderlenOld + offset * (CTickheaderlenOld + _quotecount * 2 * 8);
                    stream.Seek(pos, SeekOrigin.Begin);

                    switch (_quotecount)
                    {
                        case 1: ReadOldTicks1(ticklist, reader, offset, count); break;
                        case 5: ReadOldTicks5(ticklist, reader, offset, count); break;
                        default: throw new Exception("不支持" + _quotecount.ToString() + "档盘口");
                    }
                }

                stream.Close();

                _bValid = true;

                return true;
            }
            catch (Exception ex)
            {
                _firstRead = true;
                _bValid = false;

                if (stream != null)
                    stream.Close();
                    
                _logger.Error("读取期货tick数据失败(" + _tickFileName + ")", ex);
            }

            return false;
        }

        #region 老版本

        const int CReserveOld = 27;
        /// <summary>
        /// 每条tick开始到盘口的字节数
        /// </summary>
        const int CTickheaderlenOld = 32;
        const int CFileheaderlenOld = 64;

        // 18点后
        DateTime _pretradingDay1 = default(DateTime);

        // 9点前
        DateTime _pretradingDay2 = default(DateTime);

        private void ReadOldHeader(BinaryReader reader)
        {
            _origDays.Clear();
            _origTickOffset.Clear();
            _origdays = 0;

            int interval = reader.ReadInt16();
            int bartype = reader.ReadInt16();
            if ((bartype & 0xff) != 0)
                throw new Exception("错误的tick数据文件标识");

            _bNewVersion = false;

            if (((bartype >> 10) & 0x3) == 1)
            {
                _multiUnit = 1000;
            }
            else
            {
                _multiUnit = 100;
            }

            int year = reader.ReadInt16();
            int month = reader.ReadByte();
            int day = reader.ReadByte();

            _tradingDay = new DateTime(year, month, day);
            _pretradingDay1 = TradingDayHelper.GetPreTradingDay(_tradingDay);
            _pretradingDay2 = _pretradingDay1.AddDays(1).Date;

            _preClosePrice = reader.ReadInt32() / _multiUnit;
            _preSettlementPrice = reader.ReadInt32() / _multiUnit;
            _preInterest = reader.ReadInt32();
            _upLimit = reader.ReadInt32() / _multiUnit;
            _downLimit = reader.ReadInt32() / _multiUnit;
            _openPrice = reader.ReadInt32() / _multiUnit;
            _tickcount = reader.ReadInt32();
            _quotecount = reader.ReadByte();

            reader.ReadBytes(CReserveOld);
        }

        private void ReadOldTicks1(List<Tick> ticklist, BinaryReader reader, int offset, int count)
        {
            int len = _tickcount - offset;
            len = (len < count) ? len : count;

            for (int i = 0; i < len; ++i)
            {
                Tick tick = new Tick();
                tick.InstrumentType = _market;
                tick.OpenPrice = _openPrice;
                tick.PreClosePrice = _preClosePrice;
                tick.InstrumentID = _futureId;
                tick.ExchangeID = _exCode;
                tick.PreOpenInterest = _preInterest;
                tick.PreSettlementPrice = _preSettlementPrice;
                tick.UpLimit = _upLimit;
                tick.DropLimit = _downLimit;

                #region

                int hour = reader.ReadByte();
                int min = reader.ReadByte();
                int second = reader.ReadByte();
                int msecond = reader.ReadByte();
                msecond *= 10;

                tick.TradingDay = _tradingDay;
                if (hour < 7)
                {
                    tick.DateTime = _pretradingDay2 + new TimeSpan(0, hour, min, second, msecond);
                }
                else if (hour < 18)
                {
                    tick.DateTime = _tradingDay + new TimeSpan(0, hour, min, second, msecond);
                }
                else
                {
                    tick.DateTime = _pretradingDay1 + new TimeSpan(0, hour, min, second, msecond);
                }
                //tick.DateTime = _tradingDay + new TimeSpan(0, hour, min, second, msecond);

                tick.LastPrice = reader.ReadInt32() / _multiUnit;
                tick.HighPrice = reader.ReadInt32() / _multiUnit;
                tick.LowPrice = reader.ReadInt32() / _multiUnit;
                tick.OpenInterest = reader.ReadInt32();
                tick.Volume = reader.ReadInt32();
                tick.Turnover = reader.ReadDouble();

                tick.Quote.AskVolume1 = reader.ReadInt32();
                tick.Quote.BidVolume1 = reader.ReadInt32();
                tick.Quote.AskPrice1 = reader.ReadInt32() / _multiUnit;
                tick.Quote.BidPrice1 = reader.ReadInt32() / _multiUnit;


                #endregion

                ticklist.Add(tick);
            }
        }

        private void ReadOldTicks5(List<Tick> ticklist, BinaryReader reader, int offset, int count)
        {
            int len = _tickcount - offset;
            len = (len < count) ? len : count;

            for (int i = 0; i < len; ++i)
            {
                Tick tick = new Tick();
                tick.InstrumentType = _market;
                tick.OpenPrice = _openPrice;
                tick.PreClosePrice = _preClosePrice;
                tick.InstrumentID = _futureId;
                tick.ExchangeID = _exCode;
                tick.PreOpenInterest = _preInterest;
                tick.PreSettlementPrice = _preSettlementPrice;
                tick.UpLimit = _upLimit;
                tick.DropLimit = _downLimit;

                #region

                int hour = reader.ReadByte();
                int min = reader.ReadByte();
                int second = reader.ReadByte();
                int msecond = reader.ReadByte();
                msecond *= 10;

                tick.TradingDay = _tradingDay;
                if (hour < 7)
                {
                    tick.DateTime = _pretradingDay2 + new TimeSpan(0, hour, min, second, msecond);
                }
                else if (hour < 18)
                {
                    tick.DateTime = _tradingDay + new TimeSpan(0, hour, min, second, msecond);
                }
                else
                {
                    tick.DateTime = _pretradingDay1 + new TimeSpan(0, hour, min, second, msecond);
                }

                //tick.DateTime = _tradingDay + new TimeSpan(0, hour, min, second, msecond);

                tick.LastPrice = reader.ReadInt32() / _multiUnit;
                tick.HighPrice = reader.ReadInt32() / _multiUnit;
                tick.LowPrice = reader.ReadInt32() / _multiUnit;
                tick.OpenInterest = reader.ReadInt32();
                tick.Volume = reader.ReadInt32();
                tick.Turnover = reader.ReadDouble();

                tick.Quote.AskVolume1 = reader.ReadInt32();
                tick.Quote.AskVolume2 = reader.ReadInt32();
                tick.Quote.AskVolume3 = reader.ReadInt32();
                tick.Quote.AskVolume4 = reader.ReadInt32();
                tick.Quote.AskVolume5 = reader.ReadInt32();
                tick.Quote.BidVolume1 = reader.ReadInt32();
                tick.Quote.BidVolume2 = reader.ReadInt32();
                tick.Quote.BidVolume3 = reader.ReadInt32();
                tick.Quote.BidVolume4 = reader.ReadInt32();
                tick.Quote.BidVolume5 = reader.ReadInt32();

                tick.Quote.AskPrice1 = reader.ReadInt32() / _multiUnit;
                tick.Quote.AskPrice2 = reader.ReadInt32() / _multiUnit;
                tick.Quote.AskPrice3 = reader.ReadInt32() / _multiUnit;
                tick.Quote.AskPrice4 = reader.ReadInt32() / _multiUnit;
                tick.Quote.AskPrice5 = reader.ReadInt32() / _multiUnit;
                tick.Quote.BidPrice1 = reader.ReadInt32() / _multiUnit;
                tick.Quote.BidPrice2 = reader.ReadInt32() / _multiUnit;
                tick.Quote.BidPrice3 = reader.ReadInt32() / _multiUnit;
                tick.Quote.BidPrice4 = reader.ReadInt32() / _multiUnit;
                tick.Quote.BidPrice5 = reader.ReadInt32() / _multiUnit;

                #endregion

                ticklist.Add(tick);
            }
        }

        private void ReadOldTicks1(List<Tick> ticklist, FileStream stream, BinaryReader reader, DateTime fromtime, DateTime totime)
        {
            int blklen = CPosTicklen + _quotecount * 2 * 8 - 4;

            for (int i = 0; i < _tickcount; ++i)
            {
                #region

                int hour = reader.ReadByte();
                int min = reader.ReadByte();
                int second = reader.ReadByte();
                int msecond = reader.ReadByte();
                msecond *= 10;

                DateTime ticktime = default(DateTime);
                if (hour < 7)
                {
                    ticktime = _pretradingDay2 + new TimeSpan(0, hour, min, second, msecond);
                }
                else if (hour < 18)
                {
                    ticktime = _tradingDay + new TimeSpan(0, hour, min, second, msecond);
                }
                else
                {
                    ticktime = _pretradingDay1 + new TimeSpan(0, hour, min, second, msecond);
                }

                if (ticktime > totime)
                    return;

                if (ticktime < fromtime)
                {
                    reader.ReadBytes(blklen);
                    //stream.Seek(blklen, SeekOrigin.Current);
                    continue;
                }
                //tick.DateTime = _tradingDay + new TimeSpan(0, hour, min, second, msecond);

                Tick tick = new Tick();
                tick.InstrumentType = _market;
                tick.OpenPrice = _openPrice;
                tick.PreClosePrice = _preClosePrice;
                tick.InstrumentID = _futureId;
                tick.ExchangeID = _exCode;
                tick.PreOpenInterest = _preInterest;
                tick.PreSettlementPrice = _preSettlementPrice;
                tick.UpLimit = _upLimit;
                tick.DropLimit = _downLimit;
                tick.TradingDay = _tradingDay;
                tick.DateTime = ticktime;

                tick.LastPrice = reader.ReadInt32() / _multiUnit;
                tick.HighPrice = reader.ReadInt32() / _multiUnit;
                tick.LowPrice = reader.ReadInt32() / _multiUnit;
                tick.OpenInterest = reader.ReadInt32();
                tick.Volume = reader.ReadInt32();
                tick.Turnover = reader.ReadDouble();

                tick.Quote.AskVolume1 = reader.ReadInt32();
                tick.Quote.BidVolume1 = reader.ReadInt32();
                tick.Quote.AskPrice1 = reader.ReadInt32() / _multiUnit;
                tick.Quote.BidPrice1 = reader.ReadInt32() / _multiUnit;


                #endregion

                ticklist.Add(tick);
            }
        }

        private void ReadOldTicks5(List<Tick> ticklist, FileStream stream, BinaryReader reader, DateTime fromtime, DateTime totime)
        {
            int blklen = CPosTicklen + _quotecount * 2 * 8 - 4;

            for (int i = 0; i < _tickcount; ++i)
            {
                int hour = reader.ReadByte();
                int min = reader.ReadByte();
                int second = reader.ReadByte();
                int msecond = reader.ReadByte();
                msecond *= 10;

                DateTime ticktime = default(DateTime);
                if (hour < 7)
                {
                    ticktime = _pretradingDay2 + new TimeSpan(0, hour, min, second, msecond);
                }
                else if (hour < 18)
                {
                    ticktime = _tradingDay + new TimeSpan(0, hour, min, second, msecond);
                }
                else
                {
                    ticktime = _pretradingDay1 + new TimeSpan(0, hour, min, second, msecond);
                }

                if (ticktime > totime)
                    return;

                if (ticktime < fromtime)
                {
                    reader.ReadBytes(blklen);
                    //stream.Seek(blklen, SeekOrigin.Current);
                    continue;
                }

                Tick tick = new Tick();
                tick.InstrumentType = _market;
                tick.OpenPrice = _openPrice;
                tick.PreClosePrice = _preClosePrice;
                tick.InstrumentID = _futureId;
                tick.ExchangeID = _exCode;
                tick.PreOpenInterest = _preInterest;
                tick.PreSettlementPrice = _preSettlementPrice;
                tick.UpLimit = _upLimit;
                tick.DropLimit = _downLimit;

                #region

                tick.LastPrice = reader.ReadInt32() / _multiUnit;
                tick.HighPrice = reader.ReadInt32() / _multiUnit;
                tick.LowPrice = reader.ReadInt32() / _multiUnit;
                tick.OpenInterest = reader.ReadInt32();
                tick.Volume = reader.ReadInt32();
                tick.Turnover = reader.ReadDouble();
                tick.TradingDay = _tradingDay;
                tick.DateTime = ticktime;

                tick.Quote.AskVolume1 = reader.ReadInt32();
                tick.Quote.AskVolume2 = reader.ReadInt32();
                tick.Quote.AskVolume3 = reader.ReadInt32();
                tick.Quote.AskVolume4 = reader.ReadInt32();
                tick.Quote.AskVolume5 = reader.ReadInt32();
                tick.Quote.BidVolume1 = reader.ReadInt32();
                tick.Quote.BidVolume2 = reader.ReadInt32();
                tick.Quote.BidVolume3 = reader.ReadInt32();
                tick.Quote.BidVolume4 = reader.ReadInt32();
                tick.Quote.BidVolume5 = reader.ReadInt32();

                tick.Quote.AskPrice1 = reader.ReadInt32() / _multiUnit;
                tick.Quote.AskPrice2 = reader.ReadInt32() / _multiUnit;
                tick.Quote.AskPrice3 = reader.ReadInt32() / _multiUnit;
                tick.Quote.AskPrice4 = reader.ReadInt32() / _multiUnit;
                tick.Quote.AskPrice5 = reader.ReadInt32() / _multiUnit;
                tick.Quote.BidPrice1 = reader.ReadInt32() / _multiUnit;
                tick.Quote.BidPrice2 = reader.ReadInt32() / _multiUnit;
                tick.Quote.BidPrice3 = reader.ReadInt32() / _multiUnit;
                tick.Quote.BidPrice4 = reader.ReadInt32() / _multiUnit;
                tick.Quote.BidPrice5 = reader.ReadInt32() / _multiUnit;

                #endregion

                ticklist.Add(tick);
            }
        }

        #endregion

        /// <summary>
        /// 盘中发现openPrice有变化，修改开盘价
        /// </summary>
        /// <param name="ticklist"></param>
        /// <param name="writer"></param>
        private void UpdateTickOpenPrice(List<Tick> ticklist, BinaryWriter writer)
        {
            if (ticklist.Count > 0)
            {
                double openPrice = ticklist[0].OpenPrice;
                if (Math.Abs(openPrice - _openPrice) >= 0.0000001)
                {
                    writer.Seek(32, SeekOrigin.Begin);
                    if (openPrice < (double)(decimal.MaxValue))
                    {
                        decimal dvalue = (decimal)(openPrice);
                        writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
                        _firstRead = true;
                    }
                    else
                    {
                        _logger.Error("openPrice过大，出现异常" + openPrice);
                    }
                }
            }
        }

        /// <summary>
        /// 修复郑商所历史tick的openprice,把lastPrice赋值给openPrice
        /// </summary>
        /// <param name="ticklist"></param>
        /// <param name="writer"></param>
        public void FixTickOpenPrice(double lastPrice)
        {
             FileStream stream = null;
            try
            {
                double openPrice = lastPrice;
                stream = new FileStream(_tickFileName, FileMode.Open, FileAccess.ReadWrite, FileShare.Read,
                    1024*1024);
                BinaryWriter writer = new BinaryWriter(stream);

                writer.Seek(32, SeekOrigin.Begin);
                if (openPrice < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(openPrice);
                    writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    _logger.Error("openPrice过大，出现异常" + openPrice);
                }

                writer.Flush();
                writer.Close();
                stream.Close();
            }
            catch (Exception ex)
            {
                if (stream != null)
                    stream.Close();

                _logger.Error("写入期货tick数据失败(" + _tickFileName + ")", ex);
            }
        }

        private bool ReadHeader(BinaryReader reader)
        {
            int flag = reader.ReadInt32();
            if (flag != CFileFlag)
            {
                _bNewVersion = false;
                return false;
            }

            _bNewVersion = true;

            _origDays.Clear();
            _origTickOffset.Clear();
            _origdays = 0;

            _version = reader.ReadInt16();
            _quotecount = reader.ReadByte();

            int itmp = 1;
            int imult = reader.ReadByte();
            for (int i = 0; i < imult; ++i)
                itmp = itmp * 10;

            _multiUnit = itmp;

            int year = reader.ReadInt16();
            int month = reader.ReadByte();
            int day = reader.ReadByte();

            _tradingDay = new DateTime(year, month, day);

            _preClosePrice = reader.ReadInt32() / _multiUnit;
            _preSettlementPrice = reader.ReadInt32() / _multiUnit;
            _preInterest = reader.ReadInt32();
            _upLimit = reader.ReadInt32() / _multiUnit;
            _downLimit = reader.ReadInt32() / _multiUnit;
            _openPrice = reader.ReadInt32() / _multiUnit;
            _tickcount = reader.ReadInt32();

            int iorig = reader.ReadInt16();
            _origdays = (iorig >> 12);
            _origdayoffset = (iorig & 0x0fff);

            _tickoffset = reader.ReadInt16();

            return true;
        }

        private void ReadOrigDays(BinaryReader reader)
        {
            for(int i = 0; i < _origdays; ++i)
            {
                int year = reader.ReadInt16();
                int month = reader.ReadByte();
                int day = reader.ReadByte();
                int origtickoffset = reader.ReadInt32();

                DateTime origday = new DateTime(year, month, day);

                _origDays.Add(origday);
                _origTickOffset.Add(origtickoffset);
            }
        }


        private void ReadTicks1(List<Tick> ticklist, BinaryReader reader, int offset, int count)
        {
            int origidx = 0;
            for (; origidx < _origTickOffset.Count; ++origidx)
            {
                if (offset < _origTickOffset[origidx])
                {
                    break;
                }
            }

            --origidx;
            if (origidx < 0)
                return;

            DateTime origday = _origDays[origidx];
            int nextorigoffset = int.MaxValue;
            if (origidx < _origTickOffset.Count - 1)
            {
                nextorigoffset = _origTickOffset[origidx + 1] - 1;
            }

            int len = _tickcount - offset;
            len = (len < count) ? len : count;

            for (int i = 0; i < len; ++i)
            {
                Tick tick = new Tick();
                tick.InstrumentType = _market;
                tick.OpenPrice = _openPrice;
                tick.PreClosePrice = _preClosePrice;
                tick.InstrumentID = _futureId;
                tick.ExchangeID = _exCode;
                tick.PreOpenInterest = _preInterest;
                tick.PreSettlementPrice = _preSettlementPrice;
                tick.UpLimit = _upLimit;
                tick.DropLimit = _downLimit;

                #region

                int hour = reader.ReadByte();
                int min = reader.ReadByte();
                int second = reader.ReadByte();
                int msecond = reader.ReadByte();
                msecond *= 10;

                tick.DateTime = origday + new TimeSpan(0, hour, min, second, msecond);
                tick.TradingDay = _tradingDay;
                //tick.DateTime = _tradingDay + new TimeSpan(0, hour, min, second, msecond);

                tick.LastPrice = reader.ReadInt32() / _multiUnit;
                tick.HighPrice = reader.ReadInt32() / _multiUnit;
                tick.LowPrice = reader.ReadInt32() / _multiUnit;
                tick.OpenInterest = reader.ReadInt32();
                tick.Volume = reader.ReadInt32();
                tick.Turnover = reader.ReadDouble();

                tick.Quote.AskVolume1 = reader.ReadInt32();
                tick.Quote.BidVolume1 = reader.ReadInt32();
                tick.Quote.AskPrice1 = reader.ReadInt32() / _multiUnit;
                tick.Quote.BidPrice1 = reader.ReadInt32() / _multiUnit;


                #endregion

                ticklist.Add(tick);

                if (i + offset >= nextorigoffset)
                {
                    ++origidx;

                    if (origidx < _origDays.Count)
                        origday = _origDays[origidx];

                    nextorigoffset = int.MaxValue;
                    if (origidx < _origTickOffset.Count - 1)
                    {
                        nextorigoffset = _origTickOffset[origidx + 1] - 1;
                    }
                }
            }
        }

        private void ReadTicks5(List<Tick> ticklist, BinaryReader reader, int offset, int count)
        {
            int origidx = 0;
            for (; origidx < _origTickOffset.Count; ++origidx)
            {
                if (offset < _origTickOffset[origidx])
                {
                    break;
                }
            }

            --origidx;
            if (origidx < 0)
                return;

            DateTime origday = _origDays[origidx];
            int nextorigoffset = int.MaxValue;
            if (origidx < _origTickOffset.Count - 1)
            {
                nextorigoffset = _origTickOffset[origidx + 1] - 1;
            }

            int len = _tickcount - offset;
            len = (len < count) ? len : count;

            for (int i = 0; i < len; ++i)
            {
                Tick tick = new Tick();
                tick.InstrumentType = _market;
                tick.OpenPrice = _openPrice;
                tick.PreClosePrice = _preClosePrice;
                tick.InstrumentID = _futureId;
                tick.ExchangeID = _exCode;
                tick.PreOpenInterest = _preInterest;
                tick.PreSettlementPrice = _preSettlementPrice;
                tick.UpLimit = _upLimit;
                tick.DropLimit = _downLimit;

                #region

                int hour = reader.ReadByte();
                int min = reader.ReadByte();
                int second = reader.ReadByte();
                int msecond = reader.ReadByte();
                msecond *= 10;

                tick.TradingDay = _tradingDay;
                tick.DateTime = origday + new TimeSpan(0, hour, min, second, msecond);
                //tick.DateTime = _tradingDay + new TimeSpan(0, hour, min, second, msecond);

                tick.LastPrice = reader.ReadInt32() / _multiUnit;
                tick.HighPrice = reader.ReadInt32() / _multiUnit;
                tick.LowPrice = reader.ReadInt32() / _multiUnit;
                tick.OpenInterest = reader.ReadInt32();
                tick.Volume = reader.ReadInt32();
                tick.Turnover = reader.ReadDouble();

                tick.Quote.AskVolume1 = reader.ReadInt32();
                tick.Quote.AskVolume2 = reader.ReadInt32();
                tick.Quote.AskVolume3 = reader.ReadInt32();
                tick.Quote.AskVolume4 = reader.ReadInt32();
                tick.Quote.AskVolume5 = reader.ReadInt32();
                tick.Quote.BidVolume1 = reader.ReadInt32();
                tick.Quote.BidVolume2 = reader.ReadInt32();
                tick.Quote.BidVolume3 = reader.ReadInt32();
                tick.Quote.BidVolume4 = reader.ReadInt32();
                tick.Quote.BidVolume5 = reader.ReadInt32();

                tick.Quote.AskPrice1 = reader.ReadInt32() / _multiUnit;
                tick.Quote.AskPrice2 = reader.ReadInt32() / _multiUnit;
                tick.Quote.AskPrice3 = reader.ReadInt32() / _multiUnit;
                tick.Quote.AskPrice4 = reader.ReadInt32() / _multiUnit;
                tick.Quote.AskPrice5 = reader.ReadInt32() / _multiUnit;
                tick.Quote.BidPrice1 = reader.ReadInt32() / _multiUnit;
                tick.Quote.BidPrice2 = reader.ReadInt32() / _multiUnit;
                tick.Quote.BidPrice3 = reader.ReadInt32() / _multiUnit;
                tick.Quote.BidPrice4 = reader.ReadInt32() / _multiUnit;
                tick.Quote.BidPrice5 = reader.ReadInt32() / _multiUnit;
 
                #endregion

                ticklist.Add(tick);

                if (i + offset >= nextorigoffset)
                {
                    ++origidx;

                    if (origidx < _origDays.Count)
                        origday = _origDays[origidx];

                    nextorigoffset = int.MaxValue;
                    if (origidx < _origTickOffset.Count - 1)
                    {
                        nextorigoffset = _origTickOffset[origidx + 1] - 1;
                    }
                }
            }
        }

        private void ReadTicks1(List<Tick> ticklist, FileStream stream, BinaryReader reader, DateTime fromtime, DateTime totime)
        {
            if (_origDays.Count == 0)
                return;

            int origidx = 0;
            DateTime origday = _origDays[origidx];
            int nextorigoffset = int.MaxValue;
            if (origidx < _origTickOffset.Count - 1)
            {
                nextorigoffset = _origTickOffset[origidx + 1] - 1;
            }

            int blklen = CPosTicklen + _quotecount * 2 * 8 - 4;

            for (int i = 0; i < _tickcount; ++i)
            {
                

                #region

                int hour = reader.ReadByte();
                int min = reader.ReadByte();
                int second = reader.ReadByte();
                int msecond = reader.ReadByte();
                msecond *= 10;

                DateTime ticktime = origday + new TimeSpan(0, hour, min, second, msecond);
                if (ticktime > totime)
                    return;

                if (ticktime < fromtime)
                {
                    if (i >= nextorigoffset)
                    {
                        ++origidx;

                        if (origidx < _origDays.Count)
                            origday = _origDays[origidx];

                        nextorigoffset = int.MaxValue;
                        if (origidx < _origTickOffset.Count - 1)
                        {
                            nextorigoffset = _origTickOffset[origidx + 1] - 1;
                        }
                    }

                    reader.ReadBytes(blklen);
                    //stream.Seek(blklen, SeekOrigin.Current);
                    continue;
                }

                Tick tick = new Tick();
                tick.InstrumentType = _market;
                tick.OpenPrice = _openPrice;
                tick.PreClosePrice = _preClosePrice;
                tick.InstrumentID = _futureId;
                tick.ExchangeID = _exCode;
                tick.PreOpenInterest = _preInterest;
                tick.PreSettlementPrice = _preSettlementPrice;
                tick.UpLimit = _upLimit;
                tick.DropLimit = _downLimit;

                tick.DateTime = ticktime;
                tick.TradingDay = _tradingDay;
                //tick.DateTime = _tradingDay + new TimeSpan(0, hour, min, second, msecond);

                tick.LastPrice = reader.ReadInt32() / _multiUnit;
                tick.HighPrice = reader.ReadInt32() / _multiUnit;
                tick.LowPrice = reader.ReadInt32() / _multiUnit;
                tick.OpenInterest = reader.ReadInt32();
                tick.Volume = reader.ReadInt32();
                tick.Turnover = reader.ReadDouble();

                tick.Quote.AskVolume1 = reader.ReadInt32();
                tick.Quote.BidVolume1 = reader.ReadInt32();
                tick.Quote.AskPrice1 = reader.ReadInt32() / _multiUnit;
                tick.Quote.BidPrice1 = reader.ReadInt32() / _multiUnit;


                #endregion

                ticklist.Add(tick);

                if (i >= nextorigoffset)
                {
                    ++origidx;

                    if (origidx < _origDays.Count)
                        origday = _origDays[origidx];

                    nextorigoffset = int.MaxValue;
                    if (origidx < _origTickOffset.Count - 1)
                    {
                        nextorigoffset = _origTickOffset[origidx + 1] - 1;
                    }
                }
            }
        }

        private void ReadTicks5(List<Tick> ticklist, FileStream stream, BinaryReader reader, DateTime fromtime, DateTime totime)
        {
            if (_origDays.Count == 0)
                return;

            int origidx = 0;
            DateTime origday = _origDays[origidx];
            int nextorigoffset = int.MaxValue;
            if (origidx < _origTickOffset.Count - 1)
            {
                nextorigoffset = _origTickOffset[origidx + 1] - 1;
            }

            int blklen = CPosTicklen + _quotecount * 2 * 8 - 4;

            for (int i = 0; i < _tickcount; ++i)
            {
                

                #region

                int hour = reader.ReadByte();
                int min = reader.ReadByte();
                int second = reader.ReadByte();
                int msecond = reader.ReadByte();
                msecond *= 10;

                DateTime ticktime = origday + new TimeSpan(0, hour, min, second, msecond);
                if (ticktime > totime)
                    return;

                if (ticktime < fromtime)
                {
                    if (i >= nextorigoffset)
                    {
                        ++origidx;

                        if (origidx < _origDays.Count)
                            origday = _origDays[origidx];

                        nextorigoffset = int.MaxValue;
                        if (origidx < _origTickOffset.Count - 1)
                        {
                            nextorigoffset = _origTickOffset[origidx + 1] - 1;
                        }
                    }

                    reader.ReadBytes(blklen);
                    //stream.Seek(blklen, SeekOrigin.Current);
                    continue;
                }

                Tick tick = new Tick();
                tick.InstrumentType = _market;
                tick.OpenPrice = _openPrice;
                tick.PreClosePrice = _preClosePrice;
                tick.InstrumentID = _futureId;
                tick.ExchangeID = _exCode;
                tick.PreOpenInterest = _preInterest;
                tick.PreSettlementPrice = _preSettlementPrice;
                tick.UpLimit = _upLimit;
                tick.DropLimit = _downLimit;

                tick.TradingDay = _tradingDay;
                tick.DateTime = ticktime;
                //tick.DateTime = _tradingDay + new TimeSpan(0, hour, min, second, msecond);

                tick.LastPrice = reader.ReadInt32() / _multiUnit;
                tick.HighPrice = reader.ReadInt32() / _multiUnit;
                tick.LowPrice = reader.ReadInt32() / _multiUnit;
                tick.OpenInterest = reader.ReadInt32();
                tick.Volume = reader.ReadInt32();
                tick.Turnover = reader.ReadDouble();

                tick.Quote.AskVolume1 = reader.ReadInt32();
                tick.Quote.AskVolume2 = reader.ReadInt32();
                tick.Quote.AskVolume3 = reader.ReadInt32();
                tick.Quote.AskVolume4 = reader.ReadInt32();
                tick.Quote.AskVolume5 = reader.ReadInt32();
                tick.Quote.BidVolume1 = reader.ReadInt32();
                tick.Quote.BidVolume2 = reader.ReadInt32();
                tick.Quote.BidVolume3 = reader.ReadInt32();
                tick.Quote.BidVolume4 = reader.ReadInt32();
                tick.Quote.BidVolume5 = reader.ReadInt32();

                tick.Quote.AskPrice1 = reader.ReadInt32() / _multiUnit;
                tick.Quote.AskPrice2 = reader.ReadInt32() / _multiUnit;
                tick.Quote.AskPrice3 = reader.ReadInt32() / _multiUnit;
                tick.Quote.AskPrice4 = reader.ReadInt32() / _multiUnit;
                tick.Quote.AskPrice5 = reader.ReadInt32() / _multiUnit;
                tick.Quote.BidPrice1 = reader.ReadInt32() / _multiUnit;
                tick.Quote.BidPrice2 = reader.ReadInt32() / _multiUnit;
                tick.Quote.BidPrice3 = reader.ReadInt32() / _multiUnit;
                tick.Quote.BidPrice4 = reader.ReadInt32() / _multiUnit;
                tick.Quote.BidPrice5 = reader.ReadInt32() / _multiUnit;

                #endregion

                ticklist.Add(tick);

                if (i >= nextorigoffset)
                {
                    ++origidx;

                    if (origidx < _origDays.Count)
                        origday = _origDays[origidx];

                    nextorigoffset = int.MaxValue;
                    if (origidx < _origTickOffset.Count - 1)
                    {
                        nextorigoffset = _origTickOffset[origidx + 1] - 1;
                    }
                }
            }
        }


        public Tick GetTick(int idx)
        {
            try
            {
                List<Tick> ticklist = new List<Tick>();
                if (Read(ticklist, idx, 1))
                {
                    return ticklist[0];
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        #endregion

        #region 覆盖写入文件

        //public bool Write(List<Tick> ticklist)
        //{
        //    //??? 直接改成1 期货只有1档
        //    return Write(ticklist, 0, ticklist.Count, 1);
        //}

        public bool Write(List<Tick> ticklist, int quotecount)
        {
            return Write(ticklist, 0, ticklist.Count, quotecount);
        }

        public bool Write(List<Tick> ticklist, int start, int len, int quotecount)
        {
            if (ticklist.Count == 0)
                return true;

            FileStream stream = null;
            try
            {
                _origdays = 0;
                _origDays.Clear();
                _origTickOffset.Clear();

                _multiUnit = 1000;

                _tradingDay = ticklist[0].TradingDay;
                _preClosePrice = ticklist[0].PreClosePrice;
                _openPrice = ticklist[0].OpenPrice;
                _preInterest = ticklist[0].PreOpenInterest;
                _preSettlementPrice = ticklist[0].PreSettlementPrice;
                _upLimit = ticklist[0].UpLimit;
                _downLimit = ticklist[0].DropLimit;
                _quotecount = quotecount;

                int count = ticklist.Count - start;
                count = (len < count) ? len : count;
                _tickcount = 0;

                stream = new FileStream(_tickFileName, FileMode.Create, FileAccess.Write, FileShare.Read, 1024 * 1024);
                BinaryWriter writer = new BinaryWriter(stream);

                WriteHeader(writer);

                switch (_quotecount)
                {
                    case 1: WriteTicks1(ticklist, writer, start, count); break;
                    case 5: WriteTicks5(ticklist, writer, start, count); break;
                    default: throw new Exception("不支持" + _quotecount.ToString() + "档盘口");
                }

                stream.Seek(CFileheaderLen - 8, SeekOrigin.Begin);
                WriteOrigDays(writer);

                writer.Flush();
                stream.Close();

                return true;
            }
            catch (Exception ex)
            {
                if (stream != null)
                    stream.Close();

                _logger.Error("写入期货tick数据失败(" + _tickFileName + ")", ex);
            }

            return false;
        }

        private void WriteHeader(BinaryWriter writer)
        {
            writer.Write(CFileFlag);
            writer.Write((Int16)_version);
            writer.Write((byte)_quotecount);

            int imult = 0;
            int itmp = (int)_multiUnit;
            while (itmp != 0)
            {
                itmp = (itmp / 10);
                ++imult;
            }

            --imult;
            if (imult < 0)
                throw new Exception("写入乘数出错");

            writer.Write((byte)imult);

            writer.Write((Int16)(_tradingDay.Year));
            writer.Write((byte)(_tradingDay.Month));
            writer.Write((byte)(_tradingDay.Day));

            if (_preClosePrice < (double)(decimal.MaxValue))
            {
                decimal dvalue = (decimal)(_preClosePrice);
                writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
            }
            else
            {
                writer.Write((Int32)(0));
            }

            if (_preSettlementPrice < (double)(decimal.MaxValue))
            {
                decimal dvalue = (decimal)(_preSettlementPrice);
                writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
            }
            else
            {
                writer.Write((Int32)(0));
            }

            if (_preInterest < (double)(decimal.MaxValue))
            {
                decimal dvalue = (decimal)(_preInterest);
                writer.Write((Int32)(Math.Round(dvalue, 0, MidpointRounding.AwayFromZero)));
            }
            else
            {
                writer.Write((Int32)(0));
            }

            if (_upLimit < (double)(decimal.MaxValue))
            {
                decimal dvalue = (decimal)(_upLimit);
                writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
            }
            else
            {
                writer.Write((Int32)(0));
            }

            if (_downLimit < (double)(decimal.MaxValue))
            {
                decimal dvalue = (decimal)(_downLimit);
                writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
            }
            else
            {
                writer.Write((Int32)(0));
            }

            if (_openPrice < (double)(decimal.MaxValue))
            {
                decimal dvalue = (decimal)(_openPrice);
                writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
            }
            else
            {
                writer.Write((Int32)(0));
            }

            byte[] buffer = new byte[COrigdayLen + 8];
            writer.Write(buffer);
        }

        private void WriteOrigDays(BinaryWriter writer)
        {
            writer.Write((Int32)(_tickcount));

            int itmp = (_origDays.Count << 12) + CFileheaderLen;
            writer.Write((Int16)itmp);
            writer.Write((Int16)CTickOffset);

            for(int i = 0; i < _origDays.Count; ++i)
            {
                DateTime origday = _origDays[i];
                int origtickoffset = _origTickOffset[i];

                writer.Write((short)origday.Year);
                writer.Write((byte)origday.Month);
                writer.Write((byte)origday.Day);
                writer.Write(origtickoffset);
            }
        }

        private void WriteTicks1(List<Tick> ticklist, BinaryWriter writer, int offset, int count)
        {
            DateTime origday = default(DateTime);
            int origtickoffset = 0;
            if (_origDays.Count > 0)
            {
                origday = _origDays[_origDays.Count - 1];
                origtickoffset = _origTickOffset[_origTickOffset.Count - 1];
            }

            int end = offset + count;
            for (int i = offset; i < end; ++i)
            {
                Tick tick = ticklist[i];

                if (tick.DateTime.Date != origday)
                {
                    if (_origDays.Count > 0)
                    {
                        if (tick.DateTime.Date < _origDays[_origDays.Count - 1])
                        {
                            _logger.Error("tick时序不对,tick日期=" + tick.DateTime.ToString("yyyyMMdd") + "文件日期=" + _origDays[_origDays.Count - 1].ToString("yyyyMMdd"));
                            continue;
                        }
                        else if (tick.DateTime.Date == _origDays[_origDays.Count - 1])
                        {
                            origday = _origDays[_origDays.Count - 1];
                            origtickoffset = _origTickOffset[_origTickOffset.Count - 1];
                        }
                        else
                        {
                            origday = tick.DateTime.Date;
                            origtickoffset = _tickcount;

                            _origDays.Add(origday);
                            _origTickOffset.Add(origtickoffset);
                        }
                    }
                    else
                    {
                        origday = tick.DateTime.Date;
                        origtickoffset = _tickcount;

                        _origDays.Add(origday);
                        _origTickOffset.Add(origtickoffset);
                    }
                }

                #region

                writer.Write((byte)(tick.DateTime.Hour));
                writer.Write((byte)(tick.DateTime.Minute));
                writer.Write((byte)(tick.DateTime.Second));
                writer.Write((byte)(tick.DateTime.Millisecond / 10));

                if (tick.LastPrice < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(tick.LastPrice);
                    writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }

                if (tick.HighPrice < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(tick.HighPrice);
                    writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }

                if (tick.LowPrice < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(tick.LowPrice);
                    writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }

                if (tick.OpenInterest < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(tick.OpenInterest);
                    writer.Write((Int32)(Math.Round(dvalue, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }

                writer.Write((Int32)(tick.Volume));
                writer.Write(tick.Turnover);

                writer.Write((Int32)(tick.Quote.AskVolume1));
                //writer.Write((Int32)(tick.Quote.AskVolume2));
                //writer.Write((Int32)(tick.Quote.AskVolume3));
                //writer.Write((Int32)(tick.Quote.AskVolume4));
                //writer.Write((Int32)(tick.Quote.AskVolume5));
                writer.Write((Int32)(tick.Quote.BidVolume1));
                //writer.Write((Int32)(tick.Quote.BidVolume2));
                //writer.Write((Int32)(tick.Quote.BidVolume3));
                //writer.Write((Int32)(tick.Quote.BidVolume4));
                //writer.Write((Int32)(tick.Quote.BidVolume5));

                if (tick.AskPrice1 < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(tick.Quote.AskPrice1);
                    writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }

                
                //dvalue = (decimal)(tick.Quote.AskPrice2);
                //writer.Write((Int32)(Math.Round(dvalue * 100, 0, MidpointRounding.AwayFromZero)));
                //dvalue = (decimal)(tick.Quote.AskPrice3);
                //writer.Write((Int32)(Math.Round(dvalue * 100, 0, MidpointRounding.AwayFromZero)));
                //dvalue = (decimal)(tick.Quote.AskPrice4);
                //writer.Write((Int32)(Math.Round(dvalue * 100, 0, MidpointRounding.AwayFromZero)));
                //dvalue = (decimal)(tick.Quote.AskPrice5);
                //writer.Write((Int32)(Math.Round(dvalue * 100, 0, MidpointRounding.AwayFromZero)));

                if (tick.BidPrice1 < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(tick.Quote.BidPrice1);
                    writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }
                
                //dvalue = (decimal)(tick.Quote.BidPrice2);
                //writer.Write((Int32)(Math.Round(dvalue * 100, 0, MidpointRounding.AwayFromZero)));
                //dvalue = (decimal)(tick.Quote.BidPrice3);
                //writer.Write((Int32)(Math.Round(dvalue * 100, 0, MidpointRounding.AwayFromZero)));
                //dvalue = (decimal)(tick.Quote.BidPrice4);
                //writer.Write((Int32)(Math.Round(dvalue * 100, 0, MidpointRounding.AwayFromZero)));
                //dvalue = (decimal)(tick.Quote.BidPrice5);
                //writer.Write((Int32)(Math.Round(dvalue * 100, 0, MidpointRounding.AwayFromZero)));

                ++_tickcount;

                #endregion
            }
        }

        private void WriteTicks5(List<Tick> ticklist, BinaryWriter writer, int offset, int count)
        {
            DateTime origday = default(DateTime);
            int origtickoffset = 0;
            if (_origDays.Count > 0)
            {
                origday = _origDays[_origDays.Count - 1];
                origtickoffset = _origTickOffset[_origTickOffset.Count - 1];
            }

            int end = offset + count;
            for (int i = offset; i < end; ++i)
            {
                Tick tick = ticklist[i];

                if (tick.DateTime.Date != origday)
                {
                    if (_origDays.Count > 0)
                    {
                        if (tick.DateTime.Date < _origDays[_origDays.Count - 1])
                        {
                            _logger.Error("tick时序不对,tick日期=" + tick.DateTime.ToString("yyyyMMdd") + "文件日期=" + _origDays[_origDays.Count - 1].ToString("yyyyMMdd"));
                            continue;
                        }
                        else if (tick.DateTime.Date == _origDays[_origDays.Count - 1])
                        {
                            origday = _origDays[_origDays.Count - 1];
                            origtickoffset = _origTickOffset[_origTickOffset.Count - 1];
                        }
                        else
                        {
                            origday = tick.DateTime.Date;
                            origtickoffset = _tickcount;

                            _origDays.Add(origday);
                            _origTickOffset.Add(origtickoffset);
                        }
                    }
                    else
                    {
                        origday = tick.DateTime.Date;
                        origtickoffset = _tickcount;

                        _origDays.Add(origday);
                        _origTickOffset.Add(origtickoffset);
                    }
                }

                #region

                writer.Write((byte)(tick.DateTime.Hour));
                writer.Write((byte)(tick.DateTime.Minute));
                writer.Write((byte)(tick.DateTime.Second));
                writer.Write((byte)(tick.DateTime.Millisecond / 10));

                if (tick.LastPrice < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(tick.LastPrice);
                    writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }

                if (tick.HighPrice < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(tick.HighPrice);
                    writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }

                if (tick.LowPrice < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(tick.LowPrice);
                    writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }

                if (tick.OpenInterest < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(tick.OpenInterest);
                    writer.Write((Int32)(Math.Round(dvalue, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }

                writer.Write((Int32)(tick.Volume));
                writer.Write(tick.Turnover);

                writer.Write((Int32)(tick.Quote.AskVolume1));
                writer.Write((Int32)(tick.Quote.AskVolume2));
                writer.Write((Int32)(tick.Quote.AskVolume3));
                writer.Write((Int32)(tick.Quote.AskVolume4));
                writer.Write((Int32)(tick.Quote.AskVolume5));
                writer.Write((Int32)(tick.Quote.BidVolume1));
                writer.Write((Int32)(tick.Quote.BidVolume2));
                writer.Write((Int32)(tick.Quote.BidVolume3));
                writer.Write((Int32)(tick.Quote.BidVolume4));
                writer.Write((Int32)(tick.Quote.BidVolume5));

                if (tick.AskPrice1 < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(tick.Quote.AskPrice1);
                    writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }


                if (tick.Quote.AskPrice2 < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(tick.Quote.AskPrice2);
                    writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }

                if (tick.Quote.AskPrice3 < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(tick.Quote.AskPrice3);
                    writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }

                if (tick.Quote.AskPrice4 < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(tick.Quote.AskPrice4);
                    writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }

                if (tick.Quote.AskPrice5 < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(tick.Quote.AskPrice5);
                    writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }

                if (tick.Quote.BidPrice1 < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(tick.Quote.BidPrice1);
                    writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }

                if (tick.Quote.BidPrice2 < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(tick.Quote.BidPrice2);
                    writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }

                if (tick.Quote.BidPrice3 < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(tick.Quote.BidPrice3);
                    writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }

                if (tick.Quote.BidPrice4 < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(tick.Quote.BidPrice4);
                    writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }

                if (tick.Quote.BidPrice5 < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(tick.Quote.BidPrice5);
                    writer.Write((Int32)(Math.Round(dvalue * (decimal)_multiUnit, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }

                ++_tickcount;

                #endregion
            }
        }

        #endregion

        #region 追加写入

        //public bool Append(List<Tick> ticklist)
        //{
        //    ///改成1档
        //    return Append(ticklist, 0, ticklist.Count, 1);
        //}

        public bool Append(List<Tick> ticklist, int quotecount)
        {
            return Append(ticklist, 0, ticklist.Count, quotecount);
        }

        public bool Append(List<Tick> ticklist, int start, int len, int quotecount)
        {
            FileInfo finfo = new FileInfo(_tickFileName);
            if (!finfo.Exists)
            {
                _logger.Info("追加期货tick,文件不存在,覆盖(" + _tickFileName + ")");
                return Write(ticklist, start, len, quotecount);
            }
            
            if (finfo.Length < CFileheaderLen + COrigdayLen)
            {
                _logger.Info("追加期货tick,文件大小不对,覆盖(" + _tickFileName + ")");
                return Write(ticklist, start, len, quotecount);
            }

            if (((finfo.Length - CTickOffset)%(CPosTicklen + quotecount*2*8)) != 0)
            {
                _logger.Info("追加期货tick,文件格式不对,覆盖(" + _tickFileName + ")");
                return Write(ticklist, start, len, quotecount);
            }

            try
            {
                using (
                    var stream = new FileStream(_tickFileName, FileMode.Open, FileAccess.ReadWrite, FileShare.Read,
                        1024*1024))
                {
                    var reader = new BinaryReader(stream);

                    if (_firstRead)
                    {
                        ReadHeader(reader);
                        stream.Seek(_origdayoffset, SeekOrigin.Begin);
                        ReadOrigDays(reader);
                        _firstRead = false;
                    }

                    if (_quotecount != quotecount)
                        throw new Exception("追加期货tick失败,盘口档位不同: 文件=" + _quotecount.ToString() + ",tick=" +
                                            quotecount.ToString());

                    var ftickcount = (int)((finfo.Length - CTickOffset) / (CPosTicklen + quotecount * 2 * 8));
                    if (ftickcount != _tickcount)
                    {
                        _logger.Error("文件大小(" + ftickcount + ")和tick数(" + _tickcount + ")不符,可能有错: 文件=" + _tickFileName);
                    }

                    _tickcount = ftickcount;

                    int count = ticklist.Count - start;
                    count = (len < count) ? len : count;


                    BinaryWriter writer = new BinaryWriter(stream);

                    //如果盘中发现落盘tick的openprice发生改变，修改head中的openprice
                    UpdateTickOpenPrice(ticklist, writer);

                    writer.Seek(0, SeekOrigin.End);

                    switch (_quotecount)
                    {
                        case 1:
                            WriteTicks1(ticklist, writer, start, count);
                            break;
                        case 5:
                            WriteTicks5(ticklist, writer, start, count);
                            break;
                        default:
                            throw new Exception("不支持" + _quotecount.ToString() + "档盘口");
                    }

                    stream.Seek(CFileheaderLen - 8, SeekOrigin.Begin);
                    WriteOrigDays(writer);

                    writer.Flush();
                    stream.Close();

                    return true;
                }
            }
            catch (Exception ex)
            {
                _firstRead = false;

                _logger.Error("追加期货tick数据失败(" + _tickFileName + ")", ex);
            }

            return false;
        }

        #endregion

        #region 抽取数据文件的时间序列

        public static List<DateTimeSlice> GetDateTimeSlice(string strFile)
        {
            FileStream stream = null;
            try
            {
                if (!File.Exists(strFile))
                    return null;

                List<DateTimeSlice> timelist = new List<DateTimeSlice>();

                FutureTickFileNew tickfile = new FutureTickFileNew();

                stream = new FileStream(strFile, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024);
                BinaryReader reader = new BinaryReader(stream);

                if (tickfile.ReadHeader(reader))
                {
                    #region 新格式
                    
                    stream.Seek(tickfile._origdayoffset, SeekOrigin.Begin);
                    tickfile.ReadOrigDays(reader);

                    stream.Seek(tickfile._tickoffset, SeekOrigin.Begin);

                    int blklen = CPosTicklen + tickfile._quotecount * 2 * 8 - 4;

                    if (tickfile._origDays.Count == 0)
                        return null;

                    int origidx = 0;
                    DateTime origday = tickfile._origDays[origidx];
                    int nextorigoffset = int.MaxValue;
                    if (origidx < tickfile._origTickOffset.Count - 1)
                    {
                        nextorigoffset = tickfile._origTickOffset[origidx + 1] - 1;
                    }

                    DateTime begintime = default(DateTime);
                    for (int i = 0; i < tickfile._tickcount; ++i)
                    {
                        DateTimeSlice slice = new DateTimeSlice();

                        int hour = reader.ReadByte();
                        int min = reader.ReadByte();
                        int second = reader.ReadByte();
                        int msecond = reader.ReadByte();
                        msecond *= 10;

                        slice.BeginTime = begintime;
                        slice.EndTime = origday + new TimeSpan(0, hour, min, second, msecond);

                        begintime = slice.EndTime;

                        timelist.Add(slice);

                        reader.ReadBytes(blklen);
                        //stream.Seek(blklen, SeekOrigin.Current);

                        if (i >= nextorigoffset)
                        {
                            ++origidx;

                            if (origidx < tickfile._origDays.Count)
                                origday = tickfile._origDays[origidx];

                            nextorigoffset = int.MaxValue;
                            if (origidx < tickfile._origTickOffset.Count - 1)
                            {
                                nextorigoffset = tickfile._origTickOffset[origidx + 1] - 1;
                            }
                        }
                    }

                    #endregion
                }
                else
                {
                    #region 老格式
                   
                    stream.Seek(0, SeekOrigin.Begin);
                    tickfile.ReadOldHeader(reader);

                    int blklen = CPosTicklen + tickfile._quotecount * 2 * 8 - 4;
                    stream.Seek(CFileheaderlenOld, SeekOrigin.Begin);

                    DateTime begintime = default(DateTime);
                    for (int i = 0; i < tickfile._tickcount; ++i)
                    {
                        DateTimeSlice slice = new DateTimeSlice();

                        int hour = reader.ReadByte();
                        int min = reader.ReadByte();
                        int second = reader.ReadByte();
                        int msecond = reader.ReadByte();
                        msecond *= 10;

                        slice.BeginTime = begintime;
                        if (hour < 7)
                        {
                            slice.EndTime = tickfile._pretradingDay2 + new TimeSpan(0, hour, min, second, msecond);
                        }
                        else if (hour < 18)
                        {
                            slice.EndTime = tickfile._tradingDay + new TimeSpan(0, hour, min, second, msecond);
                        }
                        else
                        {
                            slice.EndTime = tickfile._pretradingDay1 + new TimeSpan(0, hour, min, second, msecond);
                        }

                        begintime = slice.EndTime;

                        timelist.Add(slice);

                        reader.ReadBytes(blklen);
                        //stream.Seek(blklen, SeekOrigin.Current);
                    }

                    #endregion
                }

                reader.Close();
                stream.Close();

                if (timelist.Count > 0)
                {
                    timelist[0].BeginTime = timelist[0].EndTime;
                }

                return timelist;
            }
            catch (Exception)
            {
                if (stream != null)
                    stream.Close();
            }

            return null;
        }

        public static List<TimeSlice> GetTimeSlice(string strFile)
        {
            FileStream stream = null;
            try
            {
                if (!File.Exists(strFile))
                    return null;

                List<TimeSlice> timelist = new List<TimeSlice>();

                FutureTickFileNew tickfile = new FutureTickFileNew();

                stream = new FileStream(strFile, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024);
                BinaryReader reader = new BinaryReader(stream);

                if (tickfile.ReadHeader(reader))
                {
                    stream.Seek(tickfile._origdayoffset, SeekOrigin.Begin);
                    tickfile.ReadOrigDays(reader);

                    stream.Seek(tickfile._tickoffset, SeekOrigin.Begin);

                    int blklen = CPosTicklen + tickfile._quotecount * 2 * 8 - 4;

                    TimeSpan begintime = default(TimeSpan);
                    for (int i = 0; i < tickfile._tickcount; ++i)
                    {
                        TimeSlice slice = new TimeSlice();

                        int hour = reader.ReadByte();
                        int min = reader.ReadByte();
                        int second = reader.ReadByte();
                        int msecond = reader.ReadByte();
                        msecond *= 10;

                        slice.BeginTime = begintime;
                        slice.EndTime = new TimeSpan(0, hour, min, second, msecond);

                        begintime = slice.EndTime;

                        timelist.Add(slice);

                        reader.ReadBytes(blklen);
                        //stream.Seek(blklen, SeekOrigin.Current);
                    }
                }
                else
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    tickfile.ReadOldHeader(reader);

                    int blklen = CPosTicklen + tickfile._quotecount * 2 * 8 - 4;
                    stream.Seek(CFileheaderlenOld, SeekOrigin.Begin);

                    TimeSpan begintime = default(TimeSpan);
                    for (int i = 0; i < tickfile._tickcount; ++i)
                    {
                        TimeSlice slice = new TimeSlice();

                        int hour = reader.ReadByte();
                        int min = reader.ReadByte();
                        int second = reader.ReadByte();
                        int msecond = reader.ReadByte();
                        msecond *= 10;

                        slice.BeginTime = begintime;
                        slice.EndTime = new TimeSpan(0, hour, min, second, msecond);

                        begintime = slice.EndTime;

                        timelist.Add(slice);

                        reader.ReadBytes(blklen);
                        //stream.Seek(blklen, SeekOrigin.Current);
                    }
                }

                reader.Close();
                stream.Close();

                if (timelist.Count > 0)
                {
                    timelist[0].BeginTime = timelist[0].EndTime;
                }

                return timelist;
            }
            catch (Exception)
            {
                if (stream != null)
                    stream.Close();
            }

            return null;
        }

        #endregion


        /// <summary>
        /// n < 0, 返回null
        /// n = 0，返回最后一个tick
        /// </summary>
        /// <param name="n">最后一个Tick</param>
        /// <returns></returns>
        public Tick AgoTick(int n)
        {
            try
            {
                if (n < 0) return null;

                if (!File.Exists(_tickFileName))
                {
                    _firstRead = true;
                    _bValid = false;

                    _logger.Error("读取期货tick数据失败(" + _tickFileName + "),文件不存在");
                    return null;
                }

                using (FileStream stream = new FileStream(_tickFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024))
                {
                    BinaryReader reader = new BinaryReader(stream);

                    if (_firstRead)
                    {
                        if (ReadHeader(reader))
                        {
                            stream.Seek(_origdayoffset, SeekOrigin.Begin);
                            ReadOrigDays(reader);
                        }
                        else
                        {
                            stream.Seek(0, SeekOrigin.Begin);
                            ReadOldHeader(reader);
                        }

                        _firstRead = false;
                    }

                    int offset = _tickcount - n - 1;
                    if (offset < 0)
                    {
                        stream.Close();

                        _bValid = true;

                        return null;
                    }

                    List<Tick> ticklist = new List<Tick>(1);

                    if (_bNewVersion)
                    {
                        long pos = _tickoffset + offset * (CPosTicklen + _quotecount * 2 * 8);
                        stream.Seek(pos, SeekOrigin.Begin);
                        
                        switch (_quotecount)
                        {
                            case 1: ReadTicks1(ticklist, reader, offset, 1); break;
                            case 5: ReadTicks5(ticklist, reader, offset, 1); break;
                            default: throw new Exception("不支持" + _quotecount.ToString() + "档盘口");
                        }
                    }
                    else
                    {
                        long pos = CFileheaderlenOld + offset * (CTickheaderlenOld + _quotecount * 2 * 8);
                        stream.Seek(pos, SeekOrigin.Begin);

                        switch (_quotecount)
                        {
                            case 1: ReadOldTicks1(ticklist, reader, offset, 1); break;
                            case 5: ReadOldTicks5(ticklist, reader, offset, 1); break;
                            default: throw new Exception("不支持" + _quotecount.ToString() + "档盘口");
                        }
                    }

                    stream.Close();

                    _bValid = true;

                    return ticklist[0];
                }
            }
            catch (Exception ex)
            {
                _firstRead = true;
                _bValid = false;

                _logger.Error("读取期货Tick数据失败(" + _tickFileName + ")", ex);
            }

            return null;
        }

        public Tick LastTick()
        {
            return AgoTick(0);
        }
    }
}


