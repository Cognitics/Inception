using System.Collections.Generic;

namespace Cognitics.Unity
{
    public class OpenFlightReader
    {
        public static Scene.Scene Parse(OpenFlight.Parser parser) => new OpenFlightReader(parser).Execute();

        #region private

        OpenFlight.Parser Parser;

        OpenFlightReader(OpenFlight.Parser parser)
        {
            Parser = parser;
        }

        Scene.Scene Scene = new Scene.Scene();

        Dictionary<int, OpenFlight.VertexPaletteEntry> VertexPalette;
        OpenFlight.ColorPalette ColorPalette;
        Dictionary<int, OpenFlight.MaterialPalette> MaterialPalette = new Dictionary<int, OpenFlight.MaterialPalette>();
        Dictionary<int, OpenFlight.TexturePalette> TexturePalette = new Dictionary<int, OpenFlight.TexturePalette>();
        Dictionary<int, int> SceneTextureIndexForTexturePaletteEntry = new Dictionary<int, int>();

        class WorkingMesh
        {
            public List<UnityEngine.Vector3> Vertices = new List<UnityEngine.Vector3>();
            public List<int> Triangles = new List<int>();
            public List<UnityEngine.Vector3> Normals = new List<UnityEngine.Vector3>();
            public List<UnityEngine.Color32> Colors = new List<UnityEngine.Color32>();
            public List<UnityEngine.Vector2> UVs = new List<UnityEngine.Vector2>();
            public int CurrentVertexIndex = 0;
            public int MaterialIndex;
        }

        class WorkingGroup_
        {
            public Scene.Node Parent;
            public Scene.Material Material;
            public Dictionary<Scene.Material, WorkingMesh> MeshMap = new Dictionary<Scene.Material, WorkingMesh>();
        }
        WorkingGroup_ WorkingGroup;

        Scene.Scene Execute()
        {
            HandleNode(Parser.Node(), Scene);
            return Scene;
        }

        void HandleNode(OpenFlight.Node node, Scene.Node parent)
        {
            switch (node.Record.Opcode)
            {
                case OpenFlight.Opcode.Header: HandleHeader(node, parent); break;
                case OpenFlight.Opcode.Group: HandleGroup(node, parent); break;
                case OpenFlight.Opcode.Object: HandleObject(node, parent); break;
                case OpenFlight.Opcode.Face: HandleFace(node, parent); break;
                case OpenFlight.Opcode.VertexList: HandleVertexList(node, parent); break;
                case OpenFlight.Opcode.Mesh: HandleMesh(node, parent); break;
                case OpenFlight.Opcode.LevelOfDetail: HandleLevelOfDetail(node, parent); break;
            }
        }

        void HandleHeader(OpenFlight.Node node, Scene.Node parent)
        {
            foreach(var record in node.Records)
            {
                switch (record.Opcode)
                {
                    case OpenFlight.Opcode.ColorPalette: HandleColorPalette(record); break;
                    case OpenFlight.Opcode.MaterialPalette: HandleMaterialPalette(record); break;
                    case OpenFlight.Opcode.TexturePalette: HandleTexturePalette(record); break;
                    case OpenFlight.Opcode.VertexPalette: HandleVertexPalette(record); break;
                }
            }
            if (TexturePalette.Count > 0)
            {

                /* TODO: uncomment this
                
                Scene.Textures = new List<Scene.Texture>();
                foreach (var flt_texture in TexturePalette)
                {
                    var texture = new Scene.Texture();
                    texture.Name = flt_texture.Value.Filename;
                    Scene.Textures.Add(texture);
                    SceneTextureIndexForTexturePaletteEntry.Add(flt_texture.Key, Scene.Textures.Count - 1);
                }
                */
            }
            foreach (var child in node.Children)
                HandleNode(child, parent);
        }

