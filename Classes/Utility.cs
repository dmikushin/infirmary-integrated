﻿using System;

namespace II {

    public static class _ {

        public const float  Draw_Resolve = 0.01f;       // Tracing resolution (seconds per drawing point) in seconds
        public const int    Draw_Refresh = 10;          // Tracing draw refresh time in milliseconds

        public static float Time {
            get { return (float)(DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess ().StartTime.ToUniversalTime ()).TotalSeconds; }
        }

        public static float Clamp (float value, float min, float max) {
            return (value < min) ? min : (value > max) ? max : value;
        }

        public static int Clamp (int value, int min, int max) {
            return (value < min) ? min : (value > max) ? max : value;
        }

        public static float Clamp (float value) {
            return (value < 0) ? 0 : (value > 1) ? 1 : value;
        }

        public static float Lerp (float min, float max, float t) {
            return min * t + max * (1 - t);
        }

        public static float InverseLerp (float min, float max, float current) {
            return (current - min) / (max - min);
        }

        public static float RandomFloat (float min, float max) {
            Random r = new Random ();
            return (float)r.NextDouble () * (max - min) + min;
        }

    }
}