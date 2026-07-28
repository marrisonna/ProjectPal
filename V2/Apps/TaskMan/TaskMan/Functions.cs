using System;
using System.Collections.Generic;
using System.Text;
using DBTaskMan;
using Utils;

namespace TaskMan
{
    class Functions
    {
        // Return of 100 = Medium Priority on average
        // First item in list is least significant, the last item is most significant
        public static double PriorityValue(List<PriorityValue?> priorityList)
        {
            List<PriorityValue?> priorityListHighestFirst = new List<DBTaskMan.PriorityValue?>(priorityList);
            priorityListHighestFirst.Reverse();
            double numeratorSum = 0;
            double denominatorSum = 0;
            double count = 1;
            double increment = 1;
            int priorityInt = 3;
            int? firstPriorityInt = null;
            foreach (PriorityValue? priority in priorityListHighestFirst)
            {
                priorityInt = priority.HasValue ? (int)priority.Value : 3;
                if (priorityInt < 1)
                    continue;

                if (!firstPriorityInt.HasValue)
                    firstPriorityInt = priorityInt;

                double denominator = Math.Sqrt(1 / count);

                double numerator = denominator * ((priorityInt - 3) * 25 + 100);

                numeratorSum += numerator;
                denominatorSum += denominator;

                count += increment;

            }
            if (count == 1 || !firstPriorityInt.HasValue)
                return 100;

            double factor = (1.0 + (firstPriorityInt.Value - 3.0) / 4.0);
            return numeratorSum / denominatorSum  * factor;
        }


        private static DateTime? m_lastClearVisibleCacheTime = null;

        public static void ClearDisplayCaches()
        {
            CustomGUIControls.RedisplayManager.Instance.Reset();
            DBTaskMan.Functions.ClearDateCaches();
            if (ApplicationTaskMan.Instance.LastDBUpdateTime > m_lastClearVisibleCacheTime)
            {
                m_lastClearVisibleCacheTime = ApplicationTaskMan.Instance.LastDBUpdateTime;
                DBAccess.DBObjectBase.ClearVisibleInstanceCache();
            }
            else if(m_lastClearVisibleCacheTime == null)
            {

                m_lastClearVisibleCacheTime = ApplicationTaskMan.Instance.LastDBUpdateTime;
            }
        }


        public static void PopulateComboWithCurrentUsers(System.Windows.Forms.ComboBox comboBoxUsers,
                                                         bool includeReadOnlyUsers)
        {
            comboBoxUsers.Items.Clear();
            foreach (DBTaskMan.Person currentPerson in DBTaskMan.Person.AllActiveInstances)
            {
                if (string.IsNullOrEmpty(currentPerson.DBLogin))
                    continue;
                if (!includeReadOnlyUsers && currentPerson.UserType == Permissions.UserLevel.ReadOnlyUser)
                    continue;
                comboBoxUsers.Items.Add(currentPerson.Name);

            }
        }


        public static bool AdminPassword(string password)
        {
            string password2 = password.ToLower().Trim();

            return (password2.Length == 6 &&
               password2[0] - 32 == 65 + 13 &&
               password2[1] - 32 == 65 &&
               password2[2] - 32 == 65 + 12 &&
               password2[0] == password[3] &&
               password2[1] == password[4] &&
               password2[2] == password[5]);
        }

        public static void AppendAdminPassword(ref string s)
        {
            if (s == "-SuperUser " || s == "-RunAs ")
            {
                s += (char)(65 + 13 + 32);
                s += (char)(65 + 32);
                s += (char)(65 + 12 + 32);
                s += (char)(65 + 13 + 32);
                s += (char)(65 + 32);
                s += (char)(65 + 12 + 32);
            }

        }

        static public object ToGUIObjectIfPossible(object source)
        {
            if (source is TaskMan.Tasks.GUITask ||
                source is TaskMan.Projects.GUIProject ||
                source is TaskMan.Components.GUIComponent)
                return source;

            if (source is DBTaskMan.Task)
                return TaskMan.Tasks.GUITask.GetInstanceFromDBTask(source as DBTaskMan.Task);

            if (source is DBTaskMan.Project)
                return TaskMan.Projects.GUIProject.GetInstanceFromDBProject(source as DBTaskMan.Project);

            if (source is DBTaskMan.Component)
                return TaskMan.Components.GUIComponent.GetInstanceFromDBComponent(source as DBTaskMan.Component);

            return source;
        }
    }
}
