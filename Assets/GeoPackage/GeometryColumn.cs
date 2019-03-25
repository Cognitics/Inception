using System;
using System.Collections.Generic;
using System.Text;

namespace Cognitics.GeoPackage
{
    public class GeometryColumn
    {
        public string TableName;
        public string ColumnName;
        public string GeometryTypeName;
        public long SpatialReferenceSystemID;
        public byte z;
        public byte m;
    }
}
