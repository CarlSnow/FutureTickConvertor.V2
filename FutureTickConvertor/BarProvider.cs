using System;
using System.Collections.Generic;
using Ats.Core;
using log4net;

namespace FutureTickConvertor
{
    /// <summary>
    /// K线切分
    /// </summary>
    public class BarProvider
    {
        readonly BarProviderInfo _prdinfo;

        int _posTime = 0;
        int _posBar = -1;

        public DateTime TradingDay;

        /// <summary>
        /// 添加参数是否允许空bar的生成
        /// 默认为false，如果设置为true，则允许补齐bar
        /// </summary>
        public bool AllBlankBar = false;

        readonly List<DateTimeSlice> _lstslice;

        bool _blive = true;

        readonly BarSeries _bars = new BarSeries();

        Tick _lasttick;

        TimeSpan _diff = default(TimeSpan);

        readonly LockObject _lockobj = new LockObject();

        readonly ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// 是否还有时间点
        /// </summary>
        protected bool IsEnd
        {
            get { return _posTime >= _prdinfo.Lstslice.Count; }
        }

        public bool EnableLive
        {
            get { return _blive; }
            set { _blive = value; }
        }

        /// <summary>
        /// 0-没有baropen,barclose,有更新;1-barclose;2-baropen;3-barclose/baropen;4-没有更新
        /// </summary>
        public int ChangeState { get; set; }

        public BarSeries Bars
        {
            get { return _bars; }
        }

        static List<DateTimeSlice> CreateDateTimeSlice(DateTime tradingday, List<TimeSlice> lsttimeslice)
        {
            DateTime pretradingday1 = TradingDayHelper.GetPreTradingDay(tradingday);
            DateTime pretradingday2 = pretradingday1.AddDays(1).Date;

            List<DateTimeSlice> lstslice = new List<DateTimeSlice>(lsttimeslice.Count);
            foreach (TimeSlice timeslice in lsttimeslice)
            {
                lstslice.Add(new DateTimeSlice()
                    {
                        BeginTime = YfTimeHelper.JoinDateTime(tradingday, pretradingday1, pretradingday2, timeslice.BeginTime),
                        EndTime = YfTimeHelper.JoinDateTime(tradingday, pretradingday1, pretradingday2, timeslice.EndTime)
                    });
            }

            return lstslice;
        }

        public BarProvider(DateTime tradingday, BarProviderInfo prdinfo)
        {
            TradingDay = tradingday;

            if (prdinfo != null)
            {
                _prdinfo = prdinfo;

                _lstslice = CreateDateTimeSlice(tradingday, _prdinfo.Lstslice);
            }
            else
            {
                _logger.Error("BarProvider构造函数传入BarProviderInfo为NULL");
            }

        }

        public BarProvider(DateTime tradingday, BarProviderInfo prdinfo, BarSeries barlist)
        {
            TradingDay = tradingday;
            _prdinfo = prdinfo;

            _lstslice = CreateDateTimeSlice(tradingday, _prdinfo.Lstslice);

            if (barlist != null && barlist.Count > 0)
            {
                #region

                _bars.AddRange(barlist);

                int i = 0;
                for (; i < _lstslice.Count; ++i)
                {
                    if (barlist.Last.EndTime < _lstslice[i].BeginTime)
                    {
                        _posBar = i - 1;
                        _posTime = i;
                        break;
                    }
                    else if (barlist.Last.EndTime == _lstslice[i].BeginTime)
                    {
                        if (barlist.Last.BeginTime < _lstslice[i].BeginTime)
                        {
                            _posBar = i - 1;
                            _posTime = i;
                            break;
                        }
                        else if (barlist.Last.BeginTime == _lstslice[i].BeginTime)
                        {
                            _posBar = i;
                            _posTime = i;
                            break;
                        }
                    }
                    else if (barlist.Last.EndTime < _lstslice[i].EndTime)
                    {
                        _posBar = i;
                        _posTime = i;
                        break;
                    }
                }

                if (i == _lstslice.Count)
                {
                    _posTime = _lstslice.Count;
                    _posBar = i;
                }

                #endregion
            }
        }

