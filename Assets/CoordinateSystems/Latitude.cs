using System;

namespace Cognitics.CoordinateSystems
{
    public struct Latitude : IEquatable<Latitude>
    {
        static public double MinValue => -90.0f;
        static public double MaxValue => 90.0f;
        static public implicit operator Latitude(double value) => new Latitude(value);
        static public implicit operator double(Latitude latitude) => latitude.value;

        ////////////////////////////////////////////////////////////

        public bool Equals(Latitude obj) => this == obj;

        private double value;

        Latitude(double value)
        {
            this.value = Math.Min(Math.Max(value, MinValue), MaxValue);
        }

        public int TileWidth
        {
            get
            {
                if (value >= 89.0f)
                    return 12;
                if (value >= 80.0f)
                    return 6;
                if (value >= 75.0f)
                    return 4;
                if (value >= 70.0f)
                    return 3;
                if (value >= 50.0f)
                    return 2;
                if (value >= -50.0f)
                    return 1;
                if (value >= -70.0f)
                    return 2;
                if (value >= -75.0f)
                    return 3;
                if (value >= -80.0f)
                    return 4;
                if (value >= -89.0f)
                    return 6;
                return 12;
            }
        }
}
}
