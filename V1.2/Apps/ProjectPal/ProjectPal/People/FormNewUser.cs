using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using DBProjectPal;
using System.Text;
using System.Windows.Forms;

namespace ProjectPal.People
{
    public partial class FormNewUser : Form
    {
        enum Mode { Add, Delete };

        private Mode m_windowMode = Mode.Add;

        public FormNewUser()
        {
            InitializeComponent();
        }

        public FormNewUser(string personToDelete)
        {
            InitializeComponent();
            m_windowMode = Mode.Delete;
            Text = "Delete a Person";
            label1.Text = "Delete this person?";
            textBoxName.ReadOnly = true;
            textBoxName.Text = personToDelete;
        }

        private void textBoxName_TextChanged(object sender, EventArgs e)
        {
            if (m_windowMode == Mode.Add)
            {
                bool found = false;
                string newName = textBoxName.Text.Trim();
                foreach (Person existingPerson in Person.AllInstances)
                {
                    if (string.Compare(newName, existingPerson.Name, true) == 0)
                    {
                        found = true;
                        break;
                    }
                }
                labelExists.Visible = found;

                buttonOK.Enabled = !(found || string.IsNullOrEmpty(newName));
            }
        }

        public string NewPersonName { get { return textBoxName.Text.Trim(); } }
    }
}
