using System;
using System.Text;
using log4net;
using System.IO;
using System.Configuration;
using System.Net.Mail;
using System.Net;
using System.Data.SqlClient;


namespace FileWatcherWinService
{
    public class NotifyInfo
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(NotifyInfo));
        public string folder { get; set; }
        public string storedProc { get; set; }
        public string notify { get; set; }
        public FileSystemWatcher watcher { get; set; }

        DateTime lastRead = DateTime.MinValue;

        public NotifyInfo(string fld, string ntfy, string SP)
        {
            folder = fld;
            storedProc = SP;
            notify = ntfy;

            Watch(folder);
        }

        public void Watch(string folder)
        {
            watcher = new FileSystemWatcher();
            watcher.Path = folder;
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName;
            watcher.Filter = "*.*";

            watcher.IncludeSubdirectories = true;
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.EnableRaisingEvents = true;

        }
        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            DateTime lastWriteTime = File.GetLastWriteTime(e.FullPath);
            if (lastWriteTime != lastRead)
            {
                if (storedProc != null)
                {
                    ExecuteCommand(storedProc);
                }
                SendEmail(notify, e.Name, folder);
                lastRead = lastWriteTime;
            }
        }
        private void SendEmail(string notify, string file, string folder)
        {
            string fromEmail = ConfigurationManager.AppSettings["FromEmail"];
            string SMTPServer = ConfigurationManager.AppSettings["SMTPServer"];
            string SMTPPassword = ConfigurationManager.AppSettings["SMTPPassword"];


            SmtpClient client = new SmtpClient();
            client.Credentials = new NetworkCredential(fromEmail, SMTPPassword);
            client.Port = 587;
            client.Host = SMTPServer;
            client.EnableSsl = true;

            try
            {
                MailAddress
                    maFrom = new MailAddress(fromEmail, fromEmail, Encoding.UTF8),
                    maTo = new MailAddress(notify, notify, Encoding.UTF8);
                MailMessage mmsg = new MailMessage(maFrom.Address, maTo.Address);
                mmsg.Body = "<html><body>File: " + file + " created in " + folder + "</body></html>";
                mmsg.BodyEncoding = Encoding.UTF8;
                mmsg.IsBodyHtml = true;
                mmsg.Subject = file + " in " + folder;
                mmsg.SubjectEncoding = Encoding.UTF8;

                client.Send(mmsg);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private void ExecuteCommand(string SP)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(ConfigurationManager.AppSettings["ExecScriptConnectionString"]))
                {
                    SqlCommand command = new SqlCommand(SP, connection);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
    }
}
