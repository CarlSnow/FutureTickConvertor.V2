using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Ats.Core;

namespace FutureTickConvertor
{
    /// <summary>
    /// 交易时间管理器
    /// 主要针对股指调整交易时间等事件
    /// 管理所有品种的交易时段（可以替代future.tpl）
    /// </summary>
    public class TradingFrameManager
    {
        //股票是整个交易所的
        //期货是Product可能会调整交易时间
        //主要考虑2个股票交易所+4个期货交易所

        //只有部分品种或者交易所存在交易时段变更的情况

        #region 变量

        const string IdAttr = "id";
        const string NameAttr = "name";
        const string TradingTimeTag = "TradingTime";
        const string BeginDayTag = "BeginDay";
        private const string UpLimit = "UpLimit";
        
        #region 交易时段

        /// <summary>
        /// 股票的历史交易时段
        /// 键-交易所ID
        /// </summary>
        Dictionary<string, TradingFrameSeries> _dictStockTradingFrame = new Dictionary<string, TradingFrameSeries>();

        /// <summary>
        /// 期货的历史交易时段
        /// 键-Product
        /// </summary>
        Dictionary<string, TradingFrameSeries> _dictFutureTradingFrame = new Dictionary<string, TradingFrameSeries>();

        /// <summary>
        /// 股票期权的历史交易时段
        /// 键-交易所ID
        /// </summary>
        Dictionary<string, TradingFrameSeries> _dictStockOptionTradingFrame = new Dictionary<string, TradingFrameSeries>();

        /// <summary>
        /// 期货期权的历史交易时段
        /// 键-Product
        /// </summary>
        Dictionary<string, TradingFrameSeries> _dictFutureOptionTradingFrame = new Dictionary<string, TradingFrameSeries>();

        Dictionary<string,double>_limitDictionary = new Dictionary<string, double>();

            #endregion

        #region 交易所默认时段

        //如果没有配置某个Product的交易时段，则采用其所属市场的交易所默认时段

        /// <summary>
        /// 如果没有找到任何数据，使用该默认时段
        /// 股票+股指期货 最新的时段
        /// </summary>
        List<TimeSlice> _defaultFrame = new List<TimeSlice>();

        /// <summary>
        /// 股票默认时段
        /// </summary>
        Dictionary<string, List<TimeSlice>> _dictDefaultStockTradingFrame = new Dictionary<string,List<TimeSlice>>();

        /// <summary>
        /// 股票期权默认时段
        /// </summary>
        Dictionary<string, List<TimeSlice>> _dictDefaultStockOptionTradingFrame = new Dictionary<string, List<TimeSlice>>();

        /// <summary>
        /// 期货默认时段
        /// </summary>
        Dictionary<string, List<TimeSlice>> _dictDefaultFutureTradingFrame = new Dictionary<string, List<TimeSlice>>();

        /// <summary>
        /// 期货期权默认时段
        /// </summary>
        Dictionary<string, List<TimeSlice>> _dictDefaultFutureOptionTradingFrame = new Dictionary<string, List<TimeSlice>>();
        #endregion
        #endregion


