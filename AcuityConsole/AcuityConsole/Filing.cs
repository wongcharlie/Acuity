using System;
using System.Text;
using FileHelpers;

namespace AcuityConsole
{
    [DelimitedRecord("|"), IgnoreFirst(10), IgnoreEmptyLines()]
    public class Filing
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

        [FieldIgnored]
        public Trade Trade;

        public string SqlInsert()
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
            sql.Append($"           ('{Trade.transactionDate:ddMMMyyyy}' \n");
            sql.Append($"           ,'{DateFiled:ddMMMyyyy}' \n");
            sql.Append($"           ,'{Trade.officerTitle}' \n");
            sql.Append($"           ,'{Trade.issuerTradingSymbol}' \n");
            sql.Append($"           ,{Trade.transactionShares} \n");
            sql.Append($"           ,{Trade.transactionPricePerShare} \n");
            sql.Append($"           ,{Trade.transactionPricePerShare} \n");
            sql.Append($"           ,<Price_Report, float,> \n");
            sql.Append($"           ,<priceChgPrev10D, float,> \n");
            sql.Append($"           ,<priceChgPrev1M, float,> \n");
            sql.Append($"           ,<priceChgPrev2M, float,> \n");
            sql.Append($"           ,<priceChgPrev3M, float,> \n");
            sql.Append($"           ,<priceChgNext10D, float,> \n");
            sql.Append($"           ,<priceChgNext1M, float,> \n");
            sql.Append($"           ,<priceChgNext2M, float,> \n");
            sql.Append($"           ,<priceChgNext3M, float,>) \n");

            return sql.ToString();
        }

    }
}
