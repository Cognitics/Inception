using System;
using System.Collections.Generic;
using System.Text;
//using ProjNet;
//using ProjNet.CoordinateSystems;
//using ProjNet.CoordinateSystems.Transformations;

namespace Cognitics.GeoPackage
{
    public class SpatialReferenceSystem
    {
        public string Name;
        public long ID;
        public string Organization;
        public long OrganizationCoordinateSystemID;
        public string Definition;
        public string Description;

        /*
        public static ICoordinateSystem ProjNetCoordinateSystem(string wkt)
            => new CoordinateSystemFactory().CreateFromWkt(wkt);
        public static ICoordinateTransformation ProjNetTransform(ICoordinateSystem source, ICoordinateSystem target)
            => new CoordinateTransformationFactory().CreateFromCoordinateSystems(source, target);
            */

        public SpatialReferenceSystem() { }
        public SpatialReferenceSystem(string wkt) { Definition = wkt; }
        //ICoordinateSystem ProjNetCoordinateSystem() => ProjNetCoordinateSystem(Definition);


    }
}
