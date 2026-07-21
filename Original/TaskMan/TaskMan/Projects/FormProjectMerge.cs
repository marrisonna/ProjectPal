using System;
using System.Collections.Generic;
using DBTaskMan;
using System.Text;

namespace TaskMan
{
    class FormProjectMerge : FormMerge
    {

        public FormProjectMerge(Project currentProject, Project newProject) :
            base(currentProject, newProject)
        {
            AddField("Name", "Name", null);
            AddField("DetailedDescription", "Details", null);
            AddField("Priority", "Priority", GetPriority);
            //AddField("StartDate", "Start Date", GetDate);
            AddField("DueDate", "Due Date", GetDate);


            Text = "Merge conflicting changes: Project '" + currentProject.FullName + "'";
        }
    }
}
