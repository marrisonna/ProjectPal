using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CustomGUIControls;
using Utils;

namespace ProjectPal.Remarks
{
    public partial class RemarkWindow : Form, IView
    {
        static private Dictionary<GUIRemark, RemarkWindow> s_windows = new Dictionary<GUIRemark, RemarkWindow>();
        ViewImpl m_viewImplementation = null;


        static public void ShowRemarkWindow(IDisplayItem objectToDisplay)
        {

            GUIRemark underlyingObject = objectToDisplay as GUIRemark;
            if (underlyingObject != null)
            {
                RemarkWindow window = null;
                s_windows.TryGetValue(underlyingObject, out window);

                if (window == null)
                {
                    window = new RemarkWindow(underlyingObject);
                    s_windows.Add(underlyingObject, window);
                    window.Show();
                }
                else
                {
                    window.Redisplay();
                    window.Activate();
                }
            }
        }

        private void Init()
        {
            InitializeComponent();
            m_viewImplementation = new ViewImpl(this);
            textBoxRemark.Focus();
            
        }


        public RemarkWindow()
        {
           
            Init();
            m_createTime = DBAccess.DBObjectBase.DBTime;
            m_user = DBAccess.DBObjectBase.DBUser;

            textBoxMadeBy.Text = m_user;
            textBoxOn.Text = m_createTime.ToString("dd-MMM-yyyy HH:mm:ss");
        }


        private RemarkWindow(GUIRemark remark)
        {
            Init();

            if (!Permissions.IsAllowed(remark.Owner,Permissions.EntityType.Remark,Permissions.ChangeType.Edit))
            {
                buttonOK.Enabled = false;
                textBoxRemark.ReadOnly = true;
                textBoxRemark.BackColor = textBoxMadeBy.BackColor;
            }
            m_viewImplementation.AddDisplayItem(remark);
            m_remark = remark;

            m_createTime = remark.ModifiedTime;
            m_user = remark.CreatedBy;

            textBoxRemark.Text = remark.RemarkText;
            textBoxMadeBy.Text = remark.CreatedBy;
            textBoxOn.Text = remark.ModifiedTime.ToString("dd-MMM-yyyy HH:mm:ss") ;
        }


        private void Redisplay()
        {
            if (m_viewImplementation.ItemsToDisplay.Count > 0)
            {
                GUIRemark remark = m_viewImplementation.FirstItemToDisplay as GUIRemark;
                if (remark != null)
                {

                    m_createTime = remark.ModifiedTime;
                    m_user = remark.CreatedBy;

                    textBoxRemark.Text = remark.RemarkText;
                    textBoxMadeBy.Text = remark.CreatedBy;
                    textBoxOn.Text = remark.ModifiedTime.ToString("dd-MMM-yyyy HH:mm:ss");
                }
            }
        }

        public void Redisplay(IDisplayItem itemToDisplay)
        {
            GUIRemark underlyingObject = itemToDisplay as GUIRemark;

            if (m_viewImplementation.ItemsToDisplay.Contains(underlyingObject))
            {
                Redisplay();
            }

        }

        public void AddDisplayItem(IDisplayItem itemToDisplay)
        {
            throw new Exception("AddDisplayItem doesn't make sense for RemarkWindow");
        }

        public void WindowClosed()
        {
            m_viewImplementation.WindowClosed();
        }

        public void RemoveDisplayItem(IDisplayItem itemToDisplay)
        {
            m_viewImplementation.RemoveDisplayItem(itemToDisplay);
            WindowClosed();
            Close();
        }

        public string RemarkText { get { return textBoxRemark.Text; } }
        public DateTime CreateTime { get { return m_createTime; } }
        public string RemarkedBy { get { return m_user; } }

        DateTime m_createTime;
        string m_user;
        GUIRemark m_remark;

        private void buttonOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            if (m_remark != null)
            {
                m_remark.RemarkText = textBoxRemark.Text;
                m_remark.SaveToDB();

                Functions.ClearDisplayCaches(); 
                m_remark.Redisplay();
                m_remark.RedisplayTask();
                
            }
           
            Close();
        }

        private void RemarkWindow_FormClosed(object sender, FormClosedEventArgs e)
        {            
                if (m_viewImplementation.ItemsToDisplay.Count > 0)
                    s_windows.Remove(m_viewImplementation.FirstItemToDisplay as GUIRemark);
                WindowClosed();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
