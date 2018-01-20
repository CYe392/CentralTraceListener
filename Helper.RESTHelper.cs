using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace Fisher.Utilities.CentralTraceListener.Helper
{
    internal class RESTHelper
    {
        public string ServiceUrl { get; set; }

        public RESTHelper() { }
        public RESTHelper(string serviceUrl) { ServiceUrl = serviceUrl; }

        public string Post(object message)
        {
            if (message == null || !ValidateUrl(ServiceUrl))
                return null;
            try
            {
                string jsonPayload = JsonConvert.SerializeObject(message);
                byte[] payload = Encoding.UTF8.GetBytes(jsonPayload);

                var req = (HttpWebRequest)WebRequest.Create(ServiceUrl);
                req.PreAuthenticate = true;
                req.UseDefaultCredentials = true;
                req.Method = "POST";
                req.ContentLength = payload.Length;
                req.ContentType = @"application/json";

                req.GetRequestStream().Write(payload, 0, payload.Length);
                string respMsg = "";
                using (var res = req.GetResponse())
                {
                    var reader = new StreamReader(res.GetResponseStream(), Encoding.GetEncoding("utf-8"));
                    respMsg = reader.ReadToEnd();
                    return respMsg;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = this.GetType().ToString() + ": error during append - " + ex.Message;
                HandleError(errorMessage, ex);
                return errorMessage;
            }
        }

        private void HandleError(string errorMessage, Exception ex)
        {
            Console.WriteLine(errorMessage);
            Console.WriteLine(ex.ToString());
        }

        public static bool ValidateUrl(string serviceUrl)
        {
            if (string.IsNullOrWhiteSpace(serviceUrl))
                return false;
            Uri uriResult;
            return Uri.TryCreate(serviceUrl, UriKind.Absolute, out uriResult) &&
                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}
