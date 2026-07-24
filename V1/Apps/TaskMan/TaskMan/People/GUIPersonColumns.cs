using System;
using System.Collections.Generic;
using CustomGUIControls.Grid;
using System.Text;
using Utils;
using System.Reflection;

namespace TaskMan.People
{
    class GUIPersonColumns : IGridColumns
    {

        private static GUIPersonColumns s_instance = null;

        private GUIPersonColumns()
        { }

        public static GUIPersonColumns Instance
        {
            get
            {
                if (s_instance == null)
                    s_instance = new GUIPersonColumns();
                return s_instance;
            }
        }



        internal const string s_Name = "Name";
        internal const string s_IsResource = "IsResource";
        internal const string s_IsActive = "IsActive";
        internal const string s_DBLogin = "DBLogin";
        internal const string s_UserType = "UserType";
        internal const string s_Colour = "Colour";
        internal const string s_ColourBlock = "ColourBlock";




        public IEnumerable<string> ColumnNames
        {
            get
            {
                List<string> columns = new List<string>();

                columns.Add(s_Name);
                columns.Add(s_IsResource);
                columns.Add(s_IsActive);
                columns.Add(s_DBLogin);
                columns.Add(s_UserType);
                columns.Add(s_Colour);
                columns.Add(s_ColourBlock);

                return columns;
            }
        }

        public string ColumnFormat(string columnName)
        {
            return null;
        }

        public CustomGUIControls.Grid.GridControl.ColumnTypes ColumnType(string columnName)
        {
            if (columnName == s_UserType)
                return CustomGUIControls.Grid.GridControl.ColumnTypes.DropDown;
            if (columnName == s_IsResource)
                return CustomGUIControls.Grid.GridControl.ColumnTypes.DropDown;
            if (columnName == s_IsActive)
                return CustomGUIControls.Grid.GridControl.ColumnTypes.DropDown;
            if (columnName == s_Colour)
                return CustomGUIControls.Grid.GridControl.ColumnTypes.DropDown;

            return CustomGUIControls.Grid.GridControl.ColumnTypes.Text;
        }

        public System.Windows.Forms.DataGridViewContentAlignment? ColumnAlignment(string columnName) { return null; }

        public IList<string> GetComboValues(string columnName)
        {
            return GetComboValues_static(columnName);
        }

        static private List<string> s_userTypes = null;
        static private List<string> s_yesNoValues = null;
        static private List<string> s_colours = null;

        static public void ResetColours()
        {
            s_colours = null;
        }

        private const string s_normalUser = "NormalUser";
        private const string s_powerUser = "PowerUser";
        private const string s_readOnlyUser = "ReadOnlyUser";
        private const string s_superUser = "SuperUser";


        private const string s_resourceYes = "Y";
        private const string s_resourceNo = "N";

        static public IList<string> GetComboValues_static(string columnName)
        {
            if (s_userTypes == null)
            {
                s_userTypes = new List<string>();
                s_userTypes.Add(s_superUser);
                s_userTypes.Add(s_powerUser);
                s_userTypes.Add(s_normalUser);
                s_userTypes.Add(s_readOnlyUser);
            }

            if (s_yesNoValues == null)
            {
                s_yesNoValues = new List<string>();
                s_yesNoValues.Add(s_resourceYes);
                s_yesNoValues.Add(s_resourceNo);
            }


            if (s_colours == null)
            {
                List<string> notAllowedColours = new List<string>(new string[] { "Transparent" });
                s_colours = new List<string>();
                Type c = typeof(System.Drawing.Color);
                foreach (PropertyInfo prop in c.GetProperties())
                {
                    if (prop.PropertyType == typeof(System.Drawing.Color))
                    {
                        if (!notAllowedColours.Contains(prop.Name))
                            s_colours.Add(prop.Name);
                    }
                }
                foreach (DBTaskMan.Person person in DBTaskMan.Person.AllInstances)
                {
                    if (!string.IsNullOrWhiteSpace(person.ColourName) &&
                        !s_colours.Contains(person.ColourName.Trim()))
                        s_colours.Add(person.ColourName.Trim());
                }



            }

            switch (columnName)
            {
                case s_UserType:
                    return s_userTypes;
                case s_IsResource:
                    return s_yesNoValues;
                case s_IsActive:
                    return s_yesNoValues;
                case s_Colour:
                    return s_colours;

            }

            return null;

        }


