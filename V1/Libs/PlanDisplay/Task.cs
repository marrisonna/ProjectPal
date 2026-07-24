using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Text;
using Utils;

namespace PlanDisplay
{
    public enum PriorityValue { _5_High = 5, _4_MedHigh = 4, _3_Med = 3, _2_MedLow = 2, _1_Low = 1, _0_Closed = 0, _0_Cancelled = -1 }
    public enum StatusValue { Cancelled = 0, Closed = 1, InProgress = 2, NotStarted = 3, Support = 4, Tentative = 5, Ready = 6 }



    public class Task : TimeBox
    {


        override public bool IsVisible { get { return m_image != null && this.Visibility == System.Windows.Visibility.Visible; } }

        public bool HasAssignedResources
        {
            get
            {
                if (m_resources == null || m_resources.Count == 0)
                    return false;
                return true;
            }
        }

        override public DateTime? MinStartDate
        {
            get
            {
                if (!HasAssignedResources)
                    return null;
                return m_startDate;
            }
        }


        override public DateTime? MaxEndDate
        {
            get
            {
                if (!HasAssignedResources)
                    return null;
                return m_endDate;
            }
        }

        public DateTime? StartDate { get { return m_startDate; } }
        public DateTime? EndDate { get { return m_endDate; } }
        public PlanDisplay.PriorityValue Priority { get { return m_priority; } }
        public PlanDisplay.StatusValue Status { get { return m_status; } }
        public double? EffortInDays { get { return m_effortInDays; } }
        public new List<string> Resources { get { return m_resources; } }

        override public double TotalHeight
        {
            get
            {
                return Constants.TaskHeight;
            }
        }

        override public double ScaledTotalHeight
        {
            get
            {
                return Constants.TaskHeight;
            }
        }

        private DateTime? m_startDate;
        private DateTime? m_endDate;
        private List<string> m_resources;
        private PriorityValue m_priority;
        private StatusValue m_status;
        private double? m_effortInDays;



        void Task_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {



            if (m_image.IsMouseDirectlyOver)
            {
                bool ctrlButtonDown =
                   System.Windows.Input.Keyboard.IsKeyDown(Key.LeftCtrl) ||
                   System.Windows.Input.Keyboard.IsKeyDown(Key.RightCtrl);

                if (ctrlButtonDown)
                    SendEvent(Event.CtrlMouseDoubleClick);
                else
                    SendEvent(Event.MouseDoubleClick);
            }
            e.Handled = true;
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

        void Task_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (m_startMove.HasValue)
            {
                Point currentPosition = e.GetPosition(this);

                m_taskMoveRectangle.Margin = new Thickness(m_canvas.Margin.Left + m_xOffset + m_scale.ScaleX * (currentPosition.X - m_startMove.Value.X), m_yOffset, 0, 0);


            }
            else
                if (m_image.IsMouseDirectlyOver)
                    SendEvent(Event.MouseMove);
        }

        void Task_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SendEvent(Event.MouseLeave);
        }

