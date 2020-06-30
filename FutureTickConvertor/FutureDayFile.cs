using System;
using System.IO;
using Ats.Core;
using log4net;

namespace FutureTickConvertor
{
    public sealed class FutureDayFile 
    {
        #region

        const int CFileheaderlen = 64;
        const int CBarlen = 12 * 4;
        const int CReserve = 48;
        const int CPosEnd = 8;

        string _filename = "";

        int _icount;
        bool _bValid;
        bool _firstRead;

        DateTime _begin;
        DateTime _end;

        ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        #region

        public bool IsValid
        {
            get { return _bValid; }
        }

        public int TickCount
        {
            get { return _icount; }
        }

        public DateTime BeginDay
        {
            get { return _begin; }
        }

        public DateTime EndDay
        {
            get { return _end; }
        }

        #endregion

        #region

        public void Init(string strFile)
        {
            _filename = strFile;
            _firstRead = true;
            _bValid = false;
            _icount = 0;
        }

        public bool Read(BarSeries barlist, DateTime from, DateTime to)
        {
            FileStream stream = null;
            try
            {
                if (!File.Exists(_filename))
                {
                    _firstRead = true;
                    _bValid = false;

                    _logger.Error("读取期货日k线失败(" + _filename + "),文件不存在");

                    return false;
                }

                stream = new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024);

                if ((stream.Length - CFileheaderlen) % CBarlen != 0)
                    throw new Exception("日k线文件被破坏，数据出错");

                BinaryReader reader = new BinaryReader(stream);

                long pos = CFileheaderlen;
                if (_firstRead)
                {
                    ReadHeader(reader);
                    pos -= CFileheaderlen;
                }

                stream.Seek(pos, SeekOrigin.Current);

                ReadBars(reader, barlist, from, to);

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

                _logger.Error("读取期货日k线失败(" + _filename + ")", ex);
            }

            return false;
        }

        public bool ReadHeader()
        {
            if (!_firstRead)
                return true;

            FileStream stream = null;
            try
            {
                if (!File.Exists(_filename))
                {
                    _firstRead = true;
                    _bValid = false;

                    _logger.Error("读取期货日k线失败(" + _filename + "),文件不存在");
                    return false;
                }

                stream = new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024);

                if ((stream.Length - CFileheaderlen) % CBarlen != 0)
                    throw new Exception("日k线文件被破坏，数据出错");

                BinaryReader reader = new BinaryReader(stream);

                ReadHeader(reader);

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

                _logger.Error("读取期货日k线失败(" + _filename + ")", ex);
            }

