using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Xml;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Cookie = OpenQA.Selenium.Cookie;

namespace AcuityConsole
{
    public class YahooController
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

        private XmlDocument getDataAsXmlDocument(string uri)
        {
            var res = new WebClient().DownloadString(uri);
            var filing = new XmlDocument();
            filing.LoadXml(res);

            return filing;
        }

        public string GetMarketCapitalisation(string ticker)
        {
            var filing = getDataAsXmlDocument(string.Format(@"https://query.yahooapis.com/v1/public/yql?q=select%20*%20from%20yahoo.finance.quote%20where%20symbol%3D%22{0}%22&diagnostics=true&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys", ticker.ToUpper()));
            if (filing.DocumentElement != null)
            {
                var selectSingleNode = filing.DocumentElement.SelectSingleNode("//MarketCapitalization");
                if (selectSingleNode != null)
                    return selectSingleNode.InnerText;
            }
            return string.Empty;
        }

        public string GetHistoricalPrices(string ticker, DateTime from, DateTime to)
        {
            return new WebClient().DownloadString(string.Format(@"https://query.yahooapis.com/v1/public/yql?q=select%20*%20from%20yahoo.finance.historicaldata%20where%20symbol%20%3D%20%22{0}%22%20and%20startDate%20%3D%20%22{1:yyyy-MM-dd}%22%20and%20endDate%20%3D%20%22{2:yyyy-MM-dd}%22&format=json&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys",
                ticker.ToUpper(), from, to));

        }
    }
}
