using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Utils.DragDrop
{
    public class DroppedFile
    {
        public DroppedFile(string fileName, string from, string type, string title, DateTime timeStamp)
        {
            FileName = fileName;
            From = from;
            Type = type;
            Title = title;
            TimeStamp = timeStamp;
        }
        public string FileName { get; private set; }
        public string From { get; private set; }
        public string Type { get; private set; }
        public string Title { get; private set; }
        public DateTime TimeStamp { get; private set; }
        public byte[] FileContents
        {
            get
            {
                return System.IO.File.ReadAllBytes(FileName);
            }
        }
    }
}
