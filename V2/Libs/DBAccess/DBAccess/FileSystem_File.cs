using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DBAccess
{
    public class FileSystem_File
    {
        static string s_folder;
        static string s_timeStampFileName;
        static string s_versionFileName;
        static System.Globalization.DateTimeFormatInfo s_dateTimeFormatInfo;
        static System.Globalization.DateTimeStyles s_dateTimeStyle;

        static FileSystem_File()
        {
            s_dateTimeFormatInfo = new System.Globalization.DateTimeFormatInfo();
            s_dateTimeStyle = System.Globalization.DateTimeStyles.None;



            s_folder = @"c:\windows\temp\ProjectPal\";

            if (!Directory.Exists(s_folder))
                Directory.CreateDirectory(s_folder);

            s_timeStampFileName = Path.Combine(s_folder, "TimeStamp.dat");
            s_versionFileName = Path.Combine(s_folder, "Version.dat");

            if (File.Exists(s_timeStampFileName))
            {
                // Just in case some other app is in the process of creating/deleting the file.
                System.Threading.Thread.Sleep(100);
                if (File.Exists(s_timeStampFileName))
                    File.Delete(s_timeStampFileName);
            }
        }

        static public void UpdateDirectoryTimeStamp(DateTime startOfSaveTime)
        {
            File.Delete(s_timeStampFileName);
            File.WriteAllText(s_timeStampFileName, startOfSaveTime.ToString("yyyyMMdd_HHmmss"));
        }


        static public void WriteVersion(string version)
        {
            File.Delete(s_versionFileName);
            File.WriteAllText(s_versionFileName, version);
        }

        static public void ClearFolder()
        {
            if (Directory.Exists(s_folder))
            {
                try
                {
                    Directory.Delete(s_folder, true);
                }
                catch (Exception)
                {
                    foreach (string entry in Directory.GetFileSystemEntries(s_folder))
                    {
                        try
                        {
                            if (Directory.Exists(entry))
                                Directory.Delete(entry, true);
                            else
                                File.Delete(entry);
                        }
                        catch (Exception)
                        { }
                    }
                }
            }
            Directory.CreateDirectory(s_folder);
        }

        public static DateTime LastFolderUpdateTime
        {
            get
            {
                if (File.Exists(s_timeStampFileName))
                {
                    string fileContents = File.ReadAllText(s_timeStampFileName);
                    try
                    {
                        DateTime result;
                        string[] formats = new string[] { "yyyyMMdd_HHmmss" };
                        if (DateTime.TryParseExact(fileContents, formats, s_dateTimeFormatInfo, s_dateTimeStyle, out result))
                            return result;
                    }
                    catch (Exception err)
                    { }
                }
                return Directory.GetLastWriteTime(s_folder);
            }
        }

        public static string ReleaseVersion
        {
            get
            {
                if (File.Exists(s_versionFileName))
                {
                    string contents = (File.ReadAllText(s_versionFileName) ?? "").Trim();
                    if (contents != "")
                    {
                        string result = "";
                        foreach (char c in contents)
                        {
                            if (c <= 32)
                                return result;
                            result += c;
                        }
                        return result;
                    }
                }
                return "unknown";
            }
        }


        static public List<FileSystem_File> GetAllFilesForType(Type dataType)
        {
            List<FileSystem_File> result = new List<FileSystem_File>();
            string[] files = Directory.GetFiles(s_folder, dataType.Name + "*.ppo", SearchOption.TopDirectoryOnly);
            foreach (string file in files)
            {
                result.Add(new FileSystem_File(file));
            }

            return result;
        }

        public FileSystem_File(string filename)
        {
            m_fileName = filename;
            string fileContents = File.ReadAllText(filename);
            int filePosition = 0;
            int fileLength = fileContents.Length;

            while (filePosition < fileLength)
            {
                int brace1 = fileContents.IndexOf('<', filePosition);
                if (brace1 < 0) break;
                int brace2 = fileContents.IndexOf('>', filePosition);
                if (brace2 < 0) break;
                string key = fileContents.Substring(brace1 + 1, brace2 - brace1 - 1);
                int endData = fileContents.IndexOf("</" + key + ">");
                string data = fileContents.Substring(brace2 + 1, endData - brace2 - 1);

                m_data.Add(key, data == "null" ? null : data);
                filePosition = endData + key.Length + 3;
            }
        }


        public DateTime? GetDate(string key)
        {
            string value;
            if (m_data.TryGetValue(key, out value))
            {
                if (value == null) return null;

                DateTime result;
                string[] formats = new string[] { "yyyyMMdd", "yyyyMMdd_HHmmss" };
                if (DateTime.TryParseExact(value, formats, s_dateTimeFormatInfo, s_dateTimeStyle, out result))
                    return result;

                return null;
            }
            return null;
        }
        public string GetString(string key)
        {
            string value;
            if (m_data.TryGetValue(key, out value))
            {
                return value;
            }
            return null;
        }


        public int? GetInt(string key)
        {
            string value;
            if (m_data.TryGetValue(key, out value))
            {
                if (value == null) return null;
                int result;
                if (int.TryParse(value, out result))
                    return result;
                return null;
            }
            return null;
        }

        public double? GetDouble(string key)
        {
            string value;
            if (m_data.TryGetValue(key, out value))
            {
                if (value == null) return null;
                double result;
                if (double.TryParse(value, out result))
                    return result;
                return null;
            }
            return null;
        }

        public bool? GetBool(string key)
        {
            string value;
            if (m_data.TryGetValue(key, out value))
            {
                if (value == null) return null;
                bool result;
                if (bool.TryParse(value, out result))
                    return result;
                return null;
            }
            return null;
        }


        public FileSystem_File(DBObjectBase dataType)
        {
            int databaseId = dataType.DatabaseId;
            m_databaseId = databaseId;
            if (m_databaseId < 0)
                m_databaseId = DBObjectBase.MaxDatabaseId + 1;

            m_fileName = dataType.GetType().Name + "." + m_databaseId;

            Add("DatabaseId", m_databaseId);

            m_modifiedTime = dataType.ModifiedTime ?? DateTime.Now;
            m_modifiedBy = dataType.ModifiedBy;
        }

        public int DatabaseId { get { return m_databaseId; } }
        public DateTime ModifiedTime { get { return m_modifiedTime; } }
        public string ModifiedBy { get { return m_modifiedBy; } }

        private string FileName { get { return Path.Combine(s_folder, m_fileName); } }

        public void Delete()
        {
            File.Delete(FileName + ".ppo");

            foreach (KeyValuePair<string, byte[]> byteArray in m_byteArrays)
            {
                string byteArrayFileName = FileName + "." + byteArray.Key + ".ppb";
                File.Delete(byteArrayFileName);
            }
        }

        public void Save()
        {
            Save(false);
        }

        public void Save(bool preserveModifiedInfo)
        {
            if (!preserveModifiedInfo)
            {
                m_modifiedTime = DateTime.Now;
                m_modifiedBy = Environment.UserName;
            }
            Add("ModifiedBy", m_modifiedBy);
            Add("ModifiedTime", m_modifiedTime);

            File.WriteAllText(FileName + ".ppo", m_xmlContent);

            foreach (KeyValuePair<string, byte[]> byteArray in m_byteArrays)
            {
                string byteArrayFileName = FileName + "." + byteArray.Key + ".ppb";

                if (byteArray.Value == null || byteArray.Value.Length == 0)
                    File.Delete(byteArrayFileName);
                else
                    File.WriteAllBytes(byteArrayFileName, byteArray.Value);
            }

        }

        public static byte[] GetByteArray(DBObjectBase dbObject, string field)
        {
            string filename = dbObject.GetType().Name + "." + dbObject.DatabaseId;
            string pathname = Path.Combine(s_folder, filename);
            string byteArrayFileName = pathname + "." + field + ".ppb";
            if (!File.Exists(byteArrayFileName))
                return null;

            byte[] result = File.ReadAllBytes(byteArrayFileName);
            return result;
        }

        public void Add(string key, string data)
        {
            if (data == null) { AddNull(key); return; }

            AddKey(key);
            m_xmlContent += data;
            AddEndKey(key);
        }

        public void Add(string key, int? data)
        {
            if (data == null) { AddNull(key); return; }

            AddKey(key);
            m_xmlContent += data.Value.ToString();
            AddEndKey(key);
        }


        public void Add(string key, double? data)
        {
            if (data == null) { AddNull(key); return; }

            AddKey(key);
            m_xmlContent += data.Value.ToString();
            AddEndKey(key);
        }

        public void Add(string key, bool? data)
        {
            if (data == null) { AddNull(key); return; }

            AddKey(key);
            m_xmlContent += data == true ? "true" : "false";
            AddEndKey(key);
        }

        public void Add(string key, DateTime? data)
        {
            if (data == null) { AddNull(key); return; }

            AddKey(key);
            if (data.Value.Date == data.Value) // No time element
                m_xmlContent += data.Value.ToString("yyyyMMdd");
            else
                m_xmlContent += data.Value.ToString("yyyyMMdd_HHmmss");
            AddEndKey(key);
        }

        public void Add(string key, byte[] data)
        {

            m_byteArrays.Add(key, data);

            if (data == null || data.Length == 0) { AddNull(key); return; }

            AddKey(key);
            m_xmlContent += "byte[]";
            AddEndKey(key);
        }

        /////////////////////////////////

        private void AddNull(string key)
        {
            m_xmlContent += "<" + key + ">null</" + key + ">" + Environment.NewLine;
        }
        private void AddKey(string key)
        {
            m_xmlContent += "<" + key + ">";
        }
        private void AddEndKey(string key)
        {
            m_xmlContent += "</" + key + ">" + Environment.NewLine;
        }

        int m_databaseId;
        DateTime m_modifiedTime;
        string m_modifiedBy;
        string m_fileName;
        string m_xmlContent = "";
        Dictionary<string, byte[]> m_byteArrays = new Dictionary<string, byte[]>();
        Dictionary<string, string> m_data = new Dictionary<string, string>();

    }
}
