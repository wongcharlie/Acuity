using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace AcuityConsole
{
    class TradeController
    {

        private double getPrice(DateTime date, Trade trade)
        {

            var quote = trade.PriceRequest.query.results.quote.FirstOrDefault(x => DateTime.ParseExact(x.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None) <= date);

            if (quote == null) return default(double);
            //if match is not close enough, discard.
            if (Math.Abs(((TimeSpan)(DateTime.ParseExact(quote.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None) - date)).Days) > 4)
            {
                return default(double);
            }

            return Convert.ToDouble(quote.Adj_Close);
        }

        public void FillMarketData(Filing filing)
        {

            var trade = filing.Trade;

            trade.priceReportDate = getPrice(filing.DateFiled, trade);

            trade.priceChgNext10D = getPrice(trade.transactionDate.AddDays(10), trade) / trade.transactionPricePerShare - 1;
            trade.priceChgNext1M = getPrice(trade.transactionDate.AddMonths(1), trade) / trade.transactionPricePerShare - 1;
            trade.priceChgNext2M = getPrice(trade.transactionDate.AddMonths(2), trade) / trade.transactionPricePerShare - 1;
            trade.priceChgNext3M = getPrice(trade.transactionDate.AddMonths(3), trade) / trade.transactionPricePerShare - 1;

            trade.priceChgPrev10D = trade.transactionPricePerShare / getPrice(trade.transactionDate.AddDays(-10), trade) - 1;
            trade.priceChgPrev1M = trade.transactionPricePerShare / getPrice(trade.transactionDate.AddMonths(-1), trade) - 1;
            trade.priceChgPrev2M = trade.transactionPricePerShare / getPrice(trade.transactionDate.AddMonths(-2), trade) - 1;
            trade.priceChgPrev3M = trade.transactionPricePerShare / getPrice(trade.transactionDate.AddMonths(-3), trade) - 1;


        }

        public void Save(Filing filing)
        {

            StringBuilder sql = new StringBuilder();
            sql.Append("INSERT INTO [dbo].[Trades] \n");
            sql.Append("           ([TradeDate] \n");
            sql.Append("           ,[ReportDate] \n");
            sql.Append("           ,[UserId] \n");
            sql.Append("           ,[Ticker] \n");
            sql.Append("           ,[Quantity] \n");
            sql.Append("           ,[Price] \n");
            sql.Append("           ,[Price_Trade] \n");
            sql.Append("           ,[Price_Report] \n");
            sql.Append("           ,[priceChgPrev10D] \n");
            sql.Append("           ,[priceChgPrev1M] \n");
            sql.Append("           ,[priceChgPrev2M] \n");
            sql.Append("           ,[priceChgPrev3M] \n");
            sql.Append("           ,[priceChgNext10D] \n");
            sql.Append("           ,[priceChgNext1M] \n");
            sql.Append("           ,[priceChgNext2M] \n");
            sql.Append("           ,[priceChgNext3M]) \n");
            sql.Append("     VALUES \n");
            sql.Append("           (<TradeDate, date,> \n");
            sql.Append("           ,<ReportDate, date,> \n");
            sql.Append("           ,<UserId, varchar(50),> \n");
            sql.Append("           ,<Ticker, varchar(50),> \n");
            sql.Append("           ,<Quantity, bigint,> \n");
            sql.Append("           ,<Price, float,> \n");
            sql.Append("           ,<Price_Trade, float,> \n");
            sql.Append("           ,<Price_Report, float,> \n");
            sql.Append("           ,<priceChgPrev10D, float,> \n");
            sql.Append("           ,<priceChgPrev1M, float,> \n");
            sql.Append("           ,<priceChgPrev2M, float,> \n");
            sql.Append("           ,<priceChgPrev3M, float,> \n");
            sql.Append("           ,<priceChgNext10D, float,> \n");
            sql.Append("           ,<priceChgNext1M, float,> \n");
            sql.Append("           ,<priceChgNext2M, float,> \n");
            sql.Append("           ,<priceChgNext3M, float,>) \n");
            sql.Append("GO");

        }
    }
}