        /// <summary>
        /// 载入数据
        /// </summary>
        /// <param name="configFile">配置文件路径</param>
        public void LoadData( string configFile = "" )
        {
            //默认的
            _defaultFrame.Clear();
            _defaultFrame.Add( new TimeSlice( new TimeSpan(9,30,0),new TimeSpan(11,30,0) ));
            _defaultFrame.Add( new TimeSlice( new TimeSpan(13,0,0),new TimeSpan(15,0,0) ));

            _dictDefaultStockTradingFrame.Clear();
            _dictDefaultStockOptionTradingFrame.Clear();
            _dictDefaultFutureTradingFrame.Clear();
            _dictDefaultFutureOptionTradingFrame.Clear();

            _dictStockTradingFrame.Clear();
            _dictStockOptionTradingFrame.Clear();
            _dictFutureTradingFrame.Clear();
            _dictFutureOptionTradingFrame.Clear();

            _limitDictionary.Clear();


            if (configFile == "")
            { 
                //默认文件路径
                configFile = AppDomain.CurrentDomain.BaseDirectory + "\\Config\\TradingFrame.xml";
            }

            if (File.Exists(configFile))
            { 
                //解析XML文件，载入所有的时段数据
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(configFile);

                    XmlElement rootStock = doc.DocumentElement.GetElementsByTagName("Stock")[0] as XmlElement;
                    if (rootStock != null)
                    {
                        LoadStock(rootStock);
                    }
                    XmlElement rootStockOption = doc.DocumentElement.GetElementsByTagName("StockOption")[0] as XmlElement;
                    if (rootStockOption != null)
                    {
                        LoadStockOption(rootStockOption);
                    }

                    XmlElement rootFuture = doc.DocumentElement.GetElementsByTagName("Future")[0] as XmlElement;
                    if (rootFuture != null)
                    {
                        LoadFuture(rootFuture);
                    }

                    XmlElement rootFutureOption = doc.DocumentElement.GetElementsByTagName("FutureOption")[0] as XmlElement;
                    if (rootFutureOption != null)
                    {
                        LoadFutureOption(rootFutureOption);
                    }
                }
                catch (Exception ex)
                {
                    string strex = ex.Message;
                } 
            }
        }

        /// <summary>
        /// 载入股票部分
        /// </summary>
        /// <param name="rootXml"></param>
        private void LoadStock(XmlElement rootXml)
        {
            foreach (XmlNode node in rootXml)
            {
                #region 股票

                XmlElement exElem = node as XmlElement;
                if (exElem == null) continue;

                //解析交易所ID，默认时间
                string exchangeId = exElem.GetAttribute(IdAttr).Trim();
                List<TimeSlice> lstTimeSlice = ParseTimeSlice(exElem.GetAttribute(TradingTimeTag).Trim());

                //维护股票的默认时间段
                _dictDefaultStockTradingFrame.Add(exchangeId, lstTimeSlice);

                TradingFrameSeries lstTradeFrames = new TradingFrameSeries();

                //然后读取所有的交易所
                foreach (XmlNode subNode in exElem.ChildNodes)
                {
                    XmlElement subElem = subNode as XmlElement;
                     
                    DateTime beginDay = ParseDate(subElem.GetAttribute(BeginDayTag).Trim());
                    List<TimeSlice> tradingFrameExch = ParseTimeSlice(subElem.GetAttribute(TradingTimeTag).Trim());

                    var i = 0;
                    for (; i < lstTradeFrames.Count; ++i)
                    {
                        if (beginDay < lstTradeFrames[i].BeginDay)
                        {
                            lstTradeFrames.Insert(i, new TradingFrame(beginDay, tradingFrameExch));
                            break;
                        }
                    }

                    if (i == lstTradeFrames.Count)
                        lstTradeFrames.Add(new TradingFrame(beginDay, tradingFrameExch));

                    //lstTradeFrames.Add(new TradingFrame(beginDay, tradingFrameExch));

                }
                //交易所的
                _dictStockTradingFrame.Add(exchangeId, lstTradeFrames);
                #endregion
            }
        }

        private void LoadStockOption(XmlElement rootXml)
        {
            foreach (XmlNode node in rootXml)
            {
                #region 股票期权

                XmlElement exElem = node as XmlElement;
                if (exElem == null) continue;

                //解析交易所ID，默认时间
                string exchangeId = exElem.GetAttribute(IdAttr).Trim();
                List<TimeSlice> lstTimeSlice = ParseTimeSlice(exElem.GetAttribute(TradingTimeTag).Trim());

                //维护股票的默认时间段
                _dictDefaultStockOptionTradingFrame.Add(exchangeId, lstTimeSlice);

                TradingFrameSeries lstTradeFrames = new TradingFrameSeries();

                //然后读取所有的交易所
                foreach (XmlNode subNode in exElem.ChildNodes)
                {
                    XmlElement subElem = subNode as XmlElement;
                     
                    DateTime beginDay = ParseDate(subElem.GetAttribute(BeginDayTag).Trim());
                    List<TimeSlice> tradingFrameExch = ParseTimeSlice(subElem.GetAttribute(TradingTimeTag).Trim());

                    var i = 0;
                    for (; i < lstTradeFrames.Count; ++i)
                    {
                        if (beginDay < lstTradeFrames[i].BeginDay)
                        {
                            lstTradeFrames.Insert(i, new TradingFrame(beginDay, tradingFrameExch));
                            break;
                        }
                    }

                    if (i == lstTradeFrames.Count)
                        lstTradeFrames.Add(new TradingFrame(beginDay, tradingFrameExch));

                }
                //交易所的
                _dictStockOptionTradingFrame.Add(exchangeId, lstTradeFrames);
                #endregion
            }
        }

