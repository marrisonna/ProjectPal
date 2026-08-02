using System;
using System.Collections.Generic;
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
using Utils;

namespace PlanDisplay
{
    /// <summary>
    /// Interaction logic for PlanControl.xaml
    /// </summary>
    public partial class PlanControl : UserControl
    {
        public PlanControl()
        {
            InitializeComponent();

            sliderV.Value = 50;
            sliderH.Value = 50;
            labelDate.Content = "";
            labelCaption.Content = "";
        }

        public Grid ThePlanControlGrid { get { return canvasOuter; } }

        public void ReScaleToAreaData()
        {
            m_reScaleWidth = this.ActualWidth;
            m_reScaleHeight = this.ActualHeight;

        }


        public void ReScaleToAreaData(double? width, double? height)
        {
            m_reScaleWidth = width;
            m_reScaleHeight = height;
        }

        double? m_reScaleWidth;
        double? m_reScaleHeight;

        public double VerticalScale { get { return m_verticalScaleValue; } }

        private void ReScaleToArea()
        {
            if (m_reScaleHeight.HasValue)
            {
                double fixedHeight = rowDefinition1.ActualHeight + labelStartDate.ActualHeight + canvasOuter.Margin.Top + canvasOuter.Margin.Bottom;
                double availableHeight = m_reScaleHeight.Value - fixedHeight;
                double currentHeight = (canvasOuter.ActualHeight - labelStartDate.ActualHeight) / m_verticalScaleValue;// *1.01; // 1% higher than min require

                double ch = canvasMain.ActualHeight / m_verticalScaleValue;

                double requiredScaleFactor = availableHeight / currentHeight / 1.01;



                double sliderPosition = SliderPositionFromScale(requiredScaleFactor);
                if (sliderPosition > sliderV.Maximum)
                    sliderPosition = sliderV.Maximum;

                sliderV.Value = sliderV.Maximum - sliderPosition;

                m_reScaleHeight = null;



            }

            if (m_reScaleWidth.HasValue)
            {
                double fixedWidth = columnDefinition1.ActualWidth + canvasOuter.Margin.Left + canvasOuter.Margin.Right;
                double availableWidth = m_reScaleWidth.Value - fixedWidth;
                double currentWidth = canvasOuter.ActualWidth / m_horizontalScaleValue;// *1.01; // 1% wider than min required
                double requiredScaleFactor = availableWidth / currentWidth / 1.01;

                double sliderPosition = SliderPositionFromScale(requiredScaleFactor);
                if (sliderPosition > sliderH.Maximum)
                    sliderPosition = sliderH.Maximum;

                sliderH.Value = sliderPosition;

                m_reScaleWidth = null;


            }


            // This bit ensures that the lay is done without the scroll bars on, since
            // they should not be visible if the scale above is set correctly.
            // Then, once the layout is done, enable the scroll bars incase the
            // user adjusts the scale or window size later on.
            scrollViewerPlan.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            scrollViewerPlan.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            scrollViewerPlan.UpdateLayout();
            scrollViewerPlan.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            scrollViewerPlan.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

        }

        private double SliderPositionFromScale(double scale)
        {
            if (scale < 1)
            {
                double v = Math.Pow(scale, 0.5);
                return 50.0 * v;
            }
            return (scale - 1) * 25 + 50;
        }


        private double ScaleFromSliderPosition(double position)
        {
            double scale = 1;
            if (position > 50)
                scale = 1 + (position - 50) / 25;
            else
                scale = Math.Pow(position / 50, 2);
            return scale;
        }



        double m_verticalOffset = -1, m_horizontalOffset = -1;

        private void sliderV_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double sliderValue = sliderV.Maximum - sliderV.Value;

            m_verticalScaleValue = ScaleFromSliderPosition(sliderValue);

            canvasMain.LayoutTransform = new ScaleTransform(m_horizontalScaleValue, m_verticalScaleValue);
            //canvasMain.RenderTransform = new ScaleTransform(scaleValue, scaleValue);

            setDates();

            if (m_projectOnDisplay != null)
                m_projectOnDisplay.ReDrawDependencies();
        }

