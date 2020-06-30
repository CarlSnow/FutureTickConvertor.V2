using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using Ats.Core;
using log4net;

namespace FutureTickConvertor
{
    /// <summary>
    /// 期货文件解析类
    /// </summary>
    public class FutureFile
    {
        /// <summary>
        /// 期货合约文件
        /// </summary>
        const string FileName = "future.xml";

        /// <summary>
        /// 指数合约文件
        /// </summary>
        const string IndexFile = "reffuture.xml";

        const string RootTag = "Ats";

        const string ExchangeTag = "Exchange";

        public static bool Read(string strDir, FutureManager mgr)
        {
            try
            {
                #region
               
                mgr.Clear();

                string strIndexFile = strDir + "\\" + IndexFile;
                if (File.Exists(strIndexFile))
                    ReadRefIndex(strIndexFile, mgr);

                string strFile = strDir + "\\" + FileName;
                if (!File.Exists(strFile))
                    return false;

                XmlDocument doc = new XmlDocument();
                doc.Load(strFile);

                foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    XmlElement tagNode = node as XmlElement;
                    if (tagNode == null) continue;

                    if (tagNode.Name != ExchangeTag)
                        continue;

                    Exchange exchange = new Exchange();
                    FutureExchangeXml.Get(tagNode, exchange, mgr);
                    if (exchange.ExchangeID != "")
                        mgr.AddExchange(exchange);
                }
                return true;
                #endregion
            }
            catch (Exception ex)
            {
                ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
                logger.Error("读取期货品种失败", ex);
                mgr.Clear();
            }

            return false;
        }

    

        /// <summary>
        /// 读取文件
        /// </summary>
        /// <param name="strDir">目录</param>
        /// <param name="mgr"></param>
        /// <param name="futurelist"></param>
        /// <param name="blive"></param>
        /// <returns></returns>
        public static bool Read(
            string strDir,
            FutureManager mgr, 
            List<string> futurelist,
            bool blive)
        {
            try
            {
                mgr.Clear();

                string strIndexFile = strDir + "\\" + IndexFile;
                if (File.Exists(strIndexFile))
                    ReadRefIndex(strIndexFile, mgr);

                string strFile = strDir + "\\" + FileName;
                if (!File.Exists(strFile))
                    return false;

                XmlDocument doc = new XmlDocument();
                doc.Load(strFile);

                foreach (XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    XmlElement tagNode = node as XmlElement;
                    if (tagNode == null) continue;

                    if (tagNode.Name != ExchangeTag)
                        continue;

                    Exchange exchange = new Exchange();
                    FutureExchangeXml.Get(tagNode, exchange, mgr, futurelist, blive);
                    if (exchange.ExchangeID != "")
                        mgr.AddExchange(exchange);
                }

                //Check(mgr);

                return true;
            }
            catch (Exception ex)
            {
                ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
                logger.Error("读取期货品种失败", ex);
                mgr.Clear();
            }

            return false;
        }

        /// <summary>
        /// 读取主力合约映射文件
        /// </summary>
        /// <param name="strFile">映射文件</param>
        /// <param name="fmgr">期货管理器</param>
        private static void ReadRefIndex(string strFile, FutureManager fmgr)
        {
            FileStream fs = null;
            try
            {
                #region
                
                fs = new FileStream(strFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                StreamReader reader = new StreamReader(fs, Encoding.UTF8);
                while ( !reader.EndOfStream)
                {
                    string strline = reader.ReadLine();
                    // = 分割
                    string[] linelist = strline.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (linelist.Length == 2)
                    {
                        fmgr.AddRefFuture(linelist[0].Trim(), linelist[1].Trim());
                    }
                }
                #endregion
            }
            catch (Exception)
            {
                fmgr.Clear();
            }

            if (fs != null)
                fs.Close();
        }

    }
}
