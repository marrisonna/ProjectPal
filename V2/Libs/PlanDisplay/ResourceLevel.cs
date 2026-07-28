using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Text;

namespace PlanDisplay
{
    public class ResourceLevel : System.Windows.Controls.UserControl
    {




        public ResourceLevel(DateTime startDate, DateTime endDate)
        {
            m_startDate = startDate;
            m_endDate = endDate;

            int days = (m_endDate - m_startDate).Days;
            m_resourceLevel = new double[days + 1];
            for (int i = 0; i <= days; i++)
            {
                if (Utils.Misc.IsBusinessDay(m_startDate.AddDays(i)))
                    m_resourceLevel[i] = 0;
                else
                    m_resourceLevel[i] = -1; // not a valid business day
            }

            m_canvas = new Canvas();

        }


        List<ResourceLevelTask> m_tasks = new List<ResourceLevelTask>();
        public void AddResource(DateTime? startDate, DateTime? endDate,
                                double? effortInDays,
                                List<string> resources,
                                PlanDisplay.PriorityValue priority,
                                PlanDisplay.StatusValue status)
        {
            if (startDate.HasValue && endDate.HasValue)
            {
                ResourceLevelTask thisTask = new ResourceLevelTask(
                    Utils.Misc.GoodBusinessDay(startDate.Value),
                    Utils.Misc.GoodBusinessDay(endDate.Value),
                    effortInDays,
                    resources,
                    priority,
                    status);

                m_tasks.Add(thisTask);
            }
            else
            {
                ResourceLevelTask thisTask = new ResourceLevelTask(
                    effortInDays ?? 0,
                    resources,
                    priority,
                    status);

                m_tasks.Add(thisTask);
            }


            //int start = (startDate - m_startDate).Days;
            //if (start < 0)
            //    start = 0;
            //int end = (endDate - m_startDate).Days;
            //if (end > m_resourceLevel.Length)
            //    end = m_resourceLevel.Length;

            //for (int i = start; i <= end; i++)
            //{
            //    if (m_resourceLevel[i] != -1) // Is a valid business day
            //        m_resourceLevel[i] += resourceRate;
            //}
        }

