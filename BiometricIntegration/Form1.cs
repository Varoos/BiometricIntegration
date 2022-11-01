using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace BiometricIntegration
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        public class RecordDataTableConverter : Newtonsoft.Json.Converters.DataTableConverter
        {
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                    return null;
                if (reader.TokenType == JsonToken.StartObject)
                {
                    var token = JToken.Load(reader);
                    token = new JArray(token.SelectTokens("data"));
                    using (var subReader = token.CreateReader())
                    {
                        while (subReader.TokenType == JsonToken.None)
                            subReader.Read();
                        return base.ReadJson(subReader, objectType, existingValue, serializer); // Use base class to convert
                    }
                }
                else
                {
                    return base.ReadJson(reader, objectType, existingValue, serializer);
                }
            }
        }


        DateTime starts= new DateTime();
        private void Form1_Load(object sender, EventArgs e)
        {
            this.Hide();
            string intvl = ConfigurationManager.AppSettings["Interval"];
            timer1.Interval = Convert.ToInt32(intvl) * 1000;
            timer1.Enabled = true;
            timer1.Tick += new EventHandler(timer1_Tick);
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            starts = DateTime.Now;
            StartSycingAsync();
        }
        public void StartSycingAsync()
        {
            DBServices db = new DBServices();
            string[] ip = new string[5];
            ip[0] = "192.168.11.101";
            ip[1] = "192.168.11.102";
            ip[2] = "192.168.11.103";
            ip[3] = "192.168.11.104";
            ip[4] = "192.168.11.105";
            try
            {
                foreach (string ipa in ip)
                {
                    db.SetLog("ip address = " + ipa);
                    HttpClient _client = new HttpClient();
                    _client.BaseAddress = new Uri("http://" + ipa + "/api/v1/face/");
                    db.SetLog("url = " + "http://" + ipa + "/api/v1/face/");
                    _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    string uritext = "getDeviceInfo";
                    db.SetLog("uritext = " + uritext);
                    HttpResponseMessage result= new HttpResponseMessage();
                    try
                    {
                         result = _client.GetAsync(uritext).Result;
                    } 
                    catch(AggregateException aex)
                    {
                        db.SetLog("AggregateException = " + aex.Message);
                        //db.SetErrLog(aex.Message);
                        continue;
                    }
                    db.SetLog("result = " + result.IsSuccessStatusCode.ToString());
                    if (result.IsSuccessStatusCode)
                    {
                        var value = result.Content.ReadAsStringAsync().Result;
                        db.SetLog("result content = " + value.ToString());
                        var settings = new JsonSerializerSettings
                        {
                            Converters = new[] { new RecordDataTableConverter() },
                        };

                        var table = JsonConvert.DeserializeObject<DataTable>(value, settings);
                        db.SetLog("table = " + table.ToString());
                        string deviceid = table.Rows[0]["chipid"].ToString();
                        db.SetLog("deviceid = " + deviceid);
                        int maxrecid = db.GetLatestRecordID(deviceid);
                        db.SetLog("maxrecid = " + maxrecid.ToString());
                        Hashtable header = new Hashtable
                                     {
                                         { "startId", maxrecid},
                                         { "reqCount", 10},
                                         { "needImg", false},
                                     };
                        string sContent = JsonConvert.SerializeObject(header);
                        db.SetLog("sContent = " + sContent);
                        string err = "";
                        string url = "http://" + ipa + "/api/v1/face/queryAttendRecord";
                        db.SetLog("url = " + url);
                        var response = APIServices.Post(url, sContent, ref err);
                        db.SetLog("response = " + response.ToString());
                        if (response != null)
                        {
                            var apidata = JsonConvert.DeserializeObject<APIResponse.ApiData>(response);
                            if (apidata.data != null)
                            {
                                db.SetLog("apidata body count = " + apidata.data.Count.ToString());
                                if (apidata.data.Count > 0)
                                {
                                    db.SetLog("Record Count = " + apidata.recordCount.ToString());
                                    if (apidata.data.Count > 0)
                                    {
                                        List<Log> listLog = new List<Log>();
                                        for (int i = 0; i < apidata.data.Count; i++)
                                        {
                                            if (Regex.IsMatch(apidata.data[i]["userid"].ToString(), @"^\d+$") || apidata.data[i]["userid"].ToString() == "")
                                            {
                                                Log l = new Log();
                                                l.deviceid = apidata.data[i]["deviceid"].ToString();
                                                db.SetLog("deviceid = " + l.deviceid);
                                                l.userid = Convert.ToInt32(Convert.ToDecimal(apidata.data[i]["userid"].ToString() == "" ? "0" : apidata.data[i]["userid"].ToString()));
                                                db.SetLog("userid = " + l.userid);
                                                l.personId = apidata.data[i]["bodyTemperature"].ToString();
                                                db.SetLog("personId = " + l.personId);
                                                l.timestamp = apidata.data[i]["timestamp"].ToString();
                                                db.SetLog("timestamp = " + l.timestamp);
                                                l.id = apidata.data[i]["id"].ToString();
                                                db.SetLog("id = " + l.id);
                                                listLog.Add(l);
                                            }
                                            else
                                            {
                                                db.SetLog("Given Userid = " + apidata.data[i]["userid"].ToString());
                                            }
                                           
                                        }
                                        foreach (Log dr in listLog)
                                        {
                                            db.SaveLog(dr.deviceid, dr.userid, dr.personId, Convert.ToDateTime(dr.timestamp), dr.id);
                                            db.SetLog("Saved Success");
                                        }
                                    }
                                    
                                }
                            }

                        }
                        else
                        {
                            db.SetLog(response);
                        }
                    }


                }

            }
            
            catch (Exception ex)
            {
                db.SetLog(ex.Message);
            }
        }
        public class PostingData
        {
            public PostingData()
            {
                data = new List<Hashtable>();
            }
            public List<Hashtable> data { get; set; }
        }
        public class APIResponse
        {
            public class ApiData
            {
                public List<Hashtable> data { get; set; }
                public string command { get; set; }
                public string detail { get; set; }
                public string recordCount { get; set; }
                public string status { get; set; }
                public string transmit_cast { get; set; }

            }

            public class Response
            {
                public string url { get; set; }
                public List<ApiData> body { get; set; }
                public int result { get; set; }
                public string message { get; set; }
            }
            public class PostResponse
            {
                public string url { get; set; }
                public List<Hashtable> data { get; set; }
                public int result { get; set; }
                public string message { get; set; }
            }
        }
    }
}
