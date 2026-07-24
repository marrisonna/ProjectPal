using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;
using Utils;

namespace CustomGUIControls.Grid
{
    public partial class GridControl : UserControl, IView
    {

        public enum ColumnTypes { Text, DropDown };

        //List<ComboBox> m_filters = null;
        List<GridFilter> m_filters2 = null;
        readonly static int m_filterHeight = 20;
        readonly static int m_headerHeight = 22;
        int m_fixedColumns = 1;
        int m_displayColumns = 0;
        const string ColumnZeroName = "Object";
        bool m_ignoreEvents = true;
        DataGridViewCellStyle m_readOnlyStyle = null;
        DataGridViewCellStyle m_readWriteStyle = null;

        Dictionary<string, string> m_columnFormats = new Dictionary<string, string>();

        Dictionary<string, System.Comparison<object>> m_columnFilterSortFns = new Dictionary<string, System.Comparison<object>>();

        List<string> m_hiddenColumns = new List<string>();
        ViewImpl m_viewImplementation = null;

        public int ColumnHeaderHeight { get { return dataGridView.ColumnHeadersHeight; } }
        public int RowHeight { get { return dataGridView.RowTemplate.Height; } }

        private static readonly Color ReadWriteColour = Utils.Colours.ReadWriteColour;


        private IGridColumns m_gridColumns;

        public bool AllowCellDrop
        {
            get
            {
                return m_allowCellDrop;
            }
            set
            {
                m_allowCellDrop = value;

                this.dataGridView.AllowDrop = m_allowCellDrop;
            }
        }

        private bool m_allowCellDrop = false;

        public void ColumnVisible(string column, bool visible)
        {

            if (visible)
                m_hiddenColumns.Remove(column);
            else if (!m_hiddenColumns.Contains(column))
                m_hiddenColumns.Add(column);

            if (dataGridView.Columns.Contains(column))
                dataGridView.Columns[column].Visible = visible;

        }

        public void ColumnWidth(string column, int width, int maxWidth)
        {
            ColumnWidth(column, width);
            if (dataGridView.Columns[column].Width > maxWidth)
                ColumnWidth(column, maxWidth);
        }

        public void ColumnWidth(string column, int width)
        {
            if (dataGridView.Columns.Contains(column))
            {
                if (width >= 0)
                {
                    dataGridView.Columns[column].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    dataGridView.Columns[column].Width = width;
                }
                else
                {
                    if (dataGridView.Columns[column].AutoSizeMode != DataGridViewAutoSizeColumnMode.Fill)
                    {
                        dataGridView.Columns[column].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                        dataGridView.AutoResizeColumn(dataGridView.Columns[column].Index);
                    }
                }
            }

        }


        public GridControl() :
            this(false, false)
        {
        }


        private bool m_filterIsVisible = false;

        public bool FilterIsVisible
        {
            get
            {
                return m_filterIsVisible;
            }
            set
            {
                m_filterIsVisible = value;
                showFilterToolStripMenuItem.Checked = m_filterIsVisible;
                PlaceFilters();
            }
        }

        private bool m_autoHeight = false;

        private DataGridViewCellStyle m_headerWithFilterStyle = null;
        private DataGridViewCellStyle m_headerWithoutFilterStyle = null;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="autoHeight"></param>
        public GridControl(bool autoHeight, bool showFilter)
        {
            InitializeComponent();
            this.dataGridView.AllowDrop = m_allowCellDrop;
            m_filterIsVisible = showFilter;

            dataGridView.ColumnHeadersHeight = m_headerHeight + m_filterHeight;
            showFilterToolStripMenuItem.Checked = m_filterIsVisible;

            m_viewImplementation = new ViewImpl(this);

            m_autoHeight = autoHeight;
            m_ignoreEvents = true;
            InitValues();

            //dataGridView.ColumnHeadersHeight = 2 * m_filterHeight;


            dataGridView.Columns.Clear();

            m_headerWithFilterStyle = new DataGridViewCellStyle();
            m_headerWithFilterStyle.Padding = new System.Windows.Forms.Padding(0, 0, 0, m_filterHeight);
            dataGridView.ColumnHeadersDefaultCellStyle = m_headerWithFilterStyle;

            m_headerWithoutFilterStyle = new DataGridViewCellStyle();
            m_headerWithoutFilterStyle.Padding = new System.Windows.Forms.Padding(0, 0, 0, 0);

            DataGridViewCellStyle rowStyle = new DataGridViewCellStyle();
            rowStyle.Padding = new System.Windows.Forms.Padding(0, 0, 0, 0);
            dataGridView.RowsDefaultCellStyle = rowStyle;

            DataGridViewColumn objectColumn = new DataGridViewColumn();
            objectColumn.Visible = false;
            objectColumn.Name = ColumnZeroName;
            objectColumn.CellTemplate = new DataGridViewTextBoxCell();
            objectColumn.HeaderText = ColumnZeroName;


            dataGridView.Columns.Add(objectColumn);
            dataGridView.ContextMenuStrip = contextMenuStrip1;

        }

        //private List<IGridItem> m_itemsToDisplay = new List<IGridItem>();

        private bool m_setUpDone = false;

        public void SetColumns(IGridColumns columnDefinitions)
        {
            if (!m_setUpDone)
            {
                m_gridColumns = columnDefinitions;
                m_setUpDone = true;
                SetUpGrid(columnDefinitions);
                DataGridViewRow rowTemplate = this.dataGridView.RowTemplate;
                rowTemplate.DefaultCellStyle.BackColor = ReadWriteColour;

            }

        }



        public void AddDisplayItem(IDisplayItem itemToAdd)
        {
            m_needsToBeSorted = true;
            IGridItem gridItemToAdd = itemToAdd as IGridItem;
            if (gridItemToAdd != null)
            {
                if (!m_setUpDone)
                {
                    throw new Exception("Columns have not been set up yet!");

                }
                m_viewImplementation.AddDisplayItem(itemToAdd);
                itemToAdd.AddView(this);
                DataGridViewRow row = AddRow(gridItemToAdd);
                PopulateRow(row, gridItemToAdd);

                m_ignoreEvents = false;
            }
        }

        private bool m_needToSetFilters = true;

        public void Redisplay()
        {
            int horizontalScroll = dataGridView.HorizontalScrollingOffset;
            if (m_needToSetFilters)
            {
                m_needToSetFilters = false;
                PerformSetFilters();
            }
            ApplyFiltersToRows();
            dataGridView.Sort(dataGridView.Columns[m_defaultSortColumn], m_defaultSortDirection);
            dataGridView.HorizontalScrollingOffset = horizontalScroll;
            m_needsToBeSorted = false;
        }

        public void Redisplay(IDisplayItem itemToDisplay)
        {
            m_needsToBeSorted = true;
            IGridItem underlyingObject = itemToDisplay as IGridItem;
            bool currentIgnoreEvents = m_ignoreEvents;
            m_ignoreEvents = true;
            List<DataGridViewRow> rowsCopy = new List<DataGridViewRow>();
            foreach (DataGridViewRow row in dataGridView.Rows)
                rowsCopy.Add(row);


            foreach (DataGridViewRow row in rowsCopy)
            {
                if (row.Cells[0].Value == underlyingObject)
                {
                    if (underlyingObject.IsDeleted)
                        dataGridView.Rows.Remove(row);
                    else
                        Redisplay(row);
                    break;
                }
            }
            // TODO - need to set flag to set filters, but only actually do them later, somehow
            m_needToSetFilters = true;
            m_ignoreEvents = currentIgnoreEvents;
        }

