using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CustomGUIControls
{
    public partial class GridControl : UserControl
    {

        public enum ColumnTypes { Text, DropDown };

        List<ComboBox> m_filters = null;
        readonly static int m_filterHeight = 20;
        int m_fixedColumns = 0;
        int m_displayColumns = 0;

        bool m_ignoreEvents = false;

        Dictionary<string, string> m_columnFormats = new Dictionary<string, string>();


        public GridControl()
        {
            m_ignoreEvents = true;
            InitializeComponent();

            //dataGridView.ColumnHeadersHeight = 2 * m_filterHeight;

            List<string> columnNames = new List<string>();


            m_filters = new List<ComboBox>();
            dataGridView.Columns.Clear();

            DataGridViewCellStyle cellStyle = new DataGridViewCellStyle();
            cellStyle.Padding = new System.Windows.Forms.Padding(0, 0, 0, m_filterHeight);
            dataGridView.ColumnHeadersDefaultCellStyle = cellStyle;

            DataGridViewCellStyle rowStyle = new DataGridViewCellStyle();
            rowStyle.Padding = new System.Windows.Forms.Padding(0, 0, 0, 0);
            dataGridView.RowsDefaultCellStyle = rowStyle;

            DataGridViewColumn objectColumn = new DataGridViewColumn();
            objectColumn.Visible = false;
            objectColumn.Name = "Object";
            objectColumn.CellTemplate = new DataGridViewTextBoxCell();
            objectColumn.HeaderText = "Object";


            dataGridView.Columns.Add(objectColumn);
            m_fixedColumns++;


            m_ignoreEvents = false;

        }

        private List<IGridItem> m_itemsToDisplay = null;

        public void AddItem(IGridItem itemToAdd)
        {
            if (m_itemsToDisplay == null)
            {
                SetUpGrid(itemToAdd);
                m_itemsToDisplay = new List<IGridItem>();
                DataGridViewRow rowTemplate = this.dataGridView.RowTemplate;
                rowTemplate.DefaultCellStyle.BackColor = Color.Bisque;

            }
            m_itemsToDisplay.Add(itemToAdd);
            int rowIndex = AddRow(itemToAdd);
            PopulateRow(rowIndex, itemToAdd);
        }

        public void Redisplay()
        {
            m_ignoreEvents = true;
            dataGridView.Rows.Clear();
            foreach (IGridItem item in m_itemsToDisplay)
            {
                int rowIndex = AddRow(item);
                PopulateRow(rowIndex, item);
            }
            SetFilters();
            m_ignoreEvents = false;
        }

        public void Redisplay(IList<int> affectedRows)
        {
            m_ignoreEvents = true;
            foreach (int row in affectedRows)
            {
                Redisplay(row);
            }
            SetFilters();
            SetDropDowns();
            m_ignoreEvents = false;
        }

        private void Redisplay(int rowIndex)
        {
            int currentColumn = 0;
            IGridItem underlyingObject = dataGridView.Rows[rowIndex].Cells[0].Value as IGridItem;
            foreach (object fieldValue in underlyingObject.FieldValues)
            {
                SetValue(rowIndex, currentColumn, fieldValue);
                currentColumn++;
            }
        }


        private void SetUpGrid(IGridItem itemToAdd)
        {
            foreach (string columnName in itemToAdd.ColumnNames)
            {
                AddColumn(columnName, itemToAdd.ColumnFormat(columnName), itemToAdd.ColumnType(columnName));
            }
        }

        private void PopulateRow(int rowIndex, IGridItem itemToAdd)
        {
            int currentColumn = 0;
            foreach (object fieldValue in itemToAdd.FieldValues)
            {
                SetValue(rowIndex, currentColumn, fieldValue);
                currentColumn++;
            }
        }


        /*public int AddColumn(string newColumnName)
        {
            return AddColumn(newColumnName, null);
        }*/

        private int AddColumn(string newColumnName, string format, ColumnTypes columnType)
        {
            //DataGridViewCellStyle cellStyle = new DataGridViewCellStyle();
            //cellStyle.Padding = new System.Windows.Forms.Padding(0, 0, 0, 10);



            DataGridViewColumn newColumn = null;
            if (columnType == ColumnTypes.Text)
                newColumn = new DataGridViewTextBoxColumn();
            else
            {
                DataGridViewComboBoxColumn newColumnL = new DataGridViewComboBoxColumn();
                newColumnL.FlatStyle = FlatStyle.Standard;
                newColumnL.Sorted = true;
                newColumn = newColumnL;
            }

            newColumn.Visible = true;
            newColumn.Name = newColumnName;
            //DataGridViewCell cell = new DataGridViewCell();
            //newColumn.CellTemplate = new DataGridViewTextBoxCell();

            newColumn.HeaderText = newColumnName;

            if (format != null)
            {
                DataGridViewCellStyle cellStyle = new DataGridViewCellStyle();
                cellStyle.Format = format;
                cellStyle.NullValue = null;
                newColumn.DefaultCellStyle = cellStyle;

            }




            dataGridView.Columns.Add(newColumn);

            ComboBox newFilter = new ComboBox();
            newFilter.Size = new System.Drawing.Size(121, 21);
            newFilter.SelectedIndexChanged += new System.EventHandler(this.comboBox_SelectedIndexChanged);
            newFilter.Tag = newColumnName;
            m_columnFormats.Add(newColumnName, format);

            m_filters.Add(newFilter);

            groupBox.Controls.Add(newFilter);

            PlaceFilters();

            return m_displayColumns++;

        }

        private int AddRow(IGridItem underlyingObject)
        {
            m_ignoreEvents = true;
            //rowTemplate.Height = 20;
            //rowTemplate.MinimumHeight = 20;

            //DataGridViewRow rowA = new DataGridViewRow();
            //object[] a = {underlyingObject,"test"};
            //rowA.CreateCells(dataGridView, a);


            int row = dataGridView.Rows.Add();
            DataGridViewRow newRow = dataGridView.Rows[row];
            newRow.Cells[0].Value = underlyingObject;
            newRow.DefaultCellStyle.BackColor = underlyingObject.Colour;

            m_ignoreEvents = false;
            return row;
        }

        /*public void SetValue(int row, string column, object value)
        {
            m_ignoreEvents = true;
            dataGridView.Rows[row].Cells[column].Value = value == null ? "" : value;
            m_ignoreEvents = false;
        }*/
        private void SetValue(int row, int column, object value)
        {
            m_ignoreEvents = true;
            object a = dataGridView.Rows[row].Cells[column + m_fixedColumns];
            DataGridViewComboBoxCell comboCell = a as DataGridViewComboBoxCell;
            if (comboCell != null)
            {
                int b =comboCell.Items.Count;
            }


            dataGridView.Rows[row].Cells[column + m_fixedColumns].Value = (value == null ? "" : value);
            m_ignoreEvents = false;
        }


        void PlaceFilters()
        {
            int columns = m_filters.Count;

            int sumOfColumnWidths = 0;

            for (int columnIndex = 0; columnIndex < columns; columnIndex++)
            {
                ComboBox filter = m_filters[columnIndex];
                DataGridViewColumn column = dataGridView.Columns[columnIndex + m_fixedColumns];

                int x = dataGridView.Location.X + 1 + sumOfColumnWidths - dataGridView.HorizontalScrollingOffset;
                int y = dataGridView.Location.Y +
                         dataGridView.ColumnHeadersHeight +
                    //dataGridView.ColumnHeadersDefaultCellStyle.Padding.Bottom
                         -m_filterHeight;


                filter.Location = new Point(x, y);

                filter.Width = column.Width;

                filter.BringToFront();

                sumOfColumnWidths += column.Width;

            }


        }

        private void dataGridView_Scroll(object sender, ScrollEventArgs e)
        {
            PlaceFilters();
        }

        private void dataGridView_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
        {
            if ((e.CellValue1 != null && e.CellValue1.GetType() == typeof(DateTime)) ||
                (e.CellValue2 != null && e.CellValue2.GetType() == typeof(DateTime)))
            {
                if (e.CellValue1 == null || (e.CellValue1 as string) == "")
                {
                    e.SortResult = -1;
                }
                else if (e.CellValue2 == null || (e.CellValue2 as string) == "")
                {
                    e.SortResult = 1;
                }
                else
                {
                    e.SortResult = DateTime.Compare((DateTime)e.CellValue1, (DateTime)e.CellValue2);
                }
                e.Handled = true;
                return;
            }


            double? d1 = e.CellValue1 as double?;
            double? d2 = e.CellValue2 as double?;


            if (d1.HasValue || d2.HasValue)
            {
                if (!d1.HasValue)
                {
                    e.SortResult = -1;
                }
                else if (!d2.HasValue)
                {
                    e.SortResult = 1;
                }
                else
                {
                    e.SortResult = d1 < d2 ? -1 : d1 > d2 ? 1 : 0;
                }
                e.Handled = true;
                return;
            }

        }

        private void dataGridView_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
        {
            PlaceFilters();
        }

        private void dataGridView_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 22) // Ctrl - V
            {
                //dataGridView.se
                m_ignoreEvents = true;

                DataGridViewSelectedCellCollection a = dataGridView.SelectedCells;

                var b = dataGridView.SelectedRows;

                var c = dataGridView.SelectedColumns;

                foreach (DataGridViewCell d in a)
                {
                    int rw = d.RowIndex;
                    int cl = d.ColumnIndex;
                }

                int row = dataGridView.CurrentCellAddress.Y;
                int column = dataGridView.CurrentCellAddress.X;

                string s = Clipboard.GetText();
                string[] lines = s.Replace("\r", "").Split('\n');

                int rowOffset = row;
                List<int> affectedRows = new List<int>();
                foreach (string line in lines)
                {
                    if (line.Length == 0)
                        continue;
                    affectedRows.Add(rowOffset);
                    string[] sCells = line.Split('\t');
                    int columnOffset = column;
                    foreach (string cellValue in sCells)
                    {
                        SetDataValue(rowOffset, columnOffset, cellValue);
                       
                        columnOffset++;
                    }
                    rowOffset++;
                }
                Redisplay(affectedRows);
                m_ignoreEvents = false;
            }
        }


        void SetDataValue(int row, int col, string value)
        {
            IGridItem underlyingData = dataGridView.Rows[row].Cells[0].Value as IGridItem;
            var a = dataGridView.Rows[row].Cells[0].Value;
            if (underlyingData != null)
            {
                string columnName = m_filters[col - m_fixedColumns].Tag as string;
                underlyingData.SetField(columnName, value);

            }
        }




        private void dataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (!m_ignoreEvents)
            {
                IGridItem underlyingData = dataGridView.Rows[e.RowIndex].Cells[0].Value as IGridItem;
                if (underlyingData != null)
                {
                    string columnName = m_filters[e.ColumnIndex - m_fixedColumns].Tag as string;
                    string value = dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value as string;
                    underlyingData.SetField(columnName, value);

                }
                Redisplay();
            }
            
        }

        public void SetDropDowns()
        {
            int colIndex = 0;
            foreach (DataGridViewColumn col in dataGridView.Columns)
            {
                DataGridViewComboBoxColumn comboCell = col as DataGridViewComboBoxColumn;
                if (comboCell != null)
                {
                    List<string> distintValues = new List<string>();
                    foreach (DataGridViewRow row in dataGridView.Rows)
                    {
                        string value = row.Cells[colIndex].Value as string;
                        if (!string.IsNullOrEmpty(value) && !distintValues.Contains(value))
                            distintValues.Add(value);
                    }
                    comboCell.Items.Clear();
                    comboCell.Items.AddRange(distintValues.ToArray());
                }
                colIndex++;
            }
        }


        public void SetFilters()
        {
            int columns = m_filters.Count;

            for (int columnIndex = 0; columnIndex < columns; columnIndex++)
            {
                ComboBox filter = m_filters[columnIndex];

                string format = m_columnFormats[filter.Tag as string];

                if (filter.SelectedItem != null && filter.SelectedItem.ToString() != "")
                    continue;

                filter.Items.Clear();

                List<string> newFilterItems = new List<string>();

                int rowCount = dataGridView.Rows.Count;
                int gridColumnIndex = m_fixedColumns + columnIndex;

                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    if (row.Visible == false)
                        continue;

                    string cellValue = null;
                    if (format == null)
                    {
                        cellValue = row.Cells[gridColumnIndex].Value.ToString();
                    }
                    else
                    {

                        object cellValueObj = row.Cells[gridColumnIndex].Value;
                        if (cellValueObj.GetType() == typeof(DateTime))
                            cellValue = ((DateTime)cellValueObj).ToString(format);
                    }

                    if (cellValue != null)
                    {
                        cellValue = cellValue.Trim();
                        if (cellValue != "" && !newFilterItems.Contains(cellValue))
                            newFilterItems.Add(cellValue);
                    }

                }
                newFilterItems.Sort();
                newFilterItems.Insert(0, "");
                filter.Items.AddRange(newFilterItems.ToArray());
            }
        }

        private void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender as ComboBox != null)
            {
                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    row.Visible = true;
                }

                foreach (ComboBox filter in m_filters)
                {
                    if (filter.SelectedItem != null)
                    {
                        string filterValue = filter.SelectedItem.ToString();
                        if (filterValue == "")
                            continue;
                        string column = filter.Tag as string;

                        foreach (DataGridViewRow row in dataGridView.Rows)
                        {
                            if (row.Cells[column].Value.ToString() != filterValue)
                                row.Visible = false;
                        }
                    }
                }
                SetFilters();
            }
        }
    }
}
