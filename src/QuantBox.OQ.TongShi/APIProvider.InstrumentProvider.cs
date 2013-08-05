using SmartQuant.FIX;
using SmartQuant.Providers;
using StockDll;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace QuantBox.OQ.TongShi
{
    public partial class APIProvider : IInstrumentProvider
    {
        public event SecurityDefinitionEventHandler SecurityDefinition;

        public void SendSecurityDefinitionRequest(FIXSecurityDefinitionRequest request)
        {
            lock (_dictDepthMarketData)
            {
                string symbol = request.ContainsField(EFIXField.Symbol) ? request.Symbol : null;
                string securityType = request.ContainsField(EFIXField.SecurityType) ? request.SecurityType : null;
                string securityExchange = request.ContainsField(EFIXField.SecurityExchange) ? request.SecurityExchange : null;


                #region 过滤
                List<StructRcvReportEx> list = new List<StructRcvReportEx>();
                foreach (StructRcvReport inst in _dictDepthMarketData.Values)
                {
                    StructRcvReportEx ex = new StructRcvReportEx(inst);

                    int flag = 0;
                    if (null == symbol)
                    {
                        ++flag;
                    }
                    else if (ex.newSymbol.ToUpper().StartsWith(symbol.ToUpper()))
                    {
                        ++flag;
                    }

                    if (null == securityExchange)
                    {
                        ++flag;
                    }
                    else if (ex.yahooExchange.StartsWith(securityExchange.ToUpper()))
                    {
                        ++flag;
                    }

                    if (null == securityType)
                    {
                        ++flag;
                    }
                    else
                    {
                        if (securityType == ex.securityType)
                        {
                            ++flag;
                        }
                    }

                    if (3 == flag)
                    {
                        list.Add(ex);
                    }
                }
                #endregion

                list.Sort(SortCThostFtdcInstrumentField);

                //如果查出的数据为0，应当想法立即返回
                if (0 == list.Count)
                {
                    FIXSecurityDefinition definition = new FIXSecurityDefinition
                    {
                        SecurityReqID = request.SecurityReqID,
                        SecurityResponseID = request.SecurityReqID,
                        SecurityResponseType = request.SecurityRequestType,
                        TotNoRelatedSym = 1//有个除0错误的问题
                    };
                    if (SecurityDefinition != null)
                    {
                        SecurityDefinition(this, new SecurityDefinitionEventArgs(definition));
                    }
                }

                foreach (StructRcvReportEx inst in list)
                {
                    FIXSecurityDefinition definition = new FIXSecurityDefinition
                    {
                        SecurityReqID = request.SecurityReqID,
                        //SecurityResponseID = request.SecurityReqID,
                        SecurityResponseType = request.SecurityRequestType,
                        TotNoRelatedSym = list.Count
                    };

                    {
                        definition.AddField(EFIXField.SecurityType, inst.securityType);
                    }
                    {
                        //double x = inst.PriceTick;
                        //if (x > 0.0001)
                        //{
                        //    int i = 0;
                        //    for (; x - (int)x != 0; ++i)
                        //    {
                        //        x = x * 10;
                        //    }
                        //    definition.AddField(EFIXField.PriceDisplay, string.Format("F{0}", i));
                        //    definition.AddField(EFIXField.TickSize, inst.PriceTick);
                        //}
                    }

                    definition.AddField(EFIXField.Symbol, GetYahooSymbol(inst.newSymbol, inst.yahooExchange));
                    definition.AddField(EFIXField.SecurityExchange, inst.yahooExchange);
                    definition.AddField(EFIXField.Currency, "CNY");//Currency.CNY
                    definition.AddField(EFIXField.SecurityDesc, inst.RcvReport.StockName);

                    FIXSecurityAltIDGroup group = new FIXSecurityAltIDGroup();
                    group.SecurityAltID = inst.RcvReport.StockCode;
                    group.SecurityAltExchange = inst.RcvReport.MarketType;
                    group.SecurityAltIDSource = this.Name;

                    definition.AddGroup(group);

                    //还得补全内容

                    if (SecurityDefinition != null)
                    {
                        SecurityDefinition(this, new SecurityDefinitionEventArgs(definition));
                    }
                }
            }
        }

        private int SortCThostFtdcInstrumentField(StructRcvReportEx a1, StructRcvReportEx a2)
        {
            string s1 = a1.newSymbol;
            string s2 = a2.newSymbol;

            return s1.CompareTo(s2);
        }
        #region 证券接口


        /*
         * 上海证券交易所证券代码分配规则
         * http://www.docin.com/p-417422186.html
         * 
         * http://wenku.baidu.com/view/f2e9ddf77c1cfad6195fa706.html
         */

        private string GetYahooSymbol(string InstrumentID, string ExchangeID)
        {
            return string.Format("{0}.{1}", InstrumentID, ExchangeID.Substring(0, 2));
        }

        private string GetApiSymbol(string Symbol)
        {
            var match = Regex.Match(Symbol, @"(\d+)\.(\w+)");
            if (match.Success)
            {
                var code = match.Groups[1].Value;
                return code;
            }
            return Symbol;
        }

        private string GetApiExchange(string Symbol)
        {
            var match = Regex.Match(Symbol, @"(\d+)\.(\w+)");
            if (match.Success)
            {
                var code = match.Groups[2].Value;
                return code;
            }
            return Symbol;
        }
        #endregion
    }
}
