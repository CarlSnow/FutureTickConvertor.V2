using Ats.Core;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;

namespace WpfTest
{
    public class MqTickReader
    {
        const int C_POS_TICKLEN = 32;   // 每条tick开始到盘口的字节数
        const int C_FILE_FLAG = (((int)'K') << 24) + (((int)'C') << 16) + (((int)'I') << 8) + (((int)'T'));

        string _tickFileName = "";
        string _futureId = "";
        string _exCode = "";
        bool _firstRead = true;
        bool _bValid = false;
        EnumMarket _market = EnumMarket.期货;
        DateTime _tradingDay = default(DateTime);
        ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        List<DateTime> _origDays = new List<DateTime>(4);
        List<int> _origTickOffset = new List<int>(4);

        int _origdayoffset = 0;
        int _quotecount = 0;
        int _origdays = 1;
        int _tickoffset = 0;
        int _version = 0;
        double _multiUnit = 1000;

        double _preClosePrice = 0;
        double _preSettlementPrice = 0;
        double _upLimit = 0;
        double _downLimit = 0;
        double _openPrice = 0;
        double _preInterest = 0;
        int _tickcount = 0;

        public MqTickReader(string strFile, string futureId, EnumMarket market, string exCode)
        {
            _tickFileName = strFile;
            _futureId = futureId;
            _exCode = exCode;
            _firstRead = true;
            _bValid = false;
            _tradingDay = default(DateTime);
            _market = market;
        }

        public bool Read(List<Tick> ticklist, int offset, int count)
        {
            FileStream stream = null;
            try
            {
                if (!File.Exists(_tickFileName))
                {
                    _firstRead = true;
                    _bValid = false;

                    logger.Error("读取期货tick数据失败(" + _tickFileName + "),文件不存在");

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
                    _firstRead = false;
                }

                long pos = _tickoffset + offset * (C_POS_TICKLEN + _quotecount * 2 * 8);
                stream.Seek(pos, SeekOrigin.Begin);

                switch (_quotecount)
                {
                    case 1: ReadTicks1(ticklist, reader, offset, count); break;
                    case 5: ReadTicks5(ticklist, reader, offset, count); break;
                    default: throw new Exception("不支持" + _quotecount.ToString() + "档盘口");
                }
                stream.Close();
                _bValid = true;
                return true;
            }
            catch (Exception ex)
            {
                _firstRead = true;
                _bValid = false;

                stream?.Close();
                logger.Error("读取期货tick数据失败(" + _tickFileName + ")", ex);
            }

            return false;
        }
        private bool ReadHeader(BinaryReader reader)
        {
            int flag = reader.ReadInt32();
            if (flag != C_FILE_FLAG)
            {
                return false;
            }
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
            for (int i = 0; i < _origdays; ++i)
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
    }
}
