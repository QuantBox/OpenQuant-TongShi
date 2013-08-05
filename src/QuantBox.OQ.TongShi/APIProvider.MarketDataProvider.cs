using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SmartQuant.Data;
using SmartQuant.FIX;
using SmartQuant.Instruments;
using SmartQuant.Providers;
using System.ComponentModel;

namespace QuantBox.OQ.TongShi
{
    public partial class APIProvider : IMarketDataProvider
    {
        private IBarFactory factory;

        public event MarketDataRequestRejectEventHandler MarketDataRequestReject;
        public event MarketDataSnapshotEventHandler MarketDataSnapshot;
        public event BarEventHandler NewBar;
        public event BarEventHandler NewBarOpen;
        public event BarSliceEventHandler NewBarSlice;
        public event CorporateActionEventHandler NewCorporateAction;
        public event FundamentalEventHandler NewFundamental;
        public event BarEventHandler NewMarketBar;
        public event MarketDataEventHandler NewMarketData;
        public event MarketDepthEventHandler NewMarketDepth;
        public event QuoteEventHandler NewQuote;
        public event TradeEventHandler NewTrade;

        #region IMarketDataProvider
        [Category(CATEGORY_BARFACTORY)]
        public IBarFactory BarFactory
        {
            get
            {
                return factory;
            }
            set
            {
                if (factory != null)
                {
                    factory.NewBar -= OnNewBar;
                    factory.NewBarOpen -= OnNewBarOpen;
                    factory.NewBarSlice -= OnNewBarSlice;
                }
                factory = value;
                if (factory != null)
                {
                    factory.NewBar += OnNewBar;
                    factory.NewBarOpen += OnNewBarOpen;
                    factory.NewBarSlice += OnNewBarSlice;
                }
            }
        }

        private void OnNewBarSlice(object sender, BarSliceEventArgs args)
        {
            if (NewBarSlice != null)
            {
                NewBarSlice(this, new BarSliceEventArgs(args.BarSize, this));
            }
        }

        public void SendMarketDataRequest(FIXMarketDataRequest request)
        {
            //if (!_bMdConnected)
            //{
            //    EmitError(-1, -1, "行情服务器没有连接");
            //    mdlog.Error("行情服务器没有连接");
            //    return;
            //}

            bool bSubscribe = false;
            bool bTrade = false;
            bool bQuote = false;
            bool bMarketDepth = false;
            if (request.NoMDEntryTypes > 0)
            {
                switch (request.GetMDEntryTypesGroup(0).MDEntryType)
                {
                    case FIXMDEntryType.Bid:
                    case FIXMDEntryType.Offer:
                        if (request.MarketDepth != 1)
                        {
                            bMarketDepth = true;
                            break;
                        }
                        bQuote = true;
                        break;
                    case FIXMDEntryType.Trade:
                        bTrade = true;
                        break;
                }
            }
            bSubscribe = (request.SubscriptionRequestType == DataManager.MARKET_DATA_SUBSCRIBE);

            if (bSubscribe)
            {
                for (int i = 0; i < request.NoRelatedSym; ++i)
                {
                    FIXRelatedSymGroup group = request.GetRelatedSymGroup(i);
                    Instrument inst = InstrumentManager.Instruments[group.Symbol];

                    //将用户合约转成交易所合约
                    string altSymbol = inst.GetSymbol(this.Name);
                    string altExchange = inst.GetSecurityExchange(this.Name);
                    string MarketStockCode = altExchange.ToLower() + altSymbol;

                    DataRecord record;
                    if (!_dictAltSymbol2Instrument.TryGetValue(MarketStockCode, out record))
                    {
                        record = new DataRecord();
                        record.Instrument = inst;
                        record.Symbol = altSymbol;
                        record.Exchange = altExchange;
                        _dictAltSymbol2Instrument[MarketStockCode] = record;

                        mdlog.Info("订阅合约 {0} {1} {2}", MarketStockCode, record.Symbol, record.Exchange);
                    }

                    if (bTrade)
                        record.TradeRequested = true;
                    if (bQuote)
                        record.QuoteRequested = true;
                    if (bMarketDepth)
                        record.MarketDepthRequested = true;

                    if (bMarketDepth)
                    {
                        inst.OrderBook.Clear();
                    }
                }
            }
            else
            {
                for (int i = 0; i < request.NoRelatedSym; ++i)
                {
                    FIXRelatedSymGroup group = request.GetRelatedSymGroup(i);
                    Instrument inst = InstrumentManager.Instruments[group.Symbol];

                    //将用户合约转成交易所合约
                    string altSymbol = inst.GetSymbol(this.Name);
                    string altExchange = inst.GetSecurityExchange(this.Name);
                    string MarketStockCode = altExchange.ToLower() + altSymbol;

                    DataRecord record;
                    if (!_dictAltSymbol2Instrument.TryGetValue(MarketStockCode, out record))
                    {
                        break;
                    }

                    if (bTrade)
                        record.TradeRequested = false;
                    if (bQuote)
                        record.QuoteRequested = false;
                    if (bMarketDepth)
                        record.MarketDepthRequested = false;

                    if (!record.TradeRequested && !record.QuoteRequested && !record.MarketDepthRequested)
                    {
                        _dictAltSymbol2Instrument.Remove(MarketStockCode);
                        mdlog.Info("取消合约 {0} {1} {2}", MarketStockCode, record.Symbol, record.Exchange);
                    }
                }
            }
        }

        private void EmitNewMarketDepth(IFIXInstrument instrument, MarketDepth marketDepth)
        {
            if (NewMarketDepth != null)
            {
                NewMarketDepth(this, new MarketDepthEventArgs(marketDepth, instrument, this));
            }
        }
        #endregion
    }
}