        public double RectangleScaleValue { get { return sliderV.Value; } }
        public double HorizontalScaleValue { get { return sliderH.Value; } }
        public double HorizontalScroll
        {
            get
            {
                if (m_horizontalOffset != -1)
                    return m_horizontalOffset;
                return scrollViewerPlan.HorizontalOffset;
            }
        }

        public double VerticalScroll
        {
            get
            {
                if (m_verticalOffset != -1)
                    return m_verticalOffset;
                return scrollViewerPlan.VerticalOffset;
            }
        }


        double m_verticalScaleValue = 1;
        double m_horizontalScaleValue = 1;

        private void sliderH_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double sliderValue = sliderH.Value;

            m_horizontalScaleValue = ScaleFromSliderPosition(sliderValue);

            canvasMain.LayoutTransform = new ScaleTransform(m_horizontalScaleValue, m_verticalScaleValue);
            //canvasMain.RenderTransform = new ScaleTransform(scaleValue, scaleValue);

            setDates();

            if (m_projectOnDisplay != null)
                m_projectOnDisplay.ReDrawDependencies();
        }



        private void setDates()
        {

            //labelStartDate.Margin = new Thickness(
            //    0,
            //    canvasMain.ActualHeight,
            //    0, 0);

            ////labelEndDate.FontFamily.GetTypefaces().First().

            //labelEndDate.Margin = new Thickness(
            //    canvasMain.Margin.Left + canvasMain.Width - labelEndDate.ActualWidth,
            //    canvasMain.Margin.Top + canvasMain.Height,
            //    0, 0);
        }

        public void SetScroll()
        {
            if (m_verticalOffset != -1)
                scrollViewerPlan.ScrollToVerticalOffset(m_verticalOffset);
            if (m_horizontalOffset != -1)
                scrollViewerPlan.ScrollToHorizontalOffset(m_horizontalOffset);
            m_verticalOffset = m_horizontalOffset = -1; // clear the 'cache'
        }


        Project m_projectOnDisplay = null;


        private Dictionary<PlanDisplay.StatusValue, System.Windows.Media.Color> m_statusColorMap = null;
        private Dictionary<PlanDisplay.StatusValue, System.Windows.Media.Color> StatusColorMap
        {
            get
            {
                if (m_statusColorMap == null)
                {
                    m_statusColorMap = new Dictionary<StatusValue, Color>();

                    foreach (StackPanel thisStatusPanel in stackPanelStatus.Children)
                    {
                        if (thisStatusPanel.Children.Count > 1)
                        {
                            Rectangle colourBox = thisStatusPanel.Children[0] as Rectangle;
                            CheckBox thisStatusCheckBox = thisStatusPanel.Children[1] as CheckBox;

                            if (thisStatusCheckBox != null && colourBox != null)
                            {
                                StatusValue status = StatusFromString(thisStatusCheckBox.Content as string);
                                Color fillColour = ((SolidColorBrush)(colourBox.Fill)).Color;
                                m_statusColorMap.Add(status, fillColour);
                            }
                        }
                    }
                }
                return m_statusColorMap;
            }
        }

        private Dictionary<PlanDisplay.PriorityValue, System.Windows.Media.Color> m_priorityColorMap = null;
        private Dictionary<PlanDisplay.PriorityValue, System.Windows.Media.Color> PriorityColorMap
        {
            get
            {
                if (m_priorityColorMap == null)
                {
                    m_priorityColorMap = new Dictionary<PriorityValue, Color>();

                    foreach (StackPanel thisPriorityPanel in stackPanelPriority.Children)
                    {
                        if (thisPriorityPanel.Children.Count > 1)
                        {
                            Rectangle colourBox = thisPriorityPanel.Children[0] as Rectangle;
                            CheckBox thisPriroityCheckBox = thisPriorityPanel.Children[1] as CheckBox;

                            if (thisPriroityCheckBox != null && colourBox != null)
                            {
                                PriorityValue priority = PriorityFromString(thisPriroityCheckBox.Content as string);
                                Color fillColour = ((SolidColorBrush)(colourBox.Fill)).Color;
                                m_priorityColorMap.Add(priority, fillColour);
                            }
                        }
                    }
                }
                return m_priorityColorMap;
            }
        }





        public void RedisplayProject(Project projectToRedisplay, IEnumerable<string> resources, Dictionary<string, Color> resourceColourMap)
        {

            //get current date marker and set it
            if (m_projectOnDisplay != null && m_projectOnDisplay.DateForTimeLineMarker.HasValue)
                projectToRedisplay.SetDateLine(m_projectOnDisplay.DateForTimeLineMarker.Value);

            m_projectOnDisplay = projectToRedisplay;
            m_projectOnDisplay.Redisplay();
            canvasMain.Width = m_projectOnDisplay.Width;
            canvasMain.Height = m_projectOnDisplay.Height;
            canvasMain.Children.Clear();
            canvasMain.Children.Add(m_projectOnDisplay);
            m_resourceLevelOnDisplay = null;
            //canvasMain.Width = 1000;

            labelStartDate.Content = m_projectOnDisplay.MinStartDate.HasValue ?
                m_projectOnDisplay.MinStartDate.Value.ToString("dd-MMM-yyyy") :
                "";
            labelEndDate.Content = m_projectOnDisplay.MaxVisibleEndDate.HasValue ?
                m_projectOnDisplay.MaxVisibleEndDate.Value.ToString("dd-MMM-yyyy")
                : "";

            /////
            m_resourceColourMap = resourceColourMap;
            List<string> newResources = new List<string>(resources);
            newResources.Sort();
            if (newResources.Contains(DBProjectPal.Task.OtherResource))
            {
                newResources.Remove(DBProjectPal.Task.OtherResource);
                newResources.Add(DBProjectPal.Task.OtherResource);
            }

            int newIndex = 0, existingIndex = 0;
            int newEnd = newResources.Count - (newResources.Contains(DBProjectPal.Task.OtherResource) ? 1 : 0);
            if (m_resources == null)
            {
                m_resources = new List<string>();
                stackPanelPeopleSelector.Children.Clear();
                stackPanelPeople.Children.Clear();
            }
            int existingEnd = m_resources.Count - (m_resources.Contains(DBProjectPal.Task.OtherResource) ? 1 : 0);

            while (newIndex < newEnd && existingIndex < existingEnd)
            {
                string newName = newResources[newIndex];
                //CheckBox cb = (stackPanelPeople.Children[existingIndex] as StackPanel).Children[1] as CheckBox;
                string existingName = m_resources[existingIndex];

                if (newName == existingName)
                {
                    newIndex++;
                    existingIndex++;
                    continue;
                }
                if (string.Compare(newName, existingName, true) < 0)
                {
                    // Add new name
                    StackPanel horizontalPanel = MakePersonPanel(newName);

                    stackPanelPeople.Children.Insert(existingIndex, horizontalPanel);
                    stackPanelPeopleSelector.Children.Insert(existingIndex, MakePersonCheckBox(newName));
                    m_resources.Insert(existingIndex, newName);
                    newIndex++;
                    existingEnd++;
                    existingIndex++;
                }
                else
                {
                    // Take away existing name
                    stackPanelPeople.Children.RemoveAt(existingIndex);
                    stackPanelPeopleSelector.Children.RemoveAt(existingIndex);
                    m_resources.RemoveAt(existingIndex);
                    existingEnd--;
                }

            }
            while (newIndex < newEnd)
            {
                string newName = newResources[newIndex];
                StackPanel horizontalPanel = MakePersonPanel(newName);

                stackPanelPeople.Children.Insert(existingIndex, horizontalPanel);
                stackPanelPeopleSelector.Children.Insert(existingIndex, MakePersonCheckBox(newName));
                m_resources.Insert(existingIndex, newName);
                newIndex++;
                existingEnd++;
                existingIndex++;
            }


            while (existingIndex < existingEnd)
            {
                stackPanelPeople.Children.RemoveAt(existingIndex);
                stackPanelPeopleSelector.Children.RemoveAt(existingIndex);
                m_resources.RemoveAt(existingIndex);
                existingEnd--;
            }

            if (m_resources.Count != newResources.Count) // must be a change in "Other"
            {
                if (m_resources.Count < newResources.Count)
                {
                    m_resources.Add(DBProjectPal.Task.OtherResource);
                    StackPanel horizontalPanel = MakePersonPanel(DBProjectPal.Task.OtherResource);
                    stackPanelPeople.Children.Insert(existingIndex, horizontalPanel);
                    stackPanelPeopleSelector.Children.Insert(existingIndex, MakePersonCheckBox(DBProjectPal.Task.OtherResource));
                    existingEnd++;
                }
                else
                {
                    stackPanelPeople.Children.RemoveAt(existingIndex);
                    stackPanelPeopleSelector.Children.RemoveAt(existingIndex);
                    m_resources.RemoveAt(existingIndex);
                    existingEnd--;
                }

            }

            SetTaskColors(checkBoxShowTasks.IsChecked == false);


            AddResourceDisplay();

        }



        private void AddResourceDisplay()
        {
            if (m_projectOnDisplay.MinStartDate.HasValue && m_projectOnDisplay.MaxEndDate.HasValue)
            {
                PlanDisplay.ResourceLevel resGraph = new PlanDisplay.ResourceLevel(
                   m_projectOnDisplay.MinStartDate.Value,
                   m_projectOnDisplay.MaxEndDate.Value);


                IEnumerable<PlanDisplay.Task> allTasks = m_projectOnDisplay.AllDisplayedTasks;

                foreach (PlanDisplay.Task currentTask in allTasks)
                {
                    if (!currentTask.StartDate.HasValue || !currentTask.EndDate.HasValue)
                        continue;

                    resGraph.AddResource(currentTask.StartDate.Value,
                                         currentTask.EndDate.Value,
                                         currentTask.EffortInDays,
                                         currentTask.Resources,
                                         currentTask.Priority, currentTask.Status);
                }

                SetResourceGraph(resGraph);

            }
        }



        List<string> m_resources = null;
        Dictionary<string, Color> m_resourceColourMap = null;

        private CheckBox MakePersonCheckBox(string name)
        {
            CheckBox cb = new CheckBox();
            cb.Content = name;
            if (name == "Unassigned")
                cb.IsChecked = false;
            else
                cb.IsChecked = true;
            cb.Height = 16;
            cb.Checked += new RoutedEventHandler(checkBoxPerson_Checked);
            cb.Unchecked += new RoutedEventHandler(checkBoxPerson_Unchecked);
            return cb;
        }

        private StackPanel MakePersonPanel(string name)
        {

            StackPanel horizontalPanel = new StackPanel();
            horizontalPanel.Orientation = Orientation.Horizontal;
            Rectangle colourBox = new Rectangle();
            colourBox.Height = 12;
            colourBox.Width = 16;

            colourBox.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            Brush fillBrush = ColourBoxFill;
            Color fillColour;
            if (m_resourceColourMap.TryGetValue(name, out fillColour))
            {
                fillBrush = new System.Windows.Media.SolidColorBrush(m_resourceColourMap[name]);
            }
            colourBox.Fill = fillBrush;
            colourBox.Stroke = ColourBoxBoarder;

            CheckBox cb = new CheckBox();
            cb.Content = name;
            cb.IsChecked = true;
            cb.Height = 16;
            cb.Checked += new RoutedEventHandler(cb_CheckedChanged);
            cb.Unchecked += new RoutedEventHandler(cb_CheckedChanged);

            horizontalPanel.Children.Add(colourBox);
            horizontalPanel.Children.Add(cb);

            return horizontalPanel;
        }

        void cb_CheckedChanged(object sender, RoutedEventArgs e)
        {
            SetTaskColors(checkBoxShowTasks.IsChecked == false);

            if (m_projectOnDisplay != null) 
                m_projectOnDisplay.ReDrawDependencies();
        }


        static System.Windows.Media.Brush ColourBoxFill = new System.Windows.Media.SolidColorBrush(Color.FromArgb(128, 255, 255, 255));
        static System.Windows.Media.Brush ColourBoxBoarder = new System.Windows.Media.SolidColorBrush(Color.FromArgb(128, 0, 0, 0));

        private void RedisplayEffortText()
        {
            const double manDaysPerMonth = (365 - 52 * 2 /*week ends*/
                                                - 8 /*Bank Holidays*/
                                                - 25 /*Personal holidays*/
                                                - 3 /*Sick days*/) / 12.0;

            double manDaysEffort = m_resourceLevelOnDisplay == null ? 0 : m_resourceLevelOnDisplay.EffortOnDisplay;
            labelEffortMonths.Text = (((int)(((int)(manDaysEffort + 0.99)) / manDaysPerMonth * 10.0)) / 10.0).ToString() + " months";
            labelEffortDays.Text = "(" + ((int)(manDaysEffort + 0.99)).ToString() + " days)";

        }

        public void SetResourceGraph(ResourceLevel resourceLevelToAdd)
        {
            m_resourceLevelOnDisplay = resourceLevelToAdd;
            m_resourceLevelOnDisplay.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            resourceLevelToAdd.Redisplay(m_lastSelectedPeople, m_lastPersonColourMap, m_lastPriortyColourMap, m_lastStatusColourMap, m_projectOnDisplay.MaxVisibleEndDate);
            RedisplayEffortText();


            canvasMain.Children.Add(resourceLevelToAdd);
            canvasMain.Height += resourceLevelToAdd.Height;
        }
        ResourceLevel m_resourceLevelOnDisplay = null;

        private void canvasMain_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (canvasMain.Children.Count > 0)
                {
                    Project topProject = null;
                    for (int i = 0; i < canvasMain.Children.Count; i++)
                    {
                        topProject = canvasMain.Children[i] as Project;
                        if (topProject != null)
                            break;
                    }
                    if (topProject != null)
                    {
                        Point mousePosition = e.GetPosition(canvasMain);
                        double length = canvasMain.ActualWidth;
                        object a = canvasMain.Children[0];
                        double relativeLengthPosition = mousePosition.X / length;

                        if (topProject.MinStartDate.HasValue && topProject.MaxEndDate.HasValue && topProject.MaxVisibleEndDate.HasValue)
                        {
                            DateTime startDate = topProject.MinStartDate.Value;
                            DateTime endDate = topProject.MaxVisibleEndDate.Value;
                            int busDayDiff = 1 + Utils.Misc.DiffBusinessDays(endDate, startDate);

                            double daysDiff = busDayDiff * relativeLengthPosition;

                            DateTime positionDate = Utils.Misc.AddBusinessDays(startDate, (int)(busDayDiff * relativeLengthPosition));

                            labelDate.Content = positionDate.ToString("dd-MMM-yyyy");
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Logger.LogException(err, "Error in MouseMove");
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            SetScroll();
        }

        private void radioButtonPeople_Checked(object sender, RoutedEventArgs e)
        {

            //radioButtonPriority.IsChecked = false;
            groupBoxPeople.Visibility = System.Windows.Visibility.Visible;
            SetTaskColors(checkBoxShowTasks.IsChecked == false);

            m_projectOnDisplay.ReDrawDependencies();

        }



        private void radioButtonPeople_Unchecked(object sender, RoutedEventArgs e)
        {

            groupBoxPeople.Visibility = System.Windows.Visibility.Collapsed;
            //SetTaskColors();
        }


        private void radioButtonStatus_Checked(object sender, RoutedEventArgs e)
        {

            //radioButtonPriority.IsChecked = false;
            if (groupBoxStatus != null)
            {
                groupBoxStatus.Visibility = System.Windows.Visibility.Visible;
                SetTaskColors(checkBoxShowTasks.IsChecked == false);

                m_projectOnDisplay.ReDrawDependencies();
            }

        }

        private void radioButtonStatus_Unchecked(object sender, RoutedEventArgs e)
        {

            groupBoxStatus.Visibility = System.Windows.Visibility.Collapsed;
            //SetTaskColors();
        }

        private void radioButtonNone_Checked(object sender, RoutedEventArgs e)
        {

            SetTaskColors(checkBoxShowTasks.IsChecked == false);

            if (m_projectOnDisplay != null) 
                m_projectOnDisplay.ReDrawDependencies();

        }

      



        private void radioButtonPriority_Checked(object sender, RoutedEventArgs e)
        {
            //radioButtonPeople.IsChecked = false;
            groupBoxPriority.Visibility = System.Windows.Visibility.Visible;
            SetTaskColors(checkBoxShowTasks.IsChecked == false);

            m_projectOnDisplay.ReDrawDependencies();
        }

        private void radioButtonPriority_Unchecked(object sender, RoutedEventArgs e)
        {
            groupBoxPriority.Visibility = System.Windows.Visibility.Collapsed;
            //SetTaskColors();
        }


        private Dictionary<string, Color> m_lastPersonColourMap = null;
        private Dictionary<PriorityValue, Color> m_lastPriortyColourMap = null;
        private Dictionary<StatusValue, Color> m_lastStatusColourMap = null;
        private HashSet<string> m_lastSelectedPeople = new HashSet<string>();




        private void SetTaskColors(bool hideTasks)
        {
            if (m_projectOnDisplay == null)
                return;

            m_lastSelectedPeople.Clear();
            foreach (CheckBox thisPersionCheckBox in stackPanelPeopleSelector.Children)
            {
                if (thisPersionCheckBox.IsChecked == true)
                    m_lastSelectedPeople.Add(thisPersionCheckBox.Content as string);
            }

            if (radioButtonPeople != null && radioButtonPeople.IsChecked == true)
            {
                Dictionary<string, Color> colourMap = new Dictionary<string, Color>();

                HashSet<string> highlightedAndSelectedPeople = new HashSet<string>();
                foreach (StackPanel thisPersionPanel in stackPanelPeople.Children)
                {
                    if (thisPersionPanel.Children.Count > 1)
                    {
                        CheckBox thisPersonCheckBox = thisPersionPanel.Children[1] as CheckBox;
                        if (thisPersonCheckBox != null && thisPersonCheckBox.IsChecked == true)
                        {
                            string personName = thisPersonCheckBox.Content as string;
                            Color personsColor;
                            if (m_resourceColourMap.TryGetValue(personName, out personsColor))
                            {
                                if (m_lastSelectedPeople.Contains(personName))
                                {
                                    colourMap.Add(personName, personsColor);
                                    highlightedAndSelectedPeople.Add(personName);
                                }
                            }
                        }
                    }
                }


                m_projectOnDisplay.SetPeopleColorHighlights(highlightedAndSelectedPeople, colourMap, hideTasks);
                if (m_resourceLevelOnDisplay != null)
                {
                    m_resourceLevelOnDisplay.Redisplay(highlightedAndSelectedPeople, colourMap, null, null, m_projectOnDisplay.MaxVisibleEndDate);
                    m_lastPersonColourMap = colourMap;
                    m_lastPriortyColourMap = null;
                    m_lastStatusColourMap = null;
                }
            }
            else if (radioButtonPriority != null && radioButtonPriority.IsChecked == true)
            {
                Dictionary<PriorityValue, Color> colourMap = new Dictionary<PriorityValue, Color>();

                foreach (StackPanel thisPriorityPanel in stackPanelPriority.Children)
                {
                    if (thisPriorityPanel.Children.Count > 1)
                    {
                        CheckBox thisPriroityCheckBox = thisPriorityPanel.Children[1] as CheckBox;

                        if (thisPriroityCheckBox != null && thisPriroityCheckBox.IsChecked == true)
                        {
                            PriorityValue priority = PriorityFromString(thisPriroityCheckBox.Content as string);

                            Color priorityColor;
                            if (PriorityColorMap.TryGetValue(priority, out priorityColor))
                            {
                                colourMap.Add(priority, priorityColor);
                            }
                        }
                    }
                }
                m_projectOnDisplay.SetPriorityColorHighlights(m_lastSelectedPeople, colourMap, hideTasks);
                if (m_resourceLevelOnDisplay != null)
                {
                    m_resourceLevelOnDisplay.Redisplay(m_lastSelectedPeople, null, colourMap, null, m_projectOnDisplay.MaxVisibleEndDate);
                    m_lastPersonColourMap = null;
                    m_lastPriortyColourMap = colourMap;
                    m_lastStatusColourMap = null;
                }
            }
            else if (radioButtonStatus != null && radioButtonStatus.IsChecked == true)
            {
                Dictionary<StatusValue, Color> colourMap = new Dictionary<StatusValue, Color>();

                foreach (StackPanel thisStatusPanel in stackPanelStatus.Children)
                {
                    if (thisStatusPanel.Children.Count > 1)
                    {
                        CheckBox thisStatusCheckBox = thisStatusPanel.Children[1] as CheckBox;

                        if (thisStatusCheckBox != null && thisStatusCheckBox.IsChecked == true)
                        {
                            StatusValue status = StatusFromString(thisStatusCheckBox.Content as string);

                            Color statusColor;
                            if (StatusColorMap.TryGetValue(status, out statusColor))
                            {
                                colourMap.Add(status, statusColor);
                            }
                        }
                    }
                }
                m_projectOnDisplay.SetStatusColorHighlights(m_lastSelectedPeople, colourMap, hideTasks);
                if (m_resourceLevelOnDisplay != null)
                {
                    m_resourceLevelOnDisplay.Redisplay(m_lastSelectedPeople, null, null, colourMap, m_projectOnDisplay.MaxVisibleEndDate);
                    m_lastPersonColourMap = null;
                    m_lastPriortyColourMap = null;
                    m_lastStatusColourMap = colourMap;
                }
            }
            else
            {
                m_projectOnDisplay.SetNoHighlights(m_lastSelectedPeople, hideTasks);
                if (m_resourceLevelOnDisplay != null)
                {
                    m_resourceLevelOnDisplay.Redisplay(m_lastSelectedPeople, null, null, null, m_projectOnDisplay.MaxVisibleEndDate);
                    m_resourceLevelOnDisplay.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    m_lastPersonColourMap = null;
                    m_lastPriortyColourMap = null;
                    m_lastStatusColourMap = null;
                }

            }
            m_projectOnDisplay.DrawWeekAndDayLines();


            canvasMain.Width = m_projectOnDisplay.Width;
            labelEndDate.Content = m_projectOnDisplay.MaxVisibleEndDate.HasValue ?
               m_projectOnDisplay.MaxVisibleEndDate.Value.ToString("dd-MMM-yyyy")
               : "";

            RedisplayEffortText();

        }

        private PriorityValue PriorityFromString(string priorityName)
        {
            switch (priorityName)
            {
                case "High":
                    return PriorityValue._5_High;
                case "MedHigh":
                    return PriorityValue._4_MedHigh;
                case "Med":
                    return PriorityValue._3_Med;
                case "MedLow":
                    return PriorityValue._2_MedLow;
                case "Low":
                    return PriorityValue._1_Low;
                default:
                    throw new Exception("Unrecognised Priority value of '" + priorityName + "'");
            }
        }

        private StatusValue StatusFromString(string statusName)
        {
            switch (statusName)
            {
                case "Ready":
                    return StatusValue.Ready;
                case "InProgress":
                    return StatusValue.InProgress;
                case "NotStarted":
                    return StatusValue.NotStarted;
                case "Support":
                    return StatusValue.Support;
                case "Tentative":
                    return StatusValue.Tentative;
                case "Closed":
                    return StatusValue.Closed;
                case "Cancelled":
                    return StatusValue.Cancelled;
                default:
                    throw new Exception("Unrecognised Status value of '" + statusName + "'");
            }
        }

        private void checkBoxPerson_Checked(object sender, RoutedEventArgs e)
        {
            SetTaskColors(checkBoxShowTasks.IsChecked == false);

            if (m_projectOnDisplay != null)
                m_projectOnDisplay.ReDrawDependencies();
        }

        private void checkBoxPerson_Unchecked(object sender, RoutedEventArgs e)
        {
            SetTaskColors(checkBoxShowTasks.IsChecked == false);
            if (m_projectOnDisplay != null)
                m_projectOnDisplay.ReDrawDependencies();
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ReScaleToArea();
            if (m_projectOnDisplay != null)
                m_projectOnDisplay.ReDrawDependencies();
        }

        private void buttonAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox cb in stackPanelPeopleSelector.Children)
                cb.IsChecked = true;
        }

        private void buttonNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox cb in stackPanelPeopleSelector.Children)
                cb.IsChecked = false;
        }

        private void scrollViewerPlan_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {

        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            ReScaleToAreaData();
            ReScaleToArea();
        }

        public void SetCaption(string captionText)
        {
            labelCaption.Content = captionText;
        }

        private void checkBoxShowTasks_Checked(object sender, RoutedEventArgs e)
        {

            SetTaskColors(checkBoxShowTasks.IsChecked == false);
            if (m_projectOnDisplay != null)
                m_projectOnDisplay.ReDrawDependencies();
        }



    }
}