        private void Redisplay(IList<DataGridViewRow> affectedRows)
        {
            m_needsToBeSorted = true;
            bool currentIgnoreEvents = m_ignoreEvents;
            m_ignoreEvents = true;
            foreach (DataGridViewRow row in affectedRows)
            {
                IGridItem underlyingObject = row.Cells[0].Value as IGridItem;
                underlyingObject.Redisplay();
            }
            SetFilters();
            m_ignoreEvents = currentIgnoreEvents;
        }

        private void Redisplay(DataGridViewRow row)
        {
            if (CustomGUIControls.RedisplayManager.Instance.HasItemAlreadyBeenRedisplayed(row))
                return;

            m_needsToBeSorted = true;

            int currentColumn = 0;
            //DataGridViewRow row = dataGridView.Rows[rowIndex];
            IGridItem underlyingObject = row.Cells[0].Value as IGridItem;
            row.DefaultCellStyle.BackColor = underlyingObject.Colour;
            foreach (string columnName in m_columnNames)
            {
                object fieldValue = underlyingObject.GetFieldValue(columnName);
                bool isReadOnly = underlyingObject.IsReadOnly(columnName);
                SetValue(row, currentColumn, fieldValue, isReadOnly);
                currentColumn++;
            }

        }

        List<string> m_columnNames = null;
        private void SetUpGrid(IGridColumns columnDefinitions)
        {
            m_gridColumns = columnDefinitions;
            m_columnNames = new List<string>();

            if (m_columnOrder != null)
            {
                foreach (string columnName in m_columnOrder)
                {
                    if (columnDefinitions.ColumnNames.Contains(columnName) && !m_columnNames.Contains(columnName))
                    {
                        m_columnNames.Add(columnName);
                        AddColumn(columnName, columnDefinitions.ColumnFormat(columnName),
                                  columnDefinitions.ColumnAlignment(columnName),
                                  columnDefinitions.ColumnType(columnName),
                                  columnDefinitions.GetComboValues(columnName), columnDefinitions.ColumnIsReadOnly(columnName));
                    }

                }
            }


            foreach (string columnName in columnDefinitions.ColumnNames)
            {
                if (!m_columnNames.Contains(columnName))
                {
                    m_columnNames.Add(columnName);
                    AddColumn(columnName, columnDefinitions.ColumnFormat(columnName),
                        columnDefinitions.ColumnAlignment(columnName), columnDefinitions.ColumnType(columnName),
                        columnDefinitions.GetComboValues(columnName), columnDefinitions.ColumnIsReadOnly(columnName));
                }
            }

            if (dataGridView.Columns.Count > 0)
                dataGridView.Columns[dataGridView.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

        }

        private void PopulateRow(DataGridViewRow row, IGridItem itemToAdd)
        {
            int currentColumn = 0;


            foreach (string columnName in m_columnNames)
            {
                object fieldValue = itemToAdd.GetFieldValue(columnName);
                bool isReadOnly = itemToAdd.IsReadOnly(columnName);
                SetValue(row, currentColumn, fieldValue, isReadOnly);
                currentColumn++;
            }
        }


        /*public int AddColumn(string newColumnName)
        {
            return AddColumn(newColumnName, null);
        }*/

        private Dictionary<string, IList<string>> m_comboValues = new Dictionary<string, IList<string>>();

        public void SetComboValues(string columnName, IList<string> comboValues)
        {
            m_comboValues[columnName] = comboValues;
            SetDropDowns();
        }

        private int AddColumn(string newColumnName, string format, DataGridViewContentAlignment? alignment,
                              ColumnTypes columnType, IList<string> comboValues, bool isReadOnly)
        {
            //DataGridViewCellStyle cellStyle = new DataGridViewCellStyle();
            //cellStyle.Padding = new System.Windows.Forms.Padding(0, 0, 0, 10);



            DataGridViewColumn newColumn = null;
            if (columnType == ColumnTypes.Text)
                newColumn = new DataGridViewTextBoxColumn();
            else
            {
                DataGridViewComboBoxColumn newColumnL = new DataGridViewComboBoxColumn();
                newColumnL.FlatStyle = FlatStyle.Popup;

                newColumnL.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox;

                newColumnL.Sorted = true;
                newColumnL.SortMode = DataGridViewColumnSortMode.Automatic;
                newColumn = newColumnL;
                m_comboValues.Add(newColumnName, comboValues);
            }

            newColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            newColumn.ReadOnly = isReadOnly;
            newColumn.Visible = !m_hiddenColumns.Contains(newColumnName);
            newColumn.Name = newColumnName;
            //DataGridViewCell cell = new DataGridViewCell();
            //newColumn.CellTemplate = new DataGridViewTextBoxCell();

            newColumn.HeaderText = newColumnName;
            if (alignment != null)
            {
                newColumn.HeaderCell.Style.Alignment = alignment.Value;
            }


            if (m_readOnlyStyle == null)
            {
                m_readOnlyStyle = newColumn.DefaultCellStyle.Clone();
                m_readOnlyStyle.BackColor = Utils.Colours.InactiveColour;
                m_readWriteStyle = newColumn.DefaultCellStyle.Clone();
                m_readWriteStyle.BackColor = ReadWriteColour;

            }


            if (format != null || alignment != null)
            {
                DataGridViewCellStyle cellStyle = new DataGridViewCellStyle();

                if (format != null)
                {
                    cellStyle.Format = format;
                    cellStyle.NullValue = null;
                }
                if (alignment != null)
                {
                    cellStyle.Alignment = alignment.Value;
                }
                newColumn.DefaultCellStyle = cellStyle;


            }

            dataGridView.Columns.Add(newColumn);

            //ComboBox newFilter = new ComboBox();
            //newFilter.Size = new System.Drawing.Size(121, 21);
            //newFilter.SelectedIndexChanged += new System.EventHandler(this.comboBox_SelectedIndexChanged);
            //newFilter.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.comboBox_KeyPress);
            //newFilter.Leave += new System.EventHandler(this.comboBox_Leave);
            //newFilter.Tag = newColumnName;


            //m_filters.Add(newFilter);
            //groupBoxFilters.Controls.Add(newFilter);
            if (newColumn.Visible)
            {
                GridFilter newFilter2 = new GridFilter();
                newFilter2.Size = new System.Drawing.Size(121, 21);
                newFilter2.Tag = newColumnName;
                newFilter2.FilterChanged += new System.EventHandler(this.GridFilter_FilterChanged);

                m_filters2.Add(newFilter2);
                groupBoxFilters.Controls.Add(newFilter2);
            }

            m_columnFormats.Add(newColumnName, format);

            return m_displayColumns++;

        }


        public void SetDefaultFilterValues(string column, List<string> filterValues)
        {
            foreach (GridFilter filter in m_filters2)
            {
                if ((filter.Tag as string) == column)
                {
                    filter.SetFilterValues(filterValues);
                }
            }
        }


        private string m_defaultSortColumn;
        private bool m_needsToBeSorted = true;
        private ListSortDirection m_defaultSortDirection;

        public void SetDefaultSort(string column, ListSortDirection direction)
        {
            m_needToSetFilters = true;
            m_defaultSortColumn = column;
            m_defaultSortDirection = direction;
            m_needsToBeSorted = true;
        }

        public void StopLayout()
        {
            this.SuspendLayout();
            dataGridView.SuspendLayout();
        }

        public void StartLayout()
        {
            dataGridView.ResumeLayout();
            this.ResumeLayout();
        }

        System.Diagnostics.Stopwatch total = new System.Diagnostics.Stopwatch();
        System.Diagnostics.Stopwatch inner1 = new System.Diagnostics.Stopwatch();
        System.Diagnostics.Stopwatch inner2 = new System.Diagnostics.Stopwatch();
        System.Diagnostics.Stopwatch inner3 = new System.Diagnostics.Stopwatch();

        public void LogPerformance()
        {
            Utils.Logger.Log("Total = " + (total.ElapsedMilliseconds));
            Utils.Logger.Log("Inner1 = " + (inner1.ElapsedMilliseconds));
            Utils.Logger.Log("Inner2 = " + (inner2.ElapsedMilliseconds));
            Utils.Logger.Log("Inner3 = " + (inner3.ElapsedMilliseconds));

            double p1 = ((100.0 * inner1.ElapsedTicks) / total.ElapsedTicks);
            double p2 = ((100.0 * inner2.ElapsedTicks) / total.ElapsedTicks);
            double p3 = ((100.0 * inner3.ElapsedTicks) / total.ElapsedTicks);

            Utils.Logger.Log("P1 = " + p1 + "%");
            Utils.Logger.Log("P2 = " + p2 + "%");
            Utils.Logger.Log("P3 = " + p3 + "%");

        }

        public void AddRows(int rows)
        {
            m_rowsAdded = rows;
            dataGridView.Rows.Add(rows);
            m_nextAddedRow = 0;
            m_allAddedRows = new DataGridViewRow[rows];

            int nextIndex = 0;
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                m_allAddedRows[nextIndex++] = row;
            }


        }