        void CloseWorkingGroup()
        {
            foreach (var working_mesh_index in WorkingGroup.MeshMap.Keys)
            {
                var working_mesh = WorkingGroup.MeshMap[working_mesh_index];
                var mesh = new Scene.Mesh();
                mesh.MaterialIndex = working_mesh.MaterialIndex;
                var working_material = Scene.Materials[mesh.MaterialIndex];
                mesh.Name = working_material.Name;
                mesh.Vertices = working_mesh.Vertices.ToArray();
                mesh.Triangles = working_mesh.Triangles.ToArray();
                mesh.Normals = working_mesh.Normals.ToArray();
                mesh.Colors = working_mesh.Colors.ToArray();
                mesh.UVs1 = working_mesh.UVs.ToArray();
                WorkingGroup.Parent.AddChild(mesh);
            }
            WorkingGroup = null;
        }

        void HandleGroup(OpenFlight.Node node, Scene.Node parent)
        {
            var flt_group = Parser.ParseGroup(node.Record);
            var group = new Scene.Group();
            group.Name = flt_group.ID;
            parent.AddChild(group);
            foreach (var child in node.Children)
                HandleNode(child, group);
            if (WorkingGroup != null)
                CloseWorkingGroup();
        }

        void HandleObject(OpenFlight.Node node, Scene.Node parent)
        {
            var flt_object = Parser.ParseObject(node.Record);
            var group = new Scene.Group();
            group.Name = flt_object.ID;
            parent.AddChild(group);
            foreach (var child in node.Children)
                HandleNode(child, group);
            if (WorkingGroup != null)
                CloseWorkingGroup();
        }

        Scene.Material MaterialForFace(OpenFlight.Face face)
        {
            return null;

            /*
            var flt_material = (face.MaterialIndex >= 0) ? MaterialPalette[face.MaterialIndex] : null;
            var flt_texture = (face.TexturePatternIndex >= 0) ? TexturePalette[face.TexturePatternIndex] : null;
            Scene.Material result = new Scene.Material();
            if (flt_material != null)
            {
                result.Name = flt_material.Name;
                result.Ambient = new UnityEngine.Color(flt_material.Ambient.R, flt_material.Ambient.G, flt_material.Ambient.B);
                result.Diffuse = new UnityEngine.Color(flt_material.Diffuse.R, flt_material.Diffuse.G, flt_material.Diffuse.B);
                result.Specular = new UnityEngine.Color(flt_material.Specular.R, flt_material.Specular.G, flt_material.Specular.B);
                result.Emissive = new UnityEngine.Color(flt_material.Emissive.R, flt_material.Emissive.G, flt_material.Emissive.B);
                result.Alpha = flt_material.Alpha;
                result.Shininess = flt_material.Shininess;
            }
            if (flt_texture != null)
            {
                result.TextureOffset = new UnityEngine.Vector2(flt_texture.X, flt_texture.Y);
                result.TextureScale = UnityEngine.Vector2.one;
                result.TextureID = SceneTextureIndexForTexturePaletteEntry[face.TexturePatternIndex];
                result.Name = System.IO.Path.GetFileName(Scene.Textures[result.TextureID.Value].Name);
            }
            result.BackfaceCulling = (face.DrawType == 0);
            foreach (var material in Scene.Materials)
            {
                if (material.Name != result.Name)
                    continue;
                if (material.Ambient != result.Ambient)
                    continue;
                if (material.Diffuse != result.Diffuse)
                    continue;
                if (material.Specular != result.Specular)
                    continue;
                if (material.Emissive != result.Emissive)
                    continue;
                if (material.Alpha != result.Alpha)
                    continue;
                if (material.Shininess != result.Shininess)
                    continue;
                if (material.TextureOffset != result.TextureOffset)
                    continue;
                if (material.TextureScale != result.TextureScale)
                    continue;
                if (material.BackfaceCulling != result.BackfaceCulling)
                    continue;
                if (material.TextureID != result.TextureID)
                    continue;
                return material;
            }
            Scene.Materials.Add(result);
            return result;
            */
        }

