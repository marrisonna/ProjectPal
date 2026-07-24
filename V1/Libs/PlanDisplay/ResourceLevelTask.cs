using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanDisplay
{
    internal class ResourceLevelTask
    {
        public ResourceLevelTask(DateTime startDate, DateTime endDate,
                                 double? effortInDays,
                                 List<string> resources,
                                 PlanDisplay.PriorityValue priority,
                                 PlanDisplay.StatusValue status)
        {
            StartDate = startDate;
            EndDate = endDate;
            EffortInDays = effortInDays;
            Resources = resources;
            Priority = priority;
            Status = status;
        }

        public ResourceLevelTask(double effortInDays,
                                 List<string> resources,
                                 PlanDisplay.PriorityValue priority,
                                 PlanDisplay.StatusValue status)
        {
            StartDate = null;
            EndDate = null;
            EffortInDays = effortInDays;
            Resources = resources;
            Priority = priority;
            Status = status;
        }



        public DateTime? StartDate { get; private set; }
        public DateTime? EndDate { get; private set; }
        public double? EffortInDays { get; private set; }
        public List<string> Resources { get; private set; }
        public PlanDisplay.PriorityValue Priority { get; private set; }
        public PlanDisplay.StatusValue Status { get; private set; }


    }
}
