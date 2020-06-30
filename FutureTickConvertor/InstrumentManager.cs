using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Ats.Core;

namespace FutureTickConvertor
{
    /// <summary>
    /// 品种管理器
    /// </summary>
    public class InstrumentManager
    {
        #region 内部合约编号

        /// <summary>
        /// 期货主力
        /// </summary>
        public const string FuMainId = "9999";
        /// <summary>
        /// 期货当月连续
        /// </summary>
        public const string FuCurMonthId = "9998";
        /// <summary>
        /// 期货下月连续
        /// </summary>
        public const string FuNextMonthId = "9997";
        /// <summary>
        /// 期货下季连续
        /// </summary>
        public const string FuNextQuarterId = "9996";
        /// <summary>
        /// 期货隔季连续
        /// </summary>
        public const string FuNNextQuarterId = "9995";
        /// <summary>
        /// 期货指数
        /// </summary>
        public const string FuIndexId = "9990";

        /// <summary>
        /// 股票期权当月连续
        /// </summary>
        public const string SoCurMonthId = "X8";
        /// <summary>
        /// 股票期权下月连续
        /// </summary>
        public const string SoNextMonthId = "X7";
        /// <summary>
        /// 股票期权下季连续
        /// </summary>
        public const string SoNextQuarterId = "X6";
        /// <summary>
        /// 股票期权隔季连续
        /// </summary>
        public const string SoNNextQuarterId = "X5";

        #endregion

        /// <summary>
        /// 所有的品种
        /// </summary>
        readonly Dictionary<string, Instrument> _allInstruments = new Dictionary<string, Instrument>();

        /// <summary>
        /// 所有的交易所
        /// </summary>
        readonly Dictionary<string, Exchange> _allExchanges = new Dictionary<string, Exchange>();

        /// <summary>
        /// 所有的期货产品
        /// </summary>
        readonly Dictionary<string, FutureProduct> _allProducts = new Dictionary<string, FutureProduct>();

        private Dictionary<string, DateTime> _mapRefOptionCurMonth; 

        readonly TradingFrameManager _tfMgr = new TradingFrameManager();

        /// <summary>
        /// 合约
        /// </summary>
        /// <param name="instrId"></param>
        /// <returns></returns>
        public Instrument this[string instrId]
        {
            get
            {
                Instrument instr;
                _allInstruments.TryGetValue(instrId, out instr);
                return instr;
            }
        }

        public bool HasInstrument(string instrId)
        {
            return _allInstruments.ContainsKey(instrId);
        }

        public IEnumerable<Instrument> AllInstruments
        {
            get
            {
                return _allInstruments.Values;
            }
        }

        public Exchange GetExchangeByInstrumentId(string instrumentid)
        {
            Instrument instr = this[instrumentid];
            if (instr != null)
            {
                return GetExchange(instr.ExchangeID);
            }

            return null;
        }

        public Exchange GetExchange(string exchangeid)
        {
            Exchange exchange;
            _allExchanges.TryGetValue(exchangeid, out exchange);
            return exchange;
        }

        public IEnumerable<Exchange> AllExchanges
        {
            get
            {
                return _allExchanges.Values;
            }
        }

        /// <summary>
        /// 根据枚举类型，获取交易所
        /// </summary>
        /// <param name="exchange"></param>
        /// <returns></returns>
        public Exchange GetExchange(EnumExchange exchange)
        {
            switch (exchange)
            {
                case EnumExchange.中金所: return GetExchange("CFFEX");
                case EnumExchange.大商所: return GetExchange("DCE");
                case EnumExchange.上期所: return GetExchange("SHFE");
                case EnumExchange.郑商所: return GetExchange("CZCE");
                case EnumExchange.上证所: return GetExchange("SH");
                case EnumExchange.深交所: return GetExchange("SZ");
                default: throw new Exception("非法的交易所代码:" + exchange.ToString());
            }
        }

        /// <summary>
        /// 根据交易所ID获取交易所枚举类型
        /// </summary>
        /// <param name="exchangeId"></param>
        /// <returns></returns>
        public static EnumExchange GetEnumByCode(string exchangeId)
        {
            switch ( exchangeId.ToUpper())
            {
                case "CFFEX": return EnumExchange.中金所;
                case "DCE": return EnumExchange.大商所;
                case "SHFE": return EnumExchange.上期所;
                case "CZCE": return EnumExchange.郑商所;
                case "SH": return EnumExchange.上证所;
                case "SZ": return EnumExchange.深交所;
                //default: throw new Exception("不认识的交易所代码");
                default: return EnumExchange.未知;
            }
        }

        public void Clear()
        {
            _allInstruments.Clear();
            _allExchanges.Clear();
        }


