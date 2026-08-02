using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CustomGUIControls.Grid
{
    public partial class GridFilterSelect : Form
    {
        public GridFilterSelect(GridFilter filterParent)
        {
            InitializeComponent();
            m_filterParent = filterParent;
        }

        private GridFilter m_filterParent;


        private void GridFilterSelect_Deactivate(object sender, EventArgs e)
        {
            Visible = false;
        }

        static Dictionary<string, int> s_maxWidthCache = new Dictionary<string, int>();

        static System.Drawing.Graphics s_graphics = null;

        public void SetFilterItems(List<string> filterPossibleValues, List<GridFilterItem> selectedItems)
        {

            checkedListBoxSeachItems.Items.Clear();
            int minWidth = buttonNone.Location.X + buttonNone.Width + buttonAll.Location.X;
            int maxWidth = -1;
            checkedListBoxSeachItems.Items.AddRange(filterPossibleValues.ToArray());
                        
            if (maxWidth == -1)
            {
                if(s_graphics==null)
                    s_graphics =  checkedListBoxSeachItems.CreateGraphics();

                foreach (string item in filterPossibleValues)
                {
                    int thisWidth;
                    if (!s_maxWidthCache.TryGetValue(item, out thisWidth))
                    {
                        SizeF textSize = s_graphics.MeasureString(item, checkedListBoxSeachItems.Font);
                        thisWidth = (int)textSize.Width;
                        s_maxWidthCache.Add(item, thisWidth);
                    }
                    if (thisWidth > maxWidth)
                        maxWidth = thisWidth;
                }
            }
            int borderWidths = 2;
            int neededWidth = borderWidths + maxWidth + 3 * System.Windows.Forms.SystemInformation.VerticalScrollBarWidth;
            this.Width = Math.Max(minWidth, neededWidth);



            if (selectedItems != null && selectedItems.Count > 0)
            {
                buttonNone_Click(null, null);
                foreach (GridFilterItem selectedItem in selectedItems)
                {
                    string item = selectedItem.FilterText;
                    for (int i = 0; i < checkedListBoxSeachItems.Items.Count; i++)
                    {
                        if ((checkedListBoxSeachItems.Items[i] as string) == item)
                        {
                            checkedListBoxSeachItems.SetItemCheckState(i, CheckState.Checked);
                            break;
                        }
                    }
                }

            }
            else
                buttonAll_Click(null, null);

        }

        private void buttonAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBoxSeachItems.Items.Count; i++)
            {
                if (checkedListBoxSeachItems.GetItemCheckState(i) != CheckState.Checked)
                    checkedListBoxSeachItems.SetItemCheckState(i, CheckState.Checked);
            }
        }

        private void buttonNone_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBoxSeachItems.Items.Count; i++)
            {
                checkedListBoxSeachItems.SetItemCheckState(i, CheckState.Unchecked);
            }
        }

        public bool AllSelected
        {
            get
            {
                return checkedListBoxSeachItems.CheckedItems.Count == checkedListBoxSeachItems.Items.Count;
            }
        }


        public IList<string> SelectedItems
        {
            get
            {
                List<string> selectedItems = new List<string>();
                foreach (object item in checkedListBoxSeachItems.CheckedItems)
                {
                    selectedItems.Add((string)item);
                }
                return selectedItems;
            }
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            Visible = false;
            m_filterParent.RecieveFilterSettings();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Visible = false;

        }

    }
}
