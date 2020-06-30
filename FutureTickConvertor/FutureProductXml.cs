using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Ats.Core;

namespace FutureTickConvertor
{
    class FutureProductXml
    {
        const string IdAttr = "id";
        const string NameAttr = "name";
        const string FutureTag = "Future";
        const string TradingTimeAttr = "tradingtime";

        internal static void Get(XmlElement root, FutureProduct product, FutureManager mgr)
        {
            product.ProductID = root.GetAttribute(IdAttr);
            product.ProductName = root.GetAttribute(NameAttr);
            if (root.HasAttribute(TradingTimeAttr))
            {
                #region tradingtime
                string strtradingtime = root.GetAttribute(TradingTimeAttr);
                string[] strlist = strtradingtime.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                int icount = strlist.Length / 2;

                TimeSpan tsvalue;
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

            foreach (XmlNode subNode in root.ChildNodes)
            {
                XmlElement subTagNode = subNode as XmlElement;
                if (subTagNode == null) continue;

                if (subTagNode.Name != FutureTag)
                    continue;

                Future future;
                FutureXml.Get(subTagNode, out future, false);

                if (future.ID != "")
                {
                    future.ExchangeID = product.ExchangeID;
                    future.ProductID = product.ProductID;

                    mgr.AddFuture(future);
                }
            }

            BuildRef(mgr, product.ExchangeID, product.ProductID);
        }

        static void BuildRef(FutureManager mgr, string exchangeid, string productid)
        {
            List<Future> lstfuture = mgr.GetFutures(exchangeid, productid);

            List<IndexFuture> lstidx = new List<IndexFuture>();
            foreach (Future future in lstfuture)
            {
                if (future.FutureType == EnumFutureType.Reference)
                {
                    string futureid = mgr.GetRealFuture(future.ID);
                    if (futureid.Length > 0 && mgr.HasFuture(futureid))
                    {
                        future.RealFuture = mgr[futureid];
                    }
                }
                else if (future.FutureType == EnumFutureType.Index)
                {
                    lstidx.Add(future as IndexFuture);
                }
            }

            foreach (IndexFuture idxfuture in lstidx)
            {
                foreach (Future future in lstfuture)
                {
                    if (future.FutureType == EnumFutureType.Normal)
                    {
                        idxfuture.Add(future);
                    }
                }

                if(mgr.HasFuture(productid+"9999"))
                {
                    idxfuture.RealFuture = mgr[productid + "9999"].RealFuture;
                }
            }            
        }

        internal static void Set(XmlDocument doc, XmlElement root, FutureProduct product, FutureManager mgr)
        {
            XmlAttribute attr = doc.CreateAttribute(IdAttr);
            attr.Value = product.ProductID;
            root.Attributes.Append(attr);

            attr = doc.CreateAttribute(NameAttr);
            attr.Value = product.ProductName;
            root.Attributes.Append(attr);

            if (product.AllSlice != null && product.AllSlice.Count > 0)
            {
                StringBuilder builder = new StringBuilder();
                foreach (TimeSlice slice in product.AllSlice)
                {
                    builder.Append(slice.BeginTime.ToString()).Append(",")
                        .Append(slice.EndTime.ToString()).Append(",");
                }

                builder.Remove(builder.Length - 1, 1);

                attr = doc.CreateAttribute(TradingTimeAttr);
                attr.Value = builder.ToString();
                root.Attributes.Append(attr);
            }

            List<Future> futurelist = mgr.GetFutures(product.ExchangeID, product.ProductID);
            foreach (Future future in futurelist)
            {
                XmlElement subTagNode = doc.CreateElement(FutureTag);
                root.AppendChild(subTagNode);

                FutureXml.Set(doc, subTagNode, future);
            }
        }

        internal static void Get(
            XmlElement root,
            FutureProduct product, 
            FutureManager mgr, 
            List<string> futurelist, 
            bool blive)
        {
            product.ProductID = root.GetAttribute(IdAttr);
            product.ProductName = root.GetAttribute(NameAttr);

            if (root.HasAttribute(TradingTimeAttr))
            {
                #region tradingtime
                string strtradingtime = root.GetAttribute(TradingTimeAttr);
                string[] strlist = strtradingtime.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                int icount = strlist.Length / 2;

                TimeSpan tsvalue;
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

            Dictionary<string, Future> normalmap = new Dictionary<string, Future>();
            Dictionary<string, Future> refmap = new Dictionary<string, Future>();
            Dictionary<string, Future> idxmap = new Dictionary<string, Future>();

            foreach (XmlNode subNode in root.ChildNodes)
            {
                #region

                XmlElement subTagNode = subNode as XmlElement;
                if (subTagNode == null) continue;

                if (subTagNode.Name != FutureTag)
                    continue;

                Future future;
                FutureXml.Get(subTagNode, out future, blive);

                if (future.ID != "")
                {
                    future.ExchangeID = product.ExchangeID;
                    future.ProductID = product.ProductID;

                    if (future.FutureType == EnumFutureType.Index)
                    {
                        if (!idxmap.ContainsKey(future.ID))
                            idxmap.Add(future.ID, future);
                        else
                            throw new Exception("Future.xml出错，重复的合约ID: " + future.ID);
                    }
                    else if (future.FutureType == EnumFutureType.Reference)
                    {
                        if (!refmap.ContainsKey(future.ID))
                            refmap.Add(future.ID, future);
                        else
                            throw new Exception("Future.xml出错，重复的合约ID: " + future.ID);
                    }
                    else
                    {
                        if (!normalmap.ContainsKey(future.ID))
                            normalmap.Add(future.ID, future);
                        else
                            throw new Exception("Future.xml出错，重复的合约ID: " + future.ID);
                    }
                }

                #endregion
            }

            foreach (string futureid in normalmap.Keys)
            {
                if (futurelist == null || futurelist.Count == 0 || futurelist.Contains(futureid))
                {
                    mgr.AddFuture(normalmap[futureid]);
                }
            }

            foreach (string futureid in refmap.Keys)
            {
                Future future = refmap[futureid];
                string refid = mgr.GetRealFuture(future.ID);
                if (refid != "" && normalmap.ContainsKey(refid))
                {
                    future.RealFuture = normalmap[refid];
                }

                if (futurelist == null || futurelist.Count == 0 || futurelist.Contains(futureid))
                {
                    mgr.AddFuture(future);
                    mgr.AddFuture(future.RealFuture);
                }
            }

            foreach (string futureid in idxmap.Keys)
            {
                if (futurelist == null || futurelist.Count == 0 || futurelist.Contains(futureid))
                {
                    Future future = idxmap[futureid];

                    future.RealFuture = refmap[future.ProductID + "9999"].RealFuture;

                    mgr.AddFuture(future);
                    mgr.AddFuture(future.RealFuture);

                    foreach (string normalid in normalmap.Keys)
                    {
                        mgr.AddFuture(normalmap[normalid]);
                    }
                }
            }

            BuildRef(mgr, product.ExchangeID, product.ProductID);
        }
    }
}