        /// <summary>
        /// 获取品种的交易时间
        /// </summary>
        /// <param name="instrumentid">品种</param>
        /// <returns></returns>
        public List<TimeSlice> GetTradingTime(DateTime tradingday, string instrumentid)
        {
            #region
            
            Instrument instr = this[instrumentid];
            if (instr != null && _tfMgr != null)
            {
                string productid = GetProduct(instrumentid);
                if (instr.Market == EnumMarket.期货期权)
                {
                    productid = ((Option)instr).ProductID;
                }
                return _tfMgr.GetTradingFrame(tradingday, instr.Market, instr.ExchangeID, productid);
            }

            if (instr == null)
            {
                //期货没有后缀
                var prodct = GetProduct(instrumentid);
                if (prodct == "" || prodct[0] == '.')
                {
                    return null;
                }
                FutureProduct product = GetFutureProduct(prodct);
                if (product == null) return null;

                if (product.AllSlice != null && product.AllSlice.Count > 0)
                    return product.AllSlice;

                Exchange exchange = GetExchange(product.ExchangeID);
                if (exchange == null) return null;

                return exchange.AllSlice;
            }
            else
            { 
                if (instr.Market == EnumMarket.期货)
                {
                    Future future = instr as Future;
                    if (future == null) return null;

                    FutureProduct product = GetFutureProduct(future.ProductID);
                    if (product == null) return null;

                    if (product.AllSlice != null && product.AllSlice.Count > 0)
                        return product.AllSlice;
                }

                Exchange exchange = GetExchange(instr.ExchangeID);
                if (exchange == null) return null;

                return exchange.AllSlice;
            }
            return null;
            #endregion
        }

        public FutureProduct GetFutureProduct(string productId)
        {
            FutureProduct product;
            _allProducts.TryGetValue(productId, out product);

            return product;
        }

        #region 初始化

        public void Merge(FutureManager fmgr)
        {
            foreach (Future stk in fmgr.AllInstruments)
            {
                if (!_allInstruments.ContainsKey(stk.ID))
                {
                    _allInstruments.Add(stk.ID, stk);
                }
            }

            foreach (Exchange exchange in fmgr.AllExchanges)
            {
                if (!_allExchanges.ContainsKey(exchange.ExchangeID))
                {
                    _allExchanges.Add(exchange.ExchangeID, exchange);
                }
            }

            foreach (FutureProduct product in fmgr.AllProducts)
            {
                if (!_allProducts.ContainsKey(product.ProductID))
                {
                    _allProducts.Add(product.ProductID, product);
                }
            }
        }

        /// <summary>
        /// 载入股票
        /// </summary>
        /// <param name="strDir">配置目录</param>
        /// <param name="instrIdlist">品种代码表</param>
        /// <param name="market">市场</param>
        /// <param name="isLive">是否实盘</param>
        /// <returns></returns>
        public bool Load(
            string strDir,
            List<string> instrIdlist,
            EnumMarket market, 
            bool isLive)
        {
            _tfMgr.LoadData(strDir + "\\TradingFrame.xml");

            switch (market)
            {
                case EnumMarket.期货:
                    #region
                    
                    FutureManager fmgr = new FutureManager();

                    if (isLive)
                    {
                        if (!FutureFile.Read(strDir, fmgr, instrIdlist, isLive))
                        {
                            Clear();
                            return false;
                        }
                    }
                    else
                    {
                        if (!FutureFile.Read(strDir, fmgr))
                        {
                            Clear();
                            return false;
                        }
                    }

                    Merge(fmgr);
                    return true;
                    #endregion
            }

            return false;
        }

        public bool Load(
            string strDir,
            List<string> instrIdlist,
            EnumMarket market,
            DateTime tradingDay)
        {
            _tfMgr.LoadData(strDir + "\\TradingFrame.xml");

            switch (market)
            {
                case EnumMarket.期货:
                    return Load(strDir, instrIdlist, market, false);
            }

            return false;
        }

        #endregion


        private EnumMarket GetMarketByExchangeId(EnumExchange enumExchange)
        {
            switch(enumExchange)
            {
                case EnumExchange.大商所:
                case EnumExchange.上期所:
                case EnumExchange.郑商所:
                case EnumExchange.中金所:
                    return EnumMarket.期货;
                case EnumExchange.上证所:
                case EnumExchange.深交所:
                    return EnumMarket.股票;
                case EnumExchange.新交所:
                    return EnumMarket.外盘;
                default:
                    return EnumMarket.期货;
            }
        }

        public bool IsNormalOpenCloseTime(string instrumentid)
        {
            Instrument instr = this[instrumentid];
            if (instr == null) return true;

            if (instr.Market == EnumMarket.期货)
            {
                Future future = instr as Future;
                if (future == null) return true;

                FutureProduct product = GetFutureProduct(future.ProductID);
                if (product == null) return true;

                if (product.AllSlice != null && product.AllSlice.Count > 0)
                {
                    return false;
                }
            }

            return true; 
        }