        public void Redisplay(HashSet<string> selectedPeople,
                              Dictionary<string, Color> peopleColourMap,
                              Dictionary<PriorityValue, Color> priorityColourMap,
                              Dictionary<StatusValue, Color> statusColourMap,
                              DateTime? maxVisibleEndDate)
        {
            if (maxVisibleEndDate != null)
                m_endDate = maxVisibleEndDate.Value;
            m_canvas.Children.Clear();


            for (int i = 0; i < m_resourceLevel.Length; i++)
            {
                if (Utils.Misc.IsBusinessDay(m_startDate.AddDays(i)))
                    m_resourceLevel[i] = 0;
                else
                    m_resourceLevel[i] = -1; // not a valid business day
            }

            foreach (ResourceLevelTask task in m_tasks)
            {
                if (task.StartDate.HasValue && task.EndDate.HasValue && task.EffortInDays.HasValue)
                {

                    // "Others" don't count as they are not on this team
                    List<string> resourcesToConsider = new List<string>(task.Resources);
                    if (resourcesToConsider.Count == 0)
                        resourcesToConsider.Add("Unallocated");
                    resourcesToConsider.Remove(DBProjectPal.Task.OtherResource);



                    if (resourcesToConsider.Count == 0)
                        continue;


                    double personSelectedCount = 0;
                    foreach (string person in resourcesToConsider)
                    {
                        if (selectedPeople.Contains(person))
                        {
                            personSelectedCount++;
                        }
                    }
                    if (personSelectedCount == 0)
                        continue;

                    double percentageOfResourcesSelected = personSelectedCount / resourcesToConsider.Count;
                    
                    if (peopleColourMap != null)
                    {
                        bool personFound = false;
                        foreach (string person in resourcesToConsider)
                        {
                            if (peopleColourMap.ContainsKey(person))
                            {
                                personFound = true;
                                break;
                            }
                        }
                        if (!personFound)
                            continue;
                    }

                    if (priorityColourMap != null)
                    {
                        if (!priorityColourMap.ContainsKey(task.Priority))
                            continue;
                    }

                    if (statusColourMap != null)
                    {
                        if (!statusColourMap.ContainsKey(task.Status))
                            continue;
                    }


                    int start = (task.StartDate.Value - m_startDate).Days;
                    if (start < 0)
                        start = 0;
                    int end = (task.EndDate.Value - m_startDate).Days;
                    if (end > m_resourceLevel.Length)
                        end = m_resourceLevel.Length;

                    int dayCount=0;
                    for (int i = start; i <= end; i++)
                    {
                        if (m_resourceLevel[i] != -1)
                            dayCount++;
                    }
                   //double effortPerDay = personSelectedCount * task.PercentageAllocation;
                    double effortPerDay = task.EffortInDays.Value / dayCount * percentageOfResourcesSelected;

                    for (int i = start; i <= end; i++)
                    {
                        if (m_resourceLevel[i] != -1) // Is a valid business day
                            m_resourceLevel[i] += effortPerDay;
                    }
                }
            }


            double maxResourceLevel = 0;
            for (int i = 0; i < m_resourceLevel.Length; i++)
                if (maxResourceLevel < m_resourceLevel[i])
                    maxResourceLevel = m_resourceLevel[i];

            maxResourceLevel += 0.99; // extra padding
            Height = Constants.ResourceLevelTopPadding +
                (int)(maxResourceLevel) * Constants.OneResourceLevelHeight;
            Width = (1 + Utils.Misc.DiffBusinessDays(m_endDate, m_startDate)) * Constants.PixelsPerDay;

            ///////////////

            PathFigure fig0 = new PathFigure();
            fig0.StartPoint = new Point(0, Height);
            PolyLineSegment segment0 = new PolyLineSegment();

            int businessDayNumber = 0;
            m_effortOnDisplay = 0;
            int numberOfDaysToConsider = Math.Min(1 + (m_endDate - m_startDate).Days, m_resourceLevel.Length);
            for (int i = 0; i < numberOfDaysToConsider; i++)
            {
                if (m_resourceLevel[i] == -1) // not a good business day
                    continue;
                m_effortOnDisplay += m_resourceLevel[i];
                Point p1 = new Point(businessDayNumber * Constants.PixelsPerDay,
                                     Height - m_resourceLevel[i] * Constants.OneResourceLevelHeight);

                Point p2 = new Point((businessDayNumber + 1) * Constants.PixelsPerDay,
                                     Height - m_resourceLevel[i] * Constants.OneResourceLevelHeight);

                segment0.Points.Add(p1);
                segment0.Points.Add(p2);
                businessDayNumber++;
            }

            Point p3 = new Point(businessDayNumber * Constants.PixelsPerDay, Height);
            segment0.Points.Add(p3);

            fig0.Segments.Add(segment0);
            fig0.IsClosed = true;

            PathGeometry pg0 = new PathGeometry();
            PathFigureCollection figures0 = new PathFigureCollection();
            pg0.Figures = figures0;
            pg0.Figures.Add(fig0);


            Path image = new Path();
            image.Data = pg0;


            image.Stroke = BorderStroke;
            image.StrokeThickness = 1;
            image.Fill = FillBrush;

            m_canvas.Children.Add(image);




            /// Level Lines

            for (int level = 1; level <= maxResourceLevel; level++)
            {
                PathFigure figLine = new PathFigure();
                figLine.StartPoint = new Point(0, Height - level * Constants.OneResourceLevelHeight);
                PolyLineSegment segmentLine = new PolyLineSegment();
                segmentLine.Points.Add(new Point(Width, Height - level * Constants.OneResourceLevelHeight));

                figLine.Segments.Add(segmentLine);
                figLine.IsClosed = false;

                PathGeometry pgLine = new PathGeometry();
                PathFigureCollection figuresLine = new PathFigureCollection();
                pgLine.Figures = figuresLine;
                pgLine.Figures.Add(figLine);


                Path lineImage = new Path();
                lineImage.Data = pgLine;

                lineImage.Stroke = LevelStroke;
                if (level % 5 == 0)
                    lineImage.StrokeThickness = 1.5;
                else
                    lineImage.StrokeThickness = 0.5;

                m_canvas.Children.Add(lineImage);
            }


            /// Border
            {
                PathFigure figBorder = new PathFigure();
                figBorder.StartPoint = new Point(0, 0);
                PolyLineSegment segmentBorder = new PolyLineSegment();
                segmentBorder.Points.Add(new Point(Width, 0));
                segmentBorder.Points.Add(new Point(Width, Height));
                segmentBorder.Points.Add(new Point(0, Height));

                //segmentBorder.Points.Add(new Point(0, 0));

                figBorder.Segments.Add(segmentBorder);
                figBorder.IsClosed = true;

                PathGeometry pgBorder = new PathGeometry();
                PathFigureCollection figuresBorder = new PathFigureCollection();
                pgBorder.Figures = figuresBorder;
                pgBorder.Figures.Add(figBorder);


                Path borderImage = new Path();
                borderImage.Data = pgBorder;

                borderImage.Stroke = LevelStroke;
                borderImage.StrokeThickness = 1;

                m_canvas.Children.Add(borderImage);
            }

            m_canvas.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            this.Content = m_canvas;
        }


        public double EffortOnDisplay
        {
            get
            {
                return m_effortOnDisplay;
            }
        }


        static System.Windows.Media.Brush LevelStroke = new System.Windows.Media.SolidColorBrush(Color.FromArgb(255, 0, 0, 255));
        static System.Windows.Media.Brush BorderStroke = new System.Windows.Media.SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
        static System.Windows.Media.Brush FillBrush = new System.Windows.Media.SolidColorBrush(Color.FromArgb(64, 255, 200, 210));





        DateTime m_startDate;
        DateTime m_endDate;

        double[] m_resourceLevel;
        private Canvas m_canvas;
        private double m_effortOnDisplay = 0;


    }
}
