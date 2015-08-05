using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcuityConsole
{
    class TradeController
    {

        private double getPrice(DateTime date, Trade trade)
        {

            var quote = trade.PriceRequest.query.results.quote.FirstOrDefault(x => DateTime.ParseExact(x.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None) <= date);

            if (quote ==null) return default(double);
            //if match is not close enough, discard.
            if (Math.Abs(((TimeSpan)(DateTime.ParseExact(quote.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None) - date)).Days) > 4)
            {
                return default(double);
            }

            return Convert.ToDouble(quote.Adj_Close);
        }

        public void FillMarketData(Trade trade)
        {
            trade.priceChgNext10D = getPrice(trade.transactionDate.AddDays(10), trade) / trade.transactionPricePerShare - 1;
            trade.priceChgNext1M = getPrice(trade.transactionDate.AddMonths(1), trade) / trade.transactionPricePerShare - 1;
            trade.priceChgNext2M = getPrice(trade.transactionDate.AddMonths(2), trade) / trade.transactionPricePerShare - 1;
            trade.priceChgNext3M = getPrice(trade.transactionDate.AddMonths(3), trade) / trade.transactionPricePerShare - 1;

            trade.priceChgPrev10D = trade.transactionPricePerShare / getPrice(trade.transactionDate.AddDays(-10), trade) - 1;
            trade.priceChgPrev1M = trade.transactionPricePerShare / getPrice(trade.transactionDate.AddMonths(-1), trade) - 1;
            trade.priceChgPrev2M = trade.transactionPricePerShare / getPrice(trade.transactionDate.AddMonths(-2), trade) - 1;
            trade.priceChgPrev3M = trade.transactionPricePerShare / getPrice(trade.transactionDate.AddMonths(-3), trade) - 1;

        }

        public void Save()
        {
        }
    }
}
