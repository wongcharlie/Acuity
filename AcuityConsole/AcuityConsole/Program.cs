using System;
using System.IO;
using System.Net;
using System.Threading;
using FileHelpers;

namespace AcuityConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            //historical 

            var s = new Utils().DisplayFileFromFtpServer(new Uri("ftp://ftp.sec.gov/edgar/full-index/2013/QTR1/master.idx"));
            // var s = File.ReadAllText(@"C:\Users\charlie\Documents\GitHub\Acuity\TestData\master.idx");

            FileHelperEngine engine = new FileHelperEngine(typeof(Filing));

            var ret = engine.ReadString(s) as Filing[];

            //Parallel.ForEach(ret, filing =>
            //{

            foreach (var filing in ret)
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
                    filing.Trade = mgr.GetTrade(fullFilingText);
                    if (filing.Trade != null && filing.Trade.transactionCode == "P")
                    {

                        Console.WriteLine(DateTime.Now + " " + Thread.CurrentThread.ManagedThreadId + " " + filing.Trade.ToString());

                        new TradeController().FillMarketData(filing.Trade);
                    }

                }
            }
            //});


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




}
