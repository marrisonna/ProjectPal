using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Text;
using System.Windows.Input;
using Utils;

namespace PlanDisplay
{
    public class Project : TimeBox
    {


        override public bool IsVisible { get { return this.Visibility == System.Windows.Visibility.Visible; } }

        override public DateTime? MinStartDate
        {
            get
            {
                DateTime? minDate = null;

                if (m_underlyingTaskOrProject is DBTaskMan.Project)
                {
                    minDate = (m_underlyingTaskOrProject as DBTaskMan.Project).StartDate;
                }
                foreach (TimeBox item in m_plannedItems)
                {
                    DateTime? minBoxDate = item.MinStartDate;
                    if (minDate == null || minBoxDate < minDate)
                        minDate = minBoxDate;
                }
                if (m_dueDate.HasValue && (minDate == null || m_dueDate.Value < minDate))
                    minDate = Utils.Misc.AddBusinessDays(m_dueDate.Value, -1);

                return minDate;
            }
        }


        override public DateTime? MaxEndDate
        {
            get
            {
                DateTime? maxDate = null;
                foreach (TimeBox item in m_plannedItems)
                {
                    DateTime? maxBoxDate = item.MaxEndDate;
                    if (maxDate == null || maxBoxDate > maxDate)
                        maxDate = maxBoxDate;
                }

                if (m_dueDate.HasValue && (maxDate == null || m_dueDate.Value > maxDate))
                    maxDate = Utils.Misc.AddBusinessDays(m_dueDate.Value, 1);

                return maxDate;
            }
        }


        public DateTime? MaxVisibleEndDate
        {
            get
            {
                DateTime? maxDate = null;
                foreach (TimeBox item in m_plannedItems)
                {
                    if (!item.IsVisible)
                        continue;
                    Project itemAsProject = item as Project;

                    DateTime? maxBoxDate = itemAsProject == null ? item.MaxEndDate : itemAsProject.MaxVisibleEndDate;
                    if (maxDate == null || maxBoxDate > maxDate)
                        maxDate = maxBoxDate;
                }

                if (m_dueDate.HasValue && (maxDate == null || m_dueDate.Value > maxDate))
                    maxDate = Utils.Misc.AddBusinessDays(m_dueDate.Value, 1);

                return maxDate;
            }
        }

        private DateTime? m_dueDate;

        public EventType RequiredEvents { get; private set; }

        public enum EventType { None, Project, Component }

        public Project(object underlyingObject, PlanControl theOwningPlanControl, DateTime? dueDate, EventType requiredEvents)
            : base(underlyingObject, theOwningPlanControl)
        {
            Initialise(dueDate, requiredEvents);
        }

        public Project(DBTaskMan.Project underlyingObject, PlanControl theOwningPlanControl, DateTime? dueDate, EventType requiredEvents)
            : base(underlyingObject, theOwningPlanControl)
        {
            Initialise(dueDate, requiredEvents);
        }

        private void Initialise(DateTime? dueDate, EventType requiredEvents)
        {

            m_grid = new Grid();
            m_dueDate = dueDate;
            this.Initialized += new EventHandler(Project_Initialized);
            this.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;

            this.MouseEnter += new System.Windows.Input.MouseEventHandler(Project_MouseEnter);
            this.MouseLeave += new System.Windows.Input.MouseEventHandler(Project_MouseLeave);
            this.MouseMove += new System.Windows.Input.MouseEventHandler(Project_MouseMove);
            this.MouseDoubleClick += new System.Windows.Input.MouseButtonEventHandler(Project_MouseDoubleClick);
            this.MouseDown += new System.Windows.Input.MouseButtonEventHandler(Project_MouseDown);
            this.MouseUp += new System.Windows.Input.MouseButtonEventHandler(Project_MouseUp);
            this.SizeChanged += new SizeChangedEventHandler(Project_SizeChanged);
            ZOrder = 0;

            RequiredEvents = requiredEvents;
            if (requiredEvents == EventType.Project)
            {
                ContextMenu m = new ContextMenu();
                MenuItem mi1 = new MenuItem();
                mi1.Header = "New Window";
                mi1.InputGestureText = "Ctrl Double Click";
                mi1.IsCheckable = false;
                mi1.Click += new RoutedEventHandler(mi1_Click);
                m.Items.Add(mi1);

                MenuItem mi2 = new MenuItem();
                mi2.Header = "Project Detail";
                mi2.InputGestureText = "Double Click";
                mi2.IsCheckable = false;
                mi2.Click += new RoutedEventHandler(mi2_Click);
                m.Items.Add(mi2);

                MenuItem mi3 = new MenuItem();
                mi3.Header = "Change Due Date";
                mi3.InputGestureText = "Shift Left";
                mi3.IsCheckable = false;
                mi3.Click += new RoutedEventHandler(mi3_Click);
                m.Items.Add(mi3);

                this.ContextMenu = m;
            }
            else if (requiredEvents == EventType.Component)
            {
                ContextMenu m = new ContextMenu();

                MenuItem mi2 = new MenuItem();
                mi2.Header = "Component Detail";
                mi2.InputGestureText = "Double Click";
                mi2.IsCheckable = false;
                mi2.Click += new RoutedEventHandler(mi2_Click);
                m.Items.Add(mi2);

                this.ContextMenu = m;
            }
        }