        private void LoadFuture(XmlElement rootXml)
        {
            

            foreach (XmlNode node in rootXml)
            {
                #region 期货

                XmlElement exElem = node as XmlElement;
                if (exElem == null) continue;

                //解析4个交易所

                //解析交易所ID，默认时间
                string exchangeId = exElem.GetAttribute(IdAttr).Trim();
                List<TimeSlice> lstTimeSlice = ParseTimeSlice(exElem.GetAttribute(TradingTimeTag).Trim());

                //维护股票的默认时间段
                _dictDefaultFutureTradingFrame.Add(exchangeId, lstTimeSlice);
                //然后每个交易所下面的品种都要整理出来
                if (exElem.ChildNodes.Count > 0)
                {
                    XmlNode productsNode = exElem.ChildNodes[0];

                    //然后读取所有的交易所
                    foreach (XmlNode prodNode in productsNode.ChildNodes)
                    {
                        //这里是一个品种的节点
                        XmlElement prodElem = prodNode as XmlElement;

                        string productId = prodElem.GetAttribute(IdAttr).Trim();
                        TradingFrameSeries lstTradeFrames = new TradingFrameSeries();

                        foreach (XmlNode frameNode in prodNode.ChildNodes)
                        {
                            XmlElement frameElem = frameNode as XmlElement;
                            if (frameElem != null && frameElem.Name == "TradingFrame")
                            {
                                DateTime beginDay = ParseDate(frameElem.GetAttribute(BeginDayTag).Trim());
                                List<TimeSlice> tradingFrameExch = ParseTimeSlice(frameElem.GetAttribute(TradingTimeTag).Trim());

                                var i = 0;
                                for (; i < lstTradeFrames.Count; ++i)
                                {
                                    if (beginDay < lstTradeFrames[i].BeginDay)
                                    {
                                        lstTradeFrames.Insert(i, new TradingFrame(beginDay, tradingFrameExch));
                                        break;
                                    }
                                }

                                if (i == lstTradeFrames.Count)
                                    lstTradeFrames.Add(new TradingFrame(beginDay, tradingFrameExch));

                                //lstTradeFrames.Add(new TradingFrame(beginDay, tradingFrameExch));
                            }
                            else if (frameElem != null && frameElem.Name == "LimitValue")
                            {
                                double limit = 0.0;
                                double.TryParse(frameElem.GetAttribute(UpLimit).Trim(), out limit);
                                _limitDictionary.Add(productId.ToLower(), limit);
                            }                            
                        }
                        _dictFutureTradingFrame.Add(productId, lstTradeFrames);             

                    }
                }
  
                #endregion
            }
        }

