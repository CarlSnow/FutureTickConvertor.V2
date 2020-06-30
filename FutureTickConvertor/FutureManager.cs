using System;
using System.Collections.Generic;
using System.Text;
using Ats.Core;
using log4net;

namespace FutureTickConvertor
{
    public class FutureManager
    {
        #region 变量

        /// <summary>
        /// 所有的交易所
        /// </summary>
        private Dictionary<string, Exchange> _allExchanges = new Dictionary<string, Exchange>();
        /// <summary>
        /// 所有产品
        /// </summary>
        private Dictionary<string, FutureProduct> _allProducts = new Dictionary<string, FutureProduct>();

        /// <summary>
        /// 所有的期货合约
        /// </summary>
        private Dictionary<string, Future> _allFutures = new Dictionary<string, Future>();

        /// <summary>
        /// 期货主力合约映射表
        /// </summary>
        private Dictionary<string, string> _refFutures = new Dictionary<string, string>();

        ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        #region 属性

        /// <summary>
        /// 所有交易所
        /// </summary>
        public IEnumerable<Exchange> AllExchanges
        {
            get { return _allExchanges.Values; }
        }

        /// <summary>
        /// 所有品种
        /// </summary>
        public IEnumerable<FutureProduct> AllProducts
        {
            get { return _allProducts.Values; }
        }

        /// <summary>
        /// 所有金融工具
        /// </summary>
        public IEnumerable<Future> AllInstruments
        {
            get { return _allFutures.Values; }
        }

        public IEnumerable<string> AllRefFutures
        {
            get { return _refFutures.Keys; }
        }

        public Future this[string futureId]
        {
            get
            {
                Future future;
                if (_allFutures.TryGetValue(futureId, out future))
                    return future;

                return null;
            }
        }

        #endregion

        #region 检索

        /// <summary>
        /// 获取交易所
        /// </summary>
        /// <param name="exchangeId">交易所ID</param>
        /// <returns></returns>
        public Exchange GetExchange(string exchangeId)
        {
            Exchange exchange;
            if (_allExchanges.TryGetValue(exchangeId, out exchange))
                return exchange;
            return null;
        }

        public Exchange GetExchangeByFuture(string futureid)
        {
            foreach (Future future in _allFutures.Values)
            {
                if (future.ID == futureid)
                {
                    return GetExchange(future.ExchangeID);
                }
            }

            return null;
        }

        public FutureProduct GetProductById(string productId)
        {
            FutureProduct product;
            if(_allProducts.TryGetValue(productId, out product))
                return product;

            return null;
        }

        public List<FutureProduct> GetProducts(string exchangeId)
        {
            List<FutureProduct> productlist = new List<FutureProduct>();

            foreach (FutureProduct product in _allProducts.Values)
            {
                if (product.ExchangeID == exchangeId)
                {
                    productlist.Add(product);
                }
            }

            return productlist;
        }

        /// <summary>
        /// 根据交易所获取品种信息
        /// </summary>
        /// <param name="exchange"></param>
        /// <returns></returns>
        public List<Future> GetFutures(string exchangeId)
        {
            List<Future> futurelist = new List<Future>();

            foreach (Future future in _allFutures.Values)
            {
                if (future.ExchangeID == exchangeId)
                {
                    futurelist.Add(future);
                }
            }

            return futurelist;
        }

        public List<Future> GetFutures(string exchangeId, string productId)
        {
            List<Future> futurelist = new List<Future>();

            foreach (Future future in _allFutures.Values)
            {
                if (future.ExchangeID == exchangeId &&
                    future.ProductID == productId)
                {
                    futurelist.Add(future);
                }
            }

            return futurelist;
        }

        public bool HasProduct(string prodId)
        {
            return _allProducts.ContainsKey(prodId);
        }

        public bool HasFuture(string futureid)
        {
            return _allFutures.ContainsKey(futureid);
        }

        public TimeSlice GetLivingTime(string futureid)
        {
            Future future = this[futureid];
            if (future == null) return null;

            FutureProduct product = GetProductById(future.ProductID);
            if (product == null) return null;

            if (product.AllSlice != null && product.AllSlice.Count > 0)
            {
                TimeSlice tslice = new TimeSlice();
                tslice.BeginTime = product.AllSlice[0].BeginTime;
                tslice.EndTime = product.AllSlice[product.AllSlice.Count - 1].EndTime;
                return tslice;
            }

            Exchange exchange = GetExchange(future.ExchangeID);
            if (exchange == null) return null;

            if (exchange.AllSlice != null && exchange.AllSlice.Count > 0)
            {
                TimeSlice tslice = new TimeSlice();
                tslice.BeginTime = exchange.AllSlice[0].BeginTime;
                tslice.EndTime = exchange.AllSlice[exchange.AllSlice.Count - 1].EndTime;
                return tslice;
            }

            return null;
        }

        #endregion

        #region 添加，删除

        public void Clear()
        {
            _allExchanges.Clear();
            _allFutures.Clear();
            _allProducts.Clear();
            _refFutures.Clear();
        }

        internal void AddExchange(Exchange exchange)
        {
            if (_allExchanges.ContainsKey(exchange.ExchangeID))
                return;

            _allExchanges.Add(exchange.ExchangeID, exchange);
        }

        internal void AddProduct(FutureProduct product)
        {
            if (product == null)
                return;

            if (_allProducts.ContainsKey(product.ProductID))
                return;

            _allProducts.Add(product.ProductID, product);
        }

        internal void AddFuture(Future future)
        {
            if (future == null)
                return;

            if (_allFutures.ContainsKey(future.ID))
                return;

            _allFutures.Add(future.ID, future);
        }

        /// <summary>
        /// 添加Ref
        /// </summary>
        /// <param name="reffuture"></param>
        /// <param name="realfuture"></param>
        internal void AddRefFuture(string reffuture, string realfuture)
        {
            lock (_refFutures)
            {
                if (_refFutures.ContainsKey(reffuture))
                {
                    _refFutures[reffuture] = reffuture;
                }
                else
                {
                    _refFutures.Add(reffuture, realfuture);
                }
                    
            }
       
        }

        internal string GetRealFuture(string reffuture)
        {
            if (_refFutures.ContainsKey(reffuture))
                return _refFutures[reffuture];

            return "";
        }

        #endregion

      

       
    }
}
