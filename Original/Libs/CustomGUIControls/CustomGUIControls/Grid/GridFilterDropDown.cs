using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CustomGUIControls.Grid
{
    public partial class GridFilterDropDown : UserControl
    {
        public GridFilterDropDown()
        {
            InitializeComponent();
        }

        public void Clear()
        {
            checkedListBoxSeachItems.Items.Clear();
        }

        public void Add(string item)
        {
            checkedListBoxSeachItems.Items.Add(item);
            this.Height = checkedListBoxSeachItems.Height + buttonOK.Height+2;
        }

        public int MinWidth
        {
            get
            {
                return buttonAll.Width + buttonNone.Width+ buttonOK.Width + buttonCancel.Width;
            }
        }

        private void GridFilterDropDown_SizeChanged(object sender, EventArgs e)
        {
            checkedListBoxSeachItems.Size = new Size(this.Width, checkedListBoxSeachItems.Height);
        }
    }
}
