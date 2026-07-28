using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Utils.DragDrop
{
    public class DroppedFiles
    {

        private List<DroppedFile> m_files = new List<DroppedFile>();

        private List<string> m_filenames = new List<string>();
        private List<string> m_from = new List<string>();
        private List<string> m_type = new List<string>();
        private List<string> m_title = new List<string>();
        private List<DateTime> m_timeStamps = new List<DateTime>();
        private static string s_tempPath = @"C:\Windows\Temp\";

        public IEnumerable<DroppedFile> Files
        {
            get
            {
                return m_files;
            }
        }


        static public bool IsDropable(DragEventArgs e)
        {
            System.Windows.Forms.IDataObject data = e.Data;
            object dataValue = data.GetData("FileNameW");
            if (dataValue != null)
            {
                string[] path = dataValue as string[];
                if (path != null &&
                    System.IO.File.Exists(path[0]) &&
                    (System.IO.File.GetAttributes(path[0]) & System.IO.FileAttributes.Directory) == 0)
                    return true;
            }

            System.IO.MemoryStream fgdStream = (System.IO.MemoryStream)e.Data.GetData("FileGroupDescriptor");
            if (fgdStream != null)
                return true;
            return false;
        }


        public DroppedFiles(DragEventArgs e)
        {
            System.Windows.Forms.IDataObject data = e.Data;

            string[] formats = data.GetFormats();
            foreach (string format in formats)
            {
                object dataValue = data.GetData(format);
            }


            //use IDataObject to get FileGroupDescriptor
            //as a MemoryStream and copy into a byte array
            System.IO.MemoryStream fgdStream = (System.IO.MemoryStream)e.Data.GetData("FileGroupDescriptor");
            if (fgdStream == null)
            {

                string[] droppedFileNames = data.GetData("FileDrop") as string[];
                
                foreach (string filePath in droppedFileNames)
                {
                    m_filenames.Add(filePath);
                    
                    string fileName = System.IO.Path.GetFileName(filePath);

                    //AddFileName(s_tempPath + fileName);
                    m_title.Add(fileName);
                    m_timeStamps.Add(System.IO.File.GetCreationTime(filePath));

                    m_from.Add(System.Environment.UserName);
                    m_type.Add("File");
                    //if (System.IO.File.Exists(m_filenames[0]))
                    //    System.IO.File.Delete(m_filenames[0]);
                    //System.IO.File.Copy(filePath, m_filenames[0]);
                }



            }
            else
            {

                string title = data.GetData("System.String") as string;

                string[] lines = title.Split(new string[] { System.Environment.NewLine }, StringSplitOptions.None);
                string[] keys = lines[0].Split(new char[] { '\t' });

                bool dateAdded = false, fromAdded = false, subjectAdded = false;

                for (int items = 1; items < lines.Length; items++)
                {
                    m_type.Add("Mail");
                    string[] values = lines[items].Split(new char[] { '\t' });
                    Logger.Log("Mail fields: ");
                    for (int i = 0; i < keys.Length; i++)
                    {
                        Logger.Log(keys[i] + "=" + values[i]);
                        if (keys[i] == "Received" || keys[i] == "Sent")
                        {
                            string timeStampStr = values[i];
                            DateTime timeStamp;
                            if (DateTime.TryParse(timeStampStr, out timeStamp))
                            {
                                Logger.Log("Parsed time1 = " + timeStamp.ToString("dd-MMM-yyyy HH:mm:ss"));
                                m_timeStamps.Add(timeStamp);
                                dateAdded = true;
                            }
                            else
                            {
                                string[] dateItems = values[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (dateItems.Length == 1)
                                {
                                    timeStampStr = DateTime.Now.ToString("dd-MMM-yyyy ") + dateItems[0];
                                    if (DateTime.TryParse(timeStampStr, out timeStamp))
                                    {
                                        Logger.Log("Parsed time2 = " + timeStamp.ToString("dd-MMM-yyyy HH:mm:ss"));
                                        m_timeStamps.Add(timeStamp);
                                        dateAdded = true;
                                    }
                                }
                                else if (dateItems.Length == 2)
                                {
                                    // Assume format is, e.g., 'Wed 17:14'
                                    int tryCounter =8;
                                    DateTime guessedDate = DateTime.Now.Date;
                                    while (guessedDate.ToString("ddd") != dateItems[0] && tryCounter > 0)
                                    {
                                        guessedDate = guessedDate.AddDays(-1);
                                        tryCounter--;
                                    }
                                    if (tryCounter > 0)
                                    {
                                        timeStampStr = guessedDate.ToString("dd-MMM-yyyy ") + dateItems[1];
                                        if (DateTime.TryParse(timeStampStr, out timeStamp))
                                        {
                                            Logger.Log("Parsed time3 = " + timeStamp.ToString("dd-MMM-yyyy HH:mm:ss"));
                                            m_timeStamps.Add(timeStamp);
                                            dateAdded = true;
                                        }

                                    }



                                }
                            }
                        }
                        if (keys[i] == "From")
                        {
                            m_from.Add(values[i]);
                            fromAdded = true;

                        }
                        if (keys[i] == "Subject")
                        {
                            m_title.Add(values[i]);
                            subjectAdded = true;

                        }
                    }
                    Logger.Log("Mail fields done: ");
                    

                }
                if (!dateAdded)
                    m_timeStamps.Add(new DateTime(1900, 1, 1));
                if (!fromAdded)
                    m_from.Add("unknown");
                if (!subjectAdded)
                    m_title.Add("unknown");

                byte[] fgdBytes = new byte[fgdStream.Length];
                fgdStream.Read(fgdBytes, 0, fgdBytes.Length);
                fgdStream.Close();

                //copy the file group descriptor into unmanaged memory
                IntPtr fgdaPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(fgdBytes.Length);
                System.Runtime.InteropServices.Marshal.Copy(fgdBytes, 0, fgdaPtr, fgdBytes.Length);

                //marshal the unmanaged memory to a FILEGROUPDESCRIPTORA struct
                object fgdObj = System.Runtime.InteropServices.Marshal.PtrToStructure(fgdaPtr,
                                        typeof(NativeMethods.FILEGROUPDESCRIPTORA));
                NativeMethods.FILEGROUPDESCRIPTORA fgd = (NativeMethods.FILEGROUPDESCRIPTORA)fgdObj;

                //create a array to store file names in
                string[] fileNames = new string[fgd.cItems];

                //get the pointer to the first file descriptor
                IntPtr fdPtr = IntPtr.Add(fgdaPtr, sizeof(uint));

                //loop for the number of files acording to the file group descriptor
                for (int fdIndex = 0; fdIndex < fgd.cItems; fdIndex++)
                {
                    //marshal the pointer to the file descriptor as a FILEDESCRIPTORA struct
                    object fdObj = Marshal.PtrToStructure(fdPtr,
                                          typeof(NativeMethods.FILEDESCRIPTORA));
                    NativeMethods.FILEDESCRIPTORA fd = (NativeMethods.FILEDESCRIPTORA)fdObj;

                    //get file name of file descriptor and put in array
                    fileNames[fdIndex] = fd.cFileName;
                    AddFileName(s_tempPath + fd.cFileName);



                    //move the file descriptor pointer to the next file descriptor
                    fdPtr = IntPtr.Add(fdPtr, Marshal.SizeOf(fd));
                }

                for (int messageIndex = 0; messageIndex < fileNames.Length; messageIndex++)
                {


                    //cast the default IDataObject to a com IDataObject
                    System.Runtime.InteropServices.ComTypes.IDataObject comDataObject;
                    comDataObject = (System.Runtime.InteropServices.ComTypes.IDataObject)e.Data;

                    //create a FORMATETC struct to request the data with from the com IDataObject
                    FORMATETC formatetc = new FORMATETC();
                    formatetc.cfFormat = (short)DataFormats.GetFormat("FileContents").Id;
                    formatetc.dwAspect = DVASPECT.DVASPECT_CONTENT;
                    formatetc.lindex = messageIndex; //zero based index to retrieve
                    formatetc.ptd = new IntPtr(0);
                    formatetc.tymed = TYMED.TYMED_ISTREAM | TYMED.TYMED_ISTORAGE | TYMED.TYMED_HGLOBAL;

                    //create STGMEDIUM to output request results into
                    STGMEDIUM medium = new STGMEDIUM();

                    //using the com IDataObject interface get the data using the defined FORMATETC
                    comDataObject.GetData(ref formatetc, out medium);

                    if (medium.tymed == TYMED.TYMED_ISTREAM)
                    {
                        //marshal the returned pointer to a IStream object
                        IStream iStream = (IStream)Marshal.GetObjectForIUnknown(medium.unionmember);
                        Marshal.Release(medium.unionmember);

                        //get the STATSTG of the IStream to determine how many bytes are in it
                        System.Runtime.InteropServices.ComTypes.STATSTG iStreamStat = new System.Runtime.InteropServices.ComTypes.STATSTG();
                        iStream.Stat(out iStreamStat, 0);
                        int iStreamSize = (int)iStreamStat.cbSize;

                        //read the data from the IStream into a managed byte array
                        byte[] iStreamContent = new byte[iStreamSize];
                        iStream.Read(iStreamContent, iStreamContent.Length, IntPtr.Zero);

                        //wrapped the managed byte array into a memory stream
                        //System.IO.Stream filestream = new System.IO.MemoryStream(iStreamContent);



                        System.IO.FileStream file = new System.IO.FileStream(m_filenames[messageIndex], System.IO.FileMode.Create);
                        file.Write(iStreamContent, 0, iStreamContent.Length);
                        file.Close();
                    }


                    if (medium.tymed == TYMED.TYMED_ISTORAGE)
                    {
                        NativeMethods.IStorage iStorage = null;
                        NativeMethods.IStorage iStorage2 = null;
                        NativeMethods.ILockBytes iLockBytes = null;
                        System.Runtime.InteropServices.ComTypes.STATSTG iLockBytesStat;
                        try
                        {
                            //marshal the returned pointer to a IStorage object
                            iStorage = (NativeMethods.IStorage)
                               Marshal.GetObjectForIUnknown(medium.unionmember);
                            Marshal.Release(medium.unionmember);

                            //create a ILockBytes (unmanaged byte array)
                            iLockBytes = NativeMethods.CreateILockBytesOnHGlobal(IntPtr.Zero, true);

                            //create a IStorage using the ILockBytes 
                            //(unmanaged byte array) as a backing store
                            iStorage2 = NativeMethods.StgCreateDocfileOnILockBytes(iLockBytes,
                                                                               0x00001012, 0);

                            //copy the returned IStorage into the new memory backed IStorage
                            iStorage.CopyTo(0, null, IntPtr.Zero, iStorage2);
                            iLockBytes.Flush();
                            iStorage2.Commit(0);

                            //get the STATSTG of the ILockBytes to determine 
                            //how many bytes were written to it
                            iLockBytesStat = new System.Runtime.InteropServices.ComTypes.STATSTG();
                            iLockBytes.Stat(out iLockBytesStat, 1);
                            int iLockBytesSize = (int)iLockBytesStat.cbSize;

                            //read the data from the ILockBytes 
                            //(unmanaged byte array) into a managed byte array
                            byte[] iLockBytesContent = new byte[iLockBytesSize];
                            iLockBytes.ReadAt(0, iLockBytesContent, iLockBytesContent.Length, null);

                            //wrapped the managed byte array into a memory stream
                            System.IO.Stream filestream = new System.IO.MemoryStream(iLockBytesContent);

                            byte[] fgdBytesData = new byte[filestream.Length];
                            filestream.Read(fgdBytesData, 0, fgdBytesData.Length);
                            filestream.Close();


                            System.IO.FileStream file = new System.IO.FileStream(m_filenames[messageIndex], System.IO.FileMode.Create);
                            file.Write(fgdBytesData, 0, fgdBytesData.Length);
                            file.Close();

                        }
                        finally
                        {
                            //release all unmanaged objects
                            Marshal.ReleaseComObject(iStorage2);
                            Marshal.ReleaseComObject(iLockBytes);
                            Marshal.ReleaseComObject(iStorage);
                        }
                    }
                }
            }

            m_files = new List<DroppedFile>();

            for (int i = 0; i < m_filenames.Count; i++)
            {
                m_files.Add(new DroppedFile(m_filenames[i], m_from[i], m_type[i], m_title[i], m_timeStamps[i]));
            }

        }

        private void AddFileName(string fileNameIn)
        {
            string directoryPath = System.IO.Path.GetDirectoryName(fileNameIn);
            string fileName = System.IO.Path.GetFileName(fileNameIn);

            fileName = fileName.
                        Replace(@"\", "").
                        Replace(@"/", "").
                        Replace(@":", "").
                        Replace(@"*", "").
                        Replace(@"?", "").
                        Replace("\"", "").
                        Replace(@"<", "").
                        Replace(@">", "").
                        Replace(@"|", "");

            string filePath = System.IO.Path.Combine(directoryPath, fileName);

            if (System.IO.File.Exists(filePath) || m_filenames.Contains(filePath))
            {
                string fileBase = filePath;
                string fileExtension = "";

                int lastDot = filePath.LastIndexOf('.');
                if (lastDot != -1)
                {
                    fileExtension = filePath.Substring(lastDot);
                    fileBase = filePath.Substring(0, lastDot);
                }
                int fileIndex = 0;
                string newFileName = "";
                do
                {
                    fileIndex++;
                    newFileName = fileBase + "_" + fileIndex.ToString() + fileExtension;
                }
                while (m_filenames.Contains(newFileName) || System.IO.File.Exists(newFileName));
                filePath = newFileName;
            }
            m_filenames.Add(filePath);
        }
    }
}
