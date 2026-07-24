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
    public partial class FormProgress : Form
    {
        public FormProgress()
        {
            InitializeComponent();
        }


        public double Progress
        {
            set
            {
                int val = (int)value;
                if (val < 0)
                    val = 0;
                if (val > 100)
                    val = 100;
                if (val != progressBar1.Value)
                {
                    progressBar1.Value = val;
                    progressBar1.Refresh();
                    //System.Windows.Forms.Application.DoEvents();
                }
            }
        }
    }
}
