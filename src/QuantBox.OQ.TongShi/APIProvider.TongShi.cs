using NLog;
using NLog.Config;
using SmartQuant;
using SmartQuant.Data;
using SmartQuant.Providers;
using StockDll;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using QuantBox.Helper.TongShi;
using SmartQuant.Instruments;

namespace QuantBox.OQ.TongShi
{
    public partial class APIProvider
    {
        private IStockService StockService;

        private readonly Dictionary<string, DataRecord> _dictAltSymbol2Instrument = new Dictionary<string, DataRecord>();

        private readonly Dictionary<string, StructRcvReport> _dictDepthMarketData = new Dictionary<string, StructRcvReport>();

        /// <summary>
        /// 初始化股票接口，并注册相关事件。
        /// </summary>
        void InitStockService()
        {
            StockService = StockServiceFactory.CreateStockService();
            //注册登录通知事件
            StockService.OnLoginAuth += new EventHandler<LoginAuthEventArgs>(StockService_OnLoginAuth);
            //注册补5分钟数据事件
            StockService.OnRcv5Minute += new EventHandler<Rcv5MinuteEventArgs>(StockService_OnRcv5Minute);
            //注册收到基本资料事件
            StockService.OnRcvBase += new EventHandler<RcvBaseEventArgs>(StockService_OnRcvBase);
            //注册补历史日线数据事件
            StockService.OnRcvHistory += new EventHandler<RcvHistoryEventArgs>(StockService_OnRcvHistory);
            //注册补分钟数据事件
            StockService.OnRcvMinute += new EventHandler<RcvMinuteEventArgs>(StockService_OnRcvMinute);
            //注册接收到新闻资料数据事件。
            StockService.OnRcvNew += new EventHandler<RcvNewEventArgs>(StockService_OnRcvNew);
            //注册接收到除权数据事件
            StockService.OnRcvPower += new EventHandler<RcvPowerEventArgs>(StockService_OnRcvPower);
            //注册接收到行情数据事件
            StockService.OnRcvReport += new EventHandler<RcvReportEventArgs>(StockService_OnRcvReport);
            //注册接收到财务数据事件
            StockService.OnRcvSTKFin += new EventHandler<RcvSTKFinEventArgs>(StockService_OnRcvSTKFin);
            //注册接收到证券代码表事件
            StockService.OnRcvSTKLabel += new EventHandler<RcvSTKLabelEventArgs>(StockService_OnRcvSTKLabel);
            //注册接收到分笔数据事件
            StockService.OnRcvSTKTick += new EventHandler<RcvSTKTickEventArgs>(StockService_OnRcvSTKTick);
        }

        void StockService_OnRcvSTKTick(object sender, RcvSTKTickEventArgs e)
        {
            //StringBuilder Builder = new StringBuilder();
            //Builder.Append(string.Format("消息ID{0},收到分笔数据{1},显示前20行...", e.Msg.ToString(), e.StkTicks.Length.ToString()));
            //Builder.Append(System.Environment.NewLine);
            //BuildHeader(typeof(StructSTKTICK), Builder);
            //for (int i = 0; i < 20 && i < e.StkTicks.Length; i++)
            //{
            //    BuildData(e.StkTicks[i], Builder);
            //}

            ////由于数据发送采用异步调用，更新界面必须在UI线程上执行。
            //this.Invoke(new UpdateRichTextBox(this.UpdateTextBox), Builder.ToString());

        }

        void StockService_OnRcvSTKLabel(object sender, RcvSTKLabelEventArgs e)
        {

            //StringBuilder Builder = new StringBuilder();
            //Builder.Append(string.Format("消息ID{0},收到证券代码表{1},显示前20行...", e.Msg.ToString(), e.RcvStkLabels.Length.ToString()));
            //Builder.Append(System.Environment.NewLine);
            //BuildHeader(typeof(StructSTKLABEL), Builder);
            //for (int i = 0; i < 20 && i < e.RcvStkLabels.Length; i++)
            //{
            //    BuildData(e.RcvStkLabels[i], Builder);
            //}
            ////由于数据发送采用异步调用，更新界面必须在UI线程上执行。
            //this.Invoke(new UpdateRichTextBox(this.UpdateTextBox), Builder.ToString());

        }

        void StockService_OnRcvSTKFin(object sender, RcvSTKFinEventArgs e)
        {
            //StringBuilder Builder = new StringBuilder();
            //Builder.Append(string.Format("消息ID{0},收到财务数据{1},显示前20行...", e.Msg.ToString(), e.RcvFins.Length.ToString()));
            //Builder.Append(System.Environment.NewLine);
            //BuildHeader(typeof(StructSTKFin), Builder);
            //for (int i = 0; i < 20 && i < e.RcvFins.Length; i++)
            //{
            //    BuildData(e.RcvFins[i], Builder);
            //}
            ////由于数据发送采用异步调用，更新界面必须在UI线程上执行。
            //this.Invoke(new UpdateRichTextBox(this.UpdateTextBox), Builder.ToString());
        }

