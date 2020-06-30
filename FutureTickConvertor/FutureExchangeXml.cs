using System;
using System.Collections.Generic;
using System.Text;
using Ats.Core;
using System.Xml;

namespace FutureTickConvertor
{
    /// <summary>
    /// 期货交易所XML操作类
    /// </summary>
    class FutureExchangeXml
    {
        const string IdAttr = "id";
        const string NameAttr = "name";
        const string ProductTag = "Product";
        const string SliceTag = "TradingTime";
        const string OpenTimeTag = "OpenTime";
        const string CloseTimeTag = "CloseTime";

        /// <summary>
        /// 解析内容
        /// </summary>
        /// <param name="root"></param>
        /// <param name="exchange"></param>
        /// <param name="mgr"></param>
        internal static void Get(XmlElement root, Exchange exchange, FutureManager mgr)
        {
            exchange.ExchangeID = root.GetAttribute(IdAttr);
            exchange.ExchangeName = root.GetAttribute(NameAttr);

            XmlText txtNode = null;
            DateTime dtime = default(DateTime);
            TimeSpan tsvalue;
            foreach (XmlNode subNode in root.ChildNodes)
            {
                XmlElement subTagNode = subNode as XmlElement;
                if (subTagNode == null) continue;

                switch (subTagNode.Name)
                {
                    case ProductTag:
                        FutureProduct product = new FutureProduct();
                        product.ExchangeID = exchange.ExchangeID;
                        FutureProductXml.Get(subTagNode, product, mgr);
                        if (product.ProductID != "")
                            mgr.AddProduct(product);
                        break;
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
            }
        }

        internal static void Set(XmlDocument doc, XmlElement root, Exchange exchange, FutureManager mgr)
        {
            XmlAttribute attr = doc.CreateAttribute(IdAttr);
            attr.Value = exchange.ExchangeID;
            root.Attributes.Append(attr);

            attr = doc.CreateAttribute(NameAttr);
            attr.Value = exchange.ExchangeName;
            root.Attributes.Append(attr);

            StringBuilder builder = new StringBuilder();
            foreach (TimeSlice slice in exchange.AllSlice)
            {
                builder.Append(slice.BeginTime.ToString()).Append(",").Append(slice.EndTime.ToString()).Append(",");
            }

            if (exchange.AllSlice.Count > 0)
            {
                builder.Remove(builder.Length - 1, 1);
            }

            XmlElement subTagNode = doc.CreateElement(SliceTag);
            root.AppendChild(subTagNode);
            XmlText subValueNode = doc.CreateTextNode(builder.ToString());
            subTagNode.AppendChild(subValueNode);

            subTagNode = doc.CreateElement(OpenTimeTag);
            root.AppendChild(subTagNode);
            subValueNode = doc.CreateTextNode(exchange.OpenTime.ToShortTimeString());
            subTagNode.AppendChild(subValueNode);

            subTagNode = doc.CreateElement(CloseTimeTag);
            root.AppendChild(subTagNode);
            subValueNode = doc.CreateTextNode(exchange.CloseTime.ToShortTimeString());
            subTagNode.AppendChild(subValueNode);

            List<FutureProduct> productlist = mgr.GetProducts(exchange.ExchangeID);
            foreach (FutureProduct product in productlist)
            {
                subTagNode = doc.CreateElement(ProductTag);
                root.AppendChild(subTagNode);

                FutureProductXml.Set(doc, subTagNode, product, mgr);
            }
            
        }

        internal static void Get(
            XmlElement root,
            Exchange exchange, 
            FutureManager mgr,
            List<string> futurelist, 
            bool blive)
        {
            exchange.ExchangeID = root.GetAttribute(IdAttr);
            exchange.ExchangeName = root.GetAttribute(NameAttr);

            XmlText txtNode = null;
            DateTime dtime = default(DateTime);

            TimeSpan tsvalue;
            foreach (XmlNode subNode in root.ChildNodes)
            {
                XmlElement subTagNode = subNode as XmlElement;
                if (subTagNode == null) continue;

                switch (subTagNode.Name)
                {
                    case ProductTag:
                        FutureProduct product = new FutureProduct();
                        product.ExchangeID = exchange.ExchangeID;
                        FutureProductXml.Get(subTagNode, product, mgr, futurelist, blive);
                        if (product.ProductID != "")
                            mgr.AddProduct(product);
                        break;
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
            }
        }
    }
}
