using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ats.Core;
using log4net;

namespace FutureTickConvertor
{
    public class FutureMinBarConverter
    {
        ProgMqFile _prgMq;
        InstrumentManager _fmgr;

        Dictionary<string, DateTime> _beginlist = new Dictionary<string, DateTime>();
        Dictionary<string, DateTime> _endlist = new Dictionary<string, DateTime>();
        //Dictionary<string, BarProviderInfo> tslist = new Dictionary<string, BarProviderInfo>();

        private Dictionary<string, BarSeries> _buffer = new Dictionary<string, BarSeries>();

        ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //public LogReporter logrpt = null;
        //是否补齐分钟线
        public bool AllBlankBar = false;

        public void Convert(string strSrcDir, string strDstDir, string futurexmlDir, DateTime startTime)
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

            _fmgr = new InstrumentManager();
            _fmgr.Load(futurexmlDir, null, EnumMarket.期货, false);

            string[] tmpdirlist = Directory.GetDirectories(strSrcDir);
            List<string> dirlist = new List<string>(tmpdirlist);
            dirlist.Sort();
            dirlist.Reverse();

            int month = 0;
            DateTime lastDay = default(DateTime);
            foreach (string strDir in dirlist)
            {
                try
                {
                    DateTime day;
                    string strshort = strDir.Substring(strDir.Length - 8);
                    if (
                        !DateTime.TryParseExact(strshort, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None,
                            out day))
                        continue;

                    if (month != day.Month)
                    {
                        Save(strDstDir, lastDay);

                        month = day.Month;

                        _logger.Info("进度: " + day.ToString("yyyy-MM-dd"));
                        //if (logrpt != null)
                        //    logrpt.Print("进度: " + day.ToString("yyyy-MM-dd"));
                    }

                    lastDay = day;

                    if (ConvertDay(strDir, day))
                        break;
                }
                catch (Exception ex)
                {
                    _logger.Error(strDir + ex.ToString());
                    throw new Exception("转换分钟线时出错,文件路径：" + strDir + "时出错");
                }
            }

            Save(strDstDir, lastDay);

            #region

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

            #endregion

            _logger.Info("完成T2M");
            //if (logrpt != null)
            //    logrpt.Print("完成T2M");
        }

        private void Save(string strDstDir, DateTime month)
        {
            if (month == default(DateTime))
                return;

            foreach (string futureid in _buffer.Keys)
            {
                //如果K线根数为0，表示没有K线信息，则直接跳过
                if (_buffer[futureid].Count == 0)
                {
                    continue;
                }
                string fullname = strDstDir + "\\" + month.ToString("yyyyMM");
                if (!Directory.Exists(fullname))
                    Directory.CreateDirectory(fullname);

                fullname = fullname + "\\" + futureid + ".min";

                try
                {
                    BarSeries daybarlist = new BarSeries();
                    if (_prgMq.Contains(futureid))
                    {
                        //TimeSlice livingtime = fmgr.GetLivingTime(futureid);
                        using (var barfile = new BarFileV1(fullname, EnumMarket.期货))
                        {
                            var starttime = _prgMq.BeginTime(futureid);
                            var endtime = _prgMq.EndTime(futureid);
                            var tradingdate = _buffer[futureid][0].TradingDate;
                            //如果换月，则需要从当月的1号开始
                            if (starttime.Month < tradingdate.Month)
                            {
                                starttime = new DateTime(tradingdate.Year, tradingdate.Month, 1);
                            }
                            //如果换月，则不需要读取
                            if (endtime.Month == tradingdate.Month)
                            {
                                if (_prgMq.EndTime(futureid).Date == _buffer[futureid][0].TradingDate)
                                {

                                    endtime = endtime.AddDays(-1);
                                    if (endtime >= starttime)
                                        barfile.ReadTradingDays(daybarlist, starttime, endtime);
                                }
                                else
                                {
                                    barfile.ReadTradingDays(daybarlist, _prgMq.BeginTime(futureid),
                                        _prgMq.EndTime(futureid));
                                }
                            }

                        }
                        daybarlist.AddRange(_buffer[futureid]);
                    }
                    else
                    {
                        daybarlist = _buffer[futureid];
                    }

                    if (File.Exists(fullname))
                    {
                        File.Delete(fullname);
                    }
                    if (daybarlist.Count > 0)
                    {
                        using (var barfile = new BarFileV1(fullname, EnumMarket.期货, EnumBarType.分钟, 1))
                        {
                            barfile.Write(daybarlist);
                        }
                    }
                    else
                    {
                        _logger.Error("品种" + futureid + "在" + month + "没有对应的分钟线数据，请查询对应的数据是否有错！");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(fullname + ex.ToString());
                    throw new Exception("读取文件出错" + fullname);
                }
            }

            _buffer.Clear();
        }

        private bool ConvertDay(string strSrc, DateTime day)
        {
            bool bAllFinish = true;

            string[] filelist = Directory.GetFiles(strSrc);
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
            //由于夜盘的存在，最后一天需要重复转一次
            if (_prgMq.EndTime(futureid) > day)
                return true;

            // FutureTickFile tickfile = new FutureTickFile();
            var tickfile = DataFileFactory.CreateTickFile(EnumMarket.期货);
            tickfile.Init(EnumMarket.期货, strFile, futureid, "");
            List<Tick> ticklist = new List<Tick>();
            if (!tickfile.Read(ticklist, 0, int.MaxValue))
            {
                _logger.Error("读取文件失败(" + strFile + ")");

                return false;
            }

            if (!_buffer.ContainsKey(futureid))
                _buffer.Add(futureid, new BarSeries());

            //为了避免交易时间段的变化，每天都需要新建一个barproviderinfo
            //if (!tslist.ContainsKey(futureid))
            //{
            //    tslist.Add(futureid, BaseBarHelper.CreateBars(fmgr, futureid, 0, 1, EnumBarType.分钟, day));
            //}
            #region 如果夜盘没有tick,不补齐夜盘分钟线
            bool isFillNight = true;
            var nightTicks = from d in ticklist where d.DateTime.Hour > 20 || d.DateTime.Hour < 3 select d;
            if (!nightTicks.Any())
            {
                isFillNight = false;
            }
            #endregion

            var barproviderinfo = BaseBarHelper.CreateBars(_fmgr, futureid, 0, 1, EnumBarType.分钟, day, isFillNight);

            //if (tslist.ContainsKey(futureid) && tslist[futureid] != null)
            if (barproviderinfo != null)
            {
                BarProvider provider = new BarProvider(day, barproviderinfo) { AllBlankBar = AllBlankBar };

                #region

                foreach (Tick tick in ticklist)
                {
                    provider.AddTick(tick);//如果白盘没有tick，不需要过滤，因为不会加工
                }
                if (provider.Bars != null && provider.Bars.Count > 0)
                {
                    //根据bar.volume来过滤夜盘的bar，不够严谨
                    // var bars = CheckIfNoNight(provider.Bars); //对于整个21：00-2：00夜盘bvolume为0的bar过滤掉
                    _buffer[futureid].InsertRange(0, provider.Bars);

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
                }

                #endregion
            }
            else
            {
                _logger.Error("期货转化分钟线时发现合约[" + futureid + "]在Future.XML中不存在,导致BarProvider为空");
            }

            return false;
        }

    }
}