        DataGridViewRow[] m_allAddedRows = null;
        private int m_rowsAdded = -1;
        private int m_nextAddedRow = -1;


        private DataGridViewRow AddRow(IGridItem underlyingObject)
        {
            total.Start();
            inner1.Start();

            bool currentIgnoreEvents = m_ignoreEvents;
            m_ignoreEvents = true;

            DataGridViewRow newRow;
            if (m_nextAddedRow < m_rowsAdded)
            {
                newRow = m_allAddedRows[m_nextAddedRow++];
            }
            else
            {

                int row = dataGridView.Rows.Add();

                //rowTemplate.Height = 20;
                //rowTemplate.MinimumHeight = 20;

                //DataGridViewRow rowA = new DataGridViewRow();
                //object[] a = {underlyingObject,"test"};
                //rowA.CreateCells(dataGridView, a);

                inner1.Stop();
                inner2.Start();
                newRow = dataGridView.Rows[row];
                inner2.Stop();
            }
            inner3.Start();

            newRow.Cells[0].Value = underlyingObject;
            newRow.DefaultCellStyle.BackColor = underlyingObject.Colour;

            m_ignoreEvents = currentIgnoreEvents;
            inner3.Stop();
            total.Stop();
            return newRow;
        }

        /*public void SetValue(int row, string column, object value)
        {
            m_ignoreEvents = true;
            dataGridView.Rows[row].Cells[column].Value = value == null ? "" : value;
            m_ignoreEvents = false;
        }*/
        private void SetValue(DataGridViewRow row, int column, object value, bool isReadOnly)
        {
            bool currentIgnoreEvents = m_ignoreEvents;
            m_ignoreEvents = true;
            DataGridViewCell theCell = row.Cells[column + m_fixedColumns];
            theCell.Value = (value == null ? "" : value);
            bool columnIsReadOnly = dataGridView.Columns[column + m_fixedColumns].ReadOnly;
            //string name = theCell.OwningColumn.HeaderText;
            // object cell = row.Cells[column + m_fixedColumns];
            theCell.ReadOnly = isReadOnly || columnIsReadOnly;

            //DataGridViewComboBoxCell comboCell = row.Cells[column + m_fixedColumns] as DataGridViewComboBoxCell;
            //if (comboCell != null)
            //{
            //  if (!comboCell.Items.Contains(comboCell.Value))
            //    comboCell.Items.Add(comboCell.Value);
            //}
            //    if (theCell.ReadOnly)
            //        comboCell.Style = m_readOnlyStyle;
            //    else
            //    {
            //        comboCell.Style = m_readWriteStyle;

            //    }
            //}
            //else
            //{

            IGridItem underlyingData = row.Cells[0].Value as IGridItem;
            DataGridViewCellStyle overriddenCellStyle = underlyingData.GetCellStyle(dataGridView.Columns[column + m_fixedColumns].Name,
                                                                                    theCell.OwningRow.DefaultCellStyle);
            if (overriddenCellStyle == null && underlyingData.IsActive() && theCell.ReadOnly)
                overriddenCellStyle = GetMergedStyle(m_readOnlyStyle, theCell.OwningRow.DefaultCellStyle);
            //  theCell.Style = m_readOnlyStyle;
            // }

            if (overriddenCellStyle != null)
                theCell.Style = overriddenCellStyle;
            m_ignoreEvents = currentIgnoreEvents;
        }



        Dictionary<DataGridViewCellStyle, Dictionary<DataGridViewCellStyle, DataGridViewCellStyle>> s_styleCache = new Dictionary<DataGridViewCellStyle, Dictionary<DataGridViewCellStyle, DataGridViewCellStyle>>();


        private DataGridViewCellStyle GetMergedStyle(DataGridViewCellStyle a, DataGridViewCellStyle b)
        {
            //Dictionary<DataGridViewCellStyle, DataGridViewCellStyle> innerCache;

            DataGridViewCellStyle result;
            //if (s_styleCache.TryGetValue(a, out innerCache))
            //{
            //    if (innerCache.TryGetValue(b, out result))
            //        return result;
            //}
            //else
            //{
            //    innerCache = new Dictionary<DataGridViewCellStyle, DataGridViewCellStyle>();
            //    s_styleCache.Add(a, innerCache);
            //}

            result = a.Clone();

            int weightA = 3;
            int weightB = 2;

            int A = (a.BackColor.A * weightA + b.BackColor.A * weightB) / (weightA + weightB);
            int R = (a.BackColor.R * weightA + b.BackColor.R * weightB) / (weightA + weightB);
            int G = (a.BackColor.G * weightA + b.BackColor.G * weightB) / (weightA + weightB);
            int B = (a.BackColor.B * weightA + b.BackColor.B * weightB) / (weightA + weightB);

            if (A < 0 || A > 255 || R < 0 || R > 255 || G < 0 || G > 255 || B < 0 || B > 255)
                return result;

            result.BackColor = Color.FromArgb(A, R, G, B);

            //innerCache.Add(b, result);

            return result;
        }


        internal int GridHorizontalScrollingOffset
        {
            get
            {
                return dataGridView.HorizontalScrollingOffset;
            }
        }

        void PlaceFilters()
        {
            PlaceFilters(false);
        }

        private bool IsVScrollBarVisible(Control control)
        {
            foreach (Control c in control.Controls)
            {
                if (c.GetType().Equals(typeof(VScrollBar)))
                    return c.Visible;
            }


            return false;
        }


        int? m_lastSumOfColumnWidths = null;

