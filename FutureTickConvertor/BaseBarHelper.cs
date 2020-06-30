using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Ats.Core;

namespace FutureTickConvertor
{
    public class BaseBarHelper
    {
        #region 单件

        static BaseBarHelper _inst = new BaseBarHelper();

        public static BaseBarHelper Instance
        {
            get { return _inst; }
        }

        BaseBarHelper()
        {
        }

        #endregion

        #region 获取k线时间

        static readonly TimeSpan OneDay = new TimeSpan(1, 0, 0, 0, 0);

        private Dictionary<string, BarProviderInfo> _templates = new Dictionary<string, BarProviderInfo>();

        public void Clear()
        {
            _templates.Clear();
        }

        public BarProviderInfo GetBarProviderInfo(List<TimeSlice> lstslice, int offset, int interval, EnumBarType period)
        {
            StringBuilder builder = new StringBuilder();
            foreach (TimeSlice slice in lstslice)
            {
                builder.Append(slice.BeginTime.ToString()).Append("_")
                    .Append(slice.EndTime.ToString()).Append("_");
            }
            builder.Append(offset).Append("_")
                .Append(interval).Append("_")
                .Append(period.ToString());

            string key = builder.ToString();
            BarProviderInfo tempbars;
            if (_templates.TryGetValue(key, out tempbars))
                return tempbars;

            tempbars = CreateBars(lstslice, offset, interval, period);
            _templates.Add(key, tempbars);

            return tempbars;
        }

        public static BarProviderInfo CreateBars(List<TimeSlice> lstslice, int offset, int interval, EnumBarType bartype)
        {

            List<TimeSlice> bars = CreateTimeSlices(lstslice, offset, interval, bartype);
            if (bars == null)
                return null;

            return new BarProviderInfo()
            {
                Lstslice = bars,
                Bartype = bartype,
                Interval = interval,
                Offset = offset,
                Livingtime = new TimeSlice()
                {
                    BeginTime = lstslice[0].BeginTime,
                    EndTime = lstslice[lstslice.Count - 1].EndTime
                },
            };
        }

        public static List<TimeSlice> CreateTimeSlices(List<TimeSlice> lstslice, int offset, int interval, EnumBarType bartype)
        {
            if (lstslice.Count == 0)
                return null;

            TimeSpan offtime = new TimeSpan(0, 0, offset);
            TimeSpan sp = default(TimeSpan);
            switch (bartype)
            {
                case EnumBarType.秒: sp = new TimeSpan(0, 0, interval); break;
                case EnumBarType.分钟: sp = new TimeSpan(0, interval, 0); break;
                case EnumBarType.小时: sp = new TimeSpan(interval, 0, 0); break;
                case EnumBarType.日线:
                    {
                        TimeSlice tsday = new TimeSlice();
                        tsday.BeginTime = lstslice[0].BeginTime;
                        tsday.EndTime = lstslice[lstslice.Count - 1].EndTime;
                        List<TimeSlice> daybars = new List<TimeSlice>();
                        daybars.Add(tsday);
                        return daybars;
                    }
                default: throw new Exception("不支持的k线:" + bartype.ToString());
            }

            TimeSpan beginTime = lstslice[0].BeginTime;//开始时间
            List<TimeSlice> bars = new List<TimeSlice>();
            if (offset > 0)
            {
                TimeSlice slice = new TimeSlice();
                slice.BeginTime = beginTime;
                slice.EndTime = beginTime + offtime;
                if (slice.EndTime.Days > 0)
                    slice.EndTime = slice.EndTime - OneDay;

                bars.Add(slice);

                beginTime = slice.EndTime;
            }

            TimeSpan endTime = beginTime;//结束时间
            TimeSpan tsDiff = default(TimeSpan);
            for (int i = 0; i < lstslice.Count; ++i)
            {
                TimeSlice ts = lstslice[i];
                while (true)
                {
                    TimeSpan tmp = ts.EndTime - endTime;
                    if (tmp < default(TimeSpan))
                        tmp = tmp + OneDay;

                    if (tmp + tsDiff < sp)
                        break;

                    endTime = endTime + (sp - tsDiff);
                    if (endTime >= OneDay)
                        endTime = endTime - OneDay;

                    TimeSlice slice = new TimeSlice();
                    slice.BeginTime = beginTime;
                    slice.EndTime = endTime;
                    bars.Add(slice);

                    beginTime = endTime;
                    tsDiff = default(TimeSpan);
                }

                if (i < lstslice.Count - 1)
                {
                    tsDiff = tsDiff + ts.EndTime - endTime;
                    if (tsDiff < default(TimeSpan))
                        tsDiff = tsDiff + OneDay;

                    endTime = lstslice[i + 1].BeginTime;

                    if (tsDiff == default(TimeSpan))
                        beginTime = endTime;
                }
                else
                {
                    if (beginTime < ts.EndTime)
                    {
                        TimeSlice slice = new TimeSlice();
                        slice.BeginTime = beginTime;
                        slice.EndTime = ts.EndTime;
                        bars.Add(slice);
                    }
                }
            }

            return bars;
        }


        public static BarProviderInfo CreateBars(InstrumentManager instmgr, string instid, int offset,
            int interval, EnumBarType bartype, DateTime tradingday,bool isFillNight=true)
        {
            var tradingtime = GetTradingTime(instmgr, instid, tradingday);
            if (tradingtime == null || tradingtime.Count == 0)
                return null;
            if (isFillNight == false)
            {
                var item = from d in tradingtime where d.BeginTime.Hours != 21 &&d.EndTime.Hours != 2 select d;
                tradingtime=item.ToList();
            }
            return CreateBars(tradingtime, offset, interval, bartype);
        }

        private static List<TimeSlice> GetTradingTime(InstrumentManager instmgr, string instid, DateTime tradingday)
        {
            List<TimeSlice> tradingtime = instmgr.GetTradingTime(tradingday, instid);
           
            Instrument instr = instmgr[instid];
            string productid = Regex.Replace(instid, @"\d", "");
            if (instr != null && instr.Market == EnumMarket.期货期权)
            {
                productid = ((Option)instr).ProductID;
            }
            FutureProduct product = instmgr.GetFutureProduct(productid);

            return tradingtime;
        }


        #endregion

    }
}
