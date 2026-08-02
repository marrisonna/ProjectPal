using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProjectPal
{
    /// <summary>
    /// Interaction logic for UserControlMergeField.xaml
    /// </summary>
    public partial class UserControlMergeField : UserControl
    {
        internal UserControlMergeField(string fieldName, string yourValue, string othersValue,
                                     MergeredVersion useYours, System.Reflection.PropertyInfo property)
        {
            
            InitializeComponent();
            if (yourValue == othersValue ||
              (string.IsNullOrEmpty(yourValue) && string.IsNullOrEmpty(othersValue)))
                m_versionToUse = MergeredVersion.Either;
            else
                m_versionToUse = useYours;
            textBlockFieldName.Text = fieldName;
            textBoxYour.Text = yourValue;
            textBoxOther.Text = othersValue;
            Property = property;

            m_normalWeight = new System.Windows.FontWeight();
            m_boldWeight = System.Windows.FontWeight.FromOpenTypeWeight(700);

            SetSelection();

        }

        System.Windows.FontWeight m_normalWeight;
        System.Windows.FontWeight m_boldWeight;

        private void SetSelection()
        {
            if (m_versionToUse == MergeredVersion.Either)
            {
                radioButtonOthers.IsChecked = false;
                radioButtonOthers.IsEnabled = false;

                radioButtonYours.IsChecked = false;
                radioButtonYours.IsEnabled = false;

                textBoxYour.Background = NotSelectedColour;
                textBoxOther.Background = NotSelectedColour;

                textBoxMerged.Text = textBoxOther.Text;

                m_versionToUse = MergeredVersion.Either;
            }
            else
            {
                bool usingYours = m_versionToUse == MergeredVersion.Yours;
                radioButtonOthers.IsChecked = !usingYours;
                textBoxOther.Background = !usingYours ? SelectedColour : NotSelectedColour;

                radioButtonYours.IsChecked = usingYours;
                textBoxYour.Background = usingYours ? SelectedColour : NotSelectedColour;

                textBoxMerged.Text = usingYours ? textBoxYour.Text : textBoxOther.Text;

                textBoxMerged.FontWeight = m_boldWeight;
textBoxYour.FontWeight = usingYours ? m_boldWeight : m_normalWeight;
                textBoxOther.FontWeight = !usingYours ? m_boldWeight : m_normalWeight;

            }

            textBoxYour.ToolTip = string.IsNullOrEmpty(textBoxYour.Text) ? null : textBoxYour.Text;
            textBoxOther.ToolTip = string.IsNullOrEmpty(textBoxOther.Text) ? null : textBoxOther.Text;
            textBoxMerged.ToolTip = string.IsNullOrEmpty(textBoxMerged.Text) ? null : textBoxMerged.Text;
        }

        internal enum MergeredVersion {Yours, Theirs, Either};

        private MergeredVersion m_versionToUse;
        public System.Reflection.PropertyInfo Property { get; private set; }

        internal MergeredVersion UseYours { get { return m_versionToUse; } }

        public int Xyour { get { return ColumnX(ColumnYour); } }
        public int Xother { get { return ColumnX(ColumnOther); } }
        public int Xmerged { get { return ColumnX(ColumnMerge); } }

        private int ColumnX(ColumnDefinition theColumn)
        {
            double width = 0;
            foreach (ColumnDefinition column in gridMainArea.ColumnDefinitions)
            {
                if (column.Name == theColumn.Name)
                    return (int)width;
                width += column.ActualWidth;
            }
            return 0;
        }

        private void radioButtonYours_Checked(object sender, RoutedEventArgs e)
        {
            m_versionToUse = MergeredVersion.Yours;
            SetSelection();
        }

        private void radioButtonOthers_Checked(object sender, RoutedEventArgs e)
        {
            m_versionToUse = MergeredVersion.Theirs;
            SetSelection();
        }

        private Brush NotSelectedColour = new SolidColorBrush(Colors.WhiteSmoke);
        private Brush SelectedColour = new SolidColorBrush(Colors.White);

    }

}
