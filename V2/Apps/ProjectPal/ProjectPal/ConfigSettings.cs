using System;
using System.Data.Common;
using System.Drawing;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using Utils;

namespace ProjectPal
{
    class ConfigSettings
    {
        private const string RegistryValueHideCompletedProjects = "HideCompletedProjects";
        private const string RegistryValueViewPrivateItems = "ViewPrivateItems";


        private ConfigSettings()
        {

            RegistryKey software = Registry.CurrentUser.OpenSubKey("Software", true);
            RegistryKey projectPal = software.CreateSubKey("ProjectPal");
            m_configRegistryKey = projectPal.CreateSubKey("ConfigSettings");

            m_hideCompletedProjects = 1 == (int)m_configRegistryKey.GetValue(RegistryValueHideCompletedProjects, 1);
            m_viewPrivateItems = 1 == (int)m_configRegistryKey.GetValue(RegistryValueViewPrivateItems, 1);


            
            AppVersionFull = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            AppVersionShort = AppVersionFull.Substring(0, AppVersionFull.LastIndexOf('.'));
        }

        private bool m_hideCompletedProjects;
        private bool m_viewPrivateItems;

        private Microsoft.Win32.RegistryKey m_configRegistryKey;

        public bool HideCompletedProjects
        {
            get { return m_hideCompletedProjects; }
            set
            {
                m_hideCompletedProjects = value;
                m_configRegistryKey.SetValue(RegistryValueHideCompletedProjects, m_hideCompletedProjects ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord);
            }
        }

        public bool ViewPrivateItems
        {
            get { return m_viewPrivateItems; }
            set
            {
                m_viewPrivateItems = value;
                m_configRegistryKey.SetValue(RegistryValueViewPrivateItems, m_viewPrivateItems ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord);
            }
        }


        


    


        static ConfigSettings m_instance = null;

        static public ConfigSettings Instance
        {
            get
            {
                if (m_instance == null)
                    m_instance = new ConfigSettings();
                return m_instance;
            }
        }


        public string AppVersionFull { get; private set; }
        public string AppVersionShort { get; private set; }





        
        public DateTime DBLastUpdateTime
        {
            get
            {
                if (Utils.DatabaseBase.DBType == DatabaseBase.DBTypeValues.FileSystem)
                {
                    return DBAccess.FileSystem_File.LastFolderUpdateTime;
                }
                else
                {

                    string sql = "select LastUpdateTime from " + DatabaseBase.Schema + "System";
                    using (DbDataReader systemReader = DatabaseBase.NamedInstance(this).ExecuteReader(sql))
                    {
                        if (systemReader.Read())
                        {
                            DateTime? updateTime = DatabaseBase.GetColumnValueAs<DateTime>(systemReader, 0);
                            if (updateTime.HasValue)
                                return updateTime.Value;
                        }
                    }


                }
                return DateTime.Now.AddDays(-1);
            }
        }


        public string DBReleaseVersion
        {
            get
            {
                if (Utils.DatabaseBase.DBType == DatabaseBase.DBTypeValues.FileSystem)
                {
                    return DBAccess.FileSystem_File.ReleaseVersion;
                }
                else
                {
                    string sql = "select ReleaseVersion from " + DatabaseBase.Schema + "System";
                    using (DbDataReader systemReader = DatabaseBase.NamedInstance(this).ExecuteReader(sql))
                    {
                        if (systemReader.Read())
                            return DatabaseBase.GetColumnValueAsString(systemReader, 0);
                    }
                    return null;
                }
            }
        }
    }
}