        /// <summary>
        /// 到来一个新的Tick，更新Bar
        /// </summary>
        /// <param name="tick"></param>
        public void AddTick(Tick tick)
        {
            #region

            if (tick == null)
            {
                ChangeState = 4;
                return;
            }

            double turnover = 0;
            double volume = 0;

            lock (_lockobj)
            {
                // 第一个tick
                if (_lasttick != null)
                {
                    turnover = _lasttick.Turnover;
                    volume = _lasttick.Volume;
                }
                else
                {
                    if (_blive)
                        _diff = tick.DateTime - DateTime.Now;

                    foreach (var bar in _bars)
                    {
                        if (bar.TradingDate == TradingDay)
                        {
                            turnover = turnover + bar.Turnover;
                            volume = volume + bar.Volume;
                        }
                    }

                    //for (int i = _bars.Count - 1; i >= _bars.Count - _posBar - 1; --i)
                    //{
                    //    if (_bars[i].Open > 0)
                    //        break;

                    //    _bars[i].Open = _bars[i].High = _bars[i].Low = _bars[i].Close = _bars[i].PreClose = tick.OpenPrice;
                    //    _bars[i].OpenInterest = tick.PreOpenInterest;
                    //    if (i == _bars.Count - _posBar - 1)
                    //    {
                    //        if (tick.PreSettlementPrice > 0)
                    //            _bars[i].PreClose = tick.PreSettlementPrice;
                    //        else
                    //            _bars[i].PreClose = tick.PreClosePrice;
                    //    }
                    //}
                }

                if (IsEnd)
                {
                    ChangeState = 4;
                    return;
                }

                int idx;
                int ir = MoveTo(tick.DateTime, out idx);
                if (ir == -1)
                {
                    ChangeState = 4;
                    return;
                }

                _lasttick = tick;

                var baropencount = 0;
                var barclosecount = 0;
                for (; _posTime < idx; ++_posTime)
                {
                    DateTimeSlice currentT = _lstslice[_posTime];

                    if (_posBar == _posTime)
                    {
                        ++barclosecount;

                        _bars.Last.CloseBar(currentT.EndTime);
                        OnBarClosed(_bars.Last);
                    }
                    else
                    {
                        #region 20150628 去掉空Bar

                        if (AllBlankBar)
                        {
                            ++baropencount;
                            var bar = new Bar { BeginTime = currentT.BeginTime };
                            if (_posBar >= 0)
                            {
                                bar.Close = bar.PreClose = bar.Open = bar.High = bar.Low = _bars.Last.Close;
                                bar.OpenInterest = _bars.Last.OpenInterest;
                            }
                            else if (_lasttick != null)
                            {
                                if (_lasttick.PreSettlementPrice > 0)
                                    bar.Close =
                                        bar.PreClose = bar.Open = bar.High = bar.Low = _lasttick.PreSettlementPrice;
                                else
                                    bar.Close = bar.PreClose = bar.Open = bar.High = bar.Low = _lasttick.PreClosePrice;

                                bar.OpenInterest = _lasttick.PreOpenInterest;
                            }
                            bar.TradingDate = tick.TradingDay;
                            _bars.Add(bar);

                            ++barclosecount;
                            _bars.Last.CloseBar(currentT.EndTime);
                            OnBarClosed(_bars.Last);
                        }


                        #endregion

                        ++_posBar;


                    }
                }

                if (ir == 0)
                {
                    if (_posBar == _posTime)
                    {
                        Bar bar = _bars.Last;
                        bar.AddTick(tick);
                        bar.Turnover = bar.Turnover + tick.Turnover - turnover;
                        bar.Volume = bar.Volume + tick.Volume - volume;
                    }
                    else
                    {
                        Bar lastbar = _bars.Last;

                        Bar bar = new Bar { TradingDate = TradingDay };
                        bar.OpenBar(_lstslice[_posTime].BeginTime, tick, lastbar, _posTime == 0);
                        if (lastbar != null)
                        {
                            bar.Turnover = tick.Turnover - turnover;
                            bar.Volume = tick.Volume - volume;
                        }
                        else
                        {
                            bar.Turnover = tick.Turnover;
                            bar.Volume = tick.Volume;
                        }
                        _bars.Add(bar);

                        ++baropencount;
                        OnBarOpened(bar);

                        ++_posBar;
                    }
                }

                if (baropencount == 0 && barclosecount == 0)
                {
                    ChangeState = 0;
                    return;
                }

                if (baropencount > barclosecount)
                {
                    ChangeState = 2;
                    return;
                }

                if (barclosecount > baropencount)
                {
                    ChangeState = 1;
                    return;
                }

                ChangeState = 3;
            }

            #endregion
        }