        void mi3_Click(object sender, RoutedEventArgs e)
        {
            StartMove(new Point(ActualWidth / 2, 0));
        }

        void mi2_Click(object sender, RoutedEventArgs e)
        {
            SendEvent(Event.MouseDoubleClick);
        }

        void mi1_Click(object sender, RoutedEventArgs e)
        {
            SendEvent(Event.CtrlMouseDoubleClick);
        }


        void Project_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (m_projectMoveRectangle != null)
            {
                FinishMove(e.GetPosition(this));
            }
        }

        void FinishMove(Point currentPoint)
        {
            if (m_projectMoveRectangle != null && m_startMove != null)
            {
                Point currentPosition = currentPoint;

                double daysToShift = (currentPosition.X - m_startMove.Value.X) / Constants.PixelsPerDay;
                daysToShift += Math.Sign(daysToShift) * 0.5;
                int businessDayShift = (int)(daysToShift);

                SendEvent(Event.DayShift, businessDayShift);

                //m_grid.Children.Remove(m_projectMoveRectangle);

                m_theOwningPlanControl.ThePlanControlGrid.Children.Remove(m_projectMoveRectangle);
                m_projectMoveRectangle = null;
                m_startMove = null;
                m_scale = null;

                System.Windows.Input.Mouse.Capture(null);
            }
        }


        Point? m_startMove = null;
        Path m_projectMoveRectangle = null;
        ScaleTransform m_scale = null;
        double m_xOffset = 0;
        double m_yOffset = 0;


        void Project_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

