using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ats.Core;

namespace FutureTickConvertor
{
    public class ReferenceFuture : Future
    {
        public ReferenceFuture()
            : base()
        {
            FutureType = EnumFutureType.Reference;
        }

        //public override void OnTick(Tick tick)
        //{
        //    base.OnTick(tick);
        //}
    }
}
