using System;
using System.Windows;
using System.Collections.Generic;
using System.Text;

namespace PlanDisplay
{
    abstract public class TimeBox : System.Windows.Controls.UserControl
    {
        public enum Event { MouseEnter, MouseLeave, MouseMove, MouseDoubleClick, CtrlMouseDoubleClick, DayShift };
        public delegate void EventFn(object underlyingObject, object underlyingObjectParentProject, Event e, object data);

        abstract public DateTime? MinStartDate { get; }

        protected TimeBox(DBProjectPal.ITaskOrProject underlyingObject, PlanControl theOwningPlanControl)
        {
            m_underlyingTaskOrProject = underlyingObject;
            m_theOwningPlanControl = theOwningPlanControl;
        }

        protected TimeBox(object underlyingObject, PlanControl theOwningPlanControl)
        {
            m_underlyingObject = underlyingObject;
            m_theOwningPlanControl = theOwningPlanControl;
        }


        abstract public DateTime? MaxEndDate { get; }

        abstract public double TotalHeight { get; }
        abstract public double ScaledTotalHeight { get; }

        abstract public Point? LeftDependencyNode { get; }
        abstract public Point? RightDependencyNode { get; }

        abstract public void Redisplay();
        abstract public List<TimeBox> Redisplay(double yScale, double yScaleCumulative);

        internal object UnderlyingObject { get { return m_underlyingTaskOrProject ?? m_underlyingObject; } }

        internal DBProjectPal.ITaskOrProject m_underlyingTaskOrProject = null;
        internal object m_underlyingObject = null;

        protected PlanControl m_theOwningPlanControl;

        abstract public int ZOrder { get; set; }

        abstract new public bool IsVisible { get; }

        public void SetEventFunction(EventFn fn)
        {
            m_eventFunction = fn;
        }

        protected void SendEvent(Event e)
        {
            SendEvent(e, null);
        }

        protected void SendEvent(Event e, object data)
        {
            if (m_eventFunction != null)
            {
                object underlyingObjectParent = null;
                if (m_parentProject != null)
                    underlyingObjectParent = m_parentProject.UnderlyingObject;
                m_eventFunction(UnderlyingObject, underlyingObjectParent, e, data);
            }
        }

        protected EventFn m_eventFunction = null;


        internal List<DBProjectPal.ITaskOrProject> GetDependants()
        {
            if (m_underlyingTaskOrProject == null)
                return new List<DBProjectPal.ITaskOrProject>();

            return m_underlyingTaskOrProject.PostDependencies;

        }


        internal List<Project> AllParents
        {
            get
            {
                List<Project> parents = new List<Project>();
                if (m_parentProject == null)
                    return parents;
                parents.Add(m_parentProject);
                parents.AddRange(m_parentProject.AllParents);
                return parents;
            }
        }


        internal Project ParentProject
        {
            get { return m_parentProject; }
            set { m_parentProject = value; }
        }
        protected Project m_parentProject = null;

    }
}
