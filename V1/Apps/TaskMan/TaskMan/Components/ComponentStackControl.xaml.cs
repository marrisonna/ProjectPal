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

namespace TaskMan.Components
{
    /// <summary>
    /// Interaction logic for ComponentStackControl.xaml
    /// </summary>
    public partial class ComponentStackControl : UserControl
    {

       

        public ComponentStackControl(CustomGUIControls.Grid.GridControl.CellDoubleClick taskWindowOpenFunction)
        {
            InitializeComponent();
            m_taskWindowOpenFunction = taskWindowOpenFunction;
        }

        public void AddComponent(GUIComponent itemToDisplay, bool openSubComponents, ComponentControl.TasksToDisplayFn tasksToDisplay)
        {
            ComponentControl ctrl = new ComponentControl(itemToDisplay, m_taskWindowOpenFunction, openSubComponents, tasksToDisplay);

            stackPanel1.Children.Add(ctrl);
        }

        public void InsertComponent(int index, GUIComponent itemToDisplay, bool openSubComponents, ComponentControl.TasksToDisplayFn tasksToDisplay)
        {
            ComponentControl ctrl = new ComponentControl(itemToDisplay, m_taskWindowOpenFunction, openSubComponents, tasksToDisplay);

            stackPanel1.Children.Insert(index, ctrl);
        }

        public ComponentControl ComponentAt(int index)
        {
            if (index >= stackPanel1.Children.Count)
                return null;
            return stackPanel1.Children[index] as ComponentControl;
        }

        public void RemoveComponentAt(int index)
        {
            if (index >= stackPanel1.Children.Count)
                return;
            UIElement child = stackPanel1.Children[index];
            ComponentControl ctrl = child as ComponentControl;
            if (ctrl != null)
            {
                ctrl.WindowClosed();
            }
            stackPanel1.Children.RemoveAt(index);
        }

        private CustomGUIControls.Grid.GridControl.CellDoubleClick m_taskWindowOpenFunction = null;

        public int ChildCount { get { return stackPanel1.Children.Count; } }

        public void RemoveChildren()
        {
            foreach (UIElement child in stackPanel1.Children)
            {
                ComponentControl ctrl = child as ComponentControl;
                if (ctrl != null)
                {
                    ctrl.WindowClosed();
                }

            }
            stackPanel1.Children.Clear();


        }
    }
}
