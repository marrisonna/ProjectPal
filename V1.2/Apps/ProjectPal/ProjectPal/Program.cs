using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.Reflection;

using Utils;


namespace ProjectPal
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {

            //Utils.DatabaseBase.Check();

            //double a = -0.1;
            //double b = -0.9;

            //int aa = (int)a;
            //int bb = (int)b;
          
            try
            {
                //Utils.DatabaseCE theDatabaseCe = new Utils.DatabaseCE("ProjectPal");

                //Utils.DatabaseCE.Test();


                //Utils.Database theDatabase = new Utils.Database("ProjectPal");

                //byte[] dataBytes;
                //using (System.Data.SqlClient.SqlDataReader reader = theDatabase.ExecuteReader("select EncryptByPassPhrase('NAMNAM','ABC') as Data"))
                //{

                //    while (reader.Read())
                //    {
                //        object data = Utils.Database.GetColumnValueAsObject(reader, 0);
                //        dataBytes = (byte[])data;
                //    }

                //}

                //System.Data.SqlClient.SqlDataReader reader2 = theDatabase.ExecuteReader("select convert(varchar,DECRYPTBYPASSPHRASE('NAMNAM',0x010000005AEFF66986C64D2FF24C3C9978C580D83D7D02D1E6FAADBB))");
                //while (reader2.Read())
                //{
                //    string v = Utils.Database.GetColumnValueAsString(reader2, 0);
                //}
                //reader2.Close();



                if (args.Contains("-FileSystem"))
                    Utils.DatabaseBase.DBType = Utils.DatabaseBase.DBTypeValues.FileSystem;

                if (args.Contains("-SqlServer"))
                    Utils.DatabaseBase.DBType = Utils.DatabaseBase.DBTypeValues.SQLServer;


                Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);

                
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

                if (System.Windows.Application.Current != null)
                    System.Windows.Application.Current.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(Dispatcher_UnhandledException);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Utils.Logger.Log("Starting " + "V" + ConfigSettings.Instance.AppVersionFull);

                if (ConfigSettings.Instance.AppVersionShort != ConfigSettings.Instance.DBReleaseVersion)
                {
                    Utils.Logger.Log("Version is not correct, it should be " + ConfigSettings.Instance.DBReleaseVersion);
                    MessageBox.Show(
                        "ProjectPal cannot start." + Environment.NewLine + Environment.NewLine +
                        "You are using version '" + ConfigSettings.Instance.AppVersionShort +
                        "' of ProjectPal." + Environment.NewLine +
                        "You need to use version '" + ConfigSettings.Instance.DBReleaseVersion + "'.",
                        "Incorrect version of ProjectPal", MessageBoxButtons.OK);
                    return;
                }
                


                Utils.Logger.Log("Options");
                Utils.Logger.Log("  -refreshAttachments");
                Utils.Logger.Log("  -restart");
                Utils.Logger.Log("  -SuperUser {password}");
                Utils.Logger.Log("  -RunAs {password},{DBUserName},{UserType}");

                bool onlyAllowOnceInstance = true;

                if (args.Length > 0)
                {
                    if (args.Contains("-refreshAttachments"))
                    {
                        if (Permissions.ThisUserType == Permissions.UserLevel.SuperUser)
                        {
                            Utils.Logger.Log("Refreshing attachments");
                            DBProjectPal.Attachment.ReSaveAll();
                            Utils.Logger.Log("Refreshing attachments complete.");
                        }
                        else
                        {
                            Utils.Logger.Log("Only SuperUser can run '-refreshAttachments'");
                        }
                    }


                    for (int i = 0; i < args.Length; i++)
                    {
                        string arg = args[i];

                        if (arg == "-SuperUser" && i < args.Length - 1 && Functions.AdminPassword(args[i + 1]))
                        {
                            Utils.Logger.Log("Starting as SuperUser");
                            DBAccess.TestingOverrides.SetSuperUser();
                            onlyAllowOnceInstance = false;

                        }


                        // -RunAs namnam,Ron,ReadOnlyUser
                        if (arg == "-RunAs" && i < args.Length - 1)
                        {
                            string[] runAsParams = args[i + 1].Split(',');
                            if (runAsParams.Length == 3 && Functions.AdminPassword(runAsParams[0]))
                            {
                                DBAccess.TestingOverrides.TestUserLevel newLevel;
                                if (Enum.TryParse<DBAccess.TestingOverrides.TestUserLevel>(runAsParams[2], out newLevel))
                                {

                                    Utils.Logger.Log("Starting as Different user '" + runAsParams[1] +
                                                     "' (" + newLevel.ToString() + ")");
                                    DBAccess.TestingOverrides.SetUser(runAsParams[1]);
                                    DBAccess.TestingOverrides.SetUserLevel(newLevel);
                                    onlyAllowOnceInstance = false;
                                }
                            }
                        }

                        if (arg == "-restart")
                        {
                            onlyAllowOnceInstance = false;
                        }
                    }
                }

                Utils.Permissions.DBUserName = DBAccess.DBObjectBase.DBUser;
                Utils.Permissions.UserLevel? currentUserType = DBAccess.DBObjectBase.ThisUserType;
                if (!currentUserType.HasValue)
                {

                    DBProjectPal.Person currentUser = DBProjectPal.Person.FindPersonFromDBLogin(DBAccess.DBObjectBase.DBUser);
                    if (currentUser != null)
                        currentUserType = currentUser.UserType;
                }
                if (!currentUserType.HasValue)
                    currentUserType = Utils.Permissions.UserLevel.ReadOnlyUser;
                Utils.Permissions.SetUserType(currentUserType.Value);


#if DEBUG
                onlyAllowOnceInstance = false;
#endif

                if (onlyAllowOnceInstance)
                {
                    int thisPid = Process.GetCurrentProcess().Id;
                    string thisProcessName = Process.GetCurrentProcess().ProcessName.Replace(".vshost", "");
                    Process[] processes = Process.GetProcesses();
                    bool alreadyRunning = false;
                    foreach (Process currentProcess in processes)
                    {
                        string currentProcessName = currentProcess.ProcessName.Replace(".vshost", "");
                        if (thisProcessName != currentProcessName)
                            continue;
                        if (currentProcess.Id != thisPid)
                            alreadyRunning = true;
                    }
                    if (alreadyRunning)
                    {
                        System.Windows.Forms.MessageBox.Show("'" + thisProcessName + "' is already running. Only one instance is allowed to be running.", "Already running!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return;
                    }
                }

                ApplyConfigSettings();

                DBAccess.DBObjectBase.SuperUserMayViewPrivateItems = Permissions.IsSuperUser  && ConfigSettings.Instance.ViewPrivateItems;

                DBProjectPal.Functions.LoadAllTypes();
                
                //IEnumerable<DBProjectPal.Task> tasks = DBProjectPal.Task.AllInstances;
                //IEnumerable<DBProjectPal.Attachment> attachments = DBProjectPal.Attachment.AllInstances;

                Application.Run(new MainWindow());
            }
            catch (Exception error)
            {
                Utils.Logger.LogException(error, "Exception in Main");
            }
        }

        static public void ApplyConfigSettings()
        {
            DBProjectPal.Project.HideCompletedProjects = ConfigSettings.Instance.HideCompletedProjects;
        }


        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Utils.Logger.Log("Showing ThreadException details");
            try
            {
                if (Permissions.IsSuperUser)
                {
                    MessageBox.Show("There has been a ThreadException, see log for details", "Thread Exception", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception e2)
            {
                Utils.Logger.LogException(e2, "Trying to show Exception dialog");
            }

            Utils.Logger.LogException(e.Exception as Exception, "ThreadException");
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Utils.Logger.Log("Showing UnhandledException details");
            try
            {
                if (Permissions.IsSuperUser)
                {
                    MessageBox.Show("There has been an UnhandledException, see log for details", "Unhandled Exception", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception e2)
            {
                Utils.Logger.LogException(e2, "Trying to show Exception dialog");
            }

            Utils.Logger.LogException(e.ExceptionObject as Exception, "UnhandledException");

        }

        static void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Utils.Logger.Log("Showing DispatcherUnhandledException details");
            try
            {
                if (Permissions.IsSuperUser)
                {
                    MessageBox.Show("There has been a DispatcherUnhandledException, see log for details", "Dispatcher Unhandled Exception", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception e2)
            {
                Utils.Logger.LogException(e2, "Trying to show Exception dialog");
            }

            Utils.Logger.LogException(e.Exception as Exception, "DispatcherUnhandledException");
        }



    }
}
