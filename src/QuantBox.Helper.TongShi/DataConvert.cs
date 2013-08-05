using System.Reflection;
using StockDll;

namespace QuantBox.Helper.TongShi
{
    public class DataConvert
    {
        static FieldInfo tradeField;
        static FieldInfo quoteField;

        public static bool TryConvert(OpenQuant.API.Trade trade, ref StructRcvReport DepthMarketData)
        {
            if (tradeField == null)
            {
                tradeField = typeof(OpenQuant.API.Trade).GetField("trade", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            TongShiTrade t = tradeField.GetValue(trade) as TongShiTrade;
            if (null != t)
            {
                DepthMarketData = t.DepthMarketData;
                return true;
            }
            return false;
        }

        public static bool TryConvert(OpenQuant.API.Quote quote, ref StructRcvReport DepthMarketData)
        {
            if (quoteField == null)
            {
                quoteField = typeof(OpenQuant.API.Quote).GetField("quote", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            TongShiQuote q = quoteField.GetValue(quote) as TongShiQuote;
            if (null != q)
            {
                DepthMarketData = q.DepthMarketData;
                return true;
            }
            return false;
        }
    }
}
