using System;
using System.Collections.Generic;
using System.Text;
using Ats.Core;
using System.Xml;
using log4net;

namespace FutureTickConvertor
{
    class FutureTpl
    {
        #region 变量

        private Dictionary<string, Exchange> _exlist = new Dictionary<string, Exchange>();
        private Dictionary<string, FutureProduct> _productlist = new Dictionary<string, FutureProduct>();
        private List<Future> _futurelist = new List<Future>();
        private List<Future> _defaultfuturelist = new List<Future>();

        #endregion

        #region 检索

        public Exchange GetExchange(string id)
        {
            if (_exlist.ContainsKey(id))
                return _exlist[id];

            return null;
        }

        public bool HasExchange(string id)
        {
            return _exlist.ContainsKey(id);
        }

        public FutureProduct GetProduct(string id)
        {
            if (_productlist.ContainsKey(id))
                return _productlist[id];

            return null;
        }

        public bool HasProduct(string id)
        {
            return _productlist.ContainsKey(id);
        }

        public List<Future> GetSpecialFuture(string productid)
        {
            List<Future> futurelist = new List<Future>();
            foreach (Future future in _futurelist)
            {
                if (future.ProductID == productid)
                {
                    futurelist.Add(future);
                }
            }

            return futurelist;
        }

        public List<Future> GetDefaultSpecialFuture()
        {
            return new List<Future>(_defaultfuturelist);
        }

        #endregion

        #region 文件读取

        #region Xml标签

        const string IdAttr = "id";
        const string NameAttr = "name";
        const string SliceTag = "TradingTime";
        const string OpenTimeTag = "OpenTime";
        const string CloseTimeTag = "CloseTime";
        const string ExListTag = "Exchanges";
        const string PrdListTag = "Products";
        const string FutureListTag = "Futures";
        const string TradingTimeAttr = "tradingtime";

        #endregion

        public bool LoadConfig(string strFile)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(strFile);

                #region 交易所

                XmlElement exroot = doc.DocumentElement.GetElementsByTagName(ExListTag)[0] as XmlElement;
                if (exroot == null)
                    throw new Exception("文件格式不对");

                TimeSpan tsvalue;
                foreach (XmlNode node in exroot)
                {
                    #region

                    XmlElement exElem = node as XmlElement;
                    if (exElem == null) continue;

                    Exchange exchange = new Exchange();
                    exchange.ExchangeID = exElem.GetAttribute(IdAttr).Trim();
                    exchange.ExchangeName = exElem.GetAttribute(NameAttr).Trim();
                    if (exchange.ExchangeID.Length == 0)
                        continue;

                    XmlText txtNode = null;
                    DateTime dtime = default(DateTime);

                    foreach (XmlNode subNode in exElem.ChildNodes)
                    {
                        #region

                        XmlElement subTagNode = subNode as XmlElement;
                        if (subTagNode == null) continue;

                        switch (subTagNode.Name)
                        {
                            case SliceTag:
                                txtNode = subTagNode.FirstChild as XmlText;
                                if (txtNode == null || txtNode.Value.Trim() == "")
                                    throw new Exception("配置文件格式不对");

                                string[] strlist = txtNode.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                int icount = strlist.Length / 2;
                                for (int i = 0; i < icount; ++i)
                                {
                                    TimeSlice slice = new TimeSlice();
                                    if (!TimeSpan.TryParse(strlist[i * 2], out tsvalue))
                                    {
                                        exchange.AllSlice.Clear();
                                        throw new Exception("配置文件格式不对");
                                    }
                                    slice.BeginTime = tsvalue;

                                    if (!TimeSpan.TryParse(strlist[i * 2 + 1], out tsvalue))
                                    {
                                        exchange.AllSlice.Clear();
                                        throw new Exception("配置文件格式不对");
                                    }
                                    slice.EndTime = tsvalue;

                                    exchange.AllSlice.Add(slice);
                                }

                                break;
                            case OpenTimeTag:
                                txtNode = subTagNode.FirstChild as XmlText;
                                if (txtNode == null || txtNode.Value.Trim() == "")
                                    throw new Exception("配置文件格式不对");
                                if (!DateTime.TryParse(txtNode.Value, out dtime))
                                    throw new Exception("配置文件格式不对");
                                exchange.OpenTime = dtime;
                                break;
                            case CloseTimeTag:
                                txtNode = subTagNode.FirstChild as XmlText;
                                if (txtNode == null || txtNode.Value.Trim() == "")
                                    throw new Exception("配置文件格式不对");
                                if (!DateTime.TryParse(txtNode.Value, out dtime))
                                    throw new Exception("配置文件格式不对");
                                exchange.CloseTime = dtime;
                                break;
                        }

                        #endregion
                    }

                    _exlist.Add(exchange.ExchangeID, exchange);

                    #endregion
                }