        public void OnTimer(DateTime now)
        {
            if (IsEnd || _lasttick == null)
                return;

            int ir = 0;
            DateTime extime = (now + _diff);

            lock (_lockobj)
            {
                int idx;
                ir = MoveTo(extime, out idx);
                if (ir == -1) return;

                for (; _posTime < idx; ++_posTime)
                {
                    DateTimeSlice currentT = _lstslice[_posTime];

                    if (_posBar == _posTime)
                    {
                        if (_bars.Count > 0)
                        {
                            _bars.Last.CloseBar(currentT.EndTime);
                            OnBarClosed(_bars.Last);
                        }
                    }
                    else
                    {
                        #region 20150628 去掉空Bar

                        //Bar bar = new Bar();
                        //bar.BeginTime = _tradeDay + currentT.BeginTime;
                        //if (_posBar >= 0)
                        //{
                        //    bar.Close = bar.PreClose = bar.Open = bar.High = bar.Low = _bars.Last.Close;
                        //    bar.OpenInterest = _bars.Last.OpenInterest;
                        //}
                        //else if (_lasttick != null)
                        //{
                        //    if (_lasttick.PreSettlementPrice > 0)
                        //        bar.Close = bar.PreClose = bar.Open = bar.High = bar.Low = _lasttick.PreSettlementPrice;
                        //    else
                        //        bar.Close = bar.PreClose = bar.Open = bar.High = bar.Low = _lasttick.PreClosePrice;

                        //    bar.OpenInterest = _lasttick.PreOpenInterest;
                        //}

                        //_bars.Add(bar);

                        //_bars.Last.CloseBar(_tradeDay + currentT.EndTime);

                        #endregion

                        ++_posBar;

                        //OnBarClosed(_bars.Last);
                    }
                }
            }
        }

