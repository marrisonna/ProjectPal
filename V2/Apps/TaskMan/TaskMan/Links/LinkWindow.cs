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

namespace TaskMan.Links
{
    public partial class LinkWindow : Form, IView
    {
        static private Dictionary<GUILink, LinkWindow> s_windows = new Dictionary<GUILink, LinkWindow>();
        ViewImpl m_viewImplementation = null;


        static public void ShowLinkWindow(IDisplayItem objectToDisplay)
        {

            GUILink underlyingObject = objectToDisplay as GUILink;
            if (underlyingObject != null)
            {
                LinkWindow window = null;
                s_windows.TryGetValue(underlyingObject, out window);

                if (window == null)
                {
                    window = new LinkWindow(underlyingObject);
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
            textBoxLink.Focus();

        }


        public LinkWindow()
        {

            Init();
            m_createTime = DBAccess.DBObjectBase.DBTime;
            m_user = DBAccess.DBObjectBase.DBUser;

            textBoxMadeBy.Text = m_user;
            textBoxOn.Text = m_createTime.ToString("dd-MMM-yyyy HH:mm:ss");
        }


        private LinkWindow(GUILink link)
        {
            Init();

            if (!Permissions.IsAllowed(link.Owner, Permissions.EntityType.Link, Permissions.ChangeType.Edit))
            {
                buttonOK.Enabled = false;
                textBoxLink.ReadOnly = true;
                textBoxLink.BackColor = textBoxMadeBy.BackColor;
            }
            m_viewImplementation.AddDisplayItem(link);
            m_link = link;

            m_createTime = link.ModifiedTime;
            m_user = link.CreatedBy;

            textBoxLink.Text = link.LinkText;
            textBoxMadeBy.Text = link.CreatedBy;
            textBoxOn.Text = link.ModifiedTime.ToString("dd-MMM-yyyy HH:mm:ss");
        }


        private void Redisplay()
        {
            if (m_viewImplementation.ItemsToDisplay.Count > 0)
            {
                GUILink link = m_viewImplementation.FirstItemToDisplay as GUILink;
                if (link != null)
                {

                    m_createTime = link.ModifiedTime;
                    m_user = link.CreatedBy;

                    textBoxLink.Text = link.LinkText;
                    textBoxMadeBy.Text = link.CreatedBy;
                    textBoxOn.Text = link.ModifiedTime.ToString("dd-MMM-yyyy HH:mm:ss");
                }
            }
        }

        public void Redisplay(IDisplayItem itemToDisplay)
        {
            GUILink underlyingObject = itemToDisplay as GUILink;

            if (m_viewImplementation.ItemsToDisplay.Contains(underlyingObject))
            {
                Redisplay();
            }

        }

        public void AddDisplayItem(IDisplayItem itemToDisplay)
        {
            throw new Exception("AddDisplayItem doesn't make sense for LinkWindow");
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

        public string LinkText { get { return textBoxLink.Text; } }
        public DateTime CreateTime { get { return m_createTime; } }
        public string LinkedBy { get { return m_user; } }

        DateTime m_createTime;
        string m_user;
        GUILink m_link;

        private void buttonOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            if (m_link != null)
            {
                m_link.LinkText = textBoxLink.Text;
                m_link.SaveToDB();

                Functions.ClearDisplayCaches();
                m_link.Redisplay();
                m_link.RedisplayTask();

            }

            Close();
        }

        private void LinkWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (m_viewImplementation.ItemsToDisplay.Count > 0)
                s_windows.Remove(m_viewImplementation.FirstItemToDisplay as GUILink);
            WindowClosed();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
