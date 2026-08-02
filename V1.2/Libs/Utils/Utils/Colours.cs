using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Utils
{
    public class Colours
    {


        static Colours()
        {


            //ReadWriteColour = Color.FromArgb(255, 255, 218, 202); //Color.Bisque;
            //ReadWriteColour = Color.FromArgb(255, 200, 255, 255);
            ReadWriteColour = Color.White;
            //ReadOnlyColour = Color.White;
            ReadOnlyColour = Color.FromArgb(255, 190, 190, 190);
            //InactiveColour = Color.WhiteSmoke;
            InactiveColour = Color.WhiteSmoke;
            InactiveForeGround = Color.FromArgb(255, 100, 100, 100);
            MaxUrgency = Color.FromArgb(255, 128, 128);
        }

        private static Color[] s_colorCache = null;

        public static Color UrgencyColour(double urgencyIn)
        {
            int urgency = (int)urgencyIn;

            if (s_colorCache == null)
            {
                Color urgentMaxColour = Utils.Colours.MaxUrgency;
                Color urgentMinColour = Utils.Colours.ReadWriteColour;

                s_colorCache = new Color[101];
                for (int mix = 0; mix < 101; mix++)
                {
                    double mixMax = mix / 100.0;
                    double mixMin = 1 - mixMax;

                    int A = 255;// (int)(urgentMinColour.A * mixMin + urgentMaxColour.A * mixMax);
                    int R = (int)(urgentMinColour.R * mixMin + urgentMaxColour.R * mixMax);
                    int G = (int)(urgentMinColour.G * mixMin + urgentMaxColour.G * mixMax);
                    int B = (int)(urgentMinColour.B * mixMin + urgentMaxColour.B * mixMax);

                    R = Math.Max(0, Math.Min(255, R));
                    G = Math.Max(0, Math.Min(255, G));
                    B = Math.Max(0, Math.Min(255, B));

                    Color mixColour = Color.FromArgb(A, R, G, B);

                    s_colorCache[mix] = mixColour;
                }
            }

            int mixMaxInt = Math.Max(0, Math.Min(100, urgency - 100));
            return s_colorCache[mixMaxInt];

        }

        public static Color MaxUrgency { get; private set; }
        public static Color ReadWriteColour { get; private set; }
        public static Color ReadOnlyColour { get; private set; }
        public static Color InactiveColour { get; private set; }
        public static Color InactiveForeGround { get; private set; }



    }
}
