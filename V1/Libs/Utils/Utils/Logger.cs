using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utils
{
    public class Logger
    {
        public const string LogPath0 = @"\\uk\Data\prod\ABS_IT_Shared\Neil\PPLogs\";
        public const string LogPath1 = @"\\lonwd013774\NeilsShareWrite\logs\";
        public const string LogPath2 = @"\\uk\data\prod\ABS_IT_Shared\logs\";

        private Logger()
        {

            List<string> paths = new List<string>();
            DateTime a = Utils.Misc.BuildDateTime;
            if (!(System.Environment.MachineName.ToUpper() == "HANSOLO" ||
               System.Environment.MachineName.ToUpper() == "JARJARBINKS" ||
               System.Environment.MachineName.ToUpper() == "CHIRPA" ||
               System.Environment.MachineName.ToUpper() == "PALPATINE" ||
               System.Environment.MachineName.ToUpper() == "JABBA"))
            {
                //paths.Add(LogPath0);
                //paths.Add(LogPath1);
                //paths.Add(LogPath2);
            }
            paths.Add(@"C:\tmp\logs\");

            //int d = (DateTime.Now - a).Days;
            string userName = Environment.UserName;
            List<Exception> errors = new List<Exception>();
            //double pCount = 0;

            foreach (string path in paths)
            {
                try
                {
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    string checkFile = path + userName + ".check";
                    FileStream tmp = File.OpenWrite(checkFile);
                    tmp.Close();
                    File.Delete(checkFile);
                    m_logFileName = path;
                    break;
                }

                catch (Exception exp)
                {
                    errors.Add(new Exception("Failed to for log dir '" + path + "'", exp));
                }
            }

            //if (d > 60)
            //{
            //    pCount = d;
            //    string c = System.Environment.GetEnvironmentVariable("D");
            //    int da;
            //    if (d < 180 && int.TryParse(c, out da) && d < da)
            //    {
            //        pCount = 0;

            //    }
            //}

            if (m_logFileName == null)
            {
                System.Windows.Forms.MessageBox.Show("Could not create log", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);

                m_loggingOn = false;
            }


            //if (pCount != 0)
            //    System.Environment.Exit(0);

            if (m_loggingOn)
            {
                string logPath = m_logFileName + "ProjectPal_" + userName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log";
                m_logFile = File.AppendText(logPath);

                s_instance = this;

                foreach (Exception e in errors)
                    LogException(e, "Error creating log file");

                //Log("Paths");
                //foreach (string path in paths)
                //    Log(path);
            }
        }

        ~Logger()
        {
            // m_logFile.Close();
        }

        private static Logger s_instance = null;
        private static Logger Instance
        {
            get
            {
                if (s_instance == null)
                    new Logger();
                return s_instance;
            }
        }

        private void WriteLog(string message)
        {
            if (m_loggingOn)
            {
                m_logFile.WriteLine(message);
                m_logFile.Flush();
            }

        }

        private void WriteLogException(Exception exp, string message)
        {
            if (m_loggingOn)
            {
                m_logFile.WriteLine("---- Exception ----");
                m_logFile.Write(message);
                m_logFile.WriteLine("---- Exception Details----");
                if (exp != null)
                {
                    m_logFile.WriteLine(exp.Message);
                    m_logFile.WriteLine(exp.Source);
                    m_logFile.WriteLine(exp.StackTrace);
                    if (exp.InnerException != null)
                        WriteLogException(exp.InnerException, "Inner exception");
                }
                else
                {
                    m_logFile.WriteLine("Exception Details are unknown!");
                }

                m_logFile.WriteLine("---- Exception End ----");
                m_logFile.Flush();
            }

        }

        string m_logFileName = null;
        TextWriter m_logFile;
        bool m_loggingOn = true;


        public static void Log(string message)
        {
            Instance.WriteLog(message);
        }

        public static void LogException(Exception exp, string message)
        {
            Instance.WriteLogException(exp, message);
        }

        public static string LogPath { get { return s_instance.m_logFileName; } }

    }
}
