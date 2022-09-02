using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;

namespace BiometricIntegration
{
    public class DBServices
    {
        static string ESerName = ConfigurationManager.AppSettings["ExternalServerName"];
        static string EDBName = ConfigurationManager.AppSettings["ExternalDBName"];
        static string EUID = ConfigurationManager.AppSettings["ExternalUserName"];
        static string EPWD = ConfigurationManager.AppSettings["ExternalPassword"];
        static string connection = $"data source={ESerName};initial catalog={EDBName};User ID={EUID};Password={EPWD};integrated security=True;MultipleActiveResultSets=True";
        //static string connection = $"data source={ESerName};initial catalog={EDBName};integrated security=True;MultipleActiveResultSets=True";
        SqlConnection con = new SqlConnection(connection);
        public async Task SetLog(string content)
        {
            try
            {
                string AppLocation = "";
                AppLocation = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData); ;
                string folderName = AppLocation + "\\Biometric_Integration_LogFiles";
                if (!Directory.Exists(folderName))
                {
                    Directory.CreateDirectory(folderName);
                }
                string sFilePath = folderName + "\\Biometric_Integration_Log-" + DateTime.Now.ToString("dd-MM-yyyy") + ".txt";
                using (StreamWriter outputFile = new StreamWriter(sFilePath,true))
                {
                    await outputFile.WriteLineAsync(DateTime.Now.ToString() + " " + content + Environment.NewLine);
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            
        }
        public async Task SetErrLog(string content)
        {
            try
            {
                string AppLocation = "";
                AppLocation = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData); ;
                string folderName = AppLocation + "\\Biometric_Integration_ErrFiles";
                if (!Directory.Exists(folderName))
                {
                    Directory.CreateDirectory(folderName);
                }
                string sFilePath = folderName + "\\Biometric_Integration_Error-" + DateTime.Now.ToString("dd-MM-yyyy") + ".txt";
                using (StreamWriter outputFile = new StreamWriter(sFilePath,true))
                {
                    await outputFile.WriteLineAsync(DateTime.Now.ToString() + " " + content + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public DataSet GetData(string Query)
        {
            con.Open();
            SqlCommand cmd = new SqlCommand(Query, con);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds);
            DataSet dst = ds;
            con.Close();
            return dst;
        }

        public int Update(string Vouc)
        {
            int result = 0;
            using (SqlConnection connect = new SqlConnection(connection))
            {
                string sql = $"{Vouc}";
                using (SqlCommand command = new SqlCommand(sql, connect))
                {
                    connect.Open();
                    result = command.ExecuteNonQuery();
                    connect.Close();
                }
            }
            return result;
        }
        public int GetLatestRecordID(string deviceid)
        {
            string sql = "";
            sql = $@"
                exec pCore_CommonSp @Operation = getLatestRecordID,@p1='{deviceid}'";
            DataSet dst = GetData(sql);
            return Convert.ToInt32(dst.Tables[0].Rows[0][0]);
        }

        public void SaveLog(string deviceid,int userid, string personid, DateTime logtime,string Recordid)
        {
            string sql = "";
            try
            {
                sql = $@"
                exec pCore_CommonSp @Operation = SaveLog,@p1='{deviceid}',@p2={userid},@p3='{Recordid}',@p4='{logtime}',@P7='{personid}'";
                int a = Update(sql);
            }
            catch (Exception ex)
            {
                SetErrLog(ex.Message);
            }
        }
    }
}
