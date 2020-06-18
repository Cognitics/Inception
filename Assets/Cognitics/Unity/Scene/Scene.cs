
using System.Collections.Generic;

namespace Cognitics.Unity.Scene
{
    public class Scene : Group
    {
        public List<Material> Materials;
        public double[] translate;      // TODO: remove this

    }

    public class Group : Node
    {
    }

    public class LOD : Group
    {
        public double SwitchInDistance = 0.0;
        public double SwitchOutDistance = 0.0;
    }

    public class Mesh : Node
    {
        public UnityEngine.Vector3[] Vertices = null;
        public int[] Triangles = null;
        public float[] test = null; //I don't yet know what this data type represents. 
        public UnityEngine.Vector4[] Tangents = null;
        public UnityEngine.Vector3[] Normals = null;
        public UnityEngine.Color32[] Colors = null;
        public UnityEngine.Vector2[] UVs1 = null;
        public UnityEngine.Vector2[] UVs2 = null;
        public UnityEngine.Vector2[] UVs3 = null;
        public UnityEngine.Vector2[] UVs4 = null;
        public UnityEngine.Vector2[] UVs5 = null;
        public UnityEngine.Vector2[] UVs6 = null;
        public UnityEngine.Vector2[] UVs7 = null;
        public UnityEngine.Vector2[] UVs8 = null;
        public int MaterialIndex = -1;

        public UnityEngine.Mesh UnityMesh()
        {
            var unity_mesh = new UnityEngine.Mesh();
            if (Vertices != null)
                unity_mesh.vertices = Vertices;
            if (Triangles != null)
                unity_mesh.triangles = Triangles;
            if (Normals != null)
                unity_mesh.normals = Normals;
            if (Colors != null)
                unity_mesh.colors32 = Colors;
            if (UVs1 != null)
                unity_mesh.uv = UVs1;
            if (UVs2 != null)
                unity_mesh.uv2 = UVs2;
            if (UVs3 != null)
                unity_mesh.uv3 = UVs3;
            if (UVs4 != null)
                unity_mesh.uv4 = UVs4;
            if (UVs5 != null)
                unity_mesh.uv5 = UVs5;
            if (UVs6 != null)
                unity_mesh.uv6 = UVs6;
            if (UVs7 != null)
                unity_mesh.uv7 = UVs7;
            if (UVs8 != null)
                unity_mesh.uv8 = UVs8;
            return unity_mesh;
        }

    }

    public abstract class Node
    {
        public List<Node> Children;
        public string Name;
        public UnityEngine.Matrix4x4 Matrix = UnityEngine.Matrix4x4.identity;
        public List<int> childrenNodeIndex;     // ??
        public UnityEngine.Vector3 position;    // ??

        public void SetUnityTransform(UnityEngine.Transform transform)
        {
            UnityEngine.Vector3 scale;
            scale.x = new UnityEngine.Vector4(Matrix.m00, Matrix.m10, Matrix.m20, Matrix.m30).magnitude;
            scale.y = new UnityEngine.Vector4(Matrix.m01, Matrix.m11, Matrix.m21, Matrix.m31).magnitude;
            scale.z = new UnityEngine.Vector4(Matrix.m02, Matrix.m12, Matrix.m22, Matrix.m32).magnitude;

            UnityEngine.Vector3 forward;
            forward.x = Matrix.m02;
            forward.y = Matrix.m12;
            forward.z = Matrix.m22;

            UnityEngine.Vector3 up;
            up.x = Matrix.m01;
            up.y = Matrix.m11;
            up.z = Matrix.m21;

            UnityEngine.Vector3 position;
            position.x = Matrix.m03;
            position.y = Matrix.m13;
            position.z = Matrix.m23;

            transform.localScale = scale;
            transform.rotation = UnityEngine.Quaternion.LookRotation(forward, up);
            transform.position = position;
        }

        public void AddChild(Node child)
        {
            if (Children == null)
                Children = new List<Node>();
            Children.Add(child);
        }
    }

    public class Material
    {
        public string Name;
        public UnityEngine.Color Color = new UnityEngine.Color(1.0f, 1.0f, 1.0f, 1.0f);
        public string Texture;
        public Image<byte> Image;
        public bool BackfaceCulling = true;
    }


    public class ModelGenerator
    {
        // TODO: we need to be able to generalize material access via a material manager

        public static UnityEngine.GameObject GameObjectForScene(Scene scene)
        {
            var generator = new ModelGenerator() { Scene = scene };
            return generator.GameObjectForNode(scene);
        }

        #region private

        Scene Scene;
        List<UnityEngine.Material> Materials;


        UnityEngine.GameObject GameObjectForNode(Node node)
        {
            var go = new UnityEngine.GameObject();
            go.name = node.Name;

            if (node is Scene scene)
            {
                if (scene.Materials != null)
                {
                    Materials = new List<UnityEngine.Material>();
                    foreach (var material in scene.Materials)
                    {
                        var unity_material = new UnityEngine.Material(UnityEngine.Shader.Find("Cognitics/ModelStandard"));
                        unity_material.name = material.Name;
                        if (material.Color != null)
                            unity_material.color = material.Color;

                        /* TODO: full material integration

                        var unityMaterial = new Material(Shader.Find("Cognitics/ModelStandard"));
                        unityMaterial.name = material.name;
                        unityMaterial.SetFloat("_Glossiness", material.pbrMetallicRoughness.roughnessFactor);
                        unityMaterial.SetFloat("_Metallic", material.pbrMetallicRoughness.metallicFactor);
                        if (material.pbrMetallicRoughness.baseColorFactor != null)
                        {
                            var materialcolor = material.pbrMetallicRoughness.baseColorFactor;
                            var color = new Color(materialcolor[0], materialcolor[1], materialcolor[2], materialcolor[3]);
                            unityMaterial.SetColor("_Color", color);
                        }
                        if(material.pbrMetallicRoughness.baseColorTexture != null)
                        {
                            int i = material.pbrMetallicRoughness.baseColorTexture.index;
                            int source = gltf.textures[i].source;
                            var texture = new Texture2D(1, 1);
                            // TODO: We don't deal with textures for this demo. This will need to be figured out for an implementation. 
                        }
                        */

                        Materials.Add(unity_material);
                    }
                }
            }

            if (node is Mesh mesh)
            {
                var mesh_filter = go.AddComponent<UnityEngine.MeshFilter>();
                mesh_filter.mesh = mesh.UnityMesh();
                //mesh_filter.mesh.RecalculateNormals();
                //mesh_filter.mesh.RecalculateBounds();
                //mesh_filter.mesh.RecalculateTangents();
                var mesh_renderer = go.AddComponent<UnityEngine.MeshRenderer>();
                if (mesh.MaterialIndex >= 0)
                    mesh_renderer.material = Materials[mesh.MaterialIndex];
            }

            if (node.Children == null)
                return go;

            foreach (var child in node.Children)
            {
                var child_go = GameObjectForNode(child);
                child_go.transform.SetParent(go.transform);
            }

            return go;
        }

        #endregion

    }


}
