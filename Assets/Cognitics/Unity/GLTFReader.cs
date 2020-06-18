using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Cognitics.Unity
{
    public static class GLTFReader
    {
        public static Scene.Scene Read(string name)
        {
            try
            {
                var bytes = System.IO.File.ReadAllBytes(name);
                var ext = System.IO.Path.GetExtension(name);
                if(ext == ".gltf")
                    return ParseGLTF(bytes);
                if(ext == ".glb")
                    return ParseBinaryGLTF(bytes);
                if(ext == ".b3dm")
                    return ParseBatched3DModel(bytes);
                if(ext == ".i3dm")
                    return ParseInstanced3DModel(bytes);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return null;
        }

        public static Scene.Scene Parse(string json, double[] position = null, byte[] bytes = null)
        {
            try
            {
                var gltf = glTF.glTF.Parse(json);
                var scene = new Scene.Scene();
                scene.translate = position;
                scene.Materials = new List<Scene.Material>();
                scene.Children = new List<Scene.Node>();
                for (int i = 0, c = gltf.materials.Length; i < c; ++i)
                    scene.Materials.Add(MaterialFromGLTFMaterial(gltf, bytes, i));
                for (int i = 0, c = gltf.scenes.Length; i < c; ++i)
                    scene.Children.Add(NodeFromGLTFScene(gltf, bytes, i));
                return scene;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return null;
        }

        public static Scene.Scene ParseGLTF(byte[] bytes, double[] position = null)
        {
            try
            {
                var gltf = Encoding.UTF8.GetString(bytes);
                return Parse(gltf, position);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return null;
        }

        public static Scene.Scene ParseBinaryGLTF(byte[] bytes, double[] position = null)
        {
            var parser = new BinaryParser(bytes);
            var magic = parser.UInt32LE();
            if (magic != 0x46546C67)
                return null;
            var version = parser.UInt32LE();
            var length = parser.UInt32LE();
            string json = "";
            byte[] binary = null;
            while (parser.Position < bytes.Length)
            {
                var chunk_length = parser.UInt32LE();
                var chunk_type = parser.UInt32LE();
                var chunk_data = new byte[chunk_length];
                Array.Copy(parser.Bytes, parser.Position, chunk_data, 0, chunk_data.Length);
                if (chunk_type == 0x4E4F534A)
                    json = Encoding.UTF8.GetString(chunk_data);
                if (chunk_type == 0x004E4942)
                    binary = chunk_data;
                parser.Position += (int)chunk_length;
            }
            return Parse(json, position, binary);
        }

        public static Scene.Scene ParseBatched3DModel(byte[] bytes)
        {
            var parser = new BinaryParser(bytes);
            var magic = parser.String(4);
            if (magic != "b3dm")
                return null;
            var version = parser.UInt32LE();
            var byteLength = parser.UInt32LE();
            var featureTableJSONByteLength = parser.UInt32LE();
            var featureTableBinaryByteLength = parser.UInt32LE();
            var batchTableJSONByteLength = parser.UInt32LE();
            var batchTableBinaryByteLength = parser.UInt32LE();
            var featureTableJSON = (featureTableJSONByteLength > 0) ? parser.String((int)featureTableJSONByteLength) : "";
            byte[] featureTableBytes = (featureTableBinaryByteLength > 0) ? new byte[featureTableBinaryByteLength] : null;
            if (featureTableBytes != null)
            {
                Array.Copy(parser.Bytes, parser.Position, featureTableBytes, 0, featureTableBytes.Length);
                parser.Position += (int)featureTableBinaryByteLength;
            }
            var batchTableJSON = (batchTableJSONByteLength > 0) ? parser.String((int)batchTableJSONByteLength) : "";
            byte[] batchTableBytes = (batchTableBinaryByteLength > 0) ? new byte[batchTableBinaryByteLength] : null;
            if (batchTableBytes != null)
            {
                Array.Copy(parser.Bytes, parser.Position, batchTableBytes, 0, batchTableBytes.Length);
                parser.Position += (int)batchTableBinaryByteLength;
            }

            var featureTable = C3DTiles.FeatureTable.Parse(featureTableJSON);

            // TODO: something with FeatureTable https://github.com/CesiumGS/3d-tiles/blob/master/specification/TileFormats/FeatureTable/README.md
            // TODO: something with BatchTable https://github.com/CesiumGS/3d-tiles/blob/master/specification/TileFormats/BatchTable/README.md

            var gltfBytes = new byte[bytes.Length - parser.Position];
            Array.Copy(parser.Bytes, parser.Position, gltfBytes, 0, gltfBytes.Length);


            //Debug.Log(featureTableJSON);
            //Debug.Log(batchTableJSON);
            return ParseBinaryGLTF(gltfBytes, featureTable.RTC_CENTER);
        }

        public static Scene.Scene ParseInstanced3DModel(byte[] bytes)
        {
            var parser = new BinaryParser(bytes);
            var magic = parser.String(4);
            if (magic != "i3dm")
                return null;
            var version = parser.UInt32LE();
            var byteLength = parser.UInt32LE();
            var featureTableJSONByteLength = parser.UInt32LE();
            var featureTableBinaryByteLength = parser.UInt32LE();
            var batchTableJSONByteLength = parser.UInt32LE();
            var batchTableBinaryByteLength = parser.UInt32LE();
            var gltfFormat = parser.UInt32LE();
            var featureTableJSON = (featureTableJSONByteLength > 0) ? parser.String((int)featureTableJSONByteLength) : "";
            byte[] featureTableBytes = (featureTableBinaryByteLength > 0) ? new byte[featureTableBinaryByteLength] : null;
            if (featureTableBytes != null)
            {
                Array.Copy(parser.Bytes, parser.Position, featureTableBytes, 0, featureTableBytes.Length);
                parser.Position += (int)featureTableBinaryByteLength;
            }
            var batchTableJSON = (batchTableJSONByteLength > 0) ? parser.String((int)batchTableJSONByteLength) : "";
            byte[] batchTableBytes = (batchTableBinaryByteLength > 0) ? new byte[batchTableBinaryByteLength] : null;
            if (batchTableBytes != null)
            {
                Array.Copy(parser.Bytes, parser.Position, batchTableBytes, 0, batchTableBytes.Length);
                parser.Position += (int)batchTableBinaryByteLength;
            }

            var featureTable = C3DTiles.FeatureTable.Parse(featureTableJSON);

            // TODO: something with FeatureTable https://github.com/CesiumGS/3d-tiles/blob/master/specification/TileFormats/FeatureTable/README.md
            // TODO: something with BatchTable https://github.com/CesiumGS/3d-tiles/blob/master/specification/TileFormats/BatchTable/README.md

            if (gltfFormat == 0)
            {
                var uri = parser.String(bytes.Length - parser.Position);
                // TODO
                return null;
            }

            if (gltfFormat == 1)
            {
                var gltfBytes = new byte[bytes.Length - parser.Position];
                Array.Copy(parser.Bytes, parser.Position, gltfBytes, 0, gltfBytes.Length);
                return ParseBinaryGLTF(gltfBytes, featureTable.RTC_CENTER);
            }

            return null;
        }

        static Scene.Material MaterialFromGLTFMaterial(glTF.glTF gltf, byte[] bytes, int index)
        {
            var material = gltf.materials[index];
            var result = new Scene.Material();
            result.Name = material.name;
            result.BackfaceCulling = !material.doubleSided;

            // TODO: result.Image



            /*
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
            //result.material = unityMaterial;



            return result;
        }

        static Scene.Node NodeFromGLTFScene(glTF.glTF gltf, byte[] bytes, int index)
        {
            var scene = gltf.scenes[index];
            var group = new Scene.Group();
            group.Name = scene.name;
            group.Children = new List<Scene.Node>();
            for (int i = 0, c = scene.nodes.Length; i < c; ++i)
                group.Children.Add(NodeFromGLTFNode(gltf, bytes, i));
            return group;
        }

        static Scene.Node NodeFromGLTFNode(glTF.glTF gltf, byte[] bytes, int index)
        {
            var node = gltf.nodes[index];

            Scene.Node result = new Scene.Group();
            /*
            Scene.Node result = null;
            if (node.mesh < 0)
                result = new Scene.Group();
            else
                result = new Scene.Mesh();
            */
            
            result.Name = node.name;
            if (node.translation != null)
                result.position = new Vector3(node.translation[0], node.translation[1], node.translation[2]);
            if(node.rotation != null)
            {
                // assign rotation
            }

            // TODO: assign translation to result.Matrix
            // TODO: assign rotation to result.Matrix

            if (node.mesh >= 0)
            {
                result.Children = new List<Scene.Node>();
                var mesh = gltf.meshes[node.mesh];

                foreach(var meshprimitive in gltf.meshes[index].primitives)
                {
                    var meshtoadd = new Scene.Mesh();
                    meshtoadd.Name = gltf.meshes[node.mesh].name;
                    if (meshprimitive.attributes.POSITION != -1)
                    {
                        meshtoadd.Vertices = Vector3FromBinary(gltf, bytes, meshprimitive.attributes.POSITION);
                    }
                    if(meshprimitive.attributes.NORMAL != -1)
                    {
                        meshtoadd.Normals = Vector3FromBinary(gltf, bytes, meshprimitive.attributes.NORMAL);
                    }
                    if(meshprimitive.attributes.TEXCOORD_0 != -1)
                    {
                        meshtoadd.UVs1 = Vector2FromBinary(gltf, bytes, meshprimitive.attributes.TEXCOORD_0);
                    }
                    if(meshprimitive.indices != -1)
                    {
                        meshtoadd.Triangles = TrianglesFromBinary(gltf, bytes, meshprimitive.indices);
                    }
                    if(meshprimitive.material != -1)
                    {
                        meshtoadd.MaterialIndex = meshprimitive.material;
                    }
                    result.Children.Add(meshtoadd);
                }
                
            }
            if(node.children != null)
                for (int i = 0, c = node.children.Length; i < c; ++i)
                    result.Children.Add(NodeFromGLTFNode(gltf, bytes, i));
            return result;
        }

        static Vector3[] Vector3FromBinary(glTF.glTF gltf, byte[] bytes, int index)
        {
            var vectorlist = new List<Vector3>();
            var bufferview = gltf.accessors[index];
            int i = gltf.bufferViews[bufferview.bufferview].byteOffset;
            int bufferlimit = bufferview.count != -1 ? bufferview.count * 12 : gltf.bufferViews[bufferview.bufferview].byteLength;
            bufferlimit += i;
            for(;i < bufferlimit; i += 12)
            {
                var x = BitConverter.ToSingle(bytes, i);
                var y = BitConverter.ToSingle(bytes, i + 4);
                var z = BitConverter.ToSingle(bytes, i + 8);
                vectorlist.Add(new Vector3(x, y, z));
            }
            return vectorlist.ToArray();
        }

        static Vector2[] Vector2FromBinary(glTF.glTF gltf, byte[] bytes, int index)
        {
            var vectorlist = new List<Vector2>();
            var bufferview = gltf.accessors[index];
            int i = gltf.bufferViews[bufferview.bufferview].byteOffset;
            int byteoffset = i;
            for(; i < gltf.bufferViews[bufferview.bufferview].byteLength + byteoffset; i += 8)
            {
                var x = BitConverter.ToSingle(bytes, i);
                var y = BitConverter.ToSingle(bytes, i + 4);
                vectorlist.Add(new Vector2(x, y));
            }
            return vectorlist.ToArray();
        }

        static int[] TrianglesFromBinary(glTF.glTF gltf, byte[] bytes, int index)
        {
            var trianglelist = new List<int>();
            var accessor = gltf.accessors[index];
            int valuesize = ByteCountForComponentType(accessor.componentType);
            var bufferview = gltf.bufferViews[accessor.bufferview];
            var byteOffset = accessor.byteOffset + bufferview.byteOffset;
            var result = new int[accessor.count];
            for (int i = 0; i < accessor.count; ++i)
            {
                int offset = byteOffset + (i * valuesize);
                if (valuesize == 1)
                    result[i] = bytes[offset];
                if (valuesize == 2)
                    result[i] = BitConverter.ToUInt16(bytes, offset);
                if (valuesize == 4)
                    result[i] = (int)BitConverter.ToUInt32(bytes, offset);
            }
            /*
            for (int i = 0, c = accessor.count / 3; i < c; i += 3)
            {
                //result[accessor.count + i + 0] = result[i + 0];
                //result[accessor.count + i + 1] = result[i + 2];
                //result[accessor.count + i + 2] = result[i + 1];
                int tmp = result[i + 1];
                result[i + 1] = result[i + 2];
                result[i + 2] = tmp;
            }
            */
            return result;
        }

        static int ByteCountForComponentType(int componentType)
        {
            switch (componentType)
            {
                case 5120: return 1;    // BYTE
                case 5121: return 1;    // UNSIGNED_BYTE
                case 5122: return 2;    // SHORT
                case 5123: return 2;    // UNSIGNED_SHORT
                case 5125: return 4;    // UNSIGNED_INT
                case 5126: return 4;    // FLOAT
            }
            return 1;
        }


    }
}



