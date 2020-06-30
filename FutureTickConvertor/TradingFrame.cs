using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ats.Core;

namespace FutureTickConvertor
{
    /// <summary>
    /// 交易时间阶段
    /// </summary>
    public class TradingFrame
    {
     
        /// <summary>
        /// 该交易时段开始启用的第一天
        /// 如果后面没有新的Frame，就说明是最新的交易时段
        /// </summary>
        public DateTime BeginDay;

        /// <summary>
        /// 交易时段
        /// </summary>
        public List<TimeSlice> TradingTimes;

        public TradingFrame()
        { 
        }

        public TradingFrame(DateTime beginDay, List<TimeSlice> tradingTimes)
        {
            BeginDay = beginDay;
            TradingTimes = tradingTimes;
        }
    }
}
