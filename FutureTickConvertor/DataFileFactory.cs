using System;
using Ats.Core;

namespace FutureTickConvertor
{
    /// <summary>
    /// 数据文件工厂
    /// </summary>
    public class DataFileFactory
    {
        /// <summary>
        /// 创建文件读取类
        /// </summary>
        /// <param name="market"></param>
        /// <returns></returns>
        public static FutureTickFileNew CreateTickFile(EnumMarket market)
        {
            switch (market)
            {
                case EnumMarket.期货: return new FutureTickFileNew();
            }

            throw new Exception("没有" + market.ToString() + "对应的Tick文件读取类");
        }

 
    }
}
