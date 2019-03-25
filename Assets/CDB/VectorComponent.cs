
using System.Collections.Generic;
using NetTopologySuite.Features;

namespace Cognitics.CDB
{
    public abstract class VectorComponent : Component
    {
        public readonly VectorPointFeatures PointFeatures;
        public readonly VectorPointClassAttributes PointClassAttributes;
        public readonly VectorLinealFeatures LinealFeatures;
        public readonly VectorLinealClassAttributes LinealClassAttributes;
        public readonly VectorPolygonFeatures PolygonFeatures;
        public readonly VectorPolygonClassAttributes PolygonClassAttributes;
        public readonly VectorLinealFigurePointFeatures LinealFigurePointFeatures;
        public readonly VectorLinealFigurePointClassAttributes LinealFigurePointClassAttributes;
        public readonly VectorPolygonFigurePointFeatures PolygonFigurePointFeatures;
        public readonly VectorPolygonFigurePointClassAttributes PolygonFigurePointClassAttributes;
        public readonly VectorRelationshipTileConnections2D RelationshipTileConnections2D;
        public readonly VectorRelationshipDatasetConnections2D RelationshipDatasetConnections2D;
        public readonly VectorPointExtendedAttributes PointExtendedAttributes;
        public readonly VectorLinealExtendedAttributes LinealExtendedAttributes;
        public readonly VectorPolygonExtendedAttributes PolygonExtendedAttributes;
        public readonly VectorLinealFigurePointExtendedAttributes LinealFigurePointExtendedAttributes;
        public readonly VectorPolygonFigurePointExtendedAttributes PolygonFigurePointExtendedAttributes;


        public override int Selector2 => 0;

        protected VectorComponent(Dataset dataset) : base(dataset)
        {
            PointFeatures = new VectorPointFeatures(this);
            PointClassAttributes = new VectorPointClassAttributes(this);
            LinealFeatures = new VectorLinealFeatures(this);
            LinealClassAttributes = new VectorLinealClassAttributes(this);
            PolygonFeatures = new VectorPolygonFeatures(this);
            PolygonClassAttributes = new VectorPolygonClassAttributes(this);
            LinealFigurePointFeatures = new VectorLinealFigurePointFeatures(this);
            LinealFigurePointClassAttributes = new VectorLinealFigurePointClassAttributes(this);
            PolygonFigurePointFeatures = new VectorPolygonFigurePointFeatures(this);
            PolygonFigurePointClassAttributes = new VectorPolygonFigurePointClassAttributes(this);
            RelationshipTileConnections2D = new VectorRelationshipTileConnections2D(this);
            RelationshipDatasetConnections2D = new VectorRelationshipDatasetConnections2D(this);
            PointExtendedAttributes = new VectorPointExtendedAttributes(this);
            LinealExtendedAttributes = new VectorLinealExtendedAttributes(this);
            PolygonExtendedAttributes = new VectorPolygonExtendedAttributes(this);
            LinealFigurePointExtendedAttributes = new VectorLinealFigurePointExtendedAttributes(this);
            PolygonFigurePointExtendedAttributes = new VectorPolygonFigurePointExtendedAttributes(this);
        }

    }

    public abstract class VectorComponentChild : Component
    {
        protected VectorComponent Parent;
        public override int Selector1 => Parent.Selector1;
        protected VectorComponentChild(VectorComponent parent) : base(parent.Dataset)
        {
            Parent = parent;
        }
    }

    public abstract class VectorComponentFeatures : VectorComponentChild
    {
        public override string Extension => ".shp";
        protected VectorComponentFeatures(VectorComponent parent) : base(parent) { }
        public List<Feature> Read(Tile tile) => Shapefile.ReadFeatures(Filename(tile));
    }

    public abstract class VectorComponentClassAttributes : VectorComponentChild
    {
        public override string Extension => ".dbf";
        protected VectorComponentClassAttributes(VectorComponent parent) : base(parent) { }
        public Dictionary<string, AttributesTable> Read(Tile tile) => Shapefile.ReadClassAttributes(Filename(tile));
    }

    public abstract class VectorComponentExtendedAttributes : VectorComponentChild
    {
        public override string Extension => ".dbf";
        protected VectorComponentExtendedAttributes(VectorComponent parent) : base(parent) { }
        public Dictionary<int, AttributesTable> Read(Tile tile) => Shapefile.ReadExtendedAttributes(Filename(tile));
    }

    public class VectorPointFeatures : VectorComponentFeatures
    {
        public override int Selector2 => 1;
        public override string Name => Parent.Name + " Point Features";
        internal VectorPointFeatures(VectorComponent parent) : base(parent) { }
    }

