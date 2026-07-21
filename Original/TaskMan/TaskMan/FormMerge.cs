using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Windows.Controls;
using DBTaskMan;

namespace TaskMan
{
    public partial class FormMerge : Form
    {
        protected FormMerge(DBAccess.DBObjectBase currentInstance, DBAccess.DBObjectBase newInstance)
        {
            InitializeComponent();

            m_currentDBInstance = currentInstance;
            m_newInstance = newInstance;

            m_mainPanel = new StackPanel();
            m_mainPanel.Orientation = System.Windows.Controls.Orientation.Vertical;

            elementHostMainArea.Child = m_mainPanel;

            elementHostMainArea.Margin = new System.Windows.Forms.Padding(0);



        }

        public void ShowDialogIfMergeIsRequired()
        {
            bool hasConflicts = false;
            foreach (UserControlMergeField fieldControl in m_fieldControls)
            {
                if (fieldControl.UseYours != UserControlMergeField.MergeredVersion.Either)
                {
                    hasConflicts = true;
                    break;
                }
            }


            if (!hasConflicts)
            {
                m_currentDBInstance.MergeBase(m_newInstance, true);
            }
            else
                ShowDialog();
            
        }

      


        protected delegate string GetStringValue(object sourceValue);


        protected void AddField(string property, string propertyDisplayLabel, GetStringValue stringValueFunction)
        {


            System.Reflection.PropertyInfo thisProperty = null;
            foreach (System.Reflection.PropertyInfo currentProperty in m_currentDBInstance.GetType().GetProperties())
            {
                if (currentProperty.Name == property)
                {
                    thisProperty = currentProperty;
                    break;
                }
            }
            if (thisProperty != null)
            {
                string currentDBValue;
                string newValue;

                if (stringValueFunction != null)
                {
                    currentDBValue = stringValueFunction(thisProperty.GetValue(m_currentDBInstance, null));
                    newValue = stringValueFunction(thisProperty.GetValue(m_newInstance, null));
                }
                else
                {
                    currentDBValue = thisProperty.GetValue(m_currentDBInstance, null) as string;
                    newValue = thisProperty.GetValue(m_newInstance, null) as string;
                }

                UserControlMergeField field = new UserControlMergeField(propertyDisplayLabel, currentDBValue, newValue,
                                                                        UserControlMergeField.MergeredVersion.Theirs, thisProperty);
                m_mainPanel.Children.Add(field);
                m_fieldControls.Add(field);
            }
        }

        List<UserControlMergeField> m_fieldControls = new List<UserControlMergeField>();

        static protected string GetPersonName(object source)
        {
            Person currentPerson = source as Person;
            if (currentPerson != null)
                return currentPerson.Name;
            return null;

        }

        static protected string GetComponentName(object source)
        {
            DBTaskMan.Component currentComponent = source as DBTaskMan.Component;
            if (currentComponent != null)
                return currentComponent.FullName;
            return null;

        }

        static protected string GetPriority(object source)
        {
            PriorityValue? currentPriority = source as PriorityValue?;
            if (currentPriority.HasValue)
                return Tasks.GUITaskColumns.PriorityString(currentPriority);
            return null;

        }

        static protected string GetDate(object source)
        {
            DateTime? currentDueDate = source as DateTime?;
            if (currentDueDate.HasValue)
            {
                return currentDueDate.Value.ToString("dd-MMMM-yyyy");
            }
            return null;
        }


        static protected string GetDouble(object source)
        {
            double? currentDouble = source as double?;
            if (currentDouble.HasValue)
            {
                return currentDouble.Value.ToString("#.00");
            }
            return null;
        }

        static protected string GetPercentage(object source)
        {
            double? currentDouble = source as double?;
            if (currentDouble.HasValue)
            {
                return currentDouble.Value.ToString("P0");
            }
            return null;
        }

        static protected string GetTaskType(object source)
        {
            TaskTypeValue? currentTaskType = source as TaskTypeValue?;
            if (currentTaskType.HasValue)
                return currentTaskType.Value.ToString();
            return null;
        }

        static protected string GetEffortType(object source)
        {
            EffortTypeValue? currentEffortType = source as EffortTypeValue?;
            if (currentEffortType.HasValue)
                return currentEffortType.Value.ToString();
            return null;
        }

        static protected string GetStatus(object source)
        {
            StatusValue? currentStatus = source as StatusValue?;
            if (currentStatus.HasValue)
                return currentStatus.Value.ToString();
            return null;
        }


        private DBAccess.DBObjectBase m_currentDBInstance;
        private DBAccess.DBObjectBase m_newInstance;

        private void SetScreenPositions()
        {
            UserControlMergeField firstControl = (UserControlMergeField)m_mainPanel.Children[0];
            int y = labelFieldName.Location.Y;
            labelYour.Location = new Point(firstControl.Xyour, y);
            labelOther.Location = new Point(firstControl.Xother, y);
            labelMerged.Location = new Point(firstControl.Xmerged, y);

            int yForButtons = 4 + elementHostMainArea.Location.Y + elementHostMainArea.Height;

            int buttonSpace = (this.Width - buttonCancel.Width - buttonOK.Width) / 3;
            buttonOK.Location = new Point(buttonSpace, yForButtons);

            buttonCancel.Location = new Point(buttonSpace * 2 + buttonOK.Width, yForButtons);
        }

        StackPanel m_mainPanel = null;

        private void elementHost1_ChildChanged(object sender, System.Windows.Forms.Integration.ChildChangedEventArgs e)
        {

        }

        private void FormTaskMerge_Load(object sender, EventArgs e)
        {
            SetScreenPositions();


        }

        private void FormTaskMerge_Resize(object sender, EventArgs e)
        {
            SetScreenPositions();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            bool allValuseAreDBValues = true;
            foreach (UserControlMergeField fieldControl in m_fieldControls)
            {
                switch (fieldControl.UseYours)
                {
                    case UserControlMergeField.MergeredVersion.Theirs:
                        {
                            object newValue = fieldControl.Property.GetValue(m_newInstance, null);
                            fieldControl.Property.SetValue(m_currentDBInstance, newValue, null);
                            break;
                        }
                    case UserControlMergeField.MergeredVersion.Yours:
                        {
                            object currentValue = fieldControl.Property.GetValue(m_currentDBInstance, null);
                            fieldControl.Property.SetValue(m_newInstance, currentValue, null);
                            allValuseAreDBValues = false;
                            break;
                        }
                    case UserControlMergeField.MergeredVersion.Either:
                        {
                            // nothing to do
                            break;
                        }
                }
            }
            m_currentDBInstance.MergeBase(m_newInstance, allValuseAreDBValues);
            Close();
        }
    }
}