        void PlaceFilters(bool fast)
        {
            bool currentIgnoreEvents = m_ignoreEvents;
            m_ignoreEvents = true;

            if (m_filterIsVisible)
            {

                dataGridView.ColumnHeadersHeight = m_headerHeight + m_filterHeight;
                dataGridView.ColumnHeadersDefaultCellStyle = m_headerWithFilterStyle;
            }
            else
            {
                dataGridView.ColumnHeadersHeight = m_headerHeight + 0;
                dataGridView.ColumnHeadersDefaultCellStyle = m_headerWithoutFilterStyle;
            }

            int scrollbarWidth = System.Windows.Forms.SystemInformation.VerticalScrollBarWidth;
            int scrollbarHeight = System.Windows.Forms.SystemInformation.HorizontalScrollBarHeight;

            int x = dataGridView.Location.X + 1 - dataGridView.HorizontalScrollingOffset;
            int y = dataGridView.Location.Y +
                     dataGridView.ColumnHeadersHeight +
                //dataGridView.ColumnHeadersDefaultCellStyle.Padding.Bottom
                     -m_filterHeight;

            groupBoxFilters.Visible = false;
            if (groupBoxFilters.Location.X != x || groupBoxFilters.Location.Y != y)
                groupBoxFilters.Location = new Point(x, y);
            if (!fast || m_lastSumOfColumnWidths == null)
            {
                if (groupBoxFilters.Height != m_filterHeight)
                    groupBoxFilters.Height = m_filterHeight;

                int sumOfColumnWidths = 0;
                int totalHeight = 0;


                for (int row = 0; row < dataGridView.RowCount; row++)
                {
                    if (dataGridView.Rows[row].Visible)
                        totalHeight += dataGridView.Rows[row].Height;

                }
                if (totalHeight > 0)
                    totalHeight += dataGridView.ColumnHeadersHeight + 2;





                int columns = m_filters2.Count;
                for (int columnIndex = 0; columnIndex < columns; columnIndex++)
                {
                    GridFilter filter = m_filters2[columnIndex];
                    //DataGridViewColumn column = dataGridView.Columns[columnIndex + m_fixedColumns];
                    DataGridViewColumn column = ColumnsInDisplayOrder[columnIndex];

                    if (filter.Visible != column.Visible)
                        filter.Visible = column.Visible;

                    if (!column.Visible)
                        continue;

                    if (filter.Location.X != sumOfColumnWidths || filter.Location.Y != 0)
                    {
                        filter.Location = new Point(sumOfColumnWidths, 0);
                        filter.BringToFront();
                    }

                    if (filter.Width != column.Width)
                        filter.Width = column.Width;



                    sumOfColumnWidths += column.Width;

                }
                m_lastSumOfColumnWidths = sumOfColumnWidths;

                bool verticalScrollBarVisible = IsVScrollBarVisible(dataGridView);

                // If the vertical scroll bar is visible... (how can we tell this?)
                //if (dataGridView.VerticalScrollingOffset == 0)
                //    scrollbarWidth = 0;

                //groupBoxFilters.Width = Math.Min(groupBox.Width /*- scrollbarWidth*/ - 2, sumOfColumnWidths);
                if (m_autoHeight)
                {
                    if (groupBoxFilters.Width != sumOfColumnWidths)
                        groupBoxFilters.Width = sumOfColumnWidths;
                }
                else
                {
                    int newWidth = 0;
                    if (sumOfColumnWidths > this.Width - 5 - (verticalScrollBarVisible ? scrollbarWidth : 0))
                        newWidth = dataGridView.HorizontalScrollingOffset + groupBox.Width - (verticalScrollBarVisible ? scrollbarWidth : 0) - 2;
                    else
                        newWidth = sumOfColumnWidths;
                    //groupBoxFilters.Width = Math.Min(groupBox.Width - scrollbarWidth - 2, sumOfColumnWidths);
                    if (groupBoxFilters.Width != newWidth)
                        groupBoxFilters.Width = newWidth;
                }

                if (totalHeight > 0 && m_autoHeight)
                {

                    if (sumOfColumnWidths > this.Width - 5)
                    {
                        if (dataGridView.Height != totalHeight + scrollbarHeight)
                        {
                            int q = this.Height;
                            if (this.Height != totalHeight + scrollbarHeight + 2)
                                this.Height = totalHeight + scrollbarHeight + 2;
                            //dataGridView.Parent.Height = totalHeight+scrollbarHeight;
                            // dataGridView.Height = totalHeight + scrollbarHeight;
                            //if (dataGridView.Parent.Height != totalHeight + scrollbarHeight)
                            //    dataGridView.Parent.Height = totalHeight + scrollbarHeight;
                        }
                    }
                    else
                    {
                        if (this.Height != totalHeight + 2)
                            this.Height = totalHeight + 2;
                    }
                }
                if (totalHeight == 0 && m_autoHeight)
                {
                    if (this.Height != dataGridView.ColumnHeadersHeight + 2)
                        this.Height = dataGridView.ColumnHeadersHeight + 2;
                }
            }
            else
            {
                bool verticalScrollBarVisible = IsVScrollBarVisible(dataGridView);
                int newWidth = Math.Min(m_lastSumOfColumnWidths.HasValue ? m_lastSumOfColumnWidths.Value : 10000,
                                                 dataGridView.HorizontalScrollingOffset + groupBox.Width - (m_autoHeight ? 0 : (verticalScrollBarVisible ? scrollbarWidth : 0)) - 2);
                if (groupBoxFilters.Width != newWidth)
                    groupBoxFilters.Width = newWidth;
            }
            groupBoxFilters.Visible = m_filterIsVisible;


            m_ignoreEvents = currentIgnoreEvents;
        }



        private void dataGridView_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
        {
            int nullSortDirection = dataGridView.SortOrder == SortOrder.Ascending ? -1 : 1;


            if ((e.CellValue1 != null && e.CellValue1.GetType() == typeof(DateTime)) ||
                (e.CellValue2 != null && e.CellValue2.GetType() == typeof(DateTime)))
            {
                if (e.CellValue1 == null || (e.CellValue1 as string) == "")
                {
                    e.SortResult = -1 * nullSortDirection;
                }
                else if (e.CellValue2 == null || (e.CellValue2 as string) == "")
                {
                    e.SortResult = 1 * nullSortDirection;
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
                    e.SortResult = -1 * nullSortDirection;
                }
                else if (!d2.HasValue)
                {
                    e.SortResult = 1 * nullSortDirection;
                }
                else
                {
                    e.SortResult = d1 < d2 ? -1 : d1 > d2 ? 1 : 0;
                }
                e.Handled = true;
                return;
            }

            int? i1 = e.CellValue1 as int?;
            int? i2 = e.CellValue2 as int?;

            if (i1.HasValue || i2.HasValue)
            {
                if (!i1.HasValue)
                {
                    e.SortResult = -1 * nullSortDirection;
                }
                else if (!i2.HasValue)
                {
                    e.SortResult = 1 * nullSortDirection;
                }
                else
                {
                    e.SortResult = i1 < i2 ? -1 : i1 > i2 ? 1 : 0;
                }
                e.Handled = true;
                return;
            }

            if ((e.CellValue1 != null && e.CellValue1.GetType() == typeof(string)) ||
               (e.CellValue2 != null && e.CellValue2.GetType() == typeof(string)))
            {
                if (string.IsNullOrEmpty(e.CellValue1 as string) &&
                    string.IsNullOrEmpty(e.CellValue2 as string))
                {
                    e.SortResult = 0;
                }
                else if (string.IsNullOrEmpty(e.CellValue1 as string))
                {
                    e.SortResult = -1 * nullSortDirection;
                }
                else if (string.IsNullOrEmpty(e.CellValue2 as string))
                {
                    e.SortResult = 1 * nullSortDirection;
                }
                else
                {
                    e.SortResult = string.Compare((string)e.CellValue1, (string)e.CellValue2);
                }
                e.Handled = true;
                return;
            }

            e.Handled = false;
        }

        private void dataGridView_ColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
        {
            if (e.Column.AutoSizeMode != DataGridViewAutoSizeColumnMode.Fill)
                PlaceFilters();
        }

