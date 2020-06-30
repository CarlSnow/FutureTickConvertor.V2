using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Ats.Core;

namespace FutureTickConvertor
{
    class FutureXml
    {
        const string IdAttr = "id";
        const string NameAttr = "name";
        const string ExchangeTag = "ExchangeID";
        const string ExchangeInstIdTag = "FutureID";
        //const string CreateDateTag = "CreateDate";
        //const string DeliveryYearTag = "DeliveryYear";
        //const string DeliveryMonthTag = "DeliveryMonth";
        //const string EndDelivDateTag = "EndDelivDate";
        const string ExpireDateTag = "ExpireDate";
        //const string InstLifePhaseTag = "LifeStatus";
        const string LongMarginRatioTag = "LongMarginRatio";
        //const string MaxLimitOrderVolumeTag = "MaxLimitOrderVolume";
        //const string MaxMarketOrderVolumeTag = "MaxMarketOrderVolume";
        //const string MinMarketOrderVolumeTag = "MinMarketOrderVolume";
        //const string MinLimitOrderVolumeTag = "MinLimitOrderVolume";
        const string OpenDateTag = "OpenDate";
        //const string PositionTypeTag = "PositionType";
        //const string ProductClassTag = "ProductClass";
        const string ProductIdTag = "ProductID";
        const string ShortMarginRatioTag = "ShortMarginRatio";
        const string StartDelivDateTag = "StartDelivDate";
        const string VolumeMultipleTag = "VolumeMultiple";
        const string PriceTickTag = "PriceTick";
        //const string CanTradeTag = "IsTrading";
        const string TypeAttr = "type";

