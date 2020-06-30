using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ats.Core;
using log4net;

namespace FutureTickConvertor
{
    /// <summary>
    /// 日线转换，虽然叫期货日线转换，实际上可以转换期货/股票r日线
    /// </summary>
    public class FutureDayConvert
    {
        ProgMqFile _prgMq;

        Dictionary<string, DateTime> _beginlist = new Dictionary<string, DateTime>();

        Dictionary<string, DateTime> _endlist = new Dictionary<string, DateTime>();

        private Dictionary<string, BarSeries> _buffer = new Dictionary<string, BarSeries>();
        private Dictionary<string, BarSeries> _bufferFuture = new Dictionary<string, BarSeries>();

        ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public EnumMarket Market = EnumMarket.期货;
        //public LogReporter logrpt = null;

        public void Convert(string strSrcDir, string strDstDir, DateTime startTime=default(DateTime))
        {
            // 创建目录
            if (!Directory.Exists(strDstDir))
                Directory.CreateDirectory(strDstDir);

            string progName = strDstDir + "\\prog.mq";

            if (!File.Exists(progName))
            {
                _logger.Error(progName + "文件不存在");
            }

            // 读取目录下已经存在的数据
            _prgMq = new ProgMqFile(progName, startTime);

            string[] tmpdirlist = Directory.GetDirectories(strSrcDir);
            List<string> dirlist = new List<string>(tmpdirlist);
            dirlist.Sort();
            dirlist.Reverse();

            int month = 0;
            DateTime lastDay = default(DateTime);
            foreach (string strDir in dirlist)
            {
                DateTime day;
                string strshort = strDir.Substring(strDir.Length - 8);
                if (!DateTime.TryParseExact(strshort, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out day))
                    continue;

                if (month != day.Month)
                {
                    SaveFuture(strDstDir, lastDay);
                    month = day.Month;
                    _logger.Info("进度: " + day.ToString("yyyy-MM-dd"));
                    //if (logrpt != null)
                    //    logrpt.Print("进度: " + day.ToString("yyyy-MM-dd"));
                }

                lastDay = day;

                if (ConvertDay(strDir, day))
                    break;
            }

            Save(strDstDir);
            SaveFuture(strDstDir, lastDay);

            foreach (string futureid in _beginlist.Keys)
            {
                DateTime begintime = _prgMq.BeginTime(futureid);

                DateTime endtime = _prgMq.EndTime(futureid);

                if (begintime == default(DateTime))
                    begintime = _beginlist[futureid];
                else if (begintime > _beginlist[futureid])
                    begintime = _beginlist[futureid];

                if (endtime == default(DateTime))
                    endtime = _endlist[futureid];
                else if (endtime < _endlist[futureid])
                    endtime = _endlist[futureid];

                _prgMq.Add(futureid, begintime, endtime);
            }

            _prgMq.Save(strDstDir + "\\prog.mq");

            _logger.Info("完成T2D");
            //if (logrpt != null)
            //    logrpt.Print("完成T2D");
        }

        private void Save(string strDstDir)
        {
            foreach (string futureid in _buffer.Keys)
            {
                string fullname = strDstDir + "\\" + futureid + ".day";
                var isfile = File.Exists(fullname);
                bool isRead = true;
                if (Market == EnumMarket.期货)
                {
                    FutureDayFile dayfile = new FutureDayFile();
                    dayfile.Init(fullname);
                    BarSeries daybarlist = new BarSeries();
                    if (_prgMq.Contains(futureid))
                    {
                        if (_prgMq.EndTime(futureid).Date == _buffer[futureid][0].TradingDate)
                        {
                            var endtime = _prgMq.EndTime(futureid);
                            endtime = endtime.AddDays(-1);
                            if (endtime > new DateTime())
                                isRead=dayfile.Read(daybarlist, new DateTime(), endtime);
                        }
                        else
                        {
                            isRead = dayfile.Read(daybarlist, new DateTime(), _prgMq.EndTime(futureid));
                        }

                        if (isRead == false)
                        {
                            _logger.Error("读取文件失败，不准备写入" + fullname + futureid);
                            continue;
                        }
                        daybarlist.AddRange(_buffer[futureid]);
                        dayfile.Write(daybarlist);
                    }
                    else
                        dayfile.Write(_buffer[futureid]);
                }

            }
        }