        Scene.Material MaterialForMesh(OpenFlight.Mesh mesh)
        {
            return null;

            /*
            var flt_material = (mesh.MaterialIndex >= 0) ? MaterialPalette[mesh.MaterialIndex] : null;
            var flt_texture = (mesh.TexturePatternIndex >= 0) ? TexturePalette[mesh.TexturePatternIndex] : null;
            Scene.Material result = new Scene.Material();
            if (flt_material != null)
            {
                result.Name = flt_material.Name;
                result.Ambient = new UnityEngine.Color(flt_material.Ambient.R, flt_material.Ambient.G, flt_material.Ambient.B);
                result.Diffuse = new UnityEngine.Color(flt_material.Diffuse.R, flt_material.Diffuse.G, flt_material.Diffuse.B);
                result.Specular = new UnityEngine.Color(flt_material.Specular.R, flt_material.Specular.G, flt_material.Specular.B);
                result.Emissive = new UnityEngine.Color(flt_material.Emissive.R, flt_material.Emissive.G, flt_material.Emissive.B);
                result.Alpha = flt_material.Alpha;
                result.Shininess = flt_material.Shininess;
            }
            if (flt_texture != null)
            {
                result.TextureOffset = new UnityEngine.Vector2(flt_texture.X, flt_texture.Y);
                result.TextureScale = UnityEngine.Vector2.one;
                result.TextureID = SceneTextureIndexForTexturePaletteEntry[mesh.TexturePatternIndex];
                result.Name = System.IO.Path.GetFileName(Scene.Textures[result.TextureID.Value].Name);
            }
            result.BackfaceCulling = (mesh.DrawType == 0);
            foreach (var material in Scene.Materials)
            {
                if (material.Name != result.Name)
                    continue;
                if (material.Ambient != result.Ambient)
                    continue;
                if (material.Diffuse != result.Diffuse)
                    continue;
                if (material.Specular != result.Specular)
                    continue;
                if (material.Emissive != result.Emissive)
                    continue;
                if (material.Alpha != result.Alpha)
                    continue;
                if (material.Shininess != result.Shininess)
                    continue;
                if (material.TextureOffset != result.TextureOffset)
                    continue;
                if (material.TextureScale != result.TextureScale)
                    continue;
                if (material.BackfaceCulling != result.BackfaceCulling)
                    continue;
                if (material.TextureID != result.TextureID)
                    continue;
                return material;
            }
            Scene.Materials.Add(result);
            return result;
            */
        }

        void HandleFace(OpenFlight.Node node, Scene.Node parent)
        {
            if (WorkingGroup == null)
            {
                WorkingGroup = new WorkingGroup_();
                WorkingGroup.Parent = parent;
            }
            var face = Parser.ParseFace(node.Record);
            // TODO: face properties

            WorkingGroup.Material = MaterialForFace(face);
            if (!WorkingGroup.MeshMap.ContainsKey(WorkingGroup.Material))
                WorkingGroup.MeshMap[WorkingGroup.Material] = new WorkingMesh();
            var working_mesh = WorkingGroup.MeshMap[WorkingGroup.Material];
            working_mesh.MaterialIndex = Scene.Materials.IndexOf(WorkingGroup.Material); 

            int before_index = working_mesh.CurrentVertexIndex;
            foreach (var child in node.Children)
                HandleNode(child, parent);
            int after_index = working_mesh.CurrentVertexIndex;
            if (after_index - before_index == 3)
            {
                working_mesh.Triangles.Add(before_index + 0);
                working_mesh.Triangles.Add(before_index + 2);
                working_mesh.Triangles.Add(before_index + 1);
                return;
            }
            if (after_index - before_index == 4)
            {
                working_mesh.Triangles.Add(before_index + 0);
                working_mesh.Triangles.Add(before_index + 2);
                working_mesh.Triangles.Add(before_index + 1);
                working_mesh.Triangles.Add(before_index + 2);
                working_mesh.Triangles.Add(before_index + 0);
                working_mesh.Triangles.Add(before_index + 3);
                return;
            }
            UnityEngine.Debug.LogError("FLT: invalid vertex count");
        }

