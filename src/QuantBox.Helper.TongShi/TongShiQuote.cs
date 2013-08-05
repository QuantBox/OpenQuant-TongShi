using System;

using SmartQuant.Data;
using StockDll;

namespace QuantBox.Helper.TongShi
{
    public class TongShiQuote : Quote
    {
        public TongShiQuote()
            : base()
        {
        }

        public TongShiQuote(Quote quote)
            : base(quote)
        {
        }

        public TongShiQuote(DateTime datetime, double bid, int bidSize, double ask, int askSize)
            : base(datetime, bid, bidSize, ask, askSize)
        {
        }

        public StructRcvReport DepthMarketData;
    }
}
