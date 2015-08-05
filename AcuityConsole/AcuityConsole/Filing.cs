using System;
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



    }
}