        internal static void Get(
            XmlElement root, 
            out Future future,
            bool blive)
        {
            if (blive)
            {
                #region 实盘
                
                if (root.HasAttribute(TypeAttr))
                {
                    string futuretype = root.GetAttribute(TypeAttr);
                    if (futuretype == "index")
                    {
                        future = new IndexFuture();
                    }
                    else if (futuretype == "ref")
                    {
                        future = new ReferenceFuture();
                    }
                    else
                    {
                        future = new Future();
                    }
                }
                else
                {
                    string futureid = root.GetAttribute(IdAttr);
                    if (futureid.Contains("9995") ||
                        futureid.Contains("9996") ||
                        futureid.Contains("9997") ||
                        futureid.Contains("9998") ||
                        futureid.Contains("9999"))
                    {
                        future = new ReferenceFuture();
                    }
                    else if (futureid.Contains("9990"))
                    {
                        future = new IndexFuture();
                    }
                    else
                    {
                        future = new Future();
                    }
                }
                #endregion
            }
            else
            {
                future = new Future();
                string futureid = root.GetAttribute(IdAttr);
                if (futureid.Contains("9995") ||
                    futureid.Contains("9996") ||
                    futureid.Contains("9997") ||
                    futureid.Contains("9998") ||
                    futureid.Contains("9999"))
                {
                    future.RealFuture = future;
                }
            }

            future.ID = root.GetAttribute(IdAttr);
            future.Name = root.GetAttribute(NameAttr);

            XmlText txtNode = null;
            int itmp = 0;
            double dtmp = 0;
            DateTime dttmp = default(DateTime);

            foreach (XmlNode node in root.ChildNodes)
            {
                XmlElement tagNode = node as XmlElement;
                if (tagNode == null) continue;

                switch (tagNode.Name)
                {
                    #region

                    case ExchangeTag:
                        txtNode = tagNode.FirstChild as XmlText;
                        if (txtNode == null || txtNode.Value.Trim() == "")
                            throw new Exception("文件格式不对");
                        future.ExchangeID = txtNode.Value.Trim();
                        break;
                    //case ExchangeInstIDTag:
                        //txtNode = tagNode.FirstChild as XmlText;
                        //if (txtNode == null || txtNode.Value.Trim() == "")
                        //    throw new Exception("文件格式不对");
                        //future.ExchangeInstID = txtNode.Value.Trim();
                        //break;
                    case ProductIdTag:
                        txtNode = tagNode.FirstChild as XmlText;
                        if (txtNode == null || txtNode.Value.Trim() == "")
                            throw new Exception("文件格式不对");
                        future.ProductID = txtNode.Value.Trim();
                        break;
                    //case CreateDateTag:
                    //    txtNode = tagNode.FirstChild as XmlText;
                    //    if (txtNode == null || txtNode.Value.Trim() == "")
                    //        continue;
                    //    future.CreateDate = txtNode.Value.Trim();
                    //    break;
                    //case DeliveryYearTag:
                    //    txtNode = tagNode.FirstChild as XmlText;
                    //    if (txtNode == null || txtNode.Value.Trim() == "")
                    //        continue;
                    //    int.TryParse(txtNode.Value, out itmp);
                    //    future.DeliveryYear = itmp;
                    //    break;
                    //case DeliveryMonthTag:
                    //    txtNode = tagNode.FirstChild as XmlText;
                    //    if (txtNode == null || txtNode.Value.Trim() == "")
                    //        continue;
                    //    int.TryParse(txtNode.Value, out itmp);
                    //    future.DeliveryMonth = itmp;
                    //    break;
                    //case StartDelivDateTag:
                    //    txtNode = tagNode.FirstChild as XmlText;
                    //    if (txtNode == null || txtNode.Value.Trim() == "")
                    //        continue;
                    //    DateTime.TryParse(txtNode.Value, out dttmp);
                    //    future.StartDelivDate = dttmp;
                    //    break;
                    //case EndDelivDateTag:
                    //    txtNode = tagNode.FirstChild as XmlText;
                    //    if (txtNode == null || txtNode.Value.Trim() == "")
                    //        continue;
                    //    DateTime.TryParse(txtNode.Value, out dttmp);
                    //    future.EndDelivDate = dttmp;
                    //    break;
                    case ExpireDateTag:
                        txtNode = tagNode.FirstChild as XmlText;
                        if (txtNode == null || txtNode.Value.Trim() == "")
                            continue;
                        DateTime.TryParse(txtNode.Value, out dttmp);
                        future.ExpireDate = dttmp;
                        break;
                    //case InstLifePhaseTag:
                    //    txtNode = tagNode.FirstChild as XmlText;
                    //    if (txtNode == null) continue;
                    //    if (Enum.IsDefined(typeof(EnumLifeStatus), txtNode.Value))
                    //    {
                    //        future.LifeStatus = (EnumLifeStatus)Enum.Parse(typeof(EnumLifeStatus), txtNode.Value, true);
                    //    }
                    //    break;
                    //case PositionTypeTag:
                    //    txtNode = tagNode.FirstChild as XmlText;
                    //    if (txtNode == null) continue;
                    //    if (Enum.IsDefined(typeof(EnumPositionType), txtNode.Value))
                    //    {
                    //        future.PositionType = (EnumPositionType)Enum.Parse(typeof(EnumPositionType), txtNode.Value, true);
                    //    }
                    //    break;
                    //case ProductClassTag:
                    //    txtNode = tagNode.FirstChild as XmlText;
                    //    if (txtNode == null) continue;
                    //    if (Enum.IsDefined(typeof(EnumProductClass), txtNode.Value))
                    //    {
                    //        future.ProductClass = (EnumProductClass)Enum.Parse(typeof(EnumProductClass), txtNode.Value, true);
                    //    }
                    //    break;
                    case LongMarginRatioTag:
                        txtNode = tagNode.FirstChild as XmlText;
                        if (txtNode == null) continue;
                        double.TryParse(txtNode.Value, out dtmp);
                        future.LongMarginRatio = dtmp;
                        break;
                    case ShortMarginRatioTag:
                        txtNode = tagNode.FirstChild as XmlText;
                        if (txtNode == null) continue;
                        double.TryParse(txtNode.Value, out dtmp);
                        future.ShortMarginRatio = dtmp;
                        break;
                    //case MaxLimitOrderVolumeTag:
                    //    txtNode = tagNode.FirstChild as XmlText;
                    //    if (txtNode == null || txtNode.Value.Trim() == "")
                    //        continue;
                    //    int.TryParse(txtNode.Value, out itmp);
                    //    future.MaxLimitOrderVolume = itmp;
                    //    break;
                    //case MaxMarketOrderVolumeTag:
                    //    txtNode = tagNode.FirstChild as XmlText;
                    //    if (txtNode == null || txtNode.Value.Trim() == "")
                    //        continue;
                    //    int.TryParse(txtNode.Value, out itmp);
                    //    future.MaxMarketOrderVolume = itmp;
                    //    break;
                    //case MinMarketOrderVolumeTag:
                    //    txtNode = tagNode.FirstChild as XmlText;
                    //    if (txtNode == null || txtNode.Value.Trim() == "")
                    //        continue;
                    //    int.TryParse(txtNode.Value, out itmp);
                    //    future.MinMarketOrderVolume = itmp;
                    //    break;
                    //case MinLimitOrderVolumeTag:
                    //    txtNode = tagNode.FirstChild as XmlText;
                    //    if (txtNode == null || txtNode.Value.Trim() == "")
                    //        continue;
                    //    int.TryParse(txtNode.Value, out itmp);
                    //    future.MinLimitOrderVolume = itmp;
                    //    break;
                    case OpenDateTag:
                        txtNode = tagNode.FirstChild as XmlText;
                        if (txtNode == null || txtNode.Value.Trim() == "")
                            continue;
                        DateTime.TryParse(txtNode.Value, out dttmp);
                        future.OpenDate = dttmp;
                        break;
                    case VolumeMultipleTag:
                        txtNode = tagNode.FirstChild as XmlText;
                        if (txtNode == null || txtNode.Value.Trim() == "")
                            continue;
                        int.TryParse(txtNode.Value, out itmp);
                        future.VolumeMultiple = itmp;
                        break;
                    case PriceTickTag:
                        txtNode = tagNode.FirstChild as XmlText;
                        if (txtNode == null) continue;
                        double.TryParse(txtNode.Value, out dtmp);
                        future.PriceTick = dtmp;
                        break;
                    //case CanTradeTag:
                        //txtNode = tagNode.FirstChild as XmlText;
                        //if (txtNode == null) continue;
                        //bool.TryParse(txtNode.Value, out btmp);
                        //future.CanTrade = btmp;
                        //break;
                    default:
                        break;
                    #endregion
                }
            }
        }

