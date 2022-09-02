using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Net.Http;
using System.Net.Http.Headers;
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
                    HttpClient _client = new HttpClient();
                    _client.BaseAddress = new Uri("http://" + ipa + "/api/v1/face/");
                    _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    string uritext = "getDeviceInfo";
                    HttpResponseMessage result= new HttpResponseMessage();
                    try
                    {
                         result = _client.GetAsync(uritext).Result;
                    } 
                    catch(AggregateException aex)
                    {
                        db.SetErrLog(aex.Message);
                        continue;
                    }
                    if (result.IsSuccessStatusCode)
                    {
                        var value = result.Content.ReadAsStringAsync().Result;
                        var settings = new JsonSerializerSettings
                        {
                            Converters = new[] { new RecordDataTableConverter() },
                        };

                        var table = JsonConvert.DeserializeObject<DataTable>(value, settings);
                        string deviceid = table.Rows[0]["chipid"].ToString();
                        int maxrecid = db.GetLatestRecordID(deviceid);
                        Hashtable header = new Hashtable
                                     {
                                         { "startId", maxrecid},
                                         { "reqCount", 10},
                                         { "needImg", false},
                                     };
                        string sContent = JsonConvert.SerializeObject(header);
                        string err = "";
                        string url = "http://" + ipa + "/api/v1/face/queryAttendRecord";
                        var response = APIServices.Post(url, sContent, ref err);
                        if (response != null)
                        {
                            db.SetLog("Success");
                            var data = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response);
                            if (data.data != null)
                            {
                                if (data.data.Count > 0)
                                {
                                    List<Log> listLog = new List<Log>();
                                    for (int i = 0; i < data.data.Count; i++)
                                    {
                                        Log l = new Log();
                                        l.deviceid = data.data[i]["deviceid"].ToString();
                                        l.userid = Convert.ToInt32(Convert.ToDecimal(data.data[i]["userid"].ToString() == "" ? "0" : data.data[i]["userid"].ToString()));
                                        l.personId = data.data[i]["bodyTemperature"].ToString();
                                        l.timestamp = data.data[i]["timestamp"].ToString();
                                        l.id = data.data[i]["id"].ToString();
                                        listLog.Add(l);
                                    }
                                    foreach (Log dr in listLog)
                                    {
                                        db.SaveLog(dr.deviceid, dr.userid, dr.personId, Convert.ToDateTime(dr.timestamp), dr.id);
                                        db.SetLog("Saved Success");
                                    }
                                }
                            }

                        }
                        else
                        {
                            db.SetErrLog(response);
                        }
                    }


                }

            }
            
            catch (Exception ex)
            {
                db.SetErrLog(ex.Message);
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
            public class Data
            {
                public List<Hashtable> Body { get; set; }
                public Hashtable Header { get; set; }
                public List<Hashtable> Footer { get; set; }
            }

            public class Response
            {
                public string url { get; set; }
                public List<Data> data { get; set; }
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