            return false;
        }

        private void ReadBars(BinaryReader reader, BarSeries barlist, DateTime from, DateTime to)
        {
            int i = 0;
            for (; i < _icount; ++i)
            {
                #region

                int year = reader.ReadInt16();
                int month = reader.ReadByte();
                int day = reader.ReadByte();
                reader.BaseStream.Seek(CBarlen - 4, SeekOrigin.Current);

                DateTime dt = new DateTime(year, month, day);
                if (dt >= from)
                {
                    reader.BaseStream.Seek(-CBarlen, SeekOrigin.Current);
                    break;
                }

                #endregion
            }

            for (; i < _icount; ++i)
            {
                #region

                int year = reader.ReadInt16();
                int month = reader.ReadByte();
                int day = reader.ReadByte();

                DateTime dt = new DateTime(year, month, day);

                if (dt > to)
                {
                    break;
                }

                Bar bar = new Bar();

                bar.BeginTime = dt;
                bar.EndTime = dt;
                bar.Open = reader.ReadInt32() / 1000.0;
                bar.Close = reader.ReadInt32() / 1000.0;
                bar.High = reader.ReadInt32() / 1000.0;
                bar.Low = reader.ReadInt32() / 1000.0;
                bar.PreClose = reader.ReadInt32() / 1000.0;
                bar.Volume = reader.ReadDouble();
                bar.Turnover = reader.ReadDouble();
                bar.OpenInterest = reader.ReadDouble();
                bar.IsCompleted = true;
                bar.TradingDate = dt;
                barlist.Add(bar);

                #endregion
            }
        }

        private void ReadHeader(BinaryReader reader)
        {
            int interval = reader.ReadInt16();
            int bartype = reader.ReadInt16();
            if (bartype != 4 || interval != 1)
                throw new Exception("错误的日k线文件标识");

            int year = reader.ReadInt16();
            int month = reader.ReadByte();
            int day = reader.ReadByte();
            _begin = new DateTime(year, month, day);

            year = reader.ReadInt16();
            month = reader.ReadByte();
            day = reader.ReadByte();
            _end = new DateTime(year, month, day);

            if (_begin == default(DateTime) || _end == default(DateTime) || _begin > _end)
                throw new Exception("非法的日k线文件");

            _icount = reader.ReadInt32();
            if (_icount != (reader.BaseStream.Length - CFileheaderlen) / CBarlen)
                throw new Exception("日k线文件被破坏，数据出错");

            reader.BaseStream.Seek(CReserve, SeekOrigin.Current);

            _firstRead = false;
        }

        #endregion

        #region 覆盖写入文件

        public bool Write(BarSeries barlist)
        {
            return Write(barlist, 0, barlist.Count);
        }

        public bool Write(BarSeries barlist, int start, int len)
        {
            if (barlist.Count == 0)
                return true;

            FileStream stream = null;
            try
            {
                _icount = barlist.Count - start;
                _icount = (len < _icount) ? len : _icount;

                _begin = barlist.First.BeginTime.Date;
                _end = barlist.Last.BeginTime.Date;

                stream = new FileStream(_filename, FileMode.Create, FileAccess.Write, FileShare.Read, 1024 * 1024);
                BinaryWriter writer = new BinaryWriter(stream);

                WriteHeader(writer);
                WriteBars(barlist, writer, start, _icount);

                writer.Flush();
                stream.Close();

                return true;
            }
            catch (Exception ex)
            {
                if (stream != null)
                    stream.Close();

                _logger.Error("写入股票日k线失败(" + _filename + ")", ex);
            }

            return false;
        }

        private void WriteHeader(BinaryWriter writer)
        {
            writer.Write((Int16)1);
            writer.Write((Int16)4);
            writer.Write((Int16)(_begin.Year));
            writer.Write((byte)(_begin.Month));
            writer.Write((byte)(_begin.Day));
            writer.Write((Int16)(_end.Year));
            writer.Write((byte)(_end.Month));
            writer.Write((byte)(_end.Day));
            writer.Write((Int32)_icount);

            byte[] buffer = new byte[CReserve];
            writer.Write(buffer);
        }

        private void WriteBars(BarSeries barlist, BinaryWriter writer, int start, int count)
        {
            int end = start + count;
            for (int i = start; i < end; ++i)
            {
                Bar bar = barlist[i];

                #region

                writer.Write((Int16)(bar.BeginTime.Year));
                writer.Write((byte)(bar.BeginTime.Month));
                writer.Write((byte)(bar.BeginTime.Day));

                if (bar.Open < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(bar.Open);
                    writer.Write((Int32)(Math.Round(dvalue * 1000, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }

                if (bar.Close < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(bar.Close);
                    writer.Write((Int32)(Math.Round(dvalue * 1000, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }

                if (bar.High < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(bar.High);
                    writer.Write((Int32)(Math.Round(dvalue * 1000, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }

                if (bar.Low < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(bar.Low);
                    writer.Write((Int32)(Math.Round(dvalue * 1000, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }

                if (bar.PreClose < (double)(decimal.MaxValue))
                {
                    decimal dvalue = (decimal)(bar.PreClose);
                    writer.Write((Int32)(Math.Round(dvalue * 1000, 0, MidpointRounding.AwayFromZero)));
                }
                else
                {
                    writer.Write((Int32)(0));
                }

                writer.Write(bar.Volume);
                writer.Write(bar.Turnover);
                writer.Write(bar.OpenInterest);

                #endregion
            }
        }

        #endregion

        #region 追加写入

        public bool Append(BarSeries barlist)
        {
            return Append(barlist, 0, barlist.Count);
        }

        public bool Append(BarSeries barlist, int start, int len)
        {
            FileInfo finfo = new FileInfo(_filename);
            if (!finfo.Exists || finfo.Length < CFileheaderlen)
                return Write(barlist, start, len);

            FileStream stream = null;
            try
            {
                int count = barlist.Count - start;
                count = (len < count) ? len : count;

                _icount = (int)(finfo.Length - CFileheaderlen) / CBarlen;
                _icount += count;

                _end = barlist.Last.BeginTime.Date;

                stream = new FileStream(_filename, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, 1024 * 1024);
                BinaryWriter writer = new BinaryWriter(stream);

                writer.Seek(CPosEnd, SeekOrigin.Begin);

                writer.Write((Int16)(_end.Year));
                writer.Write((byte)(_end.Month));
                writer.Write((byte)(_end.Day));
                writer.Write((Int32)_icount);
                writer.Seek(0, SeekOrigin.End);

                WriteBars(barlist, writer, start, count);

                writer.Flush();
                stream.Close();

                return true;
            }
            catch (Exception ex)
            {
                if (stream != null)
                    stream.Close();

                _logger.Error("读取股票tick数据失败(" + _filename + ")", ex);
            }

            return false;
        }

        #endregion

        public bool ReadAgo(BarSeries barlist, DateTime curTime, int agoN)
        {
            FileStream stream = null;
            try
            {
                if (!File.Exists(_filename))
                {
                    _firstRead = true;
                    _bValid = false;

                    _logger.Error("读取期货日k线失败(" + _filename + "),文件不存在");

                    return false;
                }

                stream = new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024);

                if ((stream.Length - CFileheaderlen) % CBarlen != 0)
                    throw new Exception("日k线文件被破坏，数据出错");

                BinaryReader reader = new BinaryReader(stream);

                long pos = CFileheaderlen;
                if (_firstRead)
                {
                    ReadHeader(reader);
                    pos -= CFileheaderlen;
                }

                stream.Seek(pos, SeekOrigin.Current);

                ReadBarsAgo(reader, barlist, curTime, agoN);

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

                _logger.Error("读取期货日k线失败(" + _filename + ")", ex);
            }

            return false;
        }

        private void ReadBarsAgo(BinaryReader reader, BarSeries barlist, DateTime now, int len)
        {
            int i = 0;
            for (; i < _icount; ++i)
            {
                #region

                int year = reader.ReadInt16();
                int month = reader.ReadByte();
                int day = reader.ReadByte();

                DateTime dt = new DateTime(year, month, day);

                if (dt > now)
                {
                    break;
                }

                Bar bar = new Bar();

                bar.BeginTime = dt;
                bar.EndTime = dt;
                bar.Open = reader.ReadInt32() / 1000.0;
                bar.Close = reader.ReadInt32() / 1000.0;
                bar.High = reader.ReadInt32() / 1000.0;
                bar.Low = reader.ReadInt32() / 1000.0;
                bar.PreClose = reader.ReadInt32() / 1000.0;
                bar.Volume = reader.ReadDouble();
                bar.Turnover = reader.ReadDouble();
                bar.OpenInterest = reader.ReadDouble();
                bar.IsCompleted = true;
                bar.TradingDate = dt;
                barlist.Add(bar);

                #endregion
            }

            if (i > len)
            {
                int rmlen = i - len;
                barlist.RemoveRange(barlist.Count - i, rmlen);
            }
        }
    }
}
