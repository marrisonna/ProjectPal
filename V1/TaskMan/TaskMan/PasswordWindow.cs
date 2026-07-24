using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TaskMan
{
    public partial class PasswordWindow : Form
    {
        public PasswordWindow()
        {
            InitializeComponent();
            Password = "";
        }

        private void Password_Load(object sender, EventArgs e)
        {

        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        
        public string Password { get; private set; }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            Password = textBoxPassword.Text;
        }
    }
}
