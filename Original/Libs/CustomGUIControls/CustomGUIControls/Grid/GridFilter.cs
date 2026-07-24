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
    public partial class GridFilter : UserControl
    {
        public GridFilter()
        {
            InitializeComponent();

            m_filterForm = new GridFilterSelect(this);
            m_filterForm.Visible = false;
            m_originalButtonBackColor = buttonFilter.BackColor;
        }


        private Color m_originalButtonBackColor;
        public IEnumerable<GridFilterItem> FilterItems
        {
            get
            {
                return m_filterItems;
            }
        }

        public void Clear()
        {
            m_filterItems = new List<GridFilterItem>(m_defaultFilterItems);
            m_filterPossibleValues = new List<string>();
            comboBoxSearchText.Text = "";
            if (m_filterItems.Count == 0)
                buttonFilter.BackColor = m_originalButtonBackColor;
            else
                buttonFilter.BackColor = Color.Aqua;
        }

        List<string> m_filterPossibleValues = new List<string>();
        string m_multiValueSeparator = null;

        public void SetValues(List<string> items, string multiValueSeparator)
        {
            m_multiValueSeparator = multiValueSeparator;
            m_filterItems = new List<GridFilterItem>(m_defaultFilterItems);
            m_filterPossibleValues = items;

            m_filterForm.SetFilterItems(m_filterPossibleValues, m_defaultFilterItems);

            if (!string.IsNullOrEmpty(comboBoxSearchText.Text))
                SetContainsFilter(comboBoxSearchText.Text);

        }

        public bool MatchesValues(string checkValueIn)
        {
            bool result = false;
            bool anyExactFilter = false;
            string checkValue = string.IsNullOrEmpty(checkValueIn) ? "" : checkValueIn;
            foreach (GridFilterItem filterItem in m_filterItems)
            {
                if (filterItem == null)
                    continue;
                if (filterItem.Match == GridFilterItem.MatchType.Extact)
                {
                    anyExactFilter = true;
                    if (string.IsNullOrEmpty(m_multiValueSeparator))
                    {
                        if (filterItem.FilterText == checkValue)
                        {
                            result = true;
                            break;
                        }
                    }
                    else
                    {
                        string[] values = checkValue.Split(new string[] { m_multiValueSeparator }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string value in values)
                        {
                            if (filterItem.FilterText == value)
                            {
                                result = true;
                                break;
                            }
                        }

                    }
                }
            }
            if (result == false && anyExactFilter)
                return false;

            foreach (GridFilterItem filterItem in m_filterItems)
            {
                if (filterItem == null)
                    continue;
                if (filterItem.Match == GridFilterItem.MatchType.Contains)
                {
                    if (checkValue.ToLower().Contains(filterItem.FilterText.ToLower().Trim()))
                        return true;
                    else
                        return false;
                }
            }
            return true;
        }

        public bool HasValue
        {
            get
            {
                return m_filterItems.Count > 0;
            }
        }

        private List<GridFilterItem> m_filterItems = new List<GridFilterItem>();
        private List<GridFilterItem> m_defaultFilterItems = new List<GridFilterItem>();

        private void GridFilter_SizeChanged(object sender, EventArgs e)
        {
            comboBoxSearchText.Width = this.Width;
            buttonFilter.Location = new Point(this.Width-buttonFilter.Width+1, 0);
            //gridFilterDropDownSelector.Width = this.Width;
        }



        private GridFilterSelect m_filterForm = null;

        private void buttonFilter_Click(object sender, EventArgs e)
        {

            Point topLeft = this.PointToScreen(new Point(0, 0));

            int x = topLeft.X;
            int y = topLeft.Y;

            m_filterForm.Location = new Point(x, y);

            m_filterForm.Show();

            //if (m_filterForm.ShowDialog() == DialogResult.OK)
            //{
            //    m_filterItems = new List<GridFilterItem>();
            //    comboBoxSearchText.Text = "";
            //    IList<string> selectedItems = m_filterForm.SelectedItems;
            //    foreach (string item in selectedItems)
            //    {
            //        m_filterItems.Add(new GridFilterItem(item, GridFilterItem.MatchType.Extact));
            //    }
            //    RaiseEventOnFilterChanged();
            //}
        }

        internal void RecieveFilterSettings()
        {
            m_defaultFilterItems = new List<GridFilterItem>();
            m_filterItems = new List<GridFilterItem>(m_defaultFilterItems);
            comboBoxSearchText.Text = "";

            if (m_filterForm.AllSelected == false)  // 'Everything selected' behaves as 'nothing selected' for performance
            {
                IList<string> selectedItems = m_filterForm.SelectedItems;
                foreach (string item in selectedItems)
                {
                    m_filterItems.Add(new GridFilterItem(item, GridFilterItem.MatchType.Extact, m_multiValueSeparator));
                }
            }
            RaiseEventOnFilterChanged();
        }

        public void SetFilterValues(List<string> filterValues)
        {
            m_defaultFilterItems = new List<GridFilterItem>();
            foreach (string item in filterValues)
            {
                m_defaultFilterItems.Add(new GridFilterItem(item, GridFilterItem.MatchType.Extact, m_multiValueSeparator));
                buttonFilter.BackColor = Color.Aqua;
            }

        }



        public event EventHandler FilterChanged;


        protected void RaiseEventOnFilterChanged()
        {
            if (FilterChanged != null)
            {
                if (m_filterItems.Count > 0)
                    buttonFilter.BackColor = Color.Aqua;
                else
                    buttonFilter.BackColor = m_originalButtonBackColor;
                System.EventArgs e = new EventArgs();
                FilterChanged(this, e);
            }
        }

        private bool m_textValueChanged = false;

        private void comboBoxSearchText_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                m_textValueChanged = true;
                if (e.KeyChar == 13)
                {
                    SetContainsFilter(comboBoxSearchText.Text);
                    m_textValueChanged = false;
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                string a = ex.Message;
            }
        }

        private void comboBoxSearchText_Leave(object sender, EventArgs e)
        {
            if (m_textValueChanged)
            {
                SetContainsFilter(comboBoxSearchText.Text);
                m_textValueChanged = false;
            }
        }

        private void SetContainsFilter(string searchText)
        {
            for (int i = 0; i < m_filterItems.Count; i++)
            {
                if (m_filterItems[i].Match == GridFilterItem.MatchType.Contains)
                {
                    m_filterItems.RemoveAt(i);
                    i--;
                }
            }
            if (!string.IsNullOrEmpty(searchText))
                m_filterItems.Add(new GridFilterItem(searchText, GridFilterItem.MatchType.Contains,null));

            if (m_filterItems.Count > 0)
                buttonFilter.BackColor = Color.Aqua;
            else
                buttonFilter.BackColor = m_originalButtonBackColor;

            RaiseEventOnFilterChanged();
        }
    }
}
