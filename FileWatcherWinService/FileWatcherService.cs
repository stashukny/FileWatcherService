using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Configuration;
using System.Data.SqlClient;
using log4net;

namespace FileWatcherWinService
{
    public partial class FileWatcherService : ServiceBase
    {        
        private static readonly ILog log = LogManager.GetLogger(typeof(FileWatcherService));
        private static List<NotifyInfo> nis;        
        
        public FileWatcherService()
        {
            log4net.Config.XmlConfigurator.Configure();
            InitializeComponent();
            if (Convert.ToBoolean(ConfigurationManager.AppSettings["IsDebug"]) == true)
            {
                System.Diagnostics.Debugger.Launch();
            }

        }

        protected override void OnStart(string[] args)
        {
            log.Info("Service Started");
            try
            {
                using (SqlConnection con = new SqlConnection(ConfigurationManager.AppSettings["ConnectionString"]))
                {
                    ProcessRows(con);
                }
            }
            catch (Exception ex)
            {                
                log.Error(ex);
            }
        }


        protected override void OnStop()
        {
            try
            {
                log.Info("Service Stopped");
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }


        static void ProcessRows(SqlConnection connection)
        {            
            string SP = null;

            try
            {
                using (connection)
                {
                    SqlCommand command = new SqlCommand("select * from FileWatcherConfig;", connection);
                    connection.Open();

                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.HasRows)
                    {
                        nis = new List<NotifyInfo>();
                        while (reader.Read())
                        {
                            if (reader["StoredProc"] != DBNull.Value)
                                SP = reader["StoredProc"].ToString();

                            nis.Add(new NotifyInfo(reader["Folder"].ToString(), reader["Notify"].ToString(), SP));
                        }
                    }
                    else
                    {
                        Console.WriteLine("No rows found.");
                    }

                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }        
    }
}
