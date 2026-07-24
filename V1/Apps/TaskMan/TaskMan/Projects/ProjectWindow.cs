using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TaskMan.Projects
{
    public partial class ProjectWindow : Form
    {

        private IList<CustomGUIControls.Grid.IGridItem> m_projects;

        public ProjectWindow(IList<CustomGUIControls.Grid.IGridItem> projectsToDisplay)
        {
            InitializeComponent();
            gridControl.FilterIsVisible = true;

            List<string> columnOrder = new List<string>();
            columnOrder.Add(Projects.GUIProjectColumns.s_Name);
            columnOrder.Add(Projects.GUIProjectColumns.s_Priority);
            columnOrder.Add(Projects.GUIProjectColumns.s_ParentName);

            gridControl.SetColumnOrder(columnOrder);


            m_projects = projectsToDisplay;


            gridControl.SetColumns(GUIProjectColumns.Instance);

            foreach (CustomGUIControls.Grid.IGridItem project in m_projects)
            {
                gridControl.AddDisplayItem(project);
            }

            gridControl.ColumnWidth(Projects.GUIProjectColumns.s_Name, 200);
            gridControl.ColumnWidth(Projects.GUIProjectColumns.s_Priority, 80);
            gridControl.ColumnWidth(Projects.GUIProjectColumns.s_ParentName, 325);
            gridControl.ColumnWidth(Projects.GUIProjectColumns.s_IsActive, 70);
            gridControl.ColumnWidth(Projects.GUIProjectColumns.s_TotalActiveTaskCount, 75);
            gridControl.ColumnWidth(Projects.GUIProjectColumns.s_TotalActiveTaskEffort, 50);
            gridControl.ColumnWidth(Projects.GUIProjectColumns.s_Owner, 75);
            gridControl.ColumnWidth(Projects.GUIProjectColumns.s_StartDate, 75);
            gridControl.ColumnWidth(Projects.GUIProjectColumns.s_DueDate, -1);


            List<string> IsActiveFilterValues = new List<string>();
            IsActiveFilterValues.Add(GUIProject.s_isActiveTrueString);

            gridControl.SetDefaultFilterValues(Projects.GUIProjectColumns.s_IsActive, IsActiveFilterValues);

            gridControl.SetDefaultSort(Projects.GUIProjectColumns.s_Priority, ListSortDirection.Descending);
        }


        public void SetGridDoubleClickFunction(CustomGUIControls.Grid.GridControl.CellDoubleClick doubleClickFunction)
        {
            gridControl.SetDoubleClickFunction(doubleClickFunction);
        }

       
    }
}
