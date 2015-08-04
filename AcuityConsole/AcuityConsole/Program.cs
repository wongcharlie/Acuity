using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using FileHelpers;
using NLog;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Cookie = OpenQA.Selenium.Cookie;

namespace AcuityConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //historical 

            //            var s = new Utils().DisplayFileFromFtpServer(new Uri("ftp://ftp.sec.gov/edgar/full-index/2015/QTR1/master.idx"));
            var s = File.ReadAllText(@"C:\Users\charlie\Documents\GitHub\Acuity\TestData\master.idx");

            FileHelperEngine engine = new FileHelperEngine(typeof(TradeSummary));

            var ret = engine.ReadString(s) as TradeSummary[];

            Parallel.ForEach(ret, filing =>
            {
                var mgr = new EdgarService();
                if (filing.FormType.StartsWith("4"))
                {
                    string fullFilingText = null;
                    try
                    {
                        fullFilingText = new WebClient().DownloadString(string.Format("http://www.sec.gov/Archives/{0}", filing.FileName));
                    }
                    catch (Exception)
                    {

                        return;
                    }

                    if (fullFilingText == null) return;
                    var trade = mgr.GetTrade(fullFilingText);
                    if (trade != null && trade.transactionCode == "P")
                    {
                        Console.WriteLine(DateTime.Now + " " + Thread.CurrentThread.ManagedThreadId + " " + trade.ToString());
                    }

                }
            });


            //var mgr = new WebManager(); mgr.StartSession();
            //            var mgr = new PortfolioManager(); mgr.LogIn();

            //   XmlReader reader = XmlReader.Create("https://www.sec.gov/Archives/edgar/xbrlrss.all.xml");
            //  SyndicationFeed feed = SyndicationFeed.Load(reader);

            //get all types starting with 4.

            //            service.AnalyseByCompany("APPLE", 2010, 2015);
            //todays
            new EdgarService().ReportOnTypeFours("https://www.sec.gov/cgi-bin/browse-edgar?action=getcurrent&type=4&company=&dateb=&owner=include&start=0&count=4000&output=atom");


            // get all historicl type 4's = https://www.sec.gov/cgi-bin/srch-edgar?text=form-type%3D4&start=1&count=4000&first=2012&last=2015&output=atom
            // limited to 4000 results, can limit with ticker and years.
            //https://www.sec.gov/cgi-bin/srch-edgar?text=COMPANY-NAME%3DAPPLE%20and%20%20FORM-TYPE%3D4&start=1&count=8000&first=2001&last=2015&output=atom



        }
    }

    [DelimitedRecord("|"), IgnoreFirst(10), IgnoreEmptyLines()]
    public class TradeSummary
    {
        /// <summary>
        /// CIK|Company Name|Form Type|Date Filed|Filename
        /// </summary>
        public string CIK;
        public string Company;
        public string FormType;
        [FieldConverter(ConverterKind.Date, "yyyy-MM-dd")]
        public DateTime DateFiled;
        public string FileName;

    }


    class YahooManager
    {

        public void StartSession()
        {
            //Run selenium
            ChromeDriver cd = new ChromeDriver(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "lib"));
            cd.Url = @"https://login.yahoo.com/config/login?.src=fpctx&.intl=uk&.lang=en-GB&.done=https://uk.yahoo.com/%3fp=us";
            cd.Navigate();
            IWebElement e = cd.FindElementById("login-username");
            e.SendKeys("");
            e = cd.FindElementById("login-passwd");
            e.SendKeys("");
            e = cd.FindElementById("login-signin");
            e.Click();

            CookieContainer cc = new CookieContainer();
            //Get the cookies
            foreach (Cookie c in cd.Manage().Cookies.AllCookies)
            {
                string name = c.Name;
                string value = c.Value;
                cc.Add(new System.Net.Cookie(name, value, c.Path, c.Domain));
            }
            //cd.Quit();

            //Fire off the request
            HttpWebRequest hwr = (HttpWebRequest)HttpWebRequest.Create("https://uk.finance.yahoo.com/portfolio/pf_15/view/dv");
            hwr.CookieContainer = cc;
            hwr.Method = "POST";
            hwr.ContentType = "application/x-www-form-urlencoded";
            StreamWriter swr = new StreamWriter(hwr.GetRequestStream());
            swr.Write("feeds=35");
            swr.Close();

            WebResponse wr = hwr.GetResponse();
            string s = new StreamReader(wr.GetResponseStream()).ReadToEnd();
        }

        public string GetMarketCapitalisation(string ticker)
        {
            var uri = string.Format("https://query.yahooapis.com/v1/public/yql?q=select%20*%20from%20yahoo.finance.quote%20where%20symbol%3D%22{0}%22&diagnostics=true&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys", ticker.ToUpper());

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

    class Trade
    {
        public double transactionShares;
        public double transactionPricePerShare;
        public string issuerTradingSymbol;
        public DateTime transactionDate;
        public string issuerName;
        public string marketCapString;
        public string officerTitle;
        public string transactionCode;

        public override string ToString()
        {

            return string.Format("{5:P} {1} ({0}) - {2} buys {3} on market cap of {4} (trade date: {6:ddd dd MMM})",
                   issuerName,
                  issuerTradingSymbol,
                   officerTitle,
                   transactionShares * transactionPricePerShare,
                   marketCapString,
                   (transactionShares * transactionPricePerShare) / new Utils().GetNumericValue(marketCapString),
                   transactionDate);

        }
    }

    internal class EdgarService
    {

        Logger logger = new LogFactory().GetLogger("Acuity");
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
                trade.marketCapString = new YahooManager().GetMarketCapitalisation(trade.issuerTradingSymbol);
                trade.transactionDate = Convert.ToDateTime(filing.DocumentElement.SelectSingleNode("//transactionDate//value").InnerText);
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


    internal class MarketDataService
    {


    }


    internal class Utils
    {
        public double GetNumericValue(string numericString)
        {
            if (numericString.EndsWith("M")) return Convert.ToDouble(numericString.Replace("M", "")) * 1000000;
            if (numericString.EndsWith("B")) return Convert.ToDouble(numericString.Replace("B", "")) * 1000000000;
            if (numericString.Equals(string.Empty)) return 0;
            return Convert.ToDouble(numericString);

        }

        public string DisplayFileFromFtpServer(Uri serverUri)
        {
            // The serverUri parameter should start with the ftp:// scheme. 
            if (serverUri.Scheme != Uri.UriSchemeFtp)
            {
                return null;
            }
            // Get the object used to communicate with the server.
            WebClient request = new WebClient();

            // This example assumes the FTP site uses anonymous logon.
            request.Credentials = new NetworkCredential("anonymous", "anonymous@ftp.sec.gov");
            string fileString = null;
            try
            {
                byte[] newFileData = request.DownloadData(serverUri.ToString());
                fileString = Encoding.UTF8.GetString(newFileData);
                //Console.WriteLine(fileString);
            }
            catch (WebException e)
            {
                Console.WriteLine(e.ToString());
            }

            return fileString;//.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
        }

        public string FixRssFeed(string feedXml)
        {
            var xml = new XmlDocument(); xml.LoadXml(feedXml);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(xml.NameTable);
            nsmgr.AddNamespace("x", "http://www.w3.org/2005/Atom");

            foreach (XmlNode node in xml.SelectNodes("//x:updated", nsmgr))
            {
                DateTime parsed;
                if (DateTime.TryParseExact(node.InnerText, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                    node.InnerText = parsed.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
            }
            return xml.InnerXml;
        }



    }


}
