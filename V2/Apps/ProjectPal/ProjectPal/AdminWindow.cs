using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Utils;
using System.IO;

namespace ProjectPal
{
    public partial class AdminWindow : Form
    {
        public AdminWindow()
        {
            InitializeComponent();

            if (DBAccess.DBObjectBase.HasUnsavedChanges)
            {
                buttonSuperUser.Enabled = false;
                buttonRestartAsDifferentUser.Enabled = false;

            }
            Functions.PopulateComboWithCurrentUsers(comboBoxUsers, true);

            string currentUser = (comboBoxUsers.Items.Count > 0) ? (string)(comboBoxUsers.Items[0]) : "";
            if (DBProjectPal.Person.CurrentUser != null)
                currentUser = DBProjectPal.Person.CurrentUser.Name;

            comboBoxUsers.Text = currentUser;

            checkBoxDisableEncription.Checked = !Utils.SecurityFunctions.EncryptionEnabled;


            if (Utils.DatabaseBase.DBType == Utils.DatabaseBase.DBTypeValues.SQLServer)
                buttonDBCopy.Text = "Copy To FileSystem";
            else
                buttonDBCopy.Text = "Copy To SqlServer";
        }

        private void buttonSuperUser_Click(object sender, EventArgs e)
        {
            buttonOK_Click(sender, e);

            string exeName = System.Reflection.Assembly.GetEntryAssembly().Location;

            string arguments = "-SuperUser ";
            Functions.AppendAdminPassword(ref arguments);

            System.Diagnostics.Process p = new System.Diagnostics.Process();

            System.Diagnostics.ProcessStartInfo ps = new System.Diagnostics.ProcessStartInfo(exeName, arguments);
            p.StartInfo = ps;
            p.Start();

            System.Environment.Exit(0);

            //System.Windows.Forms.Application.Exit();
        }



        private void comboBoxUsers_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBoxLevel.Text = DBProjectPal.Person.FindPerson(comboBoxUsers.Text).UserType.ToString();
        }

        private void buttonRestartAsDifferentUser_Click(object sender, EventArgs e)
        {
            string exeName = System.Reflection.Assembly.GetEntryAssembly().Location;

            string arguments = "-RunAs ";
            Functions.AppendAdminPassword(ref arguments);
            arguments += "," + DBProjectPal.Person.FindPerson(comboBoxUsers.Text).DBLogin.Trim();
            arguments += "," + textBoxLevel.Text;

            System.Diagnostics.Process p = new System.Diagnostics.Process();

            System.Diagnostics.ProcessStartInfo ps = new System.Diagnostics.ProcessStartInfo(exeName, arguments);
            p.StartInfo = ps;
            p.Start();

            System.Environment.Exit(0);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
        }

        private void buttonDBCopy_Click(object sender, EventArgs e)
        {
            DBProjectPal.Functions.LoadAllTypes();
            try
            {
                if (Utils.DatabaseBase.DBType == Utils.DatabaseBase.DBTypeValues.SQLServer)
                {
                    DBAccess.DBObjectBase.CopyToNewDB(Utils.DatabaseBase.DBTypeValues.FileSystem);
                    DBAccess.FileSystem_File.WriteVersion(ConfigSettings.Instance.AppVersionShort);
                }
                else
                    DBAccess.DBObjectBase.CopyToNewDB(Utils.DatabaseBase.DBTypeValues.SQLServer);
            }
            catch (Exception err)
            {
                Utils.Logger.LogException(err, "Error copying database");
            }
        }

        private void buttonModified_Click(object sender, EventArgs e)
        {
            DBAccess.DBObjectBase.MarkAll_NotModified_as_Modified();
        }

        private void checkBoxDisableEncription_CheckedChanged(object sender, EventArgs e)
        {
            Utils.SecurityFunctions.EncryptionEnabled = !checkBoxDisableEncription.Checked;
        }

        private void buttonLoadAttachments_Click(object sender, EventArgs e)
        {
            foreach (DBProjectPal.Attachment attachement in DBProjectPal.Attachment.AllInstances)
            {
                object dummy = attachement.Data;
            }
        }

        private void buttonExportAttachements_Click(object sender, EventArgs e)
        {
            string extractDirectory = @"C:\windows\temp\ProjectPal_AttachmentExtract_"+DateTime.Now.ToString("yyyyMMdd_HHmmss");

            if (Directory.Exists(extractDirectory))
            {
                try
                {
                    Directory.Delete(extractDirectory, true);
                }
                catch (Exception)
                {
                    foreach (string entry in Directory.GetFileSystemEntries(extractDirectory))
                    {
                        try
                        {
                            if (Directory.Exists(entry))
                                Directory.Delete(entry, true);
                            else
                                File.Delete(entry);
                        }
                        catch (Exception)
                        { }
                    }
                }
            }
            Directory.CreateDirectory(extractDirectory);

            foreach (DBProjectPal.Attachment attachement in DBProjectPal.Attachment.AllInstances)
            {
                object dummy = attachement.Data;
                string directory = "";

                if (attachement.Task != null)
                    directory = "Task_" + (int?)DBProjectPal.Attachment.GetDatabaseIdForWritingToDatabase(attachement.Task);
                if (attachement.Component != null)
                    directory = "Component_" + (int?)DBProjectPal.Attachment.GetDatabaseIdForWritingToDatabase(attachement.Component);
                if (attachement.Project != null)
                    directory = "Project_" + (int?)DBProjectPal.Attachment.GetDatabaseIdForWritingToDatabase(attachement.Project);

                if (directory == "")
                    continue;

                string directoryPath = Path.Combine(extractDirectory, directory);

                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                string filename = "";
                if (attachement.CreateTime.HasValue)
                    filename = attachement.CreateTime.Value.ToString("yyyyMMdd_HHmmss_");
                filename += FixPathChars(attachement.Name);

                if (attachement.DataType == "Mail")
                    filename += ".msg";

                string filePath = Path.Combine(directoryPath, filename);

                while(filePath.Length > 240)
                {
                    string withoutExtension = Path.GetFileNameWithoutExtension(filePath);
                    string extension = Path.GetExtension(filePath);
                    filePath = Path.Combine(directoryPath, withoutExtension.Substring(0, withoutExtension.Length - 1) + extension);
                }

                string newFilePath = filePath;
                int count = 1;
                while(File.Exists(newFilePath))
                {
                    newFilePath = Path.Combine(directoryPath, Path.GetFileNameWithoutExtension(filename) + "_" + count + Path.GetExtension(filename));
                    count++;
                }


                if (attachement.Data == null)
                    File.WriteAllBytes(newFilePath, new byte[] { });
                else
                    File.WriteAllBytes(newFilePath, attachement.Data);

            }
        }

        private static string FixPathChars(string path)
        {
            string result = "";

            HashSet<char> invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars());

            foreach (char c in path)
            {
                if (invalidChars.Contains(c))
                    result += " ";
                else
                    result += c;
            }
            return result;
        }
    }
}
