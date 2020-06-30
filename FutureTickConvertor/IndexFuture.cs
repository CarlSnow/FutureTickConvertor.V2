using System;
using System.Collections.Generic;
using System.Text;
using Ats.Core;

namespace FutureTickConvertor
{
    public class IndexFuture : Future
    {
        List<Future> _lstFuture = new List<Future>();

        public List<Future> AllFutures
        {
            get { return _lstFuture; }
        }

        public IndexFuture()
            : base()
        {
            FutureType = EnumFutureType.Index;
        }

        /// <summary>
        /// 添加需要组合的合约
        /// </summary>
        /// <param name="future"></param>
        public void Add(Future future)
        {
            if (!_lstFuture.Contains(future))
                _lstFuture.Add(future);
        }

        public override void OnTick(Tick tick)
        {
            // 指数合约 = 所有合约的“持仓量*最新价格”之和/所有和玉持仓量之和

            double price = 0;
            double openinterest = 0;
            double vol = 0;
            double turnover = 0;

            foreach (Future future in _lstFuture)
            {
                if (future.LastTick != null)
                {
                    price = price + future.LastTick.LastPrice * future.LastTick.OpenInterest;
                    openinterest = openinterest + future.LastTick.OpenInterest;
                    vol = vol + future.LastTick.Volume;
                    turnover = turnover + future.LastTick.Turnover;
                }
            }

            if (openinterest > 0)
            {
                Tick idxtick = new Tick();

                idxtick.InstrumentID = ID;
                idxtick.ExchangeID = ExchangeID;
                idxtick.DateTime = tick.DateTime;
                idxtick.TradingDay = tick.TradingDay;
                idxtick.LastPrice = price / openinterest;
                idxtick.OpenInterest = openinterest;
                idxtick.Volume = vol;
                idxtick.Turnover = turnover;

                base.OnTick(idxtick);
            }
        }
    }
}
