using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using DBTaskMan;
using System.Text;
using System.Windows.Forms;

namespace TaskMan.People
{
    public partial class FormManagePeople : Form
    {
        public FormManagePeople(MainWindow theMainWindow)
        {
            m_theMainWindow = theMainWindow;
            InitializeComponent();
        }

        private MainWindow m_theMainWindow;

        private void FormManageUsers_Load(object sender, EventArgs e)
        {

            List<string> columnOrder = new List<string>();
            columnOrder.Add("Name");
            columnOrder.Add("IsActive");
            columnOrder.Add("IsResource");
            columnOrder.Add("DBLogin");
            columnOrder.Add("UserType");
            columnOrder.Add("Colour");


            gridControlPeople.FilterIsVisible = true;

            gridControlPeople.SetColumnOrder(columnOrder);

            gridControlPeople.AllowCellDrop = true;
            gridControlPeople.SetColumns(GUIPersonColumns.Instance);

            Redisplay();
        }

        private void Redisplay()
        {
            gridControlPeople.ClearRows();

            foreach (Person currentPerson in Person.AllInstances)
            {
                gridControlPeople.AddDisplayItem(GUIPerson.GetInstanceFromDPerson(currentPerson));


            }

            gridControlPeople.SetFilters();

        }

        private void FormManagePeople_FormClosed(object sender, FormClosedEventArgs e)
        {
            m_theMainWindow.m_windowManagePeopleInstance = null;
        }

        private void buttonNewPerson_Click(object sender, EventArgs e)
        {
            FormNewUser newUserForm = new FormNewUser();
            if (newUserForm.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                Person newPerson = Person.AddNewInstance(newUserForm.NewPersonName);
                newPerson.IsActive = true;
                Redisplay();
            }
        }

        private void buttonDeletePerson_Click(object sender, EventArgs e)
        {
            GUIPerson selectedPerson = gridControlPeople.CurrentSelectedItem as GUIPerson;
            if (selectedPerson != null)
            {
                List<Task> ownedTasks = new List<Task>();
                List<Task> requestedTasks = new List<Task>();
                List<Task> resourcedTasks = new List<Task>();
                List<Project> ownedProjects = new List<Project>();

                Person personToDelete = selectedPerson.DBPerson;
                foreach (Task currentTask in Task.AllInstances)
                {
                    if (currentTask.Owner == personToDelete.DBLogin)
                        ownedTasks.Add(currentTask);

                    if (currentTask.RequestedBy == personToDelete)
                        requestedTasks.Add(currentTask);

                    foreach (IResource currentResource in currentTask.Resources)
                    {
                        if (currentResource.Name == personToDelete.Name)
                        {
                            resourcedTasks.Add(currentTask);
                            break;
                        }
                    }
                }
                foreach (Project currentProject in Project.AllInstances)
                {
                    if (currentProject.Owner == personToDelete.DBLogin)
                        ownedProjects.Add(currentProject);
                }

                if (ownedTasks.Count == 0 &&
                    requestedTasks.Count == 0 &&
                    resourcedTasks.Count == 0 &&
                    ownedProjects.Count == 0)
                {
                    FormNewUser newUserForm = new FormNewUser(selectedPerson.Name);
                    if (newUserForm.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                    {
                        selectedPerson.DBPerson.DeleteInstance();
                        Redisplay();
                    }
                }
                else
                {
                    string message = "Cannot delete '"+ personToDelete.Name+"' because..." + Environment.NewLine;
                    if (ownedTasks.Count > 0)
                    {
                        message += "This person owns "+ ownedTasks.Count+ " Tasks" + Environment.NewLine;
                    }
                    if (requestedTasks.Count > 0)
                    {
                        message += "This person is the requestor of " + requestedTasks.Count + " Tasks" + Environment.NewLine;
                    }
                    if (resourcedTasks.Count > 0)
                    {
                        message += "This person is assigned as a resource on " + resourcedTasks.Count + " Tasks" + Environment.NewLine;
                    }
                    if (ownedProjects.Count > 0)
                    {
                        message += "This person owns " + ownedProjects.Count + " Projects" + Environment.NewLine;
                    }

                    MessageBox.Show(this, message, "Cannot delete this Person");
                }

            }
        }
    }
}
