using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ats.Core;

namespace FutureTickConvertor
{
    /// <summary>
    /// 交易时段序列
    /// </summary>
    public class TradingFrameSeries : AtsList<TradingFrame>
    {

        /// <summary>
        /// 获取指定交易日时间区域的交易时段
        /// </summary>
        /// <param name="date">指定交易日</param>
        /// <returns>交易时段</returns>
        public List<TimeSlice> GetTradingTimes(DateTime date )
        {
           
            for (int i = Count-1; i >=0; i--)
            {
                TradingFrame frame = this[i];
                DateTime tBegin = frame.BeginDay.Date;

                if (date.Date >= tBegin)
                {
                    return frame.TradingTimes;
                } 
            }
            
            return null;
        }
    }
}