        private DateTime _dateTime = DateTime.Now;
        void StockService_OnRcvReport(object sender, RcvReportEventArgs e)
        {
            _dateTime = Clock.Now;

            lock (_dictDepthMarketData)
            {
                for (int i = 0; i < e.RcvReports.Length; ++i)
                {
                    StructRcvReport pDepthMarketData = e.RcvReports[i];
                    
                    StructRcvReport DepthMarket;
                    _dictDepthMarketData.TryGetValue(pDepthMarketData.MarketStockCode, out DepthMarket);

                    _dictDepthMarketData[pDepthMarketData.MarketStockCode] = pDepthMarketData;

                    DataRecord record;
                    if (_dictAltSymbol2Instrument.TryGetValue(pDepthMarketData.MarketStockCode, out record))
                    {
                        if (record.TradeRequested)
                        {
                            if (DepthMarket.NewPrice == pDepthMarketData.NewPrice
                            && DepthMarket.Volume == pDepthMarketData.Volume)
                            { }
                            else
                            {
                                float volume = pDepthMarketData.Volume - DepthMarket.Volume;
                                if (0 == DepthMarket.Volume)
                                {
                                    //没有接收到最开始的一条，所以这计算每个Bar的数据时肯定超大，强行设置为0
                                    volume = 0;
                                }
                                else if (volume < 0)
                                {
                                    //如果隔夜运行，会出现今早成交量0-昨收盘成交量，出现负数，所以当发现为负时要修改
                                    volume = pDepthMarketData.Volume;
                                }


                                TongShiTrade trade = new TongShiTrade(_dateTime,
                                    pDepthMarketData.NewPrice,
                                    (int)volume);

                                EmitNewTradeEvent(record.Instrument, trade);
                            }
                        }

                        if (record.QuoteRequested)
                        {
                            TongShiQuote quote = new TongShiQuote(_dateTime,
                                    (double)pDepthMarketData.BuyPrice1,
                                    (int)pDepthMarketData.BuyVolume1,
                                    (double)pDepthMarketData.SellPrice1,
                                    (int)pDepthMarketData.SellVolume1
                                );

                            quote.DepthMarketData = pDepthMarketData;

                            EmitNewQuoteEvent(record.Instrument, quote);
                        }

                        if (record.MarketDepthRequested)
                        {
                            bool bAsk = true;
                            bool bBid = true;

                            if(bAsk)
                                bAsk = EmitNewMarketDepth(record.Instrument, _dateTime, 0, MDSide.Ask, (double)pDepthMarketData.SellPrice1, (int)pDepthMarketData.SellVolume1);
                            if(bBid)
                                bBid = EmitNewMarketDepth(record.Instrument, _dateTime, 0, MDSide.Bid, (double)pDepthMarketData.BuyPrice1, (int)pDepthMarketData.BuyVolume1);

                            if (bAsk)
                                bAsk = EmitNewMarketDepth(record.Instrument, _dateTime, 1, MDSide.Ask, (double)pDepthMarketData.SellPrice2, (int)pDepthMarketData.SellVolume2);
                            if (bBid)
                                bBid = EmitNewMarketDepth(record.Instrument, _dateTime, 1, MDSide.Bid, (double)pDepthMarketData.BuyPrice2, (int)pDepthMarketData.BuyVolume2);

                            if (bAsk)
                                bAsk = EmitNewMarketDepth(record.Instrument, _dateTime, 2, MDSide.Ask, (double)pDepthMarketData.SellPrice3, (int)pDepthMarketData.SellVolume3);
                            if (bBid)
                                bBid = EmitNewMarketDepth(record.Instrument, _dateTime, 2, MDSide.Bid, (double)pDepthMarketData.BuyPrice3, (int)pDepthMarketData.BuyVolume3);

                            if (bAsk)
                                bAsk = EmitNewMarketDepth(record.Instrument, _dateTime, 3, MDSide.Ask, (double)pDepthMarketData.SellPrice4, (int)pDepthMarketData.SellVolume4);
                            if (bBid)
                                bBid = EmitNewMarketDepth(record.Instrument, _dateTime, 3, MDSide.Bid, (double)pDepthMarketData.BuyPrice4, (int)pDepthMarketData.BuyVolume4);

                            if (bAsk)
                                bAsk = EmitNewMarketDepth(record.Instrument, _dateTime, 4, MDSide.Ask, (double)pDepthMarketData.SellPrice5, (int)pDepthMarketData.SellVolume5);
                            if (bBid)
                                bBid = EmitNewMarketDepth(record.Instrument, _dateTime, 4, MDSide.Bid, (double)pDepthMarketData.BuyPrice5, (int)pDepthMarketData.BuyVolume5);
                        }
                    }
                }
            }
        }

        private bool EmitNewMarketDepth(Instrument instrument, DateTime datatime, int position, MDSide ask, double price, int size)
        {
            bool bRet = false;
            MDOperation insert = MDOperation.Update;
            if (MDSide.Ask == ask)
            {
                if (position >= instrument.OrderBook.Ask.Count)
                {
                    insert = MDOperation.Insert;
                }
            }
            else
            {
                if (position >= instrument.OrderBook.Bid.Count)
                {
                    insert = MDOperation.Insert;
                }
            }

            if (price != 0 && size != 0)
            {
                EmitNewMarketDepth(instrument, new MarketDepth(datatime, "", position, insert, ask, price, size));
                bRet = true;
            }
            return bRet;
        }