        public TimeSlice GetLivingTime( DateTime tradingday, string instrumentid)
        {
            Instrument instr = this[instrumentid];
            if (instr != null && _tfMgr != null)
            {
                string productid = GetProduct(instrumentid);
                return _tfMgr.GetLivingTime(tradingday, instr.Market, instr.ExchangeID, productid);
            }

            if (instr == null)
            {
                //期货没有后缀
                var prodct = GetProduct(instrumentid);
                if (prodct == "" || prodct[0] == '.')
                {
                    return null;
                }
                FutureProduct product = GetFutureProduct(prodct);
                if (product == null) return null;

                if (product.AllSlice != null && product.AllSlice.Count > 0)
                {
                    TimeSlice tslice = new TimeSlice
                    {
                        BeginTime = product.AllSlice[0].BeginTime,
                        EndTime = product.AllSlice[product.AllSlice.Count - 1].EndTime
                    };
                    return tslice;
                }
                Exchange exchange = GetExchange(product.ExchangeID);
                if (exchange == null) return null;

                if (exchange.AllSlice != null && exchange.AllSlice.Count > 0)
                {
                    TimeSlice tslice = new TimeSlice
                    {
                        BeginTime = exchange.AllSlice[0].BeginTime,
                        EndTime = exchange.AllSlice[exchange.AllSlice.Count - 1].EndTime
                    };
                    return tslice;
                }
            }
            else
            {
                if (instr.Market == EnumMarket.期货)
                {
                    Future future = instr as Future;
                    if (future == null) return null;

                    FutureProduct product = GetFutureProduct(future.ProductID);
                    if (product == null) return null;

                    if (product.AllSlice != null && product.AllSlice.Count > 0)
                    {
                        TimeSlice tslice = new TimeSlice();
                        tslice.BeginTime = product.AllSlice[0].BeginTime;
                        tslice.EndTime = product.AllSlice[product.AllSlice.Count - 1].EndTime;
                        return tslice;
                    }
                }

                Exchange exchange = GetExchange(instr.ExchangeID);
                if (exchange == null) return null;

                if (exchange.AllSlice != null && exchange.AllSlice.Count > 0)
                {
                    TimeSlice tslice = new TimeSlice();
                    tslice.BeginTime = exchange.AllSlice[0].BeginTime;
                    tslice.EndTime = exchange.AllSlice[exchange.AllSlice.Count - 1].EndTime;
                    return tslice;
                }
            }



            return null;
        }

        /// <summary>
        /// 通过品种代码获取品种(把品种代码中的数字去掉)
        /// </summary>
        /// <param name="insId">品种代码</param>
        /// <returns></returns>
        private string GetProduct(string insId)
        {
            #region 获取品种
            return Regex.Replace(insId, @"\d", "");
            #endregion
        }

        public DateTimeSlice GetLivingDateTime(DateTime tradingday, string instrumentid)
        {
            TimeSlice tslice = GetLivingTime(tradingday, instrumentid);
            return new DateTimeSlice()
            {
                BeginTime = YfTimeHelper.GetDateTimeByTradingDay(tradingday, tslice.BeginTime),
                EndTime = YfTimeHelper.GetDateTimeByTradingDay(tradingday, tslice.EndTime)
            };
        }


        public List<Instrument> ExpandInstrument(string instId, EnumMarket market)
        {
            if (market == EnumMarket.股票期权)
            {
                if (instId.Length < 11) // 510050X8.SH
                    return null;

                if (instId.Contains("X"))
                {
                    var refstock = instId.Remove(6, 2);
                    if (_mapRefOptionCurMonth.ContainsKey(refstock))
                    {
                        var curMonth = _mapRefOptionCurMonth[refstock];

                        #region 计算是哪个月的合约

                        switch (instId[7])
                        {
                            case '7': // 下月
                                curMonth = curMonth.AddMonths(1);
                                break;
                            case '6': // 下季
                                {
                                    var itmp = curMonth.Month % 3;
                                    switch (itmp)
                                    {
                                        case 0:
                                            curMonth = curMonth.AddMonths(3);
                                            break;
                                        case 1:
                                            curMonth = curMonth.AddMonths(2);
                                            break;
                                        default:
                                            curMonth = curMonth.AddMonths(4);
                                            break;
                                    }
                                    break;
                                }
                            case '5': // 隔季
                                {
                                    var itmp = curMonth.Month % 3;
                                    switch (itmp)
                                    {
                                        case 0:
                                            curMonth = curMonth.AddMonths(6);
                                            break;
                                        case 1:
                                            curMonth = curMonth.AddMonths(5);
                                            break;
                                        default:
                                            curMonth = curMonth.AddMonths(7);
                                            break;
                                    }
                                    break;
                                }
                        }

                        #endregion

                        var lstInsts = new List<Instrument>();

                        foreach (var inst in AllInstruments)
                        {
                            if (inst is Option)
                            {
                                var option = inst as Option;
                                if (option.RefInstrumentID == refstock && option.EndDate.Month == curMonth.Month)
                                {
                                    lstInsts.Add(option);
                                }
                            }
                        }

                        return lstInsts;
                    }
                }
            }

            var instrument = this[instId];
            if (instrument != null)
            {
                return new List<Instrument>(1) {instrument};
            }

            return null;
        }
    }
}