        /// <summary>
        /// 0, my; 1, not my; -1, invalid
        /// </summary>
        /// <param name="mytime"></param>
        /// <param name="nexttime"></param>
        /// <param name="sp"></param>
        /// <returns>0, my; 1, not my; 2, next</returns>
        int IsMyTime(DateTimeSlice mytime, DateTimeSlice nexttime, DateTime sp)
        {
            if (nexttime != null)
            {
                if (mytime.EndTime < nexttime.BeginTime)
                {
                    if (sp > mytime.EndTime && sp < nexttime.BeginTime)
                    {
                        TimeSpan ta = sp - mytime.EndTime;
                        TimeSpan tb = nexttime.BeginTime - sp;
                        if (tb < ta)
                        {
                            return 2;
                        }

                        return 0;
                    }
                }

                if (mytime.BeginTime <= sp && sp < nexttime.BeginTime)
                    return 0;
            }
            else
            {
                // 收盘5秒内的tick都计算
                if (mytime.BeginTime <= sp && sp < mytime.EndTime.AddSeconds(100))
                    return 0;
            }

            return 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="now"></param>
        /// <param name="idx">需要移到的位置</param>
        /// <returns>0,有效;-1无效;1,结束</returns>
        int MoveTo(DateTime now, out int idx)
        {
            idx = _posTime;

            #region -1 无效

            DateTimeSlice currentT = _lstslice[_posTime];
            if (now < currentT.BeginTime)
            {
                return -1;
            }

            #endregion

            #region 1 下一个


            for (; idx < _prdinfo.Lstslice.Count - 1; ++idx)
            {
                DateTimeSlice mytime = _lstslice[idx];
                DateTimeSlice nexttime = _lstslice[idx + 1];

                int ir = IsMyTime(mytime, nexttime, now);
                if (ir == 0) return 0;
                if (ir == 2)
                {
                    ++idx;
                    return 0;
                }
            }

            int bmy = IsMyTime(_lstslice[idx], null, now);
            if (bmy == 0) return 0;

            ++idx;

            #endregion

            return 1;
        }

        #region 事件

        public event BarEventHandler BarOpenedEvent;
        protected void OnBarOpened(Bar bar)
        {
            if (BarOpenedEvent != null)
            {
                BarOpenedEvent(bar);
            }
        }

        public event BarEventHandler BarChangedEvent;
        protected void OnBarChanged(Bar bar)
        {
            if (BarChangedEvent != null)
            {
                BarChangedEvent(bar);
            }
        }

        public event BarEventHandler BarClosedEvent;
        protected void OnBarClosed(Bar bar)
        {
            //_logger.Info("Tick 数:" + _tickcount.ToString() + "  Bar 数:" + _bars.Count.ToString());
            //_tickcount = 0;
            if (BarClosedEvent != null)
            {
                BarClosedEvent(bar);
            }
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeSpan">交易所时间 - 本地时间</param>
        public void SetDiffTime(TimeSpan timeSpan)
        {
            _diff = timeSpan;
        }

        public void AddBar(Bar newbar)
        {
            if (newbar == null)
            {
                ChangeState = 4;
                return;
            }

            lock (_lockobj)
            {
                if (IsEnd)
                {
                    ChangeState = 4;
                    return;
                }

                int idx;
                int ir = MoveTo(newbar.BeginTime, out idx);
                if (ir == -1)
                {
                    ChangeState = 4;
                    return;
                }

                var baropencount = 0;
                var barclosecount = 0;
                for (; _posTime < idx; ++_posTime)
                {
                    DateTimeSlice currentT = _lstslice[_posTime];

                    if (_posBar == _posTime)
                    {

                    }
                    else
                    {
                        #region 20150628 去掉空Bar

                        //Bar bar = new Bar();
                        //bar.BeginTime = _tradeDay + currentT.BeginTime;
                        //if (_posBar >= 0)
                        //{
                        //    bar.Close = bar.PreClose = bar.Open = bar.High = bar.Low = _bars.Last.Close;
                        //    bar.OpenInterest = _bars.Last.OpenInterest;
                        //}

                        //_bars.Add(bar);

                        //_bars.Last.CloseBar(_tradeDay + currentT.EndTime);

                        #endregion

                        ++_posBar;

                        //OnBarClosed(_bars.Last);
                    }
                }

                if (ir == 0)
                {
                    if (_posBar == _posTime)
                    {
                        Bar bar = _bars.Last;
                        bar.AddBar(newbar);
                    }
                    else
                    {
                        Bar bar = new Bar { TradingDate = newbar.TradingDate };
                        bar.OpenBar(newbar);
                        _bars.Add(bar);

                        ++baropencount;

                        OnBarOpened(bar);

                        ++_posBar;
                    }

                    //if (_posBar == _posTime)
                    {
                        DateTimeSlice currentT = _lstslice[_posTime];
                        if (newbar.EndTime >= currentT.EndTime)
                        {
                            ++barclosecount;

                            _bars.Last.CloseBar(currentT.EndTime);
                            OnBarClosed(_bars.Last);
                        }
                    }
                }

                if (baropencount == 0 && barclosecount == 0)
                {
                    ChangeState = 0;
                    return;
                }

                if (baropencount > barclosecount)
                {
                    ChangeState = 2;
                    return;
                }

                if (barclosecount > baropencount)
                {
                    ChangeState = 1;
                    return;
                }

                ChangeState = 3;
            }
        }

        public static BarProvider Create(DateTime tradeDay, InstrumentManager instmgr, string instid, int offset,
            int interval, EnumBarType bartype, BarSeries barlist)
        {
            List<TimeSlice> tradingtime = instmgr.GetTradingTime(tradeDay, instid);
            BarProviderInfo prdinfo = BaseBarHelper.Instance.GetBarProviderInfo(tradingtime, offset, interval, bartype);
            if (barlist != null)
                return new BarProvider(tradeDay, prdinfo, barlist);
            else
                return new BarProvider(tradeDay, prdinfo);

        }
    }
}