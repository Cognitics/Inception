
using System;

namespace Cognitics.CoordinateSystems
{
    public struct Longitude : IEquatable<Longitude>
    {
        static public double MinValue => -180.0f;
        static public double MaxValue => 180.0f;
        static public implicit operator Longitude(double value) => new Longitude(value);
        static public implicit operator double(Longitude longitude) => longitude.value;

        ////////////////////////////////////////////////////////////
        public bool Equals(Longitude obj) => this == obj;

        private double value;

        Longitude(double value)
        {
            this.value = Math.Min(Math.Max(value, MinValue), MaxValue);
        }

    }
}
