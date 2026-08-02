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
    public partial class NewProject : Form
    {
        public enum Mode { New, Rename };
        public NewProject(DBProjectPal.Project project, Mode windowMode)
        {
            InitializeComponent();

            DBProjectPal.Project parentProject = null;

            if (windowMode == Mode.Rename)
            {
                parentProject = project.Parent;
                textBoxProjectName.Text = project.Name;
                this.Text = "Rename Project";
                buttonCreate.Text = "Rename";
            }
            else
            {
                parentProject = project;

            }
            textBoxParentName.Text = parentProject!= null ? parentProject.FullName :"Top Level Project";
        }


        public string ProjectName { get { return m_name; } }



        private string m_name = null;

        private void textBoxProjectName_TextChanged(object sender, EventArgs e)
        {
            m_name = textBoxProjectName.Text;
        }

        private void textBoxProjectName_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Return) // Return
            {
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
                Close();
            }
        }
    }
}