        internal static void Set(XmlDocument doc, XmlElement root, Future future)
        {
            XmlAttribute attr = doc.CreateAttribute(IdAttr);
            attr.Value = future.ID;
            root.Attributes.Append(attr);

            attr = doc.CreateAttribute(NameAttr);
            attr.Value = future.Name;
            root.Attributes.Append(attr);

            XmlElement subTagNode = doc.CreateElement(ExchangeTag);
            root.AppendChild(subTagNode);
            XmlText subValueNode = doc.CreateTextNode(future.ExchangeID);
            subTagNode.AppendChild(subValueNode);

            //subTagNode = doc.CreateElement(ExchangeInstIDTag);
            //root.AppendChild(subTagNode);
            //subValueNode = doc.CreateTextNode(future.ExchangeInstID);
            //subTagNode.AppendChild(subValueNode);

            //subTagNode = doc.CreateElement(CreateDateTag);
            //root.AppendChild(subTagNode);
            //subValueNode = doc.CreateTextNode(future.CreateDate.ToString());
            //subTagNode.AppendChild(subValueNode);

            //subTagNode = doc.CreateElement(DeliveryYearTag);
            //root.AppendChild(subTagNode);
            //subValueNode = doc.CreateTextNode(future.DeliveryYear.ToString());
            //subTagNode.AppendChild(subValueNode);

            //subTagNode = doc.CreateElement(DeliveryMonthTag);
            //root.AppendChild(subTagNode);
            //subValueNode = doc.CreateTextNode(future.DeliveryMonth.ToString());
            //subTagNode.AppendChild(subValueNode);

            //subTagNode = doc.CreateElement(EndDelivDateTag);
            //root.AppendChild(subTagNode);
            //subValueNode = doc.CreateTextNode(future.EndDelivDate.ToString());
            //subTagNode.AppendChild(subValueNode);

            subTagNode = doc.CreateElement(ExpireDateTag);
            root.AppendChild(subTagNode);
            subValueNode = doc.CreateTextNode(future.ExpireDate.ToString());
            subTagNode.AppendChild(subValueNode);

            //subTagNode = doc.CreateElement(InstLifePhaseTag);
            //root.AppendChild(subTagNode);
            //subValueNode = doc.CreateTextNode(future.LifeStatus.ToString());
            //subTagNode.AppendChild(subValueNode);

            subTagNode = doc.CreateElement(LongMarginRatioTag);
            root.AppendChild(subTagNode);
            subValueNode = doc.CreateTextNode(future.LongMarginRatio.ToString());
            subTagNode.AppendChild(subValueNode);

            //subTagNode = doc.CreateElement(MaxLimitOrderVolumeTag);
            //root.AppendChild(subTagNode);
            //subValueNode = doc.CreateTextNode(future.MaxLimitOrderVolume.ToString());
            //subTagNode.AppendChild(subValueNode);

            //subTagNode = doc.CreateElement(MaxMarketOrderVolumeTag);
            //root.AppendChild(subTagNode);
            //subValueNode = doc.CreateTextNode(future.MaxMarketOrderVolume.ToString());
            //subTagNode.AppendChild(subValueNode);

            //subTagNode = doc.CreateElement(MinLimitOrderVolumeTag);
            //root.AppendChild(subTagNode);
            //subValueNode = doc.CreateTextNode(future.MinLimitOrderVolume.ToString());
            //subTagNode.AppendChild(subValueNode);

            //subTagNode = doc.CreateElement(MinMarketOrderVolumeTag);
            //root.AppendChild(subTagNode);
            //subValueNode = doc.CreateTextNode(future.MinMarketOrderVolume.ToString());
            //subTagNode.AppendChild(subValueNode);

            subTagNode = doc.CreateElement(OpenDateTag);
            root.AppendChild(subTagNode);
            subValueNode = doc.CreateTextNode(future.OpenDate.ToString());
            subTagNode.AppendChild(subValueNode);

            //subTagNode = doc.CreateElement(PositionTypeTag);
            //root.AppendChild(subTagNode);
            //subValueNode = doc.CreateTextNode(future.PositionType.ToString());
            //subTagNode.AppendChild(subValueNode);

            //subTagNode = doc.CreateElement(ProductClassTag);
            //root.AppendChild(subTagNode);
            //subValueNode = doc.CreateTextNode(future.ProductClass.ToString());
            //subTagNode.AppendChild(subValueNode);

            subTagNode = doc.CreateElement(ProductIdTag);
            root.AppendChild(subTagNode);
            subValueNode = doc.CreateTextNode(future.ProductID.ToString());
            subTagNode.AppendChild(subValueNode);

            subTagNode = doc.CreateElement(ShortMarginRatioTag);
            root.AppendChild(subTagNode);
            subValueNode = doc.CreateTextNode(future.ShortMarginRatio.ToString());
            subTagNode.AppendChild(subValueNode);

            //subTagNode = doc.CreateElement(StartDelivDateTag);
            //root.AppendChild(subTagNode);
            //subValueNode = doc.CreateTextNode(future.StartDelivDate.ToString());
            //subTagNode.AppendChild(subValueNode);

            subTagNode = doc.CreateElement(VolumeMultipleTag);
            root.AppendChild(subTagNode);
            subValueNode = doc.CreateTextNode(future.VolumeMultiple.ToString());
            subTagNode.AppendChild(subValueNode);

            subTagNode = doc.CreateElement(PriceTickTag);
            root.AppendChild(subTagNode);
            subValueNode = doc.CreateTextNode(future.PriceTick.ToString());
            subTagNode.AppendChild(subValueNode);

            //subTagNode = doc.CreateElement(CanTradeTag);
            //root.AppendChild(subTagNode);
            //subValueNode = doc.CreateTextNode(future.CanTrade.ToString());
            //subTagNode.AppendChild(subValueNode);
        }
    }
}
