using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DBProjectPal;
using CustomGUIControls;
using ProjectPal.Tasks;
using ProjectPal.Components;
using ProjectPal.Projects;

namespace ProjectPal
{
    public partial class FormFind : Form
    {
        public FormFind()
        {
            InitializeComponent();
            gridControlResults.FilterIsVisible = true;
            gridControlResults.SetFilters();
            gridControlResults.SetDoubleClickFunction(DoubleClickFunction);
        }


        static public void DoubleClickFunction(IDisplayItem objectToDisplay)
        {
            FindResult foundItem = objectToDisplay as FindResult;
            if (foundItem != null)
            {
                if (foundItem.FoundItem is Task)
                {
                    Task theTask = foundItem.FoundItem as Task;
                    if (theTask != null)
                    {
                        GUITask theGuiTask = GUITask.GetInstanceFromDBTask(theTask);
                        ProjectPal.Tasks.TaskDetail.ShowDetailWindow(theGuiTask);
                    }
                }

                if (foundItem.FoundItem is Remark)
                {
                    Remark theRemark = foundItem.FoundItem as Remark;
                    if (theRemark != null)
                    {
                        GUITask theGuiTask = GUITask.GetInstanceFromDBTask(theRemark.Task);
                        TaskDetail window = ProjectPal.Tasks.TaskDetail.GetAndShowDetailWindow(theGuiTask);
                        window.ShowRemark(ProjectPal.Remarks.GUIRemark.GetInstanceFromDBRemark(theRemark));
                    }

                    Task theTask = foundItem.FoundItem as Task;
                    if (theTask != null)
                    {
                        GUITask theGuiTask = GUITask.GetInstanceFromDBTask(theTask);
                        ProjectPal.Tasks.TaskDetail.ShowDetailWindow(theGuiTask);
                    }
                }

                if (foundItem.FoundItem is DBProjectPal.Component)
                {
                    DBProjectPal.Component theComponent = foundItem.FoundItem as DBProjectPal.Component;
                    if (theComponent != null)
                    {
                        GUIComponent theGuiComponent = GUIComponent.GetInstanceFromDBComponent(theComponent);
                        ComponentWindow newWindow = ComponentWindow.GetInstanceFromGUIComponent(theGuiComponent);
                        newWindow.Show();
                        newWindow.Focus();
                    }
                }
                if (foundItem.FoundItem is DBProjectPal.Project)
                {
                    DBProjectPal.Project theProject = foundItem.FoundItem as DBProjectPal.Project;
                    if (theProject != null)
                    {
                        GUIProject theGuiProject = GUIProject.GetInstanceFromDBProject(theProject);
                        ProjectDetail.GetAndShowDetailWindow(theGuiProject);
                    }
                }



            }

        }