            if (RequiredEvents == EventType.Project)
            {
                if (m_projectImage.IsMouseDirectlyOver)
                {
                    bool shiftButtonDown =
                        System.Windows.Input.Keyboard.IsKeyDown(Key.LeftShift) ||
                        System.Windows.Input.Keyboard.IsKeyDown(Key.RightShift);

                    bool ctrlButtonDown =
                          System.Windows.Input.Keyboard.IsKeyDown(Key.LeftCtrl) ||
                          System.Windows.Input.Keyboard.IsKeyDown(Key.RightCtrl);


                    if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed &&
                            ctrlButtonDown)
                    {
                        DragDropEffects effects = ctrlButtonDown ?
                                            DragDropEffects.Link :
                                            DragDropEffects.Move;

                        DBTaskMan.Project theProject = UnderlyingObject as DBTaskMan.Project;

                        if (Permissions.IsAllowed(theProject.Owner, Permissions.EntityType.Project, Permissions.ChangeType.Edit))
                        {
                            DataObject data = Utils.DragDrop.DragHelper.SetDraggedObjectWPF(UnderlyingObject);

                            DragDrop.DoDragDrop(this, data, effects);
                        }

                    }

                    else if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed &&
                        shiftButtonDown &&
                         m_startMove == null)
                    {
                        StartMove(e.GetPosition(this));
                    }
                    else if (m_startMove != null)
                    {
                        FinishMove(e.GetPosition(this));
                    }
                    else
                        m_startMove = null;
                }
            }
        }

        void StartMove(Point startPosition)
        {
            m_startMove = startPosition;

            if (m_projectMoveRectangle == null && m_grid != null)
            {

                //object previousParent = null;
                //object parent = m_grid.Parent;
                //double yScale = 1;

                //  ScaleTransform gridSt = m_grid.LayoutTransform as ScaleTransform;
                //  if (gridSt != null)
                //      yScale *= gridSt.ScaleY;

                //while (parent != null & (parent as PlanControl) == null)
                //{
                //    if (parent as ScrollViewer != null)
                //        m_thePlanControlGrid = previousParent as Grid;

                //    previousParent = parent;
                //    ContentControl cc = parent as ContentControl;
                //    if (cc != null)
                //    {
                //        parent = cc.Parent;
                //    }
                //    else
                //    {
                //        FrameworkElement fe = parent as FrameworkElement;
                //        if (fe != null)
                //        {
                //            parent = fe.Parent;
                //            Grid g = parent as Grid;
                //            if (g != null)
                //            {
                //                ScaleTransform st = g.LayoutTransform as ScaleTransform;
                //                if (st != null)
                //                    yScale *= st.ScaleY;
                //            }
                //        }
                //        else
                //        {
                //            parent = null;
                //        }
                //        //parent = parent.Parent;
                //    }
                //}
                //PlanControl topLevelPlanControl = (parent as PlanControl);
                m_scale = m_theOwningPlanControl.canvasMain.LayoutTransform as ScaleTransform;

                if (m_scale == null)
                {
                    Utils.Logger.Log("ERROR: Canvas has no LayoutTransform in PlanDisplay.Project.StartMove");
                    m_startMove = null;
                    return;
                }

                PathFigure fig0 = new PathFigure();
                fig0.StartPoint = new Point(0, 0);
                PolyLineSegment segment0 = new PolyLineSegment();
                segment0.Points.Add(new Point(Width * m_scale.ScaleX, 0));
                segment0.Points.Add(new Point(Width * m_scale.ScaleX, this.ActualHeight * m_scale.ScaleY * m_yScaleCumulative));
                segment0.Points.Add(new Point(0, this.ActualHeight * m_scale.ScaleY * m_yScaleCumulative));

                fig0.Segments.Add(segment0);
                fig0.IsClosed = true;

                PathGeometry pg0 = new PathGeometry();
                PathFigureCollection figures0 = new PathFigureCollection();
                pg0.Figures = figures0;
                pg0.Figures.Add(fig0);

                m_projectMoveRectangle = new Path();
                m_projectMoveRectangle.Data = pg0;
                m_projectMoveRectangle.Stroke = MoveRectangleStroke;
                m_projectMoveRectangle.StrokeThickness = 4;
                //m_projectMoveRectangle.Margin = this.Margin; 
                bool f = m_projectMoveRectangle.Focus();

                System.Windows.Input.Mouse.Capture(this);

                //m_grid.Children.Add(m_projectMoveRectangle);


                Point planPt = m_theOwningPlanControl.ThePlanControlGrid.PointToScreen(new Point(0, 0));
                Point canvasPt = m_grid.PointToScreen(new Point(0, 0));

                m_xOffset = canvasPt.X - planPt.X;
                m_yOffset = canvasPt.Y - planPt.Y;

                //m_startMove = new Point(m_startMove.Value.X + m_xOffset, m_startMove.Value.Y + m_yOffset);
                m_projectMoveRectangle.Margin = new Thickness(m_grid.Margin.Left + m_xOffset, m_grid.Margin.Top + m_yOffset, 0, 0);
                m_theOwningPlanControl.ThePlanControlGrid.Children.Add(m_projectMoveRectangle);

            }

            else
                m_startMove = null;

        }

        void Project_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (RequiredEvents != EventType.None)
            {
                if (m_projectImage.IsMouseDirectlyOver)
                {
                    bool ctrlButtonDown =
                       System.Windows.Input.Keyboard.IsKeyDown(Key.LeftCtrl) ||
                       System.Windows.Input.Keyboard.IsKeyDown(Key.RightCtrl);

                    if (ctrlButtonDown && RequiredEvents == EventType.Project)
                        SendEvent(Event.CtrlMouseDoubleClick);
                    else
                        SendEvent(Event.MouseDoubleClick);
                }


                e.Handled = true;
            }
        }

        int m_zOrder = 0;
        override public int ZOrder
        {
            get { return m_zOrder; }
            set
            {
                m_zOrder = value;
                this.SetValue(Canvas.ZIndexProperty, m_zOrder);
            }
        }


        void Project_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {

            if (m_startMove.HasValue)
            {
                Point currentPosition = e.GetPosition(this);

                m_projectMoveRectangle.Margin = new Thickness(m_grid.Margin.Left + m_xOffset + m_scale.ScaleX * (currentPosition.X - m_startMove.Value.X), m_yOffset, 0, 0);


            }
            else if (m_projectImage.IsMouseDirectlyOver)
                SendEvent(Event.MouseMove);

        }

        void Project_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (RequiredEvents != EventType.None)
                SendEvent(Event.MouseLeave);
        }

        void Project_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (RequiredEvents != EventType.None)
                SendEvent(Event.MouseEnter);
        }




        private bool m_isInitialised = false;
        void Project_Initialized(object sender, EventArgs e)
        {
            //Redisplay();
            m_isInitialised = true;
        }

        override public double TotalHeight
        {
            get
            {
                double totalHeight = 0;
                foreach (TimeBox plannedItem in m_plannedItems)
                    totalHeight += plannedItem.ScaledTotalHeight;

                return totalHeight;
            }
        }

        override public double ScaledTotalHeight
        {
            get
            {
                double totalHeight = 0;
                foreach (TimeBox plannedItem in m_plannedItems)
                    totalHeight += plannedItem.ScaledTotalHeight;

                return m_yScale * totalHeight;
            }
        }

        List<TimeBox> m_allDisplayedItems;
        bool m_eventAdded = false;
        override public void Redisplay()
        {
            m_allDisplayedItems = Redisplay(m_yScale, 1);

            m_drawDependenciesDone = false;

            if (!m_eventAdded)
            {
                m_eventAdded = true;
                this.LayoutUpdated += new EventHandler(Project_LayoutUpdated);
            }
        }


        bool m_drawDependenciesDone = false;
        void Project_LayoutUpdated(object sender, EventArgs e)
        {
            if (!m_drawDependenciesDone)
            {
                m_drawDependenciesDone = true;
                DrawDependencies();
            }
        }

        public void ReDrawDependencies()
        {
            m_drawDependenciesDone = false;
            m_theOwningPlanControl.UpdateLayout();
        }

        public void DrawDependencies()
        {
            //return;
            //this.UpdateLayout();

            // Remove existing lines
            int childCount = m_theOwningPlanControl.ThePlanControlGrid.Children.Count;
            for (int i = 0; i < childCount; i++)
            {
                if (m_theOwningPlanControl.ThePlanControlGrid.Children[i].GetType() == typeof(Line))
                {
                    m_theOwningPlanControl.ThePlanControlGrid.Children.RemoveAt(i);
                    childCount--;
                    i--;
                }
            }


            foreach (TimeBox currentItem in m_allDisplayedItems)
            {
                if (currentItem == this)
                    continue;

                if (currentItem.Visibility != System.Windows.Visibility.Visible)
                    continue;

                List<DBTaskMan.ITaskOrProject> dependants = currentItem.GetDependants();

                foreach (TimeBox currentCheckItem in m_allDisplayedItems)
                {
                    if (currentCheckItem.m_underlyingTaskOrProject == null)
                        continue;

                    if (currentCheckItem.Visibility != System.Windows.Visibility.Visible)
                        continue;

                    if (dependants.Contains(currentCheckItem.m_underlyingTaskOrProject))
                    {

                        Point? to = currentCheckItem.LeftDependencyNode;
                        Point? from = currentItem.RightDependencyNode;

                        if (from.HasValue && to.HasValue)
                        {
                            Line line = new Line();
                            line.Stroke = DependencyStroke;
                            line.StrokeThickness = 1 * m_theOwningPlanControl.VerticalScale;
                            line.X1 = from.Value.X;
                            line.Y1 = from.Value.Y;
                            line.X2 = to.Value.X;
                            line.Y2 = to.Value.Y;
                            //myLine.HorizontalAlignment = HorizontalAlignment.Left;
                            //myLine.VerticalAlignment = VerticalAlignment.Center;


                            m_theOwningPlanControl.ThePlanControlGrid.Children.Add(line);
                        }

                    }

                }

            }

        }



        double m_yScale = 1;
        double m_yScaleCumulative = 1;
        override public List<TimeBox> Redisplay(double yScale, double yScaleCumulative)
        {
            List<TimeBox> allDisplayedItems = new List<TimeBox>();
            allDisplayedItems.Add(this);

            m_yScale = yScale;
            m_yScaleCumulative = yScaleCumulative;
            m_grid.Children.Clear();

            foreach (TimeBox item in m_plannedItems)
            {
                allDisplayedItems.AddRange(item.Redisplay(/*yScale **/ 0.75, m_yScaleCumulative * yScale));
            }

            m_theStackPanel = new StackPanel();
            m_projectImage = m_theStackPanel;


            if (MaxVisibleEndDate.HasValue && MinStartDate.HasValue)
            {
                DateTime minStartDate = MinStartDate.Value;
                DateTime maxEndDate = MaxVisibleEndDate.Value;

                double outerWidth = (1 /*day*/ + Utils.Misc.DiffBusinessDays(maxEndDate, minStartDate)) * Constants.PixelsPerDay;
                Width = outerWidth;

                m_theStackPanel.Width = outerWidth;
                m_theStackPanel.Background = OuterBrush;

                m_theBorder = new Border();
                m_theBorder.Child = m_theStackPanel;
                m_theBorder.BorderThickness = new Thickness(Constants.ProjectBorderThickness);
                m_theBorder.BorderBrush = BorderStroke;

                m_grid.Children.Add(m_theBorder);
                m_grid.LayoutTransform = new ScaleTransform(1, yScale);

                for (int t = 0; t < m_plannedItems.Count; t++)
                {
                    TimeBox currentPlannedItem = m_plannedItems[t];

                    if (currentPlannedItem.MinStartDate.HasValue)
                    {
                        double xMargin = Utils.Misc.DiffBusinessDays(currentPlannedItem.MinStartDate.Value, minStartDate) * Constants.PixelsPerDay;
                        xMargin = xMargin - Constants.ProjectBorderThickness;

                        currentPlannedItem.Margin = new Thickness(xMargin, 0, 0, 0);
                        m_theStackPanel.Children.Add(currentPlannedItem);
                    }
                }
            }
            this.Content = m_grid;
            return allDisplayedItems;
        }

        void Project_SizeChanged(object sender, SizeChangedEventArgs e)
        {

            if (m_dateForTimeLineMarker.HasValue || m_dueDate.HasValue)
            {

                if (m_grid.Children.Count > 1)
                {
                    while (m_grid.Children.Count > 1)
                        m_grid.Children.RemoveAt(m_grid.Children.Count - 1);
                    if (e.NewSize.Height < e.PreviousSize.Height)
                        return;
                }
                DrawWeekAndDayLines();
            }
        }

        public void DrawWeekAndDayLines()
        {
            if (MinStartDate.HasValue && MaxVisibleEndDate.HasValue)
            {
                if (m_dateForTimeLineMarker.HasValue)
                {
                    DateTime minStartDate = MinStartDate.Value;
                    DateTime maxEndDate = MaxVisibleEndDate.Value;

                    // If Date line marker is set, then add week lines too

                    {

                        DateTime currentDate = minStartDate;
                        bool doMark = false;
                        while (currentDate <= maxEndDate)
                        {
                            if (!Utils.Misc.IsBusinessDay(currentDate))
                            {
                                doMark = true;
                                currentDate = currentDate.AddDays(1);
                                continue;
                            }
                            if (doMark)
                            {
                                doMark = false;
                                double dayLocation = m_theStackPanel.ActualWidth *
                                     ((double)Utils.Misc.DiffBusinessDays(currentDate, minStartDate)) /
                                     (1 + (double)Utils.Misc.DiffBusinessDays(maxEndDate, minStartDate));

                                PathFigure dayFigLine = new PathFigure();
                                dayFigLine.StartPoint = new Point(dayLocation, m_theBorder.BorderThickness.Top);
                                PolyLineSegment daySegmentLine = new PolyLineSegment();
                                daySegmentLine.Points.Add(new Point(dayLocation, m_theBorder.BorderThickness.Top + m_theStackPanel.ActualHeight));

                                dayFigLine.Segments.Add(daySegmentLine);
                                dayFigLine.IsClosed = false;

                                PathGeometry dayPgLine = new PathGeometry();
                                PathFigureCollection dayFiguresLine = new PathFigureCollection();
                                dayPgLine.Figures = dayFiguresLine;
                                dayPgLine.Figures.Add(dayFigLine);


                                Path dayImage = new Path();
                                dayImage.Data = dayPgLine;

                                dayImage.Stroke = DayMarkerStroke;
                                dayImage.StrokeThickness = 0.75;


                                m_grid.Children.Add(dayImage);



                            }
                            currentDate = currentDate.AddDays(1);
                        }


                    }





                    double location = m_theStackPanel.ActualWidth *
                                     ((double)Utils.Misc.DiffBusinessDays(m_dateForTimeLineMarker.Value, minStartDate)) /
                                     (1 + (double)Utils.Misc.DiffBusinessDays(maxEndDate, minStartDate));

                    PathFigure figLine = new PathFigure();
                    figLine.StartPoint = new Point(location, m_theBorder.BorderThickness.Top);
                    PolyLineSegment segmentLine = new PolyLineSegment();
                    segmentLine.Points.Add(new Point(location, m_theBorder.BorderThickness.Top + m_theStackPanel.ActualHeight));

                    figLine.Segments.Add(segmentLine);
                    figLine.IsClosed = false;

                    PathGeometry pgLine = new PathGeometry();
                    PathFigureCollection figuresLine = new PathFigureCollection();
                    pgLine.Figures = figuresLine;
                    pgLine.Figures.Add(figLine);


                    Path image = new Path();
                    image.Data = pgLine;

                    image.Stroke = ToDayMarkerStroke;
                    image.StrokeThickness = 1;


                    m_grid.Children.Add(image);

                }

                if (m_dueDate.HasValue)
                {
                    DateTime minStartDate = MinStartDate.Value;
                    DateTime maxEndDate = MaxVisibleEndDate.Value;

                    double location = m_theStackPanel.ActualWidth *
                     ((double)Utils.Misc.DiffBusinessDays(m_dueDate.Value, minStartDate)) /
                     (1 + (double)Utils.Misc.DiffBusinessDays(maxEndDate, minStartDate));

                    PathFigure figLine = new PathFigure();
                    figLine.StartPoint = new Point(location, m_theBorder.BorderThickness.Top);
                    PolyLineSegment segmentLine = new PolyLineSegment();
                    segmentLine.Points.Add(new Point(location, m_theBorder.BorderThickness.Top + m_theStackPanel.ActualHeight));

                    figLine.Segments.Add(segmentLine);
                    figLine.IsClosed = false;

                    PathGeometry pgLine = new PathGeometry();
                    PathFigureCollection figuresLine = new PathFigureCollection();
                    pgLine.Figures = figuresLine;
                    pgLine.Figures.Add(figLine);


                    Path image = new Path();
                    image.Data = pgLine;

                    image.Stroke = DueDayMarkerStroke;
                    image.StrokeThickness = 2;


                    m_grid.Children.Add(image);

                }
            }
        }


        public DateTime? DateForTimeLineMarker
        {
            get
            {
                return m_dateForTimeLineMarker;
            }
        }


        private DateTime? m_dateForTimeLineMarker = null;
        public void SetDateLine(DateTime dateForLine)
        {
            m_dateForTimeLineMarker = dateForLine;
        }



        public void AddPlannedItem(TimeBox theplannedItem)
        {
            theplannedItem.SetEventFunction(m_eventFunction);
            theplannedItem.ZOrder = ZOrder + 1;
            theplannedItem.ParentProject = this;
            m_plannedItems.Add(theplannedItem);
            if (m_isInitialised) Redisplay();
        }

        List<TimeBox> m_plannedItems = new List<TimeBox>();


        internal IEnumerable<PlanDisplay.Task> AllDisplayedTasks
        {
            get
            {
                // A task may appear multiple times, e.g., if it is part of more than one project.
                // so we use the underlying object to ensure we only count tasks once.
                Dictionary<object, PlanDisplay.Task> displayedTasks = new Dictionary<object, Task>();

                GetAllDisplayedTasks(displayedTasks);
                return displayedTasks.Values;
            }
        }

        private void GetAllDisplayedTasks(Dictionary<object, PlanDisplay.Task> displayedTasks)
        {
            foreach (TimeBox plannedItem in m_plannedItems)
            {
                Project plannedProject = plannedItem as Project;
                if (plannedProject != null)
                    plannedProject.GetAllDisplayedTasks(displayedTasks);
                else
                {
                    Task plannedTask = plannedItem as Task;
                    if (plannedTask != null)
                    {
                        // If the task is already in the dictionary, replace it.
                        displayedTasks[plannedTask.UnderlyingObject] = plannedTask;
                    }
                }
            }
        }


        public void SetPeopleColorHighlights(HashSet<string> selectedPeople,
                                             Dictionary<string, Color> colourMap,
                                             bool hideTasks)
        {
            //if (m_dateForTimeLineMarker.HasValue)
            //{
            //    while (m_grid.Children.Count > 1)
            //        m_grid.Children.RemoveAt(m_grid.Children.Count - 1);
            //}


            bool anyVisable = false;
            foreach (TimeBox plannedItem in m_plannedItems)
            {
                Project plannedProject = plannedItem as Project;
                if (plannedProject != null)
                {
                    plannedProject.SetPeopleColorHighlights(selectedPeople, colourMap, hideTasks);
                    if (plannedProject.Visibility == System.Windows.Visibility.Visible)
                        anyVisable = true;
                }
                else
                {
                    Task plannedTask = plannedItem as Task;
                    if (plannedTask != null)
                    {
                        plannedTask.SetPersonColourHighlights(selectedPeople, colourMap, hideTasks);
                        if (plannedTask.IsVisible)
                            anyVisable = true;
                    }
                }
            }
            if (anyVisable == false)
                this.Visibility = System.Windows.Visibility.Collapsed;
            else
            {
                this.Visibility = System.Windows.Visibility.Visible;
                SetVisibleWidth();
            }

        }




        public void SetNoHighlights(HashSet<string> selectedPeople, bool hideTasks)
        {
            bool anyVisable = false;
            foreach (TimeBox plannedItem in m_plannedItems)
            {
                Project plannedProject = plannedItem as Project;
                if (plannedProject != null)
                {
                    plannedProject.SetNoHighlights(selectedPeople, hideTasks);
                    if (plannedProject.Visibility == System.Windows.Visibility.Visible)
                        anyVisable = true;
                }
                else
                {
                    Task plannedTask = plannedItem as Task;
                    if (plannedTask != null)
                    {
                        plannedTask.SetNoHighlights(selectedPeople, hideTasks);
                        if (plannedTask.IsVisible)
                            anyVisable = true;
                    }
                }
            }
            if (anyVisable == false)
                this.Visibility = System.Windows.Visibility.Collapsed;
            else
            {
                this.Visibility = System.Windows.Visibility.Visible;
                SetVisibleWidth();
            }

        }

        public void SetStatusColorHighlights(HashSet<string> selectedPeople,
                                             Dictionary<StatusValue, Color> colourMap,
                                             bool hideTasks)
        {


            bool anyVisable = false;
            foreach (TimeBox plannedItem in m_plannedItems)
            {
                Project plannedProject = plannedItem as Project;
                if (plannedProject != null)
                {
                    plannedProject.SetStatusColorHighlights(selectedPeople, colourMap, hideTasks);
                    if (plannedProject.Visibility == System.Windows.Visibility.Visible)
                        anyVisable = true;
                }
                else
                {
                    Task plannedTask = plannedItem as Task;
                    if (plannedTask != null)
                    {
                        plannedTask.SetStatusColorHighlights(selectedPeople, colourMap, hideTasks);
                        if (plannedTask.IsVisible)
                            anyVisable = true;
                    }
                }
            }
            if (anyVisable == false)
                this.Visibility = System.Windows.Visibility.Collapsed;
            else
            {
                this.Visibility = System.Windows.Visibility.Visible;
                SetVisibleWidth();
            }

        }


        public void SetPriorityColorHighlights(HashSet<string> selectedPeople,
                                               Dictionary<PriorityValue, Color> colourMap,
                                               bool hideTasks)
        {


            bool anyVisable = false;
            foreach (TimeBox plannedItem in m_plannedItems)
            {
                Project plannedProject = plannedItem as Project;
                if (plannedProject != null)
                {
                    plannedProject.SetPriorityColorHighlights(selectedPeople, colourMap, hideTasks);
                    if (plannedProject.Visibility == System.Windows.Visibility.Visible)
                        anyVisable = true;
                }
                else
                {
                    Task plannedTask = plannedItem as Task;
                    if (plannedTask != null)
                    {
                        plannedTask.SetPriorityColorHighlights(selectedPeople, colourMap, hideTasks);
                        if (plannedTask.IsVisible)
                            anyVisable = true;
                    }
                }
            }
            if (anyVisable == false)
                this.Visibility = System.Windows.Visibility.Collapsed;
            else
            {
                this.Visibility = System.Windows.Visibility.Visible;
                SetVisibleWidth();
            }
        }

        private void SetVisibleWidth()
        {
            DateTime minStartDate = MinStartDate.Value;
            DateTime maxEndDate = MaxVisibleEndDate.Value;
            double outerWidth = (1 /*day*/ + Utils.Misc.DiffBusinessDays(maxEndDate, minStartDate)) * Constants.PixelsPerDay;
            Width = outerWidth;
            m_theStackPanel.Width = outerWidth;
        }

        public override Point? LeftDependencyNode
        {
            get
            {


                //Point planPt = m_theOwningPlanControl.ThePlanControlGrid.PointToScreen(new Point(0, 0));
                //Point leftNodePt = m_theBorder.PointToScreen(new Point(0, m_theBorder.ActualHeight / 2.0));


                //double xOffset = leftNodePt.X - planPt.X;
                //double yOffset = leftNodePt.Y - planPt.Y;

                //return new Point(xOffset, yOffset);

                //return this.TranslatePoint(new Point(0, Height / 2.0), m_theOwningPlanControl.ThePlanControlGrid);
                return m_theBorder.TranslatePoint(new Point(0, m_theBorder.ActualHeight / 2.0), m_theOwningPlanControl.ThePlanControlGrid);
            }
        }

        public override Point? RightDependencyNode
        {
            get
            {
                //Point planPt = m_theOwningPlanControl.ThePlanControlGrid.PointToScreen(new Point(0, 0));
                //Point leftNodePt = m_theBorder.PointToScreen(new Point(m_theBorder.ActualWidth, m_theBorder.ActualHeight / 2.0));

                //double xOffset = leftNodePt.X - planPt.X;
                //double yOffset = leftNodePt.Y - planPt.Y;

                //return new Point(xOffset, yOffset);

                return m_theBorder.TranslatePoint(new Point(m_theBorder.ActualWidth, m_theBorder.ActualHeight / 2.0), m_theOwningPlanControl.ThePlanControlGrid);
            }
        }

        private Grid m_grid;
        private Border m_theBorder;
        private StackPanel m_theStackPanel;
        private StackPanel m_projectImage;



        static System.Windows.Media.Brush MoveRectangleStroke = new System.Windows.Media.SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
        static System.Windows.Media.Brush BorderStroke = new System.Windows.Media.SolidColorBrush(Color.FromArgb(255, 75, 75, 75));
        static System.Windows.Media.Brush DependencyStroke = new System.Windows.Media.SolidColorBrush(Color.FromArgb(255, 255, 55, 55));
        static System.Windows.Media.Brush DayMarkerStroke = new System.Windows.Media.SolidColorBrush(Color.FromArgb(200, 150, 192, 158));
        static System.Windows.Media.Brush ToDayMarkerStroke = new System.Windows.Media.SolidColorBrush(Color.FromArgb(255, 255, 128, 0));
        static System.Windows.Media.Brush DueDayMarkerStroke = new System.Windows.Media.SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
        static System.Windows.Media.Brush OuterBrush = new System.Windows.Media.SolidColorBrush(Color.FromArgb(64, 200, 255, 210));
        static System.Windows.Media.Brush OnStroke = new System.Windows.Media.SolidColorBrush(Color.FromArgb(128, 0, 255, 0));
        static System.Windows.Media.Brush NotActiveBrush = new System.Windows.Media.SolidColorBrush(Color.FromArgb(255, 130, 100, 230));


    }
}
