using System;

namespace AcuityConsole
{

    public class Trade
    {
        public double transactionShares;
        public double transactionPricePerShare;
        public string issuerTradingSymbol;
        public DateTime transactionDate;
        public string issuerName;
        public string marketCapString;
        public string officerTitle;
        public string transactionCode;

        public YahooHistoricalPriceRequest PriceRequest;

        public double priceChgPrev10D;
        public double priceChgPrev1M;
        public double priceChgPrev2M;
        public double priceChgPrev3M;

        public double priceChgNext10D;
        public double priceChgNext1M;
        public double priceChgNext2M;
        public double priceChgNext3M;


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
}
