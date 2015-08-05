using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.ServiceModel.Syndication;
using System.Xml;
using Newtonsoft.Json;
using NLog;

namespace AcuityConsole
{

    internal class EdgarService
    {

        Logger logger = new LogFactory().GetLogger("Acuity");

        private YahooController mgr = new YahooController();
        public void AnalyseByCompany(string companyName, int startYear, int endYear)
        {
            for (int i = startYear; i <= endYear; i++)
            {
                ReportOnTypeFours(string.Format("https://www.sec.gov/cgi-bin/srch-edgar?text=COMPANY-NAME%3D{0}%20and%20%20FORM-TYPE%3D4&start=1&count=8000&first={1}&last={1}&output=atom", companyName, i));
            }
        }

        public Trade GetTrade(string filingText)
        {
            if (!filingText.Contains("<XML>"))
            {
                // Console.WriteLine("Not modern xml format.");
                return null;

            }
            filingText = filingText.Substring(filingText.IndexOf("<XML>", StringComparison.Ordinal) + 5, filingText.IndexOf("</XML>", StringComparison.Ordinal) - (filingText.IndexOf("<XML>", StringComparison.Ordinal) + 5)).Trim();

            var filing = new XmlDocument();
            filing.LoadXml(filingText);

            if (filing.DocumentElement == null || filing.DocumentElement.SelectSingleNode("//transactionCode") == null) return null;

            Trade trade = null;
            try
            {
                trade = new Trade();
                trade.transactionCode = filing.DocumentElement.SelectSingleNode("//transactionCode").InnerText;
                if (trade.transactionCode != "P") return null;
                trade.transactionShares = Convert.ToDouble(filing.DocumentElement.SelectSingleNode("//transactionShares//value").InnerText);
                trade.transactionPricePerShare = Convert.ToDouble(filing.DocumentElement.SelectSingleNode("//transactionPricePerShare//value").InnerText);
                trade.issuerTradingSymbol = filing.DocumentElement.SelectSingleNode("//issuerTradingSymbol").InnerText;
                trade.marketCapString = mgr.GetMarketCapitalisation(trade.issuerTradingSymbol);
                trade.transactionDate = Convert.ToDateTime(filing.DocumentElement.SelectSingleNode("//transactionDate//value").InnerText);

                var prices = mgr.GetHistoricalPrices(trade.issuerTradingSymbol, trade.transactionDate.AddMonths(-3), trade.transactionDate.AddMonths(3));
                trade.PriceRequest = JsonConvert.DeserializeObject<YahooHistoricalPriceRequest>(prices, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

            }
            catch (Exception)
            {

                return null;
            }

            return trade;

        }

        public void ReportOnTypeFours(string feedUri)
        {
            var cleanRssFeed = new Utils().FixRssFeed(new WebClient().DownloadString(feedUri));
            TextReader tr = new StringReader(cleanRssFeed);
            XmlReader reader = XmlReader.Create(tr);

            SyndicationFeed feed = SyndicationFeed.Load(reader);

            if (feed == null) return;

            var messages = new List<string>();
            foreach (var item in feed.Items)
            {
                //form 4 only. 
                if (item.Categories[0].Label.Equals("form type", StringComparison.OrdinalIgnoreCase) &&
                    item.Categories[0].Name.Equals("4", StringComparison.OrdinalIgnoreCase))
                {
                    var docUri = item.Links[0].Uri.IsAbsoluteUri ? item.Links[0].Uri.AbsoluteUri.Replace("http", "https") : string.Format("https://www.sec.gov{0}", item.Links[0].Uri.OriginalString);
                    docUri = docUri.Replace("-index.htm", ".txt");

                    var res = new WebClient().DownloadString(docUri);

                    var trade = GetTrade(res);

                    if (trade != null && trade.transactionCode == "P")
                    {

                        var theMessage = trade.ToString();

                        if (!messages.Contains(trade.ToString()))
                        {
                            messages.Add(theMessage); logger.Info(theMessage);
                        }
                    }
                }
            }
        }

    }
}