        private void dataGridView_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 22) // Ctrl - V
            {
                //dataGridView.se
                bool currentIgnoreEvents = m_ignoreEvents;
                m_ignoreEvents = true;

                DataGridViewSelectedCellCollection a = dataGridView.SelectedCells;

                var b = dataGridView.SelectedRows;

                var c = dataGridView.SelectedColumns;
                // TODO Make paste work for different size of selections and paste buffer
                foreach (DataGridViewCell d in a)
                {
                    int rw = d.RowIndex;
                    int cl = d.ColumnIndex;
                }

                int rowNo = dataGridView.CurrentCellAddress.Y;
                int column = dataGridView.CurrentCellAddress.X;

                string s = Clipboard.GetText();
                string[] lines = s.Replace("\r", "").Split('\n');

                int rowOffset = rowNo;
                List<DataGridViewRow> affectedRows = new List<DataGridViewRow>();
                foreach (string line in lines)
                {
                    if (line.Length == 0)
                        continue;
                    DataGridViewRow row = dataGridView.Rows[rowOffset];
                    affectedRows.Add(row);
                    string[] sCells = line.Split('\t');
                    int columnOffset = column;
                    foreach (string cellValue in sCells)
                    {
                        if (!dataGridView.Columns[columnOffset].ReadOnly &&
                            !row.Cells[columnOffset].ReadOnly)
                        {
                            SetDataValue(row, columnOffset, cellValue);
                        }

                        columnOffset++;
                    }
                    rowOffset++;
                }
                CustomGUIControls.RedisplayManager.Instance.Reset();
                Redisplay(affectedRows);
                m_ignoreEvents = currentIgnoreEvents;
            }

        }


        void SetDataValue(DataGridViewRow row, int col, string value)
        {
            IGridItem underlyingData = row.Cells[0].Value as IGridItem;
            if (underlyingData != null)
            {
                string columnName = dataGridView.Columns[col].Name;

                underlyingData.SetField(columnName, value);
                CustomGUIControls.RedisplayManager.Instance.Reset();
                underlyingData.Redisplay();
            }
        }




        private void dataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (!m_ignoreEvents)
            {
                CellValueChanged(dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex]);
                //IGridItem underlyingData = dataGridView.Rows[e.RowIndex].Cells[0].Value as IGridItem;
                //if (underlyingData != null)
                //{
                //    string columnName = m_filters2[e.ColumnIndex - m_fixedColumns].Tag as string;
                //    string value = dataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value as string;
                //    underlyingData.SetField(columnName, value);
                //    CustomGUIControls.RedisplayManager.Instance.Reset(this);
                //    underlyingData.Redisplay();
                //    CustomGUIControls.RedisplayManager.Instance.ClearReset(this);
                //}
            }

        }

        private void CellValueChanged(DataGridViewCell theCell)
        {
            IGridItem underlyingData = dataGridView.Rows[theCell.RowIndex].Cells[0].Value as IGridItem;
            if (underlyingData != null)
            {
                string columnName = dataGridView.Columns[theCell.ColumnIndex].Name;
                string value = dataGridView.Rows[theCell.RowIndex].Cells[theCell.ColumnIndex].Value as string;
                underlyingData.SetField(columnName, value);
                CustomGUIControls.RedisplayManager.Instance.Reset(this);
                underlyingData.Redisplay();
                CustomGUIControls.RedisplayManager.Instance.ClearReset(this);
            }

        }

        /// <summary>
        /// Set the values for the combo drop down
        /// </summary>
        private void SetDropDowns()
        {
            int colIndex = 0;
            foreach (DataGridViewColumn col in dataGridView.Columns)
            {
                DataGridViewComboBoxColumn comboCell = col as DataGridViewComboBoxColumn;
                if (comboCell != null) // If this is a combo column
                {
                    IList<string> distintValues = m_comboValues[col.Name];
                    if (distintValues == null) // If the allowable values have not beed defined, work them out from the data
                    {
                        distintValues = new List<string>();
                        foreach (DataGridViewRow row in dataGridView.Rows)
                        {
                            string value = row.Cells[colIndex].Value as string;
                            if (!string.IsNullOrEmpty(value) && !distintValues.Contains(value))
                                distintValues.Add(value);
                        }
                    }

                    if (comboCell.Items.Count == 0)
                    {
                        comboCell.Items.AddRange(distintValues.ToArray());
                        //TODO tidy
                        object z = comboCell.Items[0];
                        object q = z.GetType();

                    }
                    else
                    {

                        foreach (string item in distintValues)
                        {
                            if (!comboCell.Items.Contains(item))
                                comboCell.Items.Add(item);
                        }

                        List<string> itemsToRemove = new List<string>();
                        foreach (string item in comboCell.Items)
                        {
                            if (!distintValues.Contains(item))
                                itemsToRemove.Add(item);
                        }
                        foreach (string item in itemsToRemove)
                        {
                            comboCell.Items.Remove(item);
                        }
                    }

                    // The lines below cause a problem since after the Clear is called, an error is 
                    // produced because the cells can contains values not in the 'empty' Items !
                    //comboCell.Items.Clear();
                    //comboCell.Items.AddRange(distintValues.ToArray());

                }
                colIndex++;
            }
        }



        /// <summary>
        /// Set the possible combo values based on the data on display
        /// </summary>
        public void SetFilters()
        {
            m_needToSetFilters = true;
        }

        public void SetFilters(bool force)
        {
            if (force)
            {
                PerformSetFilters();
                m_needToSetFilters = false;
            }
            else
                m_needToSetFilters = true;
        }

        private void PerformSetFilters()
        {
            ///////////////
            bool currentIgnoreEvents = m_ignoreEvents;
            m_ignoreEvents = true;

            PlaceFilters();

            //if (m_autoHeight)
            //{
            //    int height = dataGridView.ColumnHeadersHeight;
            //    if (dataGridView.Rows.Count == 0)
            //    {
            //        height = -4;

            //    }
            //    else
            //    {
            //        dataGridView.Visible = true;
            //        foreach (DataGridViewRow row in dataGridView.Rows)
            //        {
            //            height += row.Height;

            //        }
            //    }

            //    this.Height = height + 4;
            //}

            //////////////

            SetDropDowns();
            int columns = m_filters2.Count;


            for (int columnIndex = 0; columnIndex < columns; columnIndex++)
            {
                GridFilter filter = m_filters2[columnIndex];

                string columnName = filter.Tag as string;

                string format = m_columnFormats[columnName];

                string multiValueSeparator = m_gridColumns.MultiValueSeparator(columnName);
                if (string.IsNullOrEmpty(multiValueSeparator))
                    multiValueSeparator = null;

                if (filter.FilterItems.Count() > 0)
                    continue;

                filter.Clear();

                HashSet<object> newFilterItemsRaw = new HashSet<object>();

                int rowCount = dataGridView.Rows.Count;
                int gridColumnIndex = m_fixedColumns + columnIndex;

                Type columnType = null;

                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    //if (row.Visible == false)
                    //    continue;

                    //string cellValue = null;



                    object cellValueObj = row.Cells[gridColumnIndex].Value;

                    if (cellValueObj != null && !newFilterItemsRaw.Contains(cellValueObj))
                    {
                        newFilterItemsRaw.Add(cellValueObj);

                        if (columnType == null || columnType == typeof(string))
                            columnType = cellValueObj.GetType();
                    }
                }

                System.Comparison<object> sortFn;
                List<object> sortedRawFilterItems = new List<object>(newFilterItemsRaw);
                if (m_columnFilterSortFns.TryGetValue(columnName, out sortFn))
                    sortedRawFilterItems.Sort(sortFn);
                else if (columnType == typeof(DateTime))
                    sortedRawFilterItems.Sort(DateSort);
                else if (columnType == typeof(int) || columnType == typeof(double) || columnType == typeof(float))
                    sortedRawFilterItems.Sort(NumberSort);
                else
                    sortedRawFilterItems.Sort();



                List<string> newFilterItems = new List<string>();
                HashSet<string> newFilterItemsSoFar = new HashSet<string>();
                bool addNull = false;

                foreach (object cellValueObj in sortedRawFilterItems)
                {
                    string cellValue = null;

                    if (format == null)
                    {
                        cellValue = cellValueObj.ToString();
                    }
                    else
                    {

                        if (cellValueObj.GetType() == typeof(DateTime))
                            cellValue = ((DateTime)cellValueObj).ToString(format);
                        else
                            if (cellValueObj.GetType() == typeof(int))
                                cellValue = ((int)cellValueObj).ToString(format);
                            else
                                if (cellValueObj.GetType() == typeof(double))
                                    cellValue = ((double)cellValueObj).ToString(format);
                                else
                                    if (cellValueObj.GetType() == typeof(float))
                                        cellValue = ((float)cellValueObj).ToString(format);
                    }
                    if (string.IsNullOrEmpty(cellValue))
                        addNull = true;
                    else
                    {
                        if (!newFilterItemsSoFar.Contains(cellValue)) // newFilterItems should never contain cell Value
                        {
                            if (multiValueSeparator == null)
                            {
                                newFilterItems.Add(cellValue);
                                newFilterItemsSoFar.Add(cellValue);
                            }
                            else
                            {
                                string[] values = cellValue.Split(new string[] { multiValueSeparator }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (string value in values)
                                {
                                    if (!newFilterItemsSoFar.Contains(value))
                                    {
                                        newFilterItems.Add(value);
                                        newFilterItemsSoFar.Add(value);
                                    }
                                }
                            }
                        }
                    }
                }


                if (addNull)
                    newFilterItems.Insert(0, "");
                filter.SetValues(newFilterItems, multiValueSeparator);
            }

            m_ignoreEvents = currentIgnoreEvents;
        }

        private int DateSort(object a, object b)
        {
            DateTime? aDate = a as DateTime?;
            DateTime? bDate = b as DateTime?;
            if (aDate.HasValue && bDate.HasValue)
                return DateTime.Compare(aDate.Value, bDate.Value);
            if (aDate.HasValue)
                return -1;
            if (bDate.HasValue)
                return 1;
            return string.Compare(a.ToString(), b.ToString());
        }

        private int NumberSort(object a, object b)
        {
            double? aNum = null;
            if (a != null && (a.GetType() != typeof(string) || !string.IsNullOrEmpty(a as string)))
                try { aNum = Convert.ToDouble(a); }
                catch (Exception) { }

            double? bNum = null;
            if (b != null && (b.GetType() != typeof(string) || !string.IsNullOrEmpty(b as string)))
                try { bNum = Convert.ToDouble(b); }
                catch (Exception) { }

            if (aNum.HasValue && bNum.HasValue)
                return aNum.Value > bNum.Value ? 1 : (aNum.Value < bNum.Value ? -1 : 0);
            if (aNum.HasValue)
                return -1;
            if (bNum.HasValue)
                return 1;
            return string.Compare(a.ToString(), b.ToString());
        }


        public void GridFilter_FilterChanged(object sender, EventArgs e)
        {
            ApplyFiltersToRows();
        }



        //private void comboBox_KeyPress(object sender, KeyPressEventArgs e)
        //{
        //    if (e.KeyChar == 13)
        //    {
        //        ApplyFiltersToRows();
        //    }
        //}

        //private void comboBox_Leave(object sender, EventArgs e)
        //{
        //    ApplyFiltersToRows();
        //}


        //private void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        //{

        //    ApplyFiltersToRows();
        //}


        public IEnumerable<IDisplayItem> VisibleItems
        {
            get
            {
                List<IDisplayItem> visibleItems = new List<IDisplayItem>();
                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    if (row.Visible)
                    {
                        IDisplayItem item = row.Cells[0].Value as IDisplayItem;
                        if (item != null)
                            visibleItems.Add(item);
                    }
                }
                return visibleItems;
            }
        }

        private void ApplyFiltersToRows()
        {
            // These are all for performance to stop the grid recalculing its layout when the
            // visible state on eash row is changed
            dataGridView.SuspendLayout();
            dataGridView.Visible = false;
            ScrollBars origScrollBasrs = dataGridView.ScrollBars;
            dataGridView.ScrollBars = ScrollBars.None;



            int rowCount = dataGridView.Rows.Count;
            bool[] rowVisabilties = new bool[rowCount];
            for (int r = 0; r < rowCount; r++)
                rowVisabilties[r] = true;

            //foreach (DataGridViewRow row in dataGridView.Rows)
            //{
            //    row.Visible = true;
            //}
            int currentRow = -1;

            foreach (GridFilter filter in m_filters2)
            {

                //string exactMatchString = filter.SelectedItem != null ? filter.SelectedItem.ToString() : null;
                //bool exactMatch = !string.IsNullOrEmpty(exactMatchString);
                //string containsMatchString = exactMatch ? null : filter.Text.ToLower().Trim();

                //if (!exactMatch && string.IsNullOrEmpty(containsMatchString))
                //    continue;

                if (!filter.HasValue)
                    continue;

                string column = filter.Tag as string;
                string format = m_columnFormats[column];

                currentRow = -1;
                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    currentRow++;
                    if (rowVisabilties[currentRow] == false)
                        continue;

                    string cellValue = null;

                    object cellValueObj = row.Cells[column].Value;
                    if (cellValueObj != null)
                    {

                        cellValue = CellValueAsString(cellValueObj, format);

                    }
                    if (!filter.MatchesValues(cellValue))
                        rowVisabilties[currentRow] = false;
                    //if (cellValue == null ||
                    //    (exactMatch && cellValue != exactMatchString) ||
                    //    (!exactMatch && !cellValue.ToLower().Contains(containsMatchString)))
                    //    row.Visible = false;
                }

            }

            dataGridView.CurrentCell = null;

            currentRow = -1;
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                currentRow++;
                if (rowVisabilties[currentRow] == row.Visible)
                    continue;
                try
                {
                    row.Visible = rowVisabilties[currentRow];
                    // TO DO - This throws an exception when a change to the grid makes
                    // a row disappear, and then a change to a filter is made - why?
                }
                catch (Exception err)
                {
                    Utils.Logger.LogException(err, "Error changing row visibility");
                }

            }

            // Restore layout stuff
            dataGridView.ScrollBars = origScrollBasrs;
            dataGridView.ResumeLayout(true);
            dataGridView.Visible = true;

        }

        private string CellValueAsString(object cellValueObj, string format)
        {
            string cellValue = null;
            if (format == null)
            {
                cellValue = cellValueObj.ToString();
            }
            else
            {
                if (cellValueObj.GetType() == typeof(DateTime))
                    cellValue = ((DateTime)cellValueObj).ToString(format);
                if (cellValueObj.GetType() == typeof(int))
                    cellValue = ((int)cellValueObj).ToString(format);
                if (cellValueObj.GetType() == typeof(double))
                    cellValue = ((double)cellValueObj).ToString(format);
                if (cellValueObj.GetType() == typeof(float))
                    cellValue = ((float)cellValueObj).ToString(format);
            }

            return cellValue;
        }

        public delegate void CellDoubleClick(IDisplayItem selectedItem);
        private CellDoubleClick m_doubleClickFunction = null;
        public void SetDoubleClickFunction(CellDoubleClick doubleClickFunction)
        {
            m_doubleClickFunction = doubleClickFunction;
        }

        public delegate bool CheckCellDelete(IDisplayItem selectedItem);
        private CheckCellDelete m_checkCellDeleteFunction = null;
        public void SetCheckCellDeleteFunction(CheckCellDelete checkCellDeleteFunction)
        {
            m_checkCellDeleteFunction = checkCellDeleteFunction;
        }

        public delegate void CellDelete(IDisplayItem selectedItem);
        private CellDelete m_cellDeleteFunction = null;
        public void SetCellDeleteFunction(CellDelete cellDeleteFunction)
        {
            m_cellDeleteFunction = cellDeleteFunction;
        }

        public void SetColumnFilterSortFn(string columnName, System.Comparison<object> sortFn)
        {
            m_columnFilterSortFns[columnName] = sortFn;
        }


        public void SetColumnOrder(IList<string> columnOrder)
        {
            m_columnOrder = columnOrder;
        }
        IList<string> m_columnOrder = null;

        private void dataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (!m_ignoreEvents && e.RowIndex >= 0 && e.ColumnIndex >= 0 && !dataGridView.IsCurrentCellInEditMode)
            {
                IGridItem underlyingData = dataGridView.Rows[e.RowIndex].Cells[0].Value as IGridItem;
                if (underlyingData != null && m_doubleClickFunction != null)
                {
                    m_doubleClickFunction(underlyingData);
                }
            }
        }

        public void WindowClosed()
        {

            m_viewImplementation.WindowClosed();
            InitValues();
        }

        public void ClearRows()
        {

            dataGridView.Rows.Clear();
        }


        private void InitValues()
        {

            //m_filters = new List<ComboBox>();
            m_filters2 = new List<GridFilter>();
            m_fixedColumns = 1;
            m_displayColumns = 0;

            m_columnFormats = new Dictionary<string, string>();

            m_hiddenColumns = new List<string>();
            m_setUpDone = false;
            m_comboValues = new Dictionary<string, IList<string>>();
            dataGridView.Rows.Clear();
            //Remove all columns except the first one
            while (dataGridView.Columns.Count > 1)
            {
                dataGridView.Columns.RemoveAt(1);
            }

        }

        private void GridControl_SizeChanged(object sender, EventArgs e)
        {
            if (!m_ignoreEvents)
                PlaceFilters();
        }

        private void resetAllFiltersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (GridFilter filter in m_filters2)
            {
                filter.Clear();
            }
            ApplyFiltersToRows();
        }


        public void RemoveDisplayItem(IDisplayItem itemToDisplay)
        {
            m_viewImplementation.RemoveDisplayItem(itemToDisplay);
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                if (row.Cells[0].Value == itemToDisplay)
                {
                    dataGridView.Rows.Remove(row);
                    return;
                }
            }
        }

        private void dataGridView_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyData == Keys.Delete ||// Delete
                e.KeyData == Keys.Back) // Backspace
            {
                if (!dataGridView.IsCurrentCellInEditMode && dataGridView.SelectedCells.Count > 0)
                {
                    int rowIndex = dataGridView.SelectedCells[0].RowIndex;
                    IGridItem underlyingData = dataGridView.Rows[rowIndex].Cells[0].Value as IGridItem;
                    if (underlyingData != null && m_cellDeleteFunction != null)
                    {
                        if (m_checkCellDeleteFunction == null || m_checkCellDeleteFunction(underlyingData))
                        {
                            m_cellDeleteFunction(underlyingData);
                        }
                    }
                }
            }
        }

        private void dataGridView_Scroll(object sender, ScrollEventArgs e)
        {
            if (!m_ignoreEvents)
                PlaceFilters(true);
        }

        Point? m_dragStart = null;
        static private IGridItem m_draggedItem = null;
        public static IGridItem DraggedItem { get { return m_draggedItem; } }

        private void dataGridView_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            int rowIndex = e.RowIndex;
            if (rowIndex > -1 && rowIndex < dataGridView.Rows.Count)
            {
                m_dragStart = new Point(e.X, e.Y);
                m_draggedItem = dataGridView.Rows[rowIndex].Cells[0].Value as IGridItem;
                if (m_draggedItem == null)
                    m_dragStart = null;
            }
        }

        private void dataGridView_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            if (m_dragStart != null)
            {
                Perform_Drag_n_Drop();
            }
        }

        private void dataGridView_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            m_dragStart = null;
            m_draggedItem = null;
        }




        private void dataGridView_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            m_dragStart = null;
            m_draggedItem = null;
        }

        private void dataGridView_CellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (m_dragStart != null && m_draggedItem != null)
            {
                if (Math.Abs(m_dragStart.Value.X - e.X) > 20 ||
                    Math.Abs(m_dragStart.Value.Y - e.Y) > 20)
                {
                    Perform_Drag_n_Drop();
                }
            }
        }


        private void Perform_Drag_n_Drop()
        {
            DataObject data = Utils.DragDrop.DragHelper.SetDraggedObject(m_draggedItem);

            if (m_draggedItem.PopulateDragDropDataObject(data))
            {
                DragDropEffects effectToUse = DragDropEffects.Move;
                bool ctrlButtonDown =
                        System.Windows.Input.Keyboard.IsKeyDown(Key.LeftCtrl) ||
                        System.Windows.Input.Keyboard.IsKeyDown(Key.RightCtrl);
                if (ctrlButtonDown)
                    effectToUse = DragDropEffects.Link;

                DragDropEffects dropEffect = dataGridView.DoDragDrop(data, effectToUse);
                Utils.Logger.Log("Drag done");
                m_dragStart = null;
                m_draggedItem = null;
            }

        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView.SelectAll();

            Clipboard.Clear();

            StringBuilder clipboardText = new StringBuilder();

            foreach (DataGridViewColumn col in dataGridView.Columns)
            {
                if (col.Name == ColumnZeroName)
                    continue;
                clipboardText.AppendFormat("{0}\t", col.Name);
            }
            clipboardText.AppendFormat("\n");

            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                if (row.Visible)
                {
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        if (cell.OwningColumn.Name == ColumnZeroName)
                            continue;
                        string format = m_columnFormats[cell.OwningColumn.Name];
                        object cellValueObj = cell.Value;

                        string cellValue = Utils.Misc.RemoveInvalidCharacters(CellValueAsString(cellValueObj, format));
                        clipboardText.AppendFormat("{0}\t", cellValue);
                    }
                    clipboardText.AppendFormat("\n");
                }
            }

            Clipboard.SetData(DataFormats.Text, clipboardText.ToString());

            Application.DoEvents();
            System.Threading.Thread.Sleep(200);

            dataGridView.ClearSelection();
        }



        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void dataGridView_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            m_defaultSortColumn = dataGridView.SortedColumn.Name;
            if (dataGridView.SortOrder == SortOrder.Ascending)
                m_defaultSortDirection = ListSortDirection.Ascending;
            if (dataGridView.SortOrder == SortOrder.Descending)
                m_defaultSortDirection = ListSortDirection.Descending;
            for (int row = 0; row < dataGridView.Rows.Count; row++)
            {
                if (dataGridView.Rows[row].Visible)
                {
                    dataGridView.FirstDisplayedScrollingRowIndex = row;
                    break;
                }

            }


        }

        public IDisplayItem CurrentSelectedItem
        {
            get
            {
                if (dataGridView.CurrentRow != null)
                {
                    IGridItem underlyingData = dataGridView.Rows[dataGridView.CurrentRow.Index].Cells[0].Value as IGridItem;
                    return underlyingData as IDisplayItem;
                }
                return null;
            }
        }

        public void Select(IGridItem itemToSelect)
        {
            for (int row = 0; row < dataGridView.Rows.Count; row++)
            {
                IGridItem currentRowObject = dataGridView.Rows[row].Cells[0].Value as IGridItem;
                if (currentRowObject == itemToSelect)
                {
                    dataGridView.Rows[row].Selected = true;
                }
                else
                {
                    dataGridView.Rows[row].Selected = false;
                }
            }

        }


        private void dataGridView_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (m_needToSetFilters)
            {
                m_needToSetFilters = false;
                int horizontalScroll = dataGridView.HorizontalScrollingOffset;

                PerformSetFilters();
                ApplyFiltersToRows();
                m_needToSetFilters = false;
                dataGridView.HorizontalScrollingOffset = horizontalScroll;
            }

            if (m_needsToBeSorted && m_defaultSortColumn != null)
            {
                try
                {
                    m_needsToBeSorted = false;
                    dataGridView.Sort(dataGridView.Columns[m_defaultSortColumn], m_defaultSortDirection);

                    if (dataGridView.Rows.Count > 0)
                    {
                        for (int i = 0; i < dataGridView.Rows.Count; i++)
                        {
                            if (dataGridView.Rows[i].Visible == true)
                            {
                                dataGridView.FirstDisplayedScrollingRowIndex = i;
                                break;
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    Logger.LogException(err, "Error redrawing grid cells");
                }
            }
        }

        private void dataGridView_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            DataGridViewComboBoxEditingControl ctrl = e.Control as DataGridViewComboBoxEditingControl;

            string columnName = dataGridView.Columns[dataGridView.CurrentCell.ColumnIndex].Name;
            if (ctrl != null)
            {

                IGridItem underlyingItem = dataGridView[0, dataGridView.CurrentCell.RowIndex].Value as IGridItem;

                m_gridColumns.AdjustComboEditor(columnName, underlyingItem, ctrl, this);
                ctrl.DropDownClosed -= new EventHandler(ctrl_DropDownClosed);
                ctrl.DropDownClosed += new EventHandler(ctrl_DropDownClosed);
                //ctrl.PrepareEditingControlForEdit(true);

            }
        }


        System.Windows.Threading.DispatcherTimer m_dropDownClosedTimer = null;
        Point m_dropDownCell;
        string m_dropDownText;


        void ctrl_DropDownClosed(object sender, EventArgs e)
        {
            DataGridViewComboBoxEditingControl ctrl = sender as DataGridViewComboBoxEditingControl;
            if (ctrl != null && ctrl.DropDownStyle == ComboBoxStyle.DropDownList)
            {
                DataGridViewCell cell = dataGridView.CurrentCell;
                m_dropDownCell = new Point(cell.RowIndex, cell.ColumnIndex);
                m_dropDownText = ctrl.Text;


                // A timer is needed, not sure why, it seems some events
                // have not been completed to do with the combo box drop down
                // closing.  
                // It was found, that doing the update to the data in a Timer call
                // means all the combo box edit related events had completed and
                // issue to do with the appearance of an uncommitted blank row at
                // the bottom of the grid went away.
                m_dropDownClosedTimer = new System.Windows.Threading.DispatcherTimer();
                m_dropDownClosedTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
                m_dropDownClosedTimer.Tick += new EventHandler(m_dropDownClosedTimer_Tick);
                m_dropDownClosedTimer.Start();
            }
        }

        void m_dropDownClosedTimer_Tick(object sender, EventArgs e)
        {
            bool currentIgnoreEvents = m_ignoreEvents;
            m_ignoreEvents = true;

            m_dropDownClosedTimer.Stop();
            m_dropDownClosedTimer = null;
            dataGridView.CurrentCell = null;

            DataGridViewCell affectedCell = dataGridView.Rows[m_dropDownCell.X].Cells[m_dropDownCell.Y];
            affectedCell.Value = m_dropDownText;

            m_ignoreEvents = currentIgnoreEvents;

            CellValueChanged(affectedCell);
        }

        void ctrl_TextChanged(object sender, EventArgs e)
        {
            DataGridViewComboBoxEditingControl ctrl = sender as DataGridViewComboBoxEditingControl;
            if (ctrl != null)
            {
                dataGridView.Focus();
            }
        }

        private void showFilterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            m_filterIsVisible = !m_filterIsVisible;
            showFilterToolStripMenuItem.Checked = m_filterIsVisible;
            PlaceFilters();
        }

        private void dataGridView_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {

        }

        private int SortByDisplayOrder(DataGridViewColumn a, DataGridViewColumn b)
        {
            return a.DisplayIndex - b.DisplayIndex;
        }


        List<DataGridViewColumn> ColumnsInDisplayOrder
        {
            get
            {
                List<DataGridViewColumn> columnsSortedFordisplayOrder = new List<DataGridViewColumn>();
                foreach (DataGridViewColumn currentColumn in dataGridView.Columns)
                    if (currentColumn.Visible)
                        columnsSortedFordisplayOrder.Add(currentColumn);

                columnsSortedFordisplayOrder.Sort(SortByDisplayOrder);

                return columnsSortedFordisplayOrder;
            }

        }

        private void dataGridView_ColumnDisplayIndexChanged(object sender, DataGridViewColumnEventArgs e)
        {
            List<GridFilter> newFilterOrder = new List<GridFilter>();




            foreach (DataGridViewColumn currentColumn in ColumnsInDisplayOrder)
            {
                GridFilter filterForCurrentColumn = null;
                foreach (GridFilter filterToCheck in m_filters2)
                {
                    if ((filterToCheck.Tag) as string == currentColumn.Name)
                    {
                        filterForCurrentColumn = filterToCheck;
                        break;
                    }
                }
                if (filterForCurrentColumn != null)
                    newFilterOrder.Add(filterForCurrentColumn);

            }
            m_filters2 = newFilterOrder;
            groupBoxFilters.Controls.Clear();
            groupBoxFilters.Controls.AddRange(m_filters2.ToArray());



            PlaceFilters();
        }




        IGridItem m_lastDraggedEnterObject = null;
        private void dataGridView_DragEnter(object sender, DragEventArgs e)
        {
            Utils.Logger.Log("Drag Enter");

            dataGridView_DragOver(sender, e);

            //Point localMouse = dataGridView.PointToClient(new Point(e.X, e.Y));

            //DataGridView.HitTestInfo mouseHit = dataGridView.HitTest(localMouse.X, localMouse.Y);
            //int row = mouseHit.RowIndex;
            //int col = mouseHit.ColumnIndex;

            //if (row >= 0 && col >= 0)
            //{
            //    m_lastDraggedEnterObject = dataGridView.Rows[row].Cells[0].Value as IGridItem;
            //    m_lastDraggedEnterObject.GridCellDragEnter(e);

            //    Utils.Logger.Log("Enter Task " + m_lastDraggedEnterObject.Description);
            //}

        }

        private void dataGridView_DragDrop(object sender, DragEventArgs e)
        {
            if (m_lastDraggedEnterObject != null)
                m_lastDraggedEnterObject.GridCellDragDrop(e);
        }

        private void dataGridView_DragLeave(object sender, EventArgs e)
        {
            if (m_lastDraggedEnterObject != null)
            {
                m_lastDraggedEnterObject.GridCellDragLeave(e);
                m_lastDraggedEnterObject = null;
            }
        }


        private void dataGridView_DragOver(object sender, DragEventArgs e)
        {
            Utils.Logger.Log("Dragging over");


            Point localMouse = dataGridView.PointToClient(new Point(e.X, e.Y));

            DataGridView.HitTestInfo mouseHit = dataGridView.HitTest(localMouse.X, localMouse.Y);
            int row = mouseHit.RowIndex;
            int col = mouseHit.ColumnIndex;

            if (row >= 0 && col >= 0)
            {
                IGridItem dragOverItem = dataGridView.Rows[row].Cells[0].Value as IGridItem;
                if (dragOverItem != m_lastDraggedEnterObject)
                {
                    if (m_lastDraggedEnterObject != null)
                        m_lastDraggedEnterObject.GridCellDragLeave(e);

                    m_lastDraggedEnterObject = dragOverItem;
                    m_lastDraggedEnterObject.GridCellDragEnter(e);

                    Utils.Logger.Log("Cell Enter " + m_lastDraggedEnterObject.ObjectDescription);
                }
            }

        }

        private void dataGridView_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            string m = e.Exception.Message;
            //DataGridViewCell thecell =  dataGridView[e.ColumnIndex, e.RowIndex];

            if (m == "DataGridViewComboBoxCell value is not valid.")
                e.ThrowException = false;

        }


    }
}


