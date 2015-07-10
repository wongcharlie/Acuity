using System;
using System.Net;
using System.ServiceModel.Syndication;
using System.Xml;

namespace AcuityConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //   XmlReader reader = XmlReader.Create("https://www.sec.gov/Archives/edgar/xbrlrss.all.xml");
            //  SyndicationFeed feed = SyndicationFeed.Load(reader);

            //get all types starting with 4.
            XmlReader reader = XmlReader.Create("https://www.sec.gov/cgi-bin/browse-edgar?action=getcurrent&type=4&company=&dateb=&owner=include&start=0&count=4000&output=atom");
            SyndicationFeed feed = SyndicationFeed.Load(reader);

            if (feed != null)
                foreach (var item in feed.Items)
                {
                    //form 4 only. 
                    if (item.Categories[0].Label.Equals("form type", StringComparison.OrdinalIgnoreCase) &&
                        item.Categories[0].Name.Equals("4", StringComparison.OrdinalIgnoreCase))
                    {
                        var docUri = item.Links[0].Uri.AbsoluteUri.Replace("http", "https").Replace("-index.htm", ".txt");

                        var res = new WebClient().DownloadString(docUri);
                        res = res.Substring(res.IndexOf("<XML>", StringComparison.Ordinal) + 5, res.IndexOf("</XML>", StringComparison.Ordinal) - (res.IndexOf("<XML>", StringComparison.Ordinal) + 5)).Trim();

                        var filing = new XmlDocument();
                        filing.LoadXml(res);
                        if (filing.DocumentElement != null && filing.DocumentElement.SelectSingleNode("//transactionCode[.='P']") != null)
                        {
                            var value =
                                Convert.ToDouble(
                                    filing.DocumentElement.SelectSingleNode("//transactionShares//value").InnerText) *
                                Convert.ToDouble(
                                    filing.DocumentElement.SelectSingleNode("//transactionPricePerShare//value")
                                        .InnerText);
                            var marketCapString = new MarketDataService().GetMarketCapitalisation(filing.DocumentElement.SelectSingleNode("//issuerTradingSymbol").InnerText);

                            Console.WriteLine("{0} ({1}) - {2} buys {3} on market cap of {4} - {5:P}",
                                filing.DocumentElement.SelectSingleNode("//issuerName") != null ? filing.DocumentElement.SelectSingleNode("//issuerName").InnerText : "",
                                filing.DocumentElement.SelectSingleNode("//issuerTradingSymbol") != null ? filing.DocumentElement.SelectSingleNode("//issuerTradingSymbol").InnerText : "",
                                filing.DocumentElement.SelectSingleNode("//officerTitle") != null ? filing.DocumentElement.SelectSingleNode("//officerTitle").InnerText : "",
                                value, marketCapString, value / new ParsingUtils().GetNumericValue(marketCapString));



                        }

                    }
                }
        }
    }

    internal class MarketDataService
    {
        public string GetMarketCapitalisation(string ticker)
        {
            var uri =
                string.Format(
                    "https://query.yahooapis.com/v1/public/yql?q=select%20*%20from%20yahoo.finance.quote%20where%20symbol%3D%22{0}%22&diagnostics=true&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys", ticker.ToUpper());

            var res = new WebClient().DownloadString(uri);
            var filing = new XmlDocument();
            filing.LoadXml(res);

            if (filing.DocumentElement != null)
            {
                var selectSingleNode = filing.DocumentElement.SelectSingleNode("//MarketCapitalization");
                if (selectSingleNode != null)
                    return selectSingleNode.InnerText;
            }
            return string.Empty;
        }

    }

    internal class ParsingUtils
    {
        public double GetNumericValue(string numericString)
        {
            if (numericString.EndsWith("M")) return Convert.ToDouble(numericString.Replace("M", "")) * 1000000;
            if (numericString.EndsWith("B")) return Convert.ToDouble(numericString.Replace("B", "")) * 1000000000;
            return Convert.ToDouble(numericString);

        }
    }
}
