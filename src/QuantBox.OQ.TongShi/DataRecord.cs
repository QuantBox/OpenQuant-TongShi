using SmartQuant.Instruments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuantBox.OQ.TongShi
{
    class DataRecord
    {
        public string Symbol;
        public string Exchange;
        public Instrument Instrument;
        public bool TradeRequested;
        public bool QuoteRequested;
        public bool MarketDepthRequested;
    }
}
