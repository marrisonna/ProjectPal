using System;
using System.Collections.Generic;
using DBTaskMan;
using System.Text;

namespace TaskMan
{
    class FormTaskMerge : FormMerge
    {

        public FormTaskMerge(Task currentTask, Task newTask) :
            base(currentTask, newTask)
        {

            AddField("Description", "Description", null);
            AddField("DetailedDescription", "Details", null);
            AddField("RequestedBy", "Requester", GetPersonName);
            AddField("AffectedComponent", "Component", GetComponentName);
            AddField("Priority", "Priority", GetPriority);
            AddField("DueDate", "Due Date", GetDate);

            AddField("EffortInDays", "Effort", GetDouble);
            AddField("EffortType", "Effort Type", GetEffortType);
            AddField("PercentageAllocation", "% Allocation", GetPercentage);

            AddField("TaskType", "Task Type", GetTaskType);

            AddField("Status", "Status", GetStatus);

            Text = "Task - Merge conflicting changes";



            Text = "Merge conflicting changes: Task '" + currentTask.Description + "'";


        }

    }
}