        void Task_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SendEvent(Event.MouseEnter);
        }




        public Task(DBTaskMan.Task underlyingObject, PlanControl theOwningPlanControl, DateTime? startDate, DateTime? endDate,
                    List<string> resources, PriorityValue priority, StatusValue status,
                    double? effortInDays)
            : base(underlyingObject, theOwningPlanControl)
        {
            m_startDate = startDate.HasValue ? (DateTime?)Utils.Misc.GoodBusinessDay(startDate.Value.Date) : null;
            m_endDate = endDate.HasValue ? (DateTime?)Utils.Misc.GoodBusinessDay(endDate.Value.Date) : null;
            m_resources = resources;
            m_priority = priority;
            m_status = status;
            m_effortInDays = effortInDays;
            Redisplay();

            this.MouseEnter += new System.Windows.Input.MouseEventHandler(Task_MouseEnter);
            this.MouseLeave += new System.Windows.Input.MouseEventHandler(Task_MouseLeave);
            this.MouseMove += new System.Windows.Input.MouseEventHandler(Task_MouseMove);
            this.MouseDoubleClick += new System.Windows.Input.MouseButtonEventHandler(Task_MouseDoubleClick);
            this.MouseDown += new System.Windows.Input.MouseButtonEventHandler(Task_MouseDown);
            this.MouseUp += new System.Windows.Input.MouseButtonEventHandler(Task_MouseUp);

            ZOrder = 0;

            ContextMenu m = new ContextMenu();

            MenuItem mi2 = new MenuItem();
            mi2.Header = "Task Detail";
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

        void mi3_Click(object sender, RoutedEventArgs e)
        {
            StartMove(new Point(ActualWidth / 2, 0));
        }

        void mi2_Click(object sender, RoutedEventArgs e)
        {
            SendEvent(Event.MouseDoubleClick);
        }


        void Task_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (m_taskMoveRectangle != null)
            {
                FinishMove(e.GetPosition(this));
            }
        }

        void FinishMove(Point currentPoint)
        {
            if (m_taskMoveRectangle != null && m_startMove != null)
            {
                Point currentPosition = currentPoint;
                double daysToShift = (currentPosition.X - m_startMove.Value.X) / Constants.PixelsPerDay;
                daysToShift += Math.Sign(daysToShift) * 0.5;


                int businessDayShift = (int)(daysToShift);
                SendEvent(Event.DayShift, businessDayShift);

                //if (m_thePlanControlGrid != null)
                //{
                //m_thePlanControlGrid.Children.Remove(m_taskMoveRectangle);
                m_theOwningPlanControl.ThePlanControlGrid.Children.Remove(m_taskMoveRectangle);
                //}
                //m_thePlanControlGrid = null;
                FinishMove();
            }
        }


        private void FinishMove()
        {
            m_taskMoveRectangle = null;
            m_startMove = null;
            m_scale = null;

            System.Windows.Input.Mouse.Capture(null);
        }

        Point? m_startMove = null;
        Path m_taskMoveRectangle = null;

        void Task_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                if (m_image.IsMouseDirectlyOver)
                {
                    bool shiftButtonDown =
                        System.Windows.Input.Keyboard.IsKeyDown(Key.LeftShift) ||
                        System.Windows.Input.Keyboard.IsKeyDown(Key.RightShift);

                    bool ctrlButtonDown =
                        System.Windows.Input.Keyboard.IsKeyDown(Key.LeftCtrl) ||
                        System.Windows.Input.Keyboard.IsKeyDown(Key.RightCtrl);

                    if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed &&
                       shiftButtonDown &&
                        m_startMove == null)
                    {
                        StartMove(e.GetPosition(this));
                    }
                    else if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed &&
                             ctrlButtonDown)
                    {
                        DragDropEffects effects = ctrlButtonDown ?
                                            DragDropEffects.Link :
                                            DragDropEffects.Move;

                        DBTaskMan.Task theTask = UnderlyingObject as DBTaskMan.Task;

                        if (Permissions.IsAllowed(theTask.Owner, Permissions.EntityType.Task, Permissions.ChangeType.Edit))
                        {
                            DataObject data = Utils.DragDrop.DragHelper.SetDraggedObjectWPF(UnderlyingObject);

                            DragDrop.DoDragDrop(this, data, effects);
                        }

                    }


                }
                else if (m_startMove != null)
                {
                    FinishMove(e.GetPosition(this));
                }
                else
                    m_startMove = null;

            }
            catch (Exception err)
            {
                Logger.LogException(err, "Error from PlanDisplay on mouse down");
            }
        }


        void StartMove(Point currentPosition)
        {
            if (m_taskMoveRectangle == null)
            {
                try
                {
                    m_startMove = currentPosition;

                    m_scale = m_theOwningPlanControl.canvasMain.LayoutTransform as ScaleTransform;

                    PathFigure fig0 = new PathFigure();
                    fig0.StartPoint = new Point(0, 0);
                    PolyLineSegment segment0 = new PolyLineSegment();
                    segment0.Points.Add(new Point(Width * m_scale.ScaleX, 0));
                    segment0.Points.Add(new Point(Width * m_scale.ScaleX, Height * m_scale.ScaleY * m_yScaleCumulative));
                    segment0.Points.Add(new Point(0, Height * m_scale.ScaleY * m_yScaleCumulative));

                    fig0.Segments.Add(segment0);
                    fig0.IsClosed = true;

                    PathGeometry pg0 = new PathGeometry();
                    PathFigureCollection figures0 = new PathFigureCollection();
                    pg0.Figures = figures0;
                    pg0.Figures.Add(fig0);

                    m_taskMoveRectangle = new Path();
                    m_taskMoveRectangle.Data = pg0;
                    m_taskMoveRectangle.Stroke = MoveRectangleStroke;
                    m_taskMoveRectangle.StrokeThickness = 4;

                    System.Windows.Input.Mouse.Capture(this);

                    //if (m_thePlanControlGrid != null)
                    //{
                    //Point planPt = m_thePlanControlGrid.PointToScreen(new Point(0, 0));
                    Point planPt = m_theOwningPlanControl.ThePlanControlGrid.PointToScreen(new Point(0, 0));

                    Point canvasPt = m_canvas.PointToScreen(new Point(0, 0));

                    m_xOffset = canvasPt.X - planPt.X;
                    m_yOffset = canvasPt.Y - planPt.Y;

                    m_taskMoveRectangle.Margin = new Thickness(m_canvas.Margin.Left + m_xOffset, m_canvas.Margin.Top + m_yOffset, 0, 0);
                    //m_thePlanControlGrid.Children.Add(m_taskMoveRectangle);
                    m_theOwningPlanControl.ThePlanControlGrid.Children.Add(m_taskMoveRectangle);
                    //}
                }
                catch (Exception err)
                {

                    Logger.LogException(err, "Error in StartMove on Task");
                    FinishMove();
                }
            }
        }


        //Grid m_thePlanControlGrid = null;
        ScaleTransform m_scale = null;
        double m_xOffset = 0;
        double m_yOffset = 0;



        public void SetStatusColourHighlights(Dictionary<StatusValue, Color> colourMap)
        {
            Color colour;

            if (colourMap.TryGetValue(this.m_status, out colour))
            {
                this.Visibility = System.Windows.Visibility.Visible;
                m_image.Fill = new SolidColorBrush(colour);
            }
            else
            {
                this.Visibility = System.Windows.Visibility.Collapsed;
            }
        }


        public void SetPersonColourHighlights(HashSet<string> selectedPeople,
                                              Dictionary<string, Color> colourMap,
                                              bool hideTask)
        {
            if (m_image != null)
            {
                bool personSelected = false;
                foreach (string person in m_resources)
                {
                    if (selectedPeople.Contains(person))
                    {
                        personSelected = true;
                        break;
                    }
                }

                if (!personSelected)
                {
                    this.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    List<Color> colours = new List<Color>();
                    foreach (string person in m_resources)
                    {
                        Color personColour;
                        if (colourMap.TryGetValue(person, out personColour))
                            colours.Add(personColour);
                    }
                    if (colours.Count == 0)
                        this.Visibility = System.Windows.Visibility.Collapsed;
                    else
                    {
                        this.Visibility = System.Windows.Visibility.Visible;

                        if (hideTask)
                        {
                            m_image.Fill = TransparentColour;
                            m_image.Stroke = TransparentColour;
                        }
                        else
                        {
                            m_image.Stroke = BorderStroke;
                            if (colours.Count == 1)
                            {
                                m_image.Fill = new SolidColorBrush(colours[0]);
                            }
                            else
                            {
                                GradientStopCollection colourFill = new GradientStopCollection(colours.Count);
                                double step = 1.0 / (colours.Count);
                                double currentOffset = 0;
                                foreach (Color currentColour in colours)
                                {
                                    colourFill.Add(new GradientStop(currentColour, currentOffset));
                                    colourFill.Add(new GradientStop(currentColour, currentOffset + step));
                                    currentOffset += step;
                                }
                                LinearGradientBrush brush = new LinearGradientBrush(colourFill, 45);
                                brush.StartPoint = new Point(0.5, 0);
                                brush.EndPoint = new Point(0.5, 1);
                                m_image.Fill = brush;
                            }
                        }
                    }
                }
            }
        }


        public void SetStatusColorHighlights(HashSet<string> selectedPeople,
                                             Dictionary<StatusValue, Color> colourMap,
                                             bool hideTask)
        {
            if (m_image != null)
            {
                bool personSelected = false;
                foreach (string person in m_resources)
                {
                    if (selectedPeople.Contains(person))
                    {
                        personSelected = true;
                        break;
                    }
                }

                Color colour;
                if (personSelected && colourMap.TryGetValue(this.m_status, out colour))
                {
                    this.Visibility = System.Windows.Visibility.Visible;
                    if (hideTask)
                    {
                        m_image.Fill = TransparentColour;
                        m_image.Stroke = TransparentColour;
                    }
                    else
                    {
                        m_image.Stroke = BorderStroke;
                        m_image.Fill = new SolidColorBrush(colour);
                    }
                }
                else
                {
                    this.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }



        public void SetNoHighlights(HashSet<string> selectedPeople, bool hideTask)
        {
            if (m_image != null)
            {
                bool personSelected = false;
                foreach (string person in m_resources)
                {
                    if (selectedPeople.Contains(person))
                    {
                        personSelected = true;
                        break;
                    }
                }

                if (!personSelected)
                {
                    this.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    if (hideTask)
                    {
                        m_image.Fill = TransparentColour;
                        m_image.Stroke = TransparentColour;
                    }
                    else
                    {
                        m_image.Stroke = BorderStroke;
                        m_image.Fill = NotActiveBrush;
                    }
                    this.Visibility = System.Windows.Visibility.Visible;
                }

            }

        }

        public void SetPriorityColorHighlights(HashSet<string> selectedPeople,
                                               Dictionary<PriorityValue, Color> colourMap,
                                               bool hideTask)
        {
            if (m_image != null)
            {
                bool personSelected = false;
                foreach (string person in m_resources)
                {
                    if (selectedPeople.Contains(person))
                    {
                        personSelected = true;
                        break;
                    }
                }

                Color colour;

                if (personSelected && colourMap.TryGetValue(this.m_priority, out colour))
                {
                    this.Visibility = System.Windows.Visibility.Visible;
                    if (hideTask)
                    {
                        m_image.Fill = TransparentColour;
                        m_image.Stroke = TransparentColour;
                    }
                    else
                    {
                        m_image.Fill = new SolidColorBrush(colour);
                        m_image.Stroke = BorderStroke;
                    }
                }
                else
                {
                    this.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }


        override public void Redisplay()
        {
            Redisplay(1, 1);
        }



        double m_yScaleCumulative = 1;
        override public List<TimeBox> Redisplay(double yScale, double yScaleCumulative)
        {
            List<TimeBox> allDisplayedItems = new List<TimeBox>();
            allDisplayedItems.Add(this);

            m_yScaleCumulative = yScaleCumulative;
            m_canvas = new Canvas();

            if (m_endDate.HasValue && m_startDate.HasValue)
            {
                this.Height = Constants.TaskHeight;
                this.Width = (1.0 /*day*/ + Utils.Misc.DiffBusinessDays(m_endDate.Value, m_startDate.Value)) * Constants.PixelsPerDay;

                PathFigure fig1 = new PathFigure();
                fig1.StartPoint = new Point(0, 0);

                PolyLineSegment segment = new PolyLineSegment();

                segment.Points.Add(new Point(Width, 0));
                segment.Points.Add(new Point(Width, Height));
                segment.Points.Add(new Point(0, Height));


                fig1.Segments.Add(segment);
                fig1.IsClosed = true;

                PathGeometry pg = new PathGeometry();
                PathFigureCollection figures = new PathFigureCollection();
                pg.Figures = figures;
                pg.Figures.Add(fig1);


                m_image = new Path();
                m_image.Data = pg;



                m_image.Stroke = BorderStroke;
                m_image.StrokeThickness = 1;
                m_image.Fill = NotActiveBrush;

                m_image.MouseDown += new System.Windows.Input.MouseButtonEventHandler(m_image_MouseDown);

                m_canvas.Children.Add(m_image);
                Visibility = System.Windows.Visibility.Visible;

            }
            else
            {
                Visibility = System.Windows.Visibility.Collapsed;
            }
            this.Content = m_canvas;
            this.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;

            return allDisplayedItems;

        }
        Canvas m_canvas = null;

        void m_image_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }

        public override Point? LeftDependencyNode
        {
            get
            {
                //Point planPt = m_theOwningPlanControl.ThePlanControlGrid.PointToScreen(new Point(0, 0));


                //Point leftNodePt = m_image.PointToScreen(new Point(0, m_image.ActualHeight / 2.0));

                //double xOffset = leftNodePt.X - planPt.X;
                //double yOffset = leftNodePt.Y - planPt.Y;

                //return new Point(xOffset, yOffset);

                return this.TranslatePoint(new Point(0, Height / 2.0), m_theOwningPlanControl.ThePlanControlGrid);
            }
        }

        public override Point? RightDependencyNode
        {
            get
            {
                // Point planPt = m_theOwningPlanControl.ThePlanControlGrid.PointToScreen(new Point(0, 0));


                //Point leftNodePt = m_image.PointToScreen(new Point(m_image.ActualWidth, m_image.ActualHeight / 2.0));

                //double xOffset = leftNodePt.X - planPt.X;
                //double yOffset = leftNodePt.Y - planPt.Y;

                //return new Point(xOffset, yOffset);


                return this.TranslatePoint(new Point(Width, Height / 2.0), m_theOwningPlanControl.ThePlanControlGrid);
            }
        }



        static System.Windows.Media.Brush TransparentColour = new System.Windows.Media.SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        static System.Windows.Media.Brush MoveRectangleStroke = new System.Windows.Media.SolidColorBrush(Color.FromArgb(128, 0, 0, 0));
        static System.Windows.Media.Brush NormalStroke = new System.Windows.Media.SolidColorBrush(Color.FromArgb(128, 0, 0, 0));
        static System.Windows.Media.Brush BorderStroke = new System.Windows.Media.SolidColorBrush(Color.FromArgb(128, 75, 75, 75));
        static System.Windows.Media.Brush OnStroke = new System.Windows.Media.SolidColorBrush(Color.FromArgb(128, 0, 255, 0));
        static System.Windows.Media.Brush NotActiveBrush = new System.Windows.Media.SolidColorBrush(Color.FromArgb(255, 200, 0, 128));


        private Path m_image;
    }
}