        void HandleVertexList(OpenFlight.Node node, Scene.Node parent)
        {
            var working_mesh = WorkingGroup.MeshMap[WorkingGroup.Material];
            var vertex_list = Parser.ParseVertexList(node.Record);
            foreach (var offset in vertex_list.Offsets)
            {
                ++working_mesh.CurrentVertexIndex;
                var vertex = VertexPalette[offset];
                // TODO: transform
                double x = vertex.Coordinate.X;
                double y = vertex.Coordinate.Z;
                double z = vertex.Coordinate.Y;

                var normal = vertex.HasNormal ? new UnityEngine.Vector3(vertex.Normal.I, vertex.Normal.K, vertex.Normal.J) : UnityEngine.Vector3.zero;
                var uv = vertex.HasUV ? new UnityEngine.Vector2(vertex.UV.U, vertex.UV.V) : UnityEngine.Vector2.zero;
                var color = vertex.IsPackedColor ? new UnityEngine.Color32(vertex.PackedColor.R, vertex.PackedColor.G, vertex.PackedColor.B, vertex.PackedColor.A) : new UnityEngine.Color32(255, 255, 255, 255);
                working_mesh.Vertices.Add(new UnityEngine.Vector3((float)x, (float)y, (float)z));
                working_mesh.Normals.Add(normal);
                working_mesh.UVs.Add(uv);
                working_mesh.Colors.Add(color);
            }
        }

