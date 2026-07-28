using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TaskMan
{
    class AttachmentLoader
    {

        public AttachmentLoader()
        {

        }


        public void Run()
        {
            string path = @"C:\tmp\PlanMailCopy";

            ProcessAttachmentDirectory(path);

        }


        private void ProcessAttachmentDirectory(string dir)
        {

            foreach (string subDir in System.IO.Directory.GetDirectories(dir))
            {
                ProcessAttachmentDirectory(subDir);
            }


            string itemNumber = "";
            string dirPath = dir;

            while (string.IsNullOrEmpty(itemNumber) && !string.IsNullOrEmpty(dirPath))
            {

                string subDirName = System.IO.Path.GetFileName(dirPath);
                dirPath = System.IO.Path.GetDirectoryName(dirPath);
                itemNumber = GetItemNumber(subDirName);
            }


            DBTaskMan.Task theTask = null;
            if (!string.IsNullOrEmpty(itemNumber))
            {
                foreach (DBTaskMan.Task currentTask in DBTaskMan.Task.AllInstances)
                {
                    string taskItemNumber = GetItemNumber(currentTask.OrigTaskNumber);
                    if (itemNumber == GetItemNumber(taskItemNumber))
                    {
                        theTask = currentTask;
                        break;
                    }
                }
            }

            DBTaskMan.Component theComponent = null;
            if (!string.IsNullOrEmpty(itemNumber))
            {
                foreach (DBTaskMan.Component currentComponent in DBTaskMan.Component.AllInstances)
                {
                    string taskItemNumber = GetItemNumber(currentComponent.OrigComponentRef);
                    if (itemNumber == GetItemNumber(taskItemNumber))
                    {
                        theComponent = currentComponent;
                        break;
                    }
                }
            }

            if (theTask == null && theComponent == null)
            {
                Utils.Logger.Log("There is no task or component for " + dir);
                return;
            }

            if (theTask != null && theComponent != null)
            {
                Utils.Logger.Log("There is both a task and a compoent for  " + dir);
                return;
            }

            string detailsFileName = dir + @"\Details.txt";
            System.IO.StreamReader detailsFile = new System.IO.StreamReader(detailsFileName);

            string line;
            while ((line = detailsFile.ReadLine()) != null)
            {
                string[] items = line.Split(new char[] { '|' });
                string messageNumber = items[0];
                string dateStr = items[1];
                string from = items[2];
                string filePath = items[3];
                string title = items[4];

                DateTime messageDate = DateTime.Parse(dateStr);

                byte[] fileData = System.IO.File.ReadAllBytes(filePath);

                if(theTask != null)
                    DBTaskMan.Attachment.AddNewInstance(theTask, title, "Mail", from, messageDate, fileData);
                if(theComponent != null)
                    DBTaskMan.Attachment.AddNewInstance(theComponent, title, "Mail", from, messageDate, fileData);

            }
        }

        private string GetItemNumber(string dirName)
        {
            string result = "";

            foreach (char c in dirName.Trim())
            {
                if ((c >= '0' && c <= '9') || c == '.')
                    result += c;
                else
                    break;
            }
            return ConsolidateItemNumber(result);
        }

        private string ConsolidateItemNumber(string dirName)
        {
            string result = "";
            string[] items = dirName.Trim(new char[] { '.' }).Split(new char[] { '.' });
            foreach (string item in items)
            {
                if (result.Length > 0)
                    result += ".";
                result += item.TrimStart(new char[] { '0' });

            }
            return result;
        }
    }
}