    public class VectorPointClassAttributes : VectorComponentClassAttributes
    {
        public override int Selector2 => 2;
        public override string Name => Parent.Name + " Point Class-Level Attributes";
        internal VectorPointClassAttributes(VectorComponent parent) : base(parent) { }
    }

    public class VectorLinealFeatures : VectorComponentFeatures
    {
        public override int Selector2 => 3;
        public override string Name => Parent.Name + " Lineal Features";
        internal VectorLinealFeatures(VectorComponent parent) : base(parent) { }
    }

    public class VectorLinealClassAttributes : VectorComponentClassAttributes
    {
        public override int Selector2 => 4;
        public override string Name => Parent.Name + " Lineal Class-Level Attributes";
        internal VectorLinealClassAttributes(VectorComponent parent) : base(parent) { }
    }

    public class VectorPolygonFeatures : VectorComponentFeatures
    {
        public override int Selector2 => 5;
        public override string Name => Parent.Name + " Polygon Features";
        internal VectorPolygonFeatures(VectorComponent parent) : base(parent) { }
    }

    public class VectorPolygonClassAttributes : VectorComponentClassAttributes
    {
        public override int Selector2 => 6;
        public override string Name => Parent.Name + " Polygon Class-Level Attributes";
        internal VectorPolygonClassAttributes(VectorComponent parent) : base(parent) { }
    }

    public class VectorLinealFigurePointFeatures : VectorComponentFeatures
    {
        public override int Selector2 => 7;
        public override string Name => Parent.Name + " Lineal Figure Point Features";
        internal VectorLinealFigurePointFeatures(VectorComponent parent) : base(parent) { }
    }

    public class VectorLinealFigurePointClassAttributes : VectorComponentClassAttributes
    {
        public override int Selector2 => 8;
        public override string Name => Parent.Name + " Lineal Figure Point Class-Level Attributes";
        internal VectorLinealFigurePointClassAttributes(VectorComponent parent) : base(parent) { }
    }

    public class VectorPolygonFigurePointFeatures : VectorComponentFeatures
    {
        public override int Selector2 => 9;
        public override string Name => Parent.Name + " Polygon Figure Point Features";
        internal VectorPolygonFigurePointFeatures(VectorComponent parent) : base(parent) { }
    }

    public class VectorPolygonFigurePointClassAttributes : VectorComponentClassAttributes
    {
        public override int Selector2 => 10;
        public override string Name => Parent.Name + " Polygon Figure Point Class-Level Attributes";
        internal VectorPolygonFigurePointClassAttributes(VectorComponent parent) : base(parent) { }
    }

    public class VectorRelationshipTileConnections2D : VectorComponentFeatures
    {
        public override int Selector2 => 11;
        public override string Name => Parent.Name + " 2D Relationship Tile Connections";
        internal VectorRelationshipTileConnections2D(VectorComponent parent) : base(parent) { }
    }

    public class VectorRelationshipDatasetConnections2D : VectorComponentFeatures
    {
        public override int Selector2 => 15;
        public override string Name => Parent.Name + " 2D Relationship Dataset Connections";
        internal VectorRelationshipDatasetConnections2D(VectorComponent parent) : base(parent) { }
    }

    public class VectorPointExtendedAttributes : VectorComponentExtendedAttributes
    {
        public override int Selector2 => 16;
        public override string Name => Parent.Name + " Point Extended-Level Attributes";
        internal VectorPointExtendedAttributes(VectorComponent parent) : base(parent) { }
    }

    public class VectorLinealExtendedAttributes : VectorComponentExtendedAttributes
    {
        public override int Selector2 => 17;
        public override string Name => Parent.Name + " Lineal Extended-Level Attributes";
        internal VectorLinealExtendedAttributes(VectorComponent parent) : base(parent) { }
    }

    public class VectorPolygonExtendedAttributes : VectorComponentExtendedAttributes
    {
        public override int Selector2 => 18;
        public override string Name => Parent.Name + " Polygon Extended-Level Attributes";
        internal VectorPolygonExtendedAttributes(VectorComponent parent) : base(parent) { }
    }

    public class VectorLinealFigurePointExtendedAttributes : VectorComponentExtendedAttributes
    {
        public override int Selector2 => 19;
        public override string Name => Parent.Name + " Lineal Figure Point Extended-Level Attributes";
        internal VectorLinealFigurePointExtendedAttributes(VectorComponent parent) : base(parent) { }
    }

    public class VectorPolygonFigurePointExtendedAttributes : VectorComponentExtendedAttributes
    {
        public override int Selector2 => 20;
        public override string Name => Parent.Name + " Polygon Figure Point Extended-Level Attributes";
        internal VectorPolygonFigurePointExtendedAttributes(VectorComponent parent) : base(parent) { }
    }

}
