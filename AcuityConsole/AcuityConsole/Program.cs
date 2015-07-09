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

            XmlReader reader = XmlReader.Create("https://www.sec.gov/cgi-bin/browse-edgar?action=getcurrent&type=4&company=&dateb=&owner=include&start=0&count=4000&output=atom");
            //returning anything starting with 4 for some reason
            SyndicationFeed feed = SyndicationFeed.Load(reader);

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
                    if (filing.DocumentElement.SelectSingleNode("//transactionCode[.='P']") != null)
                    {

                        Console.WriteLine("{0} ({1}) - {2} buys {3}",
                            filing.DocumentElement.SelectSingleNode("//issuerName") != null ? filing.DocumentElement.SelectSingleNode("//issuerName").InnerText : "",
                            filing.DocumentElement.SelectSingleNode("//issuerTradingSymbol") != null ? filing.DocumentElement.SelectSingleNode("//issuerTradingSymbol").InnerText : "",
                            filing.DocumentElement.SelectSingleNode("//officerTitle") != null ? filing.DocumentElement.SelectSingleNode("//officerTitle").InnerText : "",
                        Convert.ToDecimal(filing.DocumentElement.SelectSingleNode("//transactionShares//value").InnerText) * Convert.ToDecimal(filing.DocumentElement.SelectSingleNode("//transactionPricePerShare//value").InnerText)
                            );
                    }
                }
            }
        }

    }

}
