using OpenQA.Selenium.Chrome;
using MaasOne;
using MaasOne.Base;

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.ServiceModel.Syndication;
using System.Xml;
using NLog;
using MaasOne.Finance.YahooPortfolio;


namespace AcuityConsole
{
    class Program
    {
        static void Main(string[] args)
        {

            var mgr = new WebManager(); mgr.StartSession();
            //            var mgr = new PortfolioManager(); mgr.LogIn();



            //   XmlReader reader = XmlReader.Create("https://www.sec.gov/Archives/edgar/xbrlrss.all.xml");
            //  SyndicationFeed feed = SyndicationFeed.Load(reader);

            //get all types starting with 4.
            var service = new MarketDataService();
            //            service.AnalyseByCompany("APPLE", 2010, 2015);
            //todays
            service.ReportOnTypeFours("https://www.sec.gov/cgi-bin/browse-edgar?action=getcurrent&type=4&company=&dateb=&owner=include&start=0&count=4000&output=atom");


            // get all historicl type 4's = https://www.sec.gov/cgi-bin/srch-edgar?text=form-type%3D4&start=1&count=4000&first=2012&last=2015&output=atom
            // limited to 4000 results, can limit with ticker and years.
            //https://www.sec.gov/cgi-bin/srch-edgar?text=COMPANY-NAME%3DAPPLE%20and%20%20FORM-TYPE%3D4&start=1&count=8000&first=2001&last=2015&output=atom



        }
    }
    class WebManager
    {

        public void StartSession()
        {
            //Run selenium
            ChromeDriver cd = new ChromeDriver(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "lib"));
            cd.Url = @"https://login.yahoo.com/config/login?.src=fpctx&.intl=uk&.lang=en-GB&.done=https://uk.yahoo.com/%3fp=us";
            cd.Navigate();
            OpenQA.Selenium.IWebElement e = cd.FindElementById("login-username");
            e.SendKeys("wong.charlie90");
            e = cd.FindElementById("login-passwd");
            e.SendKeys("nextlevel11FU");
            e = cd.FindElementById("login-signin");
            e.Click();

            CookieContainer cc = new CookieContainer();
            //Get the cookies
            foreach (OpenQA.Selenium.Cookie c in cd.Manage().Cookies.AllCookies)
            {
                string name = c.Name;
                string value = c.Value;
                cc.Add(new System.Net.Cookie(name, value, c.Path, c.Domain));
            }

            //Fire off the request
            HttpWebRequest hwr = (HttpWebRequest)HttpWebRequest.Create("https://uk.finance.yahoo.com/portfolio/pf_15/view/dv");
            hwr.CookieContainer = cc;
            hwr.Method = "POST";
            hwr.ContentType = "application/x-www-form-urlencoded";
            StreamWriter swr = new StreamWriter(hwr.GetRequestStream());
            swr.Write("feeds=35");
            swr.Close();

            WebResponse wr = hwr.GetResponse();
            string s = new System.IO.StreamReader(wr.GetResponseStream()).ReadToEnd();
        }
    }

    internal class MarketDataService
    {
        NLog.Logger logger = new LogFactory().GetLogger("Acuity");

        public void AnalyseByCompany(string companyName, int startYear, int endYear)
        {
            for (int i = startYear; i <= endYear; i++)
            {
                ReportOnTypeFours(string.Format("https://www.sec.gov/cgi-bin/srch-edgar?text=COMPANY-NAME%3D{0}%20and%20%20FORM-TYPE%3D4&start=1&count=8000&first={1}&last={1}&output=atom", companyName, i));
            }
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

        public void ReportOnTypeFours(string feedUri)
        {
            var cleanRssFeed = FixRssFeed(new WebClient().DownloadString(feedUri));
            TextReader tr = new StringReader(cleanRssFeed);
            XmlReader reader = XmlReader.Create(tr);

            SyndicationFeed feed = SyndicationFeed.Load(reader);

            if (feed == null) return;

            var messages = new System.Collections.Generic.List<string>();
            foreach (var item in feed.Items)
            {
                //form 4 only. 
                if (item.Categories[0].Label.Equals("form type", StringComparison.OrdinalIgnoreCase) &&
                    item.Categories[0].Name.Equals("4", StringComparison.OrdinalIgnoreCase))
                {
                    var docUri = item.Links[0].Uri.IsAbsoluteUri ? item.Links[0].Uri.AbsoluteUri.Replace("http", "https") : string.Format("https://www.sec.gov{0}", item.Links[0].Uri.OriginalString);
                    docUri = docUri.Replace("-index.htm", ".txt");

                    var res = new WebClient().DownloadString(docUri);
                    if (!res.Contains("<XML>")) { Console.WriteLine("Not modern xml format."); continue; }
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

                        var transactionDate = Convert.ToDateTime(filing.DocumentElement.SelectSingleNode("//transactionDate//value").InnerText);

                        var theMessage = string.Format("{5:P} {1} ({0}) - {2} buys {3} on market cap of {4} (trade date: {6:ddd dd MMM})",
                               filing.DocumentElement.SelectSingleNode("//issuerName") != null ? filing.DocumentElement.SelectSingleNode("//issuerName").InnerText : "",
                               filing.DocumentElement.SelectSingleNode("//issuerTradingSymbol") != null ? filing.DocumentElement.SelectSingleNode("//issuerTradingSymbol").InnerText.ToUpper() : "",
                               filing.DocumentElement.SelectSingleNode("//officerTitle") != null ? filing.DocumentElement.SelectSingleNode("//officerTitle").InnerText : "",
                               value,
                               marketCapString,
                               value / new ParsingUtils().GetNumericValue(marketCapString),
                               transactionDate);

                        if (!messages.Contains(theMessage))
                        {
                            messages.Add(theMessage); logger.Info(theMessage);
                        }
                    }
                }
            }
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

    class PortfolioManager
    {

        private YPortfolioManager mManager = new YPortfolioManager();
        public void LogIn()
        {
            System.Net.NetworkCredential cred = new System.Net.NetworkCredential();
            cred.UserName = "wong.charlie90";
            cred.Password = "nextlevel11FU";
            bool isLoggedIn = mManager.LogIn(cred);

            //

            Response<Portfolio> addResp = mManager.AddPortfolioItem(getPortfolio("InsiderBuys").ID, "GOOG");
            Portfolio pf = addResp.Result;
            //foreach (IID id in pf.IDs)
            //{
            //    if (id.ID == "GOOG")
            //    {
            //        Debug.WriteLine("found");
            //    }
            //}


        }

        PortfolioInfo getPortfolio(string name)
        {
            if (mManager.IsLoggedIn)
            {
                PortfolioInfoDownload dl = new PortfolioInfoDownload();
                dl.Settings.Account = mManager;
                Response<PortfolioInfoResult> resp = dl.Download();
                foreach (PortfolioInfo pfi in resp.Result.Items)
                {
                    if (pfi.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return pfi;

                    //string id = pfi.ID;
                    //string name = pfi.Name;
                }
            }
            return null;
        }

    }
    internal class ParsingUtils
    {
        public double GetNumericValue(string numericString)
        {
            if (numericString.EndsWith("M")) return Convert.ToDouble(numericString.Replace("M", "")) * 1000000;
            if (numericString.EndsWith("B")) return Convert.ToDouble(numericString.Replace("B", "")) * 1000000000;
            if (numericString.Equals(string.Empty)) return 0;
            return Convert.ToDouble(numericString);

        }
    }


}
