using SmartQuant.FIX;
using StockDll;
using System;
using NLog;

namespace QuantBox.OQ.TongShi
{
    public class StructRcvReportEx
    {
        private static readonly Logger mdlog = LogManager.GetLogger("TongShi.M");

        string[,] CodeCover = {
                                    {"1A0001","000001"},
                                    {"1A0002","000002"},
                                    {"1A0003","000003"},
                                    {"1B0001","000004"},
                                    {"1B0002","000005"},
                                    {"1B0004","000006"},
                                    {"1B0005","000007"},
                                    {"1B0006","000008"},
                                    {"1B0007","000010"},
                                    {"1B0008","000011"},
                                    {"1B0009","000012"},
                                    {"1B0010","000013"},
                                    {"1B0015","000015"},
                                    {"1B0016","000016"},
                                    {"1B0017","000017"},

            };

        public StructRcvReportEx(StructRcvReport RcvReport)
        {
            this.RcvReport = RcvReport;
            newSymbol = GetNewSymbol(RcvReport.StockCode);
            yahooExchange = GetYahooSecurityExchange(RcvReport.MarketType);
            securityType = GetSecurityType();
        }

        public StructRcvReport RcvReport { get; private set; }
        public string newSymbol;
        public string yahooExchange;
        public string securityType;

        private string GetNewSymbol(string symbol)
        {
            if (symbol[1] >= 'A')
            {
                for (int i = 0; i < 15; ++i)
                {
                    if (symbol.CompareTo(CodeCover[i,0]) == 0)
                    {
                        return CodeCover[i, 1];
                    }
                }
            }
            return symbol;
        }

        private string GetYahooSecurityExchange(string MarketType)
        {
            if (MarketType == "SZ")
            {
                return "SZ";
            }
            return "SS";
        }

        private string GetSecurityType()
        {
            if (yahooExchange == "SZ")
            {
                return GetSecurityTypeSZ(newSymbol);
            }
            else
            {
                return GetSecurityTypeSS(newSymbol);
            }
        }

        private string GetSecurityTypeSS(string stockCode)
        {
            string securityType = FIXSecurityType.NoSecurityType;
            try
            {
                int i = Convert.ToInt32(stockCode.Substring(0, 3));
                if (i == 0)
                {
                    securityType = FIXSecurityType.Index;
                }
                else if (i < 399)
                {
                    securityType = FIXSecurityType.USTreasuryBond;
                }
                else if (i < 599)
                {
                    securityType = FIXSecurityType.USTreasuryBond;
                }
                else if (i < 699)
                {
                    securityType = FIXSecurityType.CommonStock;
                }
                else if (i < 700)
                {
                    securityType = FIXSecurityType.ExchangeTradedFund;
                }
                else
                {
                    securityType = FIXSecurityType.CommonStock;
                }
            }
            catch(Exception ex)
            {
                mdlog.Warn("异常信息：{0}, 代码名：{1}", ex.Message, stockCode);
            }

            return securityType;
        }

        private string GetSecurityTypeSZ(string stockCode)
        {
            string securityType = FIXSecurityType.NoSecurityType;
            int i = Convert.ToInt32(stockCode.Substring(0, 2));
            if (i < 10)
            {
                securityType = FIXSecurityType.CommonStock;
            }
            else if (i < 15)
            {
                securityType = FIXSecurityType.USTreasuryBond;
            }
            else if (i < 20)
            {
                securityType = FIXSecurityType.ExchangeTradedFund;
            }
            else if (i < 30)
            {
                securityType = FIXSecurityType.CommonStock;
            }
            else if (i < 39)
            {
                securityType = FIXSecurityType.CommonStock;
            }
            else if (i == 39)
            {
                securityType = FIXSecurityType.Index;
            }
            else
            {
                securityType = FIXSecurityType.NoSecurityType;
            }
            return securityType;
        }
    }
}