        private void LoadFutureOption(XmlElement rootXml)
        {
            foreach (XmlNode node in rootXml)
            {
                #region 期货期权

                XmlElement exElem = node as XmlElement;
                if (exElem == null) continue;

                //解析4个交易所

                //解析交易所ID，默认时间
                string exchangeId = exElem.GetAttribute(IdAttr).Trim();
                List<TimeSlice> lstTimeSlice = ParseTimeSlice(exElem.GetAttribute(TradingTimeTag).Trim());

                //维护股票的默认时间段
                _dictDefaultFutureOptionTradingFrame.Add(exchangeId, lstTimeSlice);
                //然后每个交易所下面的品种都要整理出来
                if (exElem.ChildNodes.Count > 0)
                {
                    XmlNode productsNode = exElem.ChildNodes[0];

                    //然后读取所有的交易所
                    foreach (XmlNode prodNode in productsNode.ChildNodes)
                    {
                        //这里是一个品种的节点
                        XmlElement prodElem = prodNode as XmlElement;

                        string productId = prodElem.GetAttribute(IdAttr).Trim();
                        TradingFrameSeries lstTradeFrames = new TradingFrameSeries();

                        foreach (XmlNode frameNode in prodNode.ChildNodes)
                        {
                            XmlElement frameElem = frameNode as XmlElement;
                            if (frameElem != null && frameElem.Name == "TradingFrame")
                            {
                                DateTime beginDay = ParseDate(frameElem.GetAttribute(BeginDayTag).Trim());
                                List<TimeSlice> tradingFrameExch = ParseTimeSlice(frameElem.GetAttribute(TradingTimeTag).Trim());

                                var i = 0;
                                for (; i < lstTradeFrames.Count; ++i)
                                {
                                    if (beginDay < lstTradeFrames[i].BeginDay)
                                    {
                                        lstTradeFrames.Insert(i, new TradingFrame(beginDay, tradingFrameExch));
                                        break;
                                    }
                                }

                                if (i == lstTradeFrames.Count)
                                    lstTradeFrames.Add(new TradingFrame(beginDay, tradingFrameExch));

                                //lstTradeFrames.Add(new TradingFrame(beginDay, tradingFrameExch));
                            }
                            else if (frameElem != null && frameElem.Name == "LimitValue")
                            {
                                double limit = 0.0;
                                double.TryParse(frameElem.GetAttribute(UpLimit).Trim(), out limit);
                                _limitDictionary.Add(productId.ToLower(), limit);
                            }
                        }
                        _dictFutureOptionTradingFrame.Add(productId, lstTradeFrames);


                    }
                }

                #endregion
            }
        }

        private DateTime ParseDate(string str)
        { 
            int year = int.Parse( str.Substring(0,4) );
            int month = int.Parse(str.Substring(4,2));
            int day = int.Parse(str.Substring(6));
            DateTime date = new DateTime( year,month,day);
            return date;
        }


        /// <summary>
        /// 解析时间段
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private List<TimeSlice> ParseTimeSlice(string str)
        {
            List<TimeSlice> lst = new List<TimeSlice>();

            string[] strlist = str.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            TimeSpan tsvalue;
            int icount = strlist.Length / 2;
            for (int i = 0; i < icount; ++i)
            {
                TimeSlice slice = new TimeSlice();
                if (!TimeSpan.TryParse(strlist[i * 2], out tsvalue))
                {
                     
                }
                slice.BeginTime = tsvalue;

                if (!TimeSpan.TryParse(strlist[i * 2 + 1], out tsvalue))
                {
              
                }
                slice.EndTime = tsvalue;

                lst.Add(slice);
            }

            return lst;
        }

        /// <summary>
        /// 给定一个交易日，获取这个交易日的交易时段
        /// </summary>
        /// <param name="theDate">指定交易日</param>
        /// <param name="market">市场</param>
        /// <param name="exchangeId">交易所ID</param>
        /// <param name="productId">产品ID（股票和股票期权填空）</param>
        /// <returns>指定的交易时段</returns>
        public List<TimeSlice> GetTradingFrame(DateTime theDate, EnumMarket market, string exchangeId, string productId = "")
        {
            if (theDate == default(DateTime))
                return GetLastTradingFrame(market, exchangeId, productId);

            TradingFrameSeries frameSeries = GetAllTradingTimeFrames(market, exchangeId, productId);

            if (frameSeries != null)
            {
                List<TimeSlice> timeSlice = frameSeries.GetTradingTimes(theDate);

                if (timeSlice != null)
                {
                    return timeSlice;
                }
            }
       
            //如果找不到，则返回默认的
            return GetDefaultTradingFrame(market, exchangeId);
        }

