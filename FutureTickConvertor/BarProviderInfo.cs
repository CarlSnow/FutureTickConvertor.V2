using System.Collections.Generic;
using Ats.Core;

namespace FutureTickConvertor
{
    /// <summary>
    /// K线提供器信息
    /// </summary>
    public class BarProviderInfo
    {
        /// <summary>
        /// 如果offset>0，slice[0]是offset
        /// 如果offset=0，slice[0]是第一个bar
        /// </summary>
        public List<TimeSlice> Lstslice;

        /// <summary>
        /// 偏移，单位：秒
        /// offset >= 0
        /// </summary>
        public int Offset;

        /// <summary>
        /// 开盘时间，收盘时间
        /// </summary>
        public TimeSlice Livingtime;

        /// <summary>
        /// k线宽度数量
        /// </summary>
        public int Interval;

        /// <summary>
        /// k线宽度单位
        /// </summary>
        public EnumBarType Bartype;
    }
}
