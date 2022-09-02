using System;
using System.Net;
using System.Text;

namespace BiometricIntegration
{
    public class APIServices
    {
        public static string Post(string url, string data, ref string err)
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.Encoding = Encoding.UTF8;
                    client.Headers.Add("Content-Type", "application/json");
                    var response = client.UploadString(url, data);

                    return response;
                }
            }
            catch (Exception e)
            {

                err = e.Message;
                return null;
            }

        }
    }
}
