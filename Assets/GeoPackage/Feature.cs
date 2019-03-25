
using System.Collections.Generic;

namespace Cognitics.GeoPackage
{
    public class Feature
    {
        public Geometry Geometry = null;
        public Dictionary<string, object> Attributes = new Dictionary<string, object>();
    }
}
