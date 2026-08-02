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
    public partial class NewComponent : Form
    {
        public enum Mode {New, Rename};
        public NewComponent(DBProjectPal.Component component, Mode windowMode)
        {
            InitializeComponent();
            DBProjectPal.Component parentComponent = null;

            if (windowMode == Mode.Rename)
            {
                parentComponent = component.Parent;
                textBoxComponentName.Text = component.Name;
                this.Text = "Rename Component";
                buttonCreate.Text = "Rename";
            }
            else
            {
                parentComponent = component;
               
            }
            textBoxParentName.Text = parentComponent != null ? parentComponent.FullName : "Top Level Component";
        }


        public string ComponentName { get { return m_name; } }



        private string m_name = null;

        private void textBoxComponentName_TextChanged(object sender, EventArgs e)
        {
            m_name = textBoxComponentName.Text;
        }

        private void textBoxComponentName_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Return) // Return
            {
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
                Close();
            }
        }
    }
}