        void StockService_OnRcvPower(object sender, RcvPowerEventArgs e)
        {
            //StringBuilder Builder = new StringBuilder();
            //Builder.Append(string.Format("消息ID{0},收到除权数据{1},显示前20行...", e.Msg.ToString(), e.RcvPowers.Length.ToString()));
            //Builder.Append(System.Environment.NewLine);
            //BuildHeader(typeof(StructRcvPower), Builder);
            //for (int i = 0; i < 20 && i < e.RcvPowers.Length; i++)
            //{
            //    BuildData(e.RcvPowers[i], Builder);
            //}
            ////由于数据发送采用异步调用，更新界面必须在UI线程上执行。
            //this.Invoke(new UpdateRichTextBox(this.UpdateTextBox), Builder.ToString());
        }

        void StockService_OnRcvNew(object sender, RcvNewEventArgs e)
        {
            //StringBuilder Builder = new StringBuilder();
            //Builder.AppendFormat("消息ID{0},收到新闻资料：", e.Msg);
            //Builder.Append(e.FileHead.FileName);
            //Builder.Append(System.Environment.NewLine);
            ////由于数据发送采用异步调用，更新界面必须在UI线程上执行。
            //this.Invoke(new UpdateRichTextBox(this.UpdateTextBox), Builder.ToString());
        }

        void StockService_OnRcvMinute(object sender, RcvMinuteEventArgs e)
        {
            //StringBuilder Builder = new StringBuilder();
            //Builder.Append(string.Format("消息ID{0},收到1分钟数据{1},显示前20行...", e.Msg.ToString(), e.RcvMinutes.Length.ToString()));
            //Builder.Append(System.Environment.NewLine);
            //BuildHeader(typeof(StructRcvMinute), Builder);
            //for (int i = 0; i < 20 && i < e.RcvMinutes.Length; i++)
            //{
            //    BuildData(e.RcvMinutes[i], Builder);
            //}
            ////由于数据发送采用异步调用，更新界面必须在UI线程上执行。
            //this.Invoke(new UpdateRichTextBox(this.UpdateTextBox), Builder.ToString());

        }

        void StockService_OnRcvHistory(object sender, RcvHistoryEventArgs e)
        {
            //StringBuilder Builder = new StringBuilder();
            //Builder.Append(string.Format("消息ID{0},收到历史日线数据{1},显示前20行...", e.Msg.ToString(), e.RcvHistorys.Length.ToString()));
            //Builder.Append(System.Environment.NewLine);
            //BuildHeader(typeof(StructRcvHistory), Builder);
            //for (int i = 0; i < 20 && i < e.RcvHistorys.Length; i++)
            //{
            //    BuildData(e.RcvHistorys[i], Builder);
            //}
            ////由于数据发送采用异步调用，更新界面必须在UI线程上执行。
            //this.Invoke(new UpdateRichTextBox(this.UpdateTextBox), Builder.ToString());
        }

        void StockService_OnRcvBase(object sender, RcvBaseEventArgs e)
        {
            //StringBuilder Builder = new StringBuilder();
            //Builder.AppendFormat("消息ID{0},收到基本资料：", e.Msg);
            //Builder.Append(e.FileHead.FileName);
            //Builder.Append(System.Environment.NewLine);
            ////由于数据发送采用异步调用，更新界面必须在UI线程上执行。
            //this.Invoke(new UpdateRichTextBox(this.UpdateTextBox), Builder.ToString());
        }

        void StockService_OnRcv5Minute(object sender, Rcv5MinuteEventArgs e)
        {
            //StringBuilder Builder = new StringBuilder();
            //Builder.Append(string.Format("消息ID{0},收到5分钟数据{1},显示前20行...", e.Msg.ToString(), e.RcvHistorys.Length.ToString()));
            //Builder.Append(System.Environment.NewLine);
            //BuildHeader(typeof(StructRcvHistory), Builder);
            //for (int i = 0; i < 20 && i < e.RcvHistorys.Length; i++)
            //{
            //    BuildData(e.RcvHistorys[i], Builder);
            //}
            ////由于数据发送采用异步调用，更新界面必须在UI线程上执行。
            //this.Invoke(new UpdateRichTextBox(this.UpdateTextBox), Builder.ToString());
        }

        void StockService_OnLoginAuth(object sender, LoginAuthEventArgs e)
        {
            StringBuilder Builder = new StringBuilder();
            if (e.StkLoginAuth.bAuthorizationState == 1)
            {
                ChangeStatus(ProviderStatus.LoggedIn);
                isConnected = true;
                EmitConnectedEvent();
            }
            else
            {
                ChangeStatus(ProviderStatus.Disconnected);
                isConnected = false;
                EmitDisconnectedEvent();
            }
        }
        
    }
}
