using System;
using System.Collections.Generic;

namespace AcuityConsole
{

    /// <summary>
    /// generated from  http://jsonutils.com/ using
    /// https://query.yahooapis.com/v1/public/yql?q=select%20*%20from%20yahoo.finance.historicaldata%20where%20symbol%20%3D%20%22CLB%22%20and%20startDate%20%3D%20%222015-05-24%22%20and%20endDate%20%3D%20%222015-09-24%22&format=json&env=store%3A%2F%2Fdatatables.org%2Falltableswithkeys&callback=
    /// </summary>
    public class Quote
    {
        public string Symbol { get; set; }
        public string Date { get; set; }
        public string Open { get; set; }
        public string High { get; set; }
        public string Low { get; set; }
        public string Close { get; set; }
        public string Volume { get; set; }
        public string Adj_Close { get; set; }
    }

    public class Results
    {
        public IList<Quote> quote { get; set; }
    }

    public class Query
    {
        public int count { get; set; }
        public DateTime created { get; set; }
        public string lang { get; set; }
        public Results results { get; set; }
    }

    public class YahooHistoricalPriceRequest
    {
        public Query query { get; set; }
    }
}
