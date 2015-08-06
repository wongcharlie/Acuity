
using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FileHelpers;

namespace AcuityConsole
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //download
            if (args[0].Equals("DOWNLOAD", StringComparison.OrdinalIgnoreCase))
            {
                Download();
                return;
            }



            //historical 
            if (args[0].Equals("parse", StringComparison.OrdinalIgnoreCase))
            {
                Parallel.ForEach(Directory.GetFiles(@"c:\temp\downloads\", "*.idx"), file =>
                {
                    //foreach (var file in Directory.GetFiles(@"c:\temp\downloads\", "2005*.idx"))
                    //{
                    var s = File.ReadAllText(file);
                    FileHelperEngine engine = new FileHelperEngine(typeof(Filing));
                    var filings = engine.ReadString(s) as Filing[];


                    Parallel.ForEach(filings.Where(x => x.FormType.StartsWith("4")), filing =>
                    {
                        //foreach (var filing in filings.wher)
                        //{
                        //    if (filing.FormType.StartsWith("4"))
                        //    {
                        //Task.Factory.StartNew(() =>
                        //{
                        var mgr = new EdgarService();
                        var tradeController = new TradeController();

                        Console.WriteLine($"{file} {filing.DateFiled} {filing.CIK}");
                        string fullFilingText;
                        try
                        {
                            fullFilingText = new WebClient().DownloadString($"http://www.sec.gov/Archives/{filing.FileName}");

                            if (fullFilingText == null) return;
                            filing.Trade = mgr.GetTrade(fullFilingText);
                            if (filing.Trade != null && filing.Trade.transactionCode == "P")
                            {
                                Console.WriteLine(DateTime.Now + " " + Thread.CurrentThread.ManagedThreadId + " " + filing.Trade);
                                tradeController.FillMarketData(filing);
                                new DbController().ExecuteNonQuery(filing.SqlInsert());
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);

                        }
                    });





                });

                //} //var s = new Utils().DisplayFileFromFtpServer(new Uri("ftp://ftp.sec.gov/edgar/full-index/2013/QTR2/master.idx"));
            }


            //var mgr = new WebManager(); mgr.StartSession();
            //            var mgr = new PortfolioManager(); mgr.LogIn();

            //   XmlReader reader = XmlReader.Create("https://www.sec.gov/Archives/edgar/xbrlrss.all.xml");
            //  SyndicationFeed feed = SyndicationFeed.Load(reader);

            //get all types starting with 4.

            //            service.AnalyseByCompany("APPLE", 2010, 2015);
            //todays
            if (args[0].Equals("today", StringComparison.OrdinalIgnoreCase))
            {
                new EdgarService().ReportOnTypeFours("https://www.sec.gov/cgi-bin/browse-edgar?action=getcurrent&type=4&company=&dateb=&owner=include&start=0&count=4000&output=atom");


                // get all historicl type 4's = https://www.sec.gov/cgi-bin/srch-edgar?text=form-type%3D4&start=1&count=4000&first=2012&last=2015&output=atom
                // limited to 4000 results, can limit with ticker and years.
                //https://www.sec.gov/cgi-bin/srch-edgar?text=COMPANY-NAME%3DAPPLE%20and%20%20FORM-TYPE%3D4&start=1&count=8000&first=2001&last=2015&output=atom

            }


        }

        private static void Download()
        {
            var tools = new Utils();
            for (int i = 1993; i < 2016; i++)
            {
                for (int j = 1; j < 5; j++)
                {
                    var source = $"ftp://ftp.sec.gov/edgar/full-index/{i}/QTR{j}/master.idx";
                    var target = $@"c:\temp\downloads\{i}_{j}_master.idx";

                    if (File.Exists(target)) continue;


                    Console.WriteLine($"source {source} target {target}");
                    tools.DownloadFileFromFtpServer(new Uri(source), target);

                }
            }

        }

    }
}

internal class DbController
{

    public void ExecuteNonQuery(string sql)
    {
        using (var sqlConnection = new SqlConnection("Data Source=devminky;Initial Catalog=bloombergstatic;Integrated Security=SSPI"))
        {
            using (var sqlCommand = new SqlCommand(sql, sqlConnection))
            {
                sqlConnection.Open();
                sqlCommand.ExecuteNonQuery();
            }
        }

    }
}



