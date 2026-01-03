using System.Drawing;

namespace Library_Management_System.Common.Constants
{
    public static class ThemeConstants
    {
        // Primary Maroon Colors
        public static readonly Color PrimaryMaroon = Color.FromArgb(128, 0, 0);        // #800000
        public static readonly Color PrimaryMaroonDark = Color.FromArgb(102, 0, 0);   // #660000
        public static readonly Color PrimaryMaroonLight = Color.FromArgb(153, 0, 0);  // #990000

        // Secondary Maroon Colors
        public static readonly Color SecondaryMaroon = Color.FromArgb(128, 0, 32);    // #800020
        public static readonly Color SecondaryMaroonDark = Color.FromArgb(102, 0, 26); // #66001A
        public static readonly Color SecondaryMaroonLight = Color.FromArgb(153, 0, 38); // #990026

        // Accent Maroon Colors
        public static readonly Color AccentMaroon = Color.FromArgb(150, 0, 38);       // #960026
        public static readonly Color AccentMaroonHover = Color.FromArgb(180, 0, 45);  // #B4002D

        // Background Colors
        public static readonly Color BackgroundWhite = Color.White;
        public static readonly Color BackgroundLight = Color.FromArgb(248, 249, 250);
        public static readonly Color BackgroundMedium = Color.FromArgb(233, 236, 239);
        public static readonly Color BackgroundDark = Color.FromArgb(33, 37, 41);

        // Text Colors
        public static readonly Color TextPrimary = Color.FromArgb(33, 37, 41);
        public static readonly Color TextSecondary = Color.FromArgb(108, 117, 125);
        public static readonly Color TextLight = Color.FromArgb(173, 181, 189);
        public static readonly Color TextWhite = Color.White;

        // Status Colors
        public static readonly Color SuccessGreen = Color.FromArgb(34, 197, 94);
        public static readonly Color WarningOrange = Color.FromArgb(255, 152, 0);
        public static readonly Color ErrorRed = Color.FromArgb(244, 67, 54);
        public static readonly Color InfoBlue = Color.FromArgb(33, 150, 243);

        // Border and Shadow Colors
        public static readonly Color BorderLight = Color.FromArgb(222, 226, 230);
        public static readonly Color BorderMedium = Color.FromArgb(206, 212, 218);
        public static readonly Color ShadowLight = Color.FromArgb(0, 0, 0, 10);
        public static readonly Color ShadowMedium = Color.FromArgb(0, 0, 0, 20);

        // Fonts
        public static readonly Font FontTitle = new Font("Segoe UI", 24F, FontStyle.Bold);
        public static readonly Font FontSubtitle = new Font("Segoe UI", 14F, FontStyle.Regular);
        public static readonly Font FontHeader = new Font("Segoe UI", 16F, FontStyle.Bold);
        public static readonly Font FontBodyLarge = new Font("Segoe UI", 12F, FontStyle.Regular);
        public static readonly Font FontBody = new Font("Segoe UI", 10F, FontStyle.Regular);
        public static readonly Font FontBodySmall = new Font("Segoe UI", 9F, FontStyle.Regular);
        public static readonly Font FontButton = new Font("Segoe UI", 11F, FontStyle.Bold);

        // Common Measurements
        public static readonly int BorderRadius = 8;
        public static readonly int ButtonHeight = 40;
        public static readonly int InputHeight = 35;
        public static readonly int PaddingSmall = 8;
        public static readonly int PaddingMedium = 16;
        public static readonly int PaddingLarge = 24;

        // Helper Methods
        public static Color GetHoverColor(Color baseColor)
        {
            // Make color slightly lighter for hover effects
            int r = System.Math.Min(255, baseColor.R + 20);
            int g = System.Math.Min(255, baseColor.G + 20);
            int b = System.Math.Min(255, baseColor.B + 20);
            return Color.FromArgb(r, g, b);
        }

        public static Color GetPressedColor(Color baseColor)
        {
            // Make color slightly darker for pressed effects
            int r = System.Math.Max(0, baseColor.R - 20);
            int g = System.Math.Max(0, baseColor.G - 20);
            int b = System.Math.Max(0, baseColor.B - 20);
            return Color.FromArgb(r, g, b);
        }
    }
}