        void HandleMesh(OpenFlight.Node node, Scene.Node parent)
        {
            var flt_mesh = Parser.ParseMesh(node.Record);
            OpenFlight.LocalVertexPool local_vertex_pool = null;
            OpenFlight.MeshPrimitive mesh_primitive = null;
            foreach (var record in node.Records)
            {
                switch (record.Opcode)
                {
                    case OpenFlight.Opcode.LocalVertexPool:
                        local_vertex_pool = Parser.ParseLocalVertexPool(record);
                        break;
                    case OpenFlight.Opcode.MeshPrimitive:
                        mesh_primitive = Parser.ParseMeshPrimitive(record);
                        break;
                }
            }
            if (local_vertex_pool == null)
                return;
            if (mesh_primitive == null)
                return;
            if (!local_vertex_pool.HasPosition)
                return;
            if (mesh_primitive.Type == 4)
            {
                UnityEngine.Debug.LogFormat("{0}: indexed poly is not currently supported.", node.Record.ToString());
                return;
            }

            var mesh = new Scene.Mesh();
            parent.AddChild(mesh);
            var material = MaterialForMesh(flt_mesh);
            mesh.MaterialIndex = Scene.Materials.IndexOf(material);
            mesh.Name = flt_mesh.ID;

            var count = local_vertex_pool.Coordinates.Length;

            mesh.Vertices = new UnityEngine.Vector3[count];
            for (int i = 0; i < count; ++i)
            {
                // TODO: transform
                mesh.Vertices[i].x = (float)local_vertex_pool.Coordinates[i].X;
                mesh.Vertices[i].y = (float)local_vertex_pool.Coordinates[i].Z;
                mesh.Vertices[i].z = (float)local_vertex_pool.Coordinates[i].Y;
            }
            if (local_vertex_pool.HasNormal)
            {
                mesh.Normals = new UnityEngine.Vector3[count];
                for (int i = 0; i < count; ++i)
                {
                    mesh.Normals[i].x = local_vertex_pool.Normals[i].I;
                    mesh.Normals[i].y = local_vertex_pool.Normals[i].K;
                    mesh.Normals[i].z = local_vertex_pool.Normals[i].J;
                }
            }
            if (local_vertex_pool.HasBaseUV)
            {
                mesh.UVs1 = new UnityEngine.Vector2[count];
                for (int i = 0; i < count; ++i)
                {
                    mesh.UVs1[i].x = local_vertex_pool.UVBase[i].U;
                    mesh.UVs1[i].y = local_vertex_pool.UVBase[i].V;
                }
            }
            if (local_vertex_pool.HasRGBAColor)
            {
                mesh.Colors = new UnityEngine.Color32[count];
                for (int i = 0; i < count; ++i)
                {
                    mesh.Colors[i].r = local_vertex_pool.Colors[i].R;
                    mesh.Colors[i].g = local_vertex_pool.Colors[i].G;
                    mesh.Colors[i].b = local_vertex_pool.Colors[i].B;
                    mesh.Colors[i].a = local_vertex_pool.Colors[i].A;
                }
            }

            var index_count = mesh_primitive.Indices.Length;
            if (mesh_primitive.Type == 1)   // triangle strip
            {
                mesh.Triangles = new int[(index_count - 2) * 3];
                for (int i = 0; i + 2 < index_count; ++i)
                {
                    if (i % 2 == 0)
                    {
                        mesh.Triangles[(i * 3) + 0] = mesh_primitive.Indices[i + 1];
                        mesh.Triangles[(i * 3) + 1] = mesh_primitive.Indices[i + 0];
                        mesh.Triangles[(i * 3) + 2] = mesh_primitive.Indices[i + 2];
                    }
                    else
                    {
                        mesh.Triangles[(i * 3) + 0] = mesh_primitive.Indices[i + 0];
                        mesh.Triangles[(i * 3) + 1] = mesh_primitive.Indices[i + 1];
                        mesh.Triangles[(i * 3) + 2] = mesh_primitive.Indices[i + 2];
                    }
                }
            }
            if (mesh_primitive.Type == 2)   // triangle fan
            {
                mesh.Triangles = new int[(index_count - 2) * 3];
                for (int i = 0; i + 2 < index_count; ++i)
                {
                    mesh.Triangles[(i * 3) + 0] = mesh_primitive.Indices[0];
                    mesh.Triangles[(i * 3) + 1] = mesh_primitive.Indices[i + 1];
                    mesh.Triangles[(i * 3) + 2] = mesh_primitive.Indices[i + 2];
                }
            }
            if (mesh_primitive.Type == 3)   // quad strip
            {
                mesh.Triangles = new int[((index_count / 2) - 1) * 6];
                for (int i = 0; i + 1 < index_count; i += 2)
                {
                    mesh.Triangles[(i * 6) + 0] = mesh_primitive.Indices[i + 0];
                    mesh.Triangles[(i * 6) + 1] = mesh_primitive.Indices[i + 1];
                    mesh.Triangles[(i * 6) + 2] = mesh_primitive.Indices[i + 2];
                    mesh.Triangles[(i * 6) + 3] = mesh_primitive.Indices[i + 2];
                    mesh.Triangles[(i * 6) + 4] = mesh_primitive.Indices[i + 1];
                    mesh.Triangles[(i * 6) + 5] = mesh_primitive.Indices[i + 3];
                }
            }
        }

        void HandleLevelOfDetail(OpenFlight.Node node, Scene.Node parent)
        {
            var flt_lod = Parser.ParseLevelOfDetail(node.Record);
            var lod = new Scene.LOD();
            lod.Name = flt_lod.ID;
            lod.SwitchInDistance = flt_lod.SwitchInDistance;
            lod.SwitchOutDistance = flt_lod.SwitchOutDistance;
            parent.AddChild(lod);
            foreach (var child in node.Children)
                HandleNode(child, lod);
        }

        void HandleColorPalette(OpenFlight.Record record)
        {
            ColorPalette = Parser.ParseColorPalette(record);
        }

        void HandleMaterialPalette(OpenFlight.Record record)
        {
            var entry = Parser.ParseMaterialPalette(record);
            MaterialPalette.Add(entry.Index, entry);
        }

        void HandleTexturePalette(OpenFlight.Record record)
        {
            var entry = Parser.ParseTexturePalette(record);
            TexturePalette.Add(entry.Index, entry);
        }

        void HandleVertexPalette(OpenFlight.Record record)
        {
            VertexPalette = Parser.ParseVertexPalette(record);
        }

        #endregion

    }

}
