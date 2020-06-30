using Ats.Core;
using System;

namespace WpfTest
{
    public class ViewMin
    {
        public DateTime TradingDate { get; set; }
        public DateTime BeginTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; }
        public double High { get; set; }
        public double Open { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public double PreClose { get; set; }
        public double Volume { get; set; }
        public double Turnover { get; set; }
        public double OpenInterest { get; set; }
        public double BarChange { get; }
        public double Change { get; }
        public bool IsCompleted { get; set; }
        public string BeginTimeStr { get; set; }
        public string EndTimeStr { get; set; }

        public ViewMin(Bar bar)
        {
            TradingDate = bar.TradingDate;
            BeginTime = bar.BeginTime;
            EndTime = bar.EndTime;
            Duration = bar.Duration;
            High = bar.High;
            Open = bar.Open;
            Low = bar.Low;
            Close = bar.Close;
            PreClose = bar.PreClose;
            Volume = bar.Volume;
            Turnover = bar.Turnover;
            OpenInterest = bar.OpenInterest;
            BarChange = bar.BarChange;
            Change = bar.Change;
            IsCompleted = bar.IsCompleted;

            BeginTimeStr = string.Format("{0}:{1}:{2}", BeginTime.Hour.ToString("00"), BeginTime.Minute.ToString("00"), BeginTime.Second.ToString("00"));
            EndTimeStr = string.Format("{0}:{1}:{2}", EndTime.Hour.ToString("00"), EndTime.Minute.ToString("00"), EndTime.Second.ToString("00"));
        }
    }
}
