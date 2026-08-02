using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ProjectPal
{
    public partial class ConfigWindow : Form
    {
        public ConfigWindow()
        {
            InitializeComponent();

            checkBoxHideProjects.Checked = ConfigSettings.Instance.HideCompletedProjects;

            if (DBAccess.DBObjectBase.HasUnsavedChanges)
            {
                buttonRestart.Enabled = false;
            }

            checkBoxViewPrivate.Checked = ConfigSettings.Instance.ViewPrivateItems;
            if (!Utils.Permissions.IsSuperUser)
                checkBoxViewPrivate.Visible = false;

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            ConfigSettings.Instance.HideCompletedProjects = checkBoxHideProjects.Checked;

            if (Utils.Permissions.IsSuperUser)
                ConfigSettings.Instance.ViewPrivateItems = checkBoxViewPrivate.Checked;
        }

        private void buttonRestart_Click(object sender, EventArgs e)
        {
            buttonOK_Click(sender, e);

            string exeName = System.Reflection.Assembly.GetEntryAssembly().Location;

            string arguments = "-restart";
            
            System.Diagnostics.Process p = new System.Diagnostics.Process();

            System.Diagnostics.ProcessStartInfo ps = new System.Diagnostics.ProcessStartInfo(exeName, arguments);
            p.StartInfo = ps;
            p.Start();

            System.Environment.Exit(0);
        }

       

       
    }
}
