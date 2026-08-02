using System;
using System.Collections.Generic;
using System.Text;
using DBProjectPal;
using Utils;

namespace ProjectPal
{
    class Functions
    {
        // Return of 100 = Medium Priority on average
        // First item in list is least significant, the last item is most significant
        public static double PriorityValue(List<PriorityValue?> priorityList)
        {
            List<PriorityValue?> priorityListHighestFirst = new List<DBProjectPal.PriorityValue?>(priorityList);
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
            DBProjectPal.Functions.ClearDateCaches();
            if (ApplicationProjectPal.Instance.LastDBUpdateTime > m_lastClearVisibleCacheTime)
            {
                m_lastClearVisibleCacheTime = ApplicationProjectPal.Instance.LastDBUpdateTime;
                DBAccess.DBObjectBase.ClearVisibleInstanceCache();
            }
            else if(m_lastClearVisibleCacheTime == null)
            {

                m_lastClearVisibleCacheTime = ApplicationProjectPal.Instance.LastDBUpdateTime;
            }
        }


        public static void PopulateComboWithCurrentUsers(System.Windows.Forms.ComboBox comboBoxUsers,
                                                         bool includeReadOnlyUsers)
        {
            comboBoxUsers.Items.Clear();
            foreach (DBProjectPal.Person currentPerson in DBProjectPal.Person.AllActiveInstances)
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
            if (source is ProjectPal.Tasks.GUITask ||
                source is ProjectPal.Projects.GUIProject ||
                source is ProjectPal.Components.GUIComponent)
                return source;

            if (source is DBProjectPal.Task)
                return ProjectPal.Tasks.GUITask.GetInstanceFromDBTask(source as DBProjectPal.Task);

            if (source is DBProjectPal.Project)
                return ProjectPal.Projects.GUIProject.GetInstanceFromDBProject(source as DBProjectPal.Project);

            if (source is DBProjectPal.Component)
                return ProjectPal.Components.GUIComponent.GetInstanceFromDBComponent(source as DBProjectPal.Component);

            return source;
        }
    }
}
