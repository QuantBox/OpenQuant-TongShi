using System;

using SmartQuant.Data;
using StockDll;

namespace QuantBox.Helper.TongShi
{
    public class TongShiTrade : Trade
    {
        public TongShiTrade()
            : base()
        {
        }

        public TongShiTrade(Trade trade)
            : base(trade)
        {
        }

        public TongShiTrade(DateTime datetime, double price, int size)
            : base(datetime, price, size)
        {
        }

        public StructRcvReport DepthMarketData;
    }
}