                #endregion

                #region 品种

                XmlElement productroot = doc.DocumentElement.GetElementsByTagName(PrdListTag)[0] as XmlElement;
                if (productroot == null)
                    throw new Exception("文件格式不对");

                foreach (XmlNode node in productroot)
                {
                    XmlElement productElem = node as XmlElement;
                    if (productElem == null) continue;

                    FutureProduct product = new FutureProduct();
                    product.ProductID = productElem.GetAttribute("id").Trim();
                    product.ProductName = productElem.GetAttribute("name").Trim();
                    product.ExchangeID = productElem.GetAttribute("exchange").Trim();

                    if (productElem.HasAttribute(TradingTimeAttr))
                    {
                        #region tradingtime
                        string strtradingtime = productElem.GetAttribute(TradingTimeAttr);
                        string[] strlist = strtradingtime.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        int icount = strlist.Length / 2;
                        for (int i = 0; i < icount; ++i)
                        {
                            TimeSlice slice = new TimeSlice();
                            if (!TimeSpan.TryParse(strlist[i * 2], out tsvalue))
                            {
                                product.AllSlice.Clear();
                                throw new Exception("配置文件future.xml格式不对, Product/tradingtime解析出错");
                            }
                            slice.BeginTime = tsvalue;

                            if (!TimeSpan.TryParse(strlist[i * 2 + 1], out tsvalue))
                            {
                                product.AllSlice.Clear();
                                throw new Exception("配置文件future.xml格式不对, Product/tradingtime解析出错");
                            }
                            slice.EndTime = tsvalue;

                            product.AllSlice.Add(slice);
                        }
                        #endregion
                    }

                    if(product.ProductID.Length != 0)
                        _productlist.Add(product.ProductID, product);
                }

                #endregion

                #region 合约

                XmlElement futureroot = doc.DocumentElement.GetElementsByTagName(FutureListTag)[0] as XmlElement;
                if (futureroot == null)
                    throw new Exception("文件格式不对");

                foreach (XmlNode node in futureroot)
                {
                    XmlElement futureElem = node as XmlElement;
                    if (futureElem == null) continue;

                    Future future = new Future();
                    future.ID = futureElem.GetAttribute("id").Trim();
                    future.Name = futureElem.GetAttribute("name").Trim();
                    future.ProductID = futureElem.GetAttribute("pid").Trim();

                    if (future.ID.Length != 0)
                    {
                        if (future.ProductID == "default")
                            _defaultfuturelist.Add(future);
                        else
                            _futurelist.Add(future);
                    }
                }

                #endregion

                return true;
            }
            catch (XmlException xmlex)
            {
                ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
                logger.Error("加载配置文件 " + strFile + " 失败", xmlex);
            }
            catch (Exception e)
            {
                ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
                logger.Error("加载配置文件 " + strFile + " 失败", e);
            }

            _exlist.Clear();

            return false;
        }

        #endregion
    }
}