        /// <summary>
        /// 获取默认的交易时段
        /// </summary>
        /// <param name="market"></param>
        /// <param name="exchangeId"></param>
        /// <returns></returns>
        public List<TimeSlice> GetDefaultTradingFrame(EnumMarket market, string exchangeId)
        {
            if (market == EnumMarket.股票)
            {
                if (_dictDefaultStockTradingFrame.ContainsKey(exchangeId))
                {
                    return _dictDefaultStockTradingFrame[exchangeId];
                }
                
            }
            else if (market == EnumMarket.股票期权)
            {
                if (_dictDefaultStockOptionTradingFrame.ContainsKey(exchangeId))
                {
                    return _dictDefaultStockOptionTradingFrame[exchangeId];
                }
               
            }
            else if (market == EnumMarket.期货)
            {
                if (_dictDefaultFutureTradingFrame.ContainsKey(exchangeId))
                {
                    return _dictDefaultFutureTradingFrame[exchangeId];
                }
                
            }
            else if (market == EnumMarket.期货期权)
            {
                if (_dictDefaultFutureOptionTradingFrame.ContainsKey(exchangeId))
                {
                    return _dictDefaultFutureOptionTradingFrame[exchangeId];
                }
                
            }

            return _defaultFrame;
        }

        /// <summary>
        /// 获取最后的（当前执行的）交易时段
        /// 实盘的时候调用这个方法
        /// </summary> 
        /// <param name="market">市场</param>
        /// <param name="exchangeId">交易所ID</param>
        /// <param name="productId">产品ID（股票和股票期权填空）</param>
        /// <returns>指定的交易时段</returns>
        public List<TimeSlice> GetLastTradingFrame(EnumMarket market, string exchangeId, string productId = "")
        {
            TradingFrameSeries frameSeries = GetAllTradingTimeFrames( market,exchangeId,productId);

            if ( frameSeries != null)
            {
                return frameSeries.Last.TradingTimes;
            }
            else
            {
                //没有历史数据，取其交易所的
                return GetDefaultTradingFrame(market, exchangeId);
            }  
        }
 

        /// <summary>
        /// 所有的历史交易时段
        /// </summary>
        /// <param name="market">市场类型</param>
        /// <param name="exchangeId">交易所ID</param>
        /// <param name="productId">期货和期货期权需要填写-ProductID</param>
        /// <returns>所有的交易时段</returns>
        public TradingFrameSeries GetAllTradingTimeFrames(EnumMarket market, string exchangeId, string productId = "")
        {
            if (market == EnumMarket.股票)
            {
                if (_dictStockTradingFrame.ContainsKey(exchangeId))
                {
                    return _dictStockTradingFrame[exchangeId];
                }
            }
            else if (market == EnumMarket.股票期权)
            {
                if (_dictStockOptionTradingFrame.ContainsKey(exchangeId))
                {
                    return _dictStockOptionTradingFrame[exchangeId];
                }
            }
            else if (market == EnumMarket.期货)
            {
                if (_dictFutureTradingFrame.ContainsKey(productId))
                {
                    return _dictFutureTradingFrame[productId];
                }
            }
            else if (market == EnumMarket.期货期权)
            {
                if (_dictFutureOptionTradingFrame.ContainsKey(productId))
                {
                    return _dictFutureOptionTradingFrame[productId];
                }
            }
            return null;
        }

        public TimeSlice GetLivingTime(DateTime tradingday, EnumMarket market, string exchangid, string productid = "")
        {
            List<TimeSlice> lstslice = GetTradingFrame(tradingday, market, exchangid, productid);
            if (lstslice != null && lstslice.Count > 0)
            {
                return new TimeSlice()
                {
                    BeginTime = lstslice[0].BeginTime,
                    EndTime = lstslice[lstslice.Count - 1].EndTime
                };
            }

            return null;
        }

        public TimeSpan GetOpenTime(DateTime tradingday, EnumMarket market, string exchangid, string productid = "")
        {
            List<TimeSlice> lstslice = GetTradingFrame(tradingday, market, exchangid, productid);
            if (lstslice != null && lstslice.Count > 0)
            {
                return lstslice[0].BeginTime;
            }

            return default(TimeSpan);
        }

        public TimeSpan GetCloseTime(DateTime tradingday, EnumMarket market, string exchangid, string productid = "")
        {
            List<TimeSlice> lstslice = GetTradingFrame(tradingday, market, exchangid, productid);
            if (lstslice != null && lstslice.Count > 0)
            {
                return lstslice[lstslice.Count - 1].EndTime;
            }

            return default(TimeSpan);
        }

        public double GetLimit(string instrumentid)
        {
            var result = 0.0;
            _limitDictionary.TryGetValue(instrumentid.ToLower(), out result);

            return result;
        }
    }
}
