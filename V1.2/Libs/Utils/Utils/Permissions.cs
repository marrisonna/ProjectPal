using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utils
{

    public class Permissions
    {
        public enum UserLevel { SuperUser, PowerUser, NormalUser, ReadOnlyUser }
        public enum EntityType { Remark, Project, Task, Attachment, Component }
        public enum ChangeType { Create, Edit, Delete }

        public static string DBUserName{get;set;}
        public static void SetUserType(UserLevel type)
        {
            s_thisUserType = type;
        }


        private static UserLevel? s_thisUserType = null;

        public static bool IsAllowed(string objectOwner, EntityType entity, ChangeType change)
        {
            UserLevel thisUserLever = ThisUserType;
            if (thisUserLever == UserLevel.SuperUser)
                return true;
            if (thisUserLever == UserLevel.ReadOnlyUser)
            {
                if (entity == EntityType.Remark && change == ChangeType.Create)
                    return true;

                return false;
            }

            string userName = DBUserName;


            bool userOwnsThisObject = userName == objectOwner;


            if (userOwnsThisObject && change == ChangeType.Edit)
                return true;



            switch (entity)
            {
                case EntityType.Component:
                    {
                        if (change == ChangeType.Create && thisUserLever == UserLevel.PowerUser)
                            return true;
                        if (change == ChangeType.Delete && userOwnsThisObject)
                            return true;
                        break;
                    }
                case EntityType.Task:
                    {
                        if (change == ChangeType.Delete && userOwnsThisObject)
                            return true;
                        if (change == ChangeType.Edit && userOwnsThisObject)
                            return true;
                        if (change == ChangeType.Create && thisUserLever == UserLevel.PowerUser)
                            return true;
                        break;
                    }
                case EntityType.Remark:
                    {
                        if (change == ChangeType.Create ||
                            (change == ChangeType.Delete && userOwnsThisObject))
                            return true;
                        break;
                    }
                case EntityType.Attachment:
                    {
                        if (change == ChangeType.Create ||
                            (change == ChangeType.Delete && userOwnsThisObject))
                            return true;
                        break;
                    }

                case EntityType.Project:
                    {
                        if (thisUserLever == UserLevel.PowerUser &&
                                change == ChangeType.Create)
                            return true;
                        break;
                    }

            }


            //string ownerComment = string.IsNullOrEmpty(objectOwner) ? "" : "(owned by '" + objectOwner + "')";
            //Utils.Logger.Log(string.Format("Permission denied to user '{0}' on item '{1}' {2}", userName, entity, ownerComment));

            return false;
        }





        public static UserLevel ThisUserType
        {
            get
            {
                if (s_thisUserType == null)
                    s_thisUserType = UserLevel.ReadOnlyUser;
                return s_thisUserType.Value;
            }
        }




        public static bool IsReadOnly
        {
            get
            {
                return ThisUserType == UserLevel.ReadOnlyUser;
            }
        }


        public static bool IsPowerUser
        {
            get
            {
                return ThisUserType == UserLevel.PowerUser;
            }
        }

        public static bool IsSuperUser
        {
            get
            {
                return ThisUserType == UserLevel.SuperUser;
            }
        }



    }
}
