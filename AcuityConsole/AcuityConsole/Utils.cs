using System;
using System.Globalization;
using System.Net;
using System.Text;
using System.Xml;

namespace AcuityConsole
{
    public class Utils
    {
        public double GetNumericValue(string numericString)
        {
            if (numericString.EndsWith("M")) return Convert.ToDouble(numericString.Replace("M", "")) * 1000000;
            if (numericString.EndsWith("B")) return Convert.ToDouble(numericString.Replace("B", "")) * 1000000000;
            if (numericString.Equals(string.Empty)) return 0;
            return Convert.ToDouble(numericString);

        }

        public string DisplayFileFromFtpServer(Uri serverUri)
        {
            // The serverUri parameter should start with the ftp:// scheme. 
            if (serverUri.Scheme != Uri.UriSchemeFtp)
            {
                return null;
            }
            // Get the object used to communicate with the server.
            WebClient request = new WebClient();

            // This example assumes the FTP site uses anonymous logon.
            request.Credentials = new NetworkCredential("anonymous", "anonymous@ftp.sec.gov");
            string fileString = null;
            try
            {
                byte[] newFileData = request.DownloadData(serverUri.ToString());
                fileString = Encoding.UTF8.GetString(newFileData);
                //Console.WriteLine(fileString);
            }
            catch (WebException e)
            {
                Console.WriteLine(e.ToString());
            }

            return fileString;//.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
        }

        public void DownloadFileFromFtpServer(Uri serverUri, string localFileName)
        {
            // The serverUri parameter should start with the ftp:// scheme. 
            if (serverUri.Scheme != Uri.UriSchemeFtp)
            {
                return;
            }
            // Get the object used to communicate with the server.
            WebClient request = new WebClient();

            // This example assumes the FTP site uses anonymous logon.
            request.Credentials = new NetworkCredential("anonymous", "anonymous@ftp.sec.gov");
            string fileString = null;
            try
            {
                request.DownloadFile(serverUri.ToString(), localFileName);

                //Console.WriteLine(fileString);
            }
            catch (WebException e)
            {
                Console.WriteLine(e.ToString());
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



    }

}
