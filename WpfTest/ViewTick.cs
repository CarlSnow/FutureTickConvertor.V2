using Ats.Core;
using System;

namespace WpfTest
{
    public class ViewTick
    {
        public double Change { get; }
        public double Turnover { get; set; }
        public double Volume { get; set; }
        public double LastPrice { get; set; }
        public double LowPrice { get; set; }
        public double HighPrice { get; set; }
        public double OpenPrice { get; set; }
        public int BidVolume1 { get; }
        public double BidPrice1 { get; }
        public int AskVolume1 { get; }
        public double AskPrice1 { get; }
        public Quote Quote { get; set; }
        public DateTime TradingDay { get; set; }
        public TimeSpan TimeNow { get; }
        public DateTime DateTime { get; set; }
        public string InstrumentID { get; set; }
        public string TickNow { get; set; }
        public double VolumeNow { get; set; }
        public ViewTick(Tick tick, double preVolume)
        {
            Change = tick.Change;
            Turnover = tick.Turnover;
            Volume = tick.Volume;
            LastPrice = tick.LastPrice;
            LowPrice = tick.LowPrice;
            HighPrice = tick.HighPrice;
            OpenPrice = tick.OpenPrice;
            BidVolume1 = tick.BidVolume1;
            BidPrice1 = tick.BidPrice1;
            AskVolume1 = tick.AskVolume1;
            AskPrice1 = tick.AskPrice1;
            Quote = tick.Quote;
            TradingDay = tick.TradingDay;
            TimeNow = tick.TimeNow;
            DateTime = tick.DateTime;
            InstrumentID = tick.InstrumentID;
            TickNow = string.Format("{0}:{1}:{2}.{3}", TimeNow.Hours.ToString("00"), TimeNow.Minutes.ToString("00"), TimeNow.Seconds.ToString("00"), TimeNow.Milliseconds.ToString("000"));
            VolumeNow = Volume - preVolume;
        }
    }
}