        public string[] SearchText
        {
            get
            {
                return textBoxSearchText.Text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        public bool SearchRemarks { get { return checkBoxRemarks.Checked; } }
        public bool SearchTasks { get { return checkBoxTasks.Checked; } }
        public bool SearchProjects { get { return checkBoxProjects.Checked; } }
        public bool SearchComponents { get { return checkBoxComponents.Checked; } }
        public bool SearchAttachements { get { return checkBoxAttachments.Checked; } }
        public bool IncludeClosedItems { get { return checkBoxClosedItems.Checked; } }

        private void buttonFind_Click(object sender, EventArgs e)
        {
            //DBAccess.DBObjectBase.EnableVisibleInstanceCache(true);
            int errorCount = 0;
            try
            {

                string[] seachTextItems = SearchText;
                for (int i = 0; i < seachTextItems.Count(); i++)
                {
                    seachTextItems[i] = seachTextItems[i].ToLower();
                }


                List<FindResult> foundItems = new List<FindResult>();
                bool searchAttachments = SearchAttachements;
                bool seachClosedItems = IncludeClosedItems;
                List<ISearchable> sourceItems = new List<ISearchable>();

                if (SearchTasks)
                    sourceItems.AddRange(DBProjectPal.Task.AllInstances);
                if (SearchComponents)
                    sourceItems.AddRange(DBProjectPal.Component.AllInstances);
                if (SearchProjects)
                    sourceItems.AddRange(DBProjectPal.Project.AllInstances);
                if (SearchRemarks)
                    sourceItems.AddRange(DBProjectPal.Remark.AllInstances);

                FormProgress progress = new FormProgress();
                progress.StartPosition = FormStartPosition.Manual;
                progress.Location = new Point(this.Location.X + (this.Width - progress.Width) / 2,
                                              this.Location.Y + (this.Height - progress.Height) / 2);
                progress.Height = 50;
                progress.Show(this);
                System.Windows.Forms.Application.DoEvents();

                int totalCount = sourceItems.Count;
                int totalProcessed = 0;
                int logPeriod = 25;

                Utils.Logger.Log("Searching " + totalCount + " items");

                foreach (ISearchable sourceItem in sourceItems)
                {
                    if (totalProcessed % logPeriod == 0)
                        Utils.Logger.Log("Searched " + totalProcessed + " items");


                    totalProcessed++;
                    progress.Progress = 100 * totalProcessed / totalCount;

                    try
                    {
                        if (!seachClosedItems && sourceItem.IsClosed)
                            continue;

                        List<string> attachmentSearch = new List<string>();
                        bool foundAll = true;
                        foreach (string searchValue in seachTextItems)
                        {
                            if (!sourceItem.ContainsText(searchValue))
                            {
                                foundAll = false;
                                if (!searchAttachments)
                                    break;
                                attachmentSearch.Add(searchValue);
                            }
                        }
                        if (!foundAll && searchAttachments)
                        {
                            foreach (Attachment att in sourceItem.Attachments)
                            {
                                string[] foundValues;
                                try
                                {
                                    foundValues = att.ContainsText(attachmentSearch.ToArray());
                                }
                                catch (Exception err)
                                {
                                    errorCount++;
                                    Utils.Logger.LogException(err, "Error searching attachments for item: " + sourceItem.ObjectDescription +
                                        " : " + err.Message);
                                    continue;
                                }
                                foreach (string foundValue in foundValues)
                                    attachmentSearch.Remove(foundValue);
                                if (attachmentSearch.Count == 0)
                                    break;
                            }
                            if (attachmentSearch.Count == 0)
                                foundAll = true;
                        }

                        if (foundAll)
                            foundItems.Add(new FindResult(sourceItem));

                    }
                    catch (Exception err)
                    {
                        errorCount++;
                        Utils.Logger.LogException(err, "Error searching item: " + sourceItem.ObjectDescription +
                            " : " + err.Message);
                    }
                }

                Utils.Logger.Log("Searched " + totalProcessed + " items");

                progress.Close();

                gridControlResults.ClearRows();


                gridControlResults.SetColumns(FindResultColumns.Instance);

                foundItems.Sort(SortFn);

                foreach (FindResult foundItem in foundItems)
                {
                    gridControlResults.AddDisplayItem(foundItem);
                }
                gridControlResults.ColumnWidth(FindResultColumns.s_Description, 250);
                gridControlResults.SetFilters();

                this.Height = 120 + gridControlResults.ColumnHeaderHeight + Math.Min(10, foundItems.Count) * (1 + gridControlResults.RowHeight);

            }
            catch (Exception err)
            {
                errorCount++;
                Utils.Logger.LogException(err, "Error performing Find: " + err.Message);
            }
            if (errorCount > 0)
                MessageBox.Show("There were " + errorCount + " errors while performing 'Find'. See log for details.", "Find Errors");
        }



        private static List<string> s_typeOrder = new List<string>(new string[] { "Task", "Component", "Project", "Remark" });

        private static int SortFn(FindResult a, FindResult b)
        {

            if (a.Colour != Color.White &&
                b.Colour == Color.White)
                return -1;

            if (a.Colour != b.Colour)
                return 1;


            int aIndex = s_typeOrder.IndexOf(a.TypeString);
            int bIndex = s_typeOrder.IndexOf(b.TypeString);
            if (aIndex < bIndex)
                return -1;
            if (aIndex > bIndex)
                return 1;


            return a.Description.CompareTo(b.Description);

        }

    }
}