        private void SaveFuture(string strDstDir, DateTime month)
        {
            if (month == default(DateTime))
                return;

            foreach (string futureid in _bufferFuture.Keys)
            {
                //如果K线根数为0，表示没有K线信息，则直接跳过
                if (_bufferFuture[futureid].Count == 0)
                {
                    continue;
                }
                string fullname = strDstDir + "\\" + month.ToString("yyyyMM");
                if (!Directory.Exists(fullname))
                    Directory.CreateDirectory(fullname);


                fullname = fullname + "\\" + futureid + ".day";

                if (Market == EnumMarket.期货)
                {
                    FutureDayFile dayfile = new FutureDayFile();
                    dayfile.Init(fullname);
                    BarSeries daybarlist = new BarSeries();
                    if (_prgMq.Contains(futureid))
                    {
                        var starttime = _prgMq.BeginTime(futureid);
                        var endtime = _prgMq.EndTime(futureid);
                        var tradingdate = _bufferFuture[futureid][0].TradingDate;
                        //如果换月，则需要从当月的1号开始
                        if (starttime.Month < tradingdate.Month)
                        {
                            starttime = new DateTime(tradingdate.Year, tradingdate.Month, 1);
                        }
                        //如果换月，则不需要读取
                        if (endtime.Month == tradingdate.Month)
                        {
                            if (_prgMq.EndTime(futureid).Date == _bufferFuture[futureid][0].TradingDate)
                            {

                                endtime = endtime.AddDays(-1);
                                if (endtime >= starttime)
                                    dayfile.Read(daybarlist, starttime, endtime);
                                else
                                {
                                    _logger.Error(starttime + "没有读取到日线数据" + endtime + fullname);
                                }
                            }
                            else
                            {
                                dayfile.Read(daybarlist, _prgMq.BeginTime(futureid),
                                    _prgMq.EndTime(futureid));
                            }
                        }

                        daybarlist.AddRange(_bufferFuture[futureid]);
                    }
                    else
                    {
                        daybarlist = _bufferFuture[futureid];
                    }

                    if (File.Exists(fullname))
                    {
                        File.Delete(fullname);
                    }
                    if (daybarlist.Count > 0)
                    {
                        dayfile.Write(daybarlist);
                    }
                    else
                    {
                        _logger.Error("品种" + futureid + "在" + month + "没有对应的分钟线数据，请查询对应的数据是否有错！");
                    }
                }
            }

            _bufferFuture.Clear();
        }

        private void ModifyBars(BarSeries daybarlist, BarSeries addedBarSeries)
        {
            var lastdate = daybarlist.Last.BeginTime.Date;

            foreach (var bar in addedBarSeries)
            {
                var bardate = bar.BeginTime.Date;
                if (bardate <= lastdate)
                {
                    var barindex = daybarlist.GetIndex(bardate);
                    if (barindex != -1)
                    {
                        daybarlist[barindex] = bar;
                    }

                }
                else
                {
                    daybarlist.Add(bar);
                }
            }
        }

        private bool ConvertDay(string strSrc, DateTime day)
        {
            bool bAllFinish = true;

            string[] filelist = Directory.GetFiles(strSrc);
            if (!filelist.Any())
            {
                return false;
            }
            foreach (string strFile in filelist)
            {
                FileInfo finfo = new FileInfo(strFile);
                //把.tk去掉
                string str = finfo.Name;
                string futureId = str.Replace(".tk", "");
                if (futureId.ToLower() != "index")
                {
                    if (!ConvertFile(strFile, futureId, day))
                        bAllFinish = false;
                }

            }

            return bAllFinish;
        }

        private bool ConvertFile(string strFile, string futureid, DateTime day)
        {
            try
            {
                if (_prgMq.EndTime(futureid) > day)
                    return true;

                var tickfile = DataFileFactory.CreateTickFile(Market);
                tickfile.Init(Market, strFile, futureid, "");
                var lasttick = tickfile.AgoTick(0);
                if (lasttick == null)
                {
                    _logger.Error("获取构造日k线的tick失败 : " + strFile);
                    return false;
                }

                if (!_buffer.ContainsKey(futureid))
                    _buffer.Add(futureid, new BarSeries());

                if (!_bufferFuture.ContainsKey(futureid))
                    _bufferFuture.Add(futureid, new BarSeries());

                //构建一根日K线,
                //如果Tick的成交量和成交额=0 （成交稀疏为0）（高开低=0）            
                //B 日线的高=开=低=收= Tick的最新价
                //C 日线的量=0 成交额=0
                var bar = new Bar
                {
                    BeginTime = day,
                    EndTime = day,
                    Close = lasttick.LastPrice,
                    High = Math.Abs(lasttick.HighPrice) < 0.00001 ? lasttick.LastPrice : lasttick.HighPrice,
                    Low = Math.Abs(lasttick.LowPrice) < 0.00001 ? lasttick.LastPrice : lasttick.LowPrice,
                    Open = Math.Abs(lasttick.OpenPrice) < 0.00001 ? lasttick.LastPrice : lasttick.OpenPrice,

                    PreClose = Math.Abs(lasttick.PreSettlementPrice) > 0.00001
                        ? lasttick.PreSettlementPrice
                        : lasttick.PreClosePrice,
                    Volume = lasttick.Volume,
                    Turnover = lasttick.Turnover,
                    OpenInterest = lasttick.OpenInterest,
                    TradingDate = lasttick.TradingDay
                };

                _buffer[futureid].Insert(0, bar);
                _bufferFuture[futureid].Insert(0, bar);

                // 更新
                if (_beginlist.ContainsKey(futureid))
                {
                    if (_beginlist[futureid] > day)
                        _beginlist[futureid] = day;
                }
                else
                {
                    _beginlist.Add(futureid, day);
                }

                if (_endlist.ContainsKey(futureid))
                {
                    if (_endlist[futureid] < day)
                        _endlist[futureid] = day;
                }
                else
                {
                    _endlist.Add(futureid, day);
                }

                return false;

            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
                return false;
            }
        }
    }
}