        public bool ColumnIsReadOnly(string columnName)
        {

            return !Permissions.IsSuperUser;
        }

        public string MultiValueSeparator(string columnName)
        {
            return null;
        }


        GUIPerson m_currentPerson;
        string m_currentcolumn;
        GridControl m_theGridBeingEdited = null;

        public void AdjustComboEditor(string columnName, IGridItem underlyingItem,
                                    System.Windows.Forms.DataGridViewComboBoxEditingControl comboEditor,
            GridControl theGrid)
        {
            m_currentPerson = underlyingItem as GUIPerson;
            m_currentcolumn = columnName;
            m_theGridBeingEdited = theGrid;

            if (columnName == s_Colour)
            {

                comboEditor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;

                if (m_comboEditor != comboEditor)
                {
                    m_comboEditor = comboEditor;

                    //m_comboEditor.Leave += new EventHandler(m_comboEditor_Leave);
                    m_comboEditor.LostFocus += new EventHandler(m_comboEditor_LostFocus);

                    //comboEditor.DropDownClosed += new EventHandler(comboEditor_DropDownClosed);
                    //comboEditor.SelectedValueChanged += new EventHandler(comboEditor_SelectedValueChanged);
                    //comboEditor.SelectionChangeCommitted += new EventHandler(comboEditor_SelectionChangeCommitted);
                    ////comboEditor.TextChanged += new EventHandler(comboEditor_TextChanged);
                    System.Windows.Forms.Control.ControlCollection ctrls = comboEditor.Controls;

                }

            }
            else
            {
                comboEditor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            }
        }

        void m_comboEditor_LostFocus(object sender, EventArgs e)
        {
            System.Windows.Forms.DataGridViewComboBoxEditingControl comboEditor = sender as System.Windows.Forms.DataGridViewComboBoxEditingControl;
            string theValue = comboEditor.EditingControlFormattedValue as string;

            if (m_currentPerson != null)
            {
                comboEditor.EditingControlDataGridView.CurrentCell.Value = theValue;

                m_theGridBeingEdited.SetComboValues(m_currentcolumn, GetComboValues(m_currentcolumn));
            }


        }

        void m_comboEditor_Leave(object sender, EventArgs e)
        {
            System.Windows.Forms.DataGridViewComboBoxEditingControl comboEditor = sender as System.Windows.Forms.DataGridViewComboBoxEditingControl;
            object selection = comboEditor.SelectedValue;
        }


        private System.Windows.Forms.DataGridViewComboBoxEditingControl m_comboEditor = null;


        void comboEditor_TextChanged(object sender, EventArgs e)
        {
            System.Windows.Forms.DataGridViewComboBoxEditingControl comboEditor = sender as System.Windows.Forms.DataGridViewComboBoxEditingControl;
            object selection = comboEditor.SelectedValue;

        }

        void comboEditor_SelectionChangeCommitted(object sender, EventArgs e)
        {
            System.Windows.Forms.DataGridViewComboBoxEditingControl comboEditor = sender as System.Windows.Forms.DataGridViewComboBoxEditingControl;
            object selection = comboEditor.SelectedValue;
        }

        void comboEditor_SelectedValueChanged(object sender, EventArgs e)
        {
            System.Windows.Forms.DataGridViewComboBoxEditingControl comboEditor = sender as System.Windows.Forms.DataGridViewComboBoxEditingControl;
            object selection = comboEditor.SelectedValue;
        }

        void comboEditor_DropDownClosed(object sender, EventArgs e)
        {
            System.Windows.Forms.DataGridViewComboBoxEditingControl comboEditor = sender as System.Windows.Forms.DataGridViewComboBoxEditingControl;
            object selection = comboEditor.SelectedValue;
        }






    }
}
