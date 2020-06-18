
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cognitics.glTF
{
    // https://github.com/KhronosGroup/glTF/tree/master/specification/2.0/schema

    // https://github.com/KhronosGroup/glTF/blob/master/specification/2.0/schema/glTF.schema.json
    [Serializable]
    public class glTF
    {
        public string[] extensionsUsed;
        public string[] extensionsRequired;
        public Accessor[] accessors;
        public Animation[] animations;
        public Asset asset;
        public Buffer[] buffers;
        public BufferView[] bufferViews;
        public Camera[] cameras;
        public Image[] images;
        public Material[] materials;
        public Mesh[] meshes;
        public Node[] nodes;
        public Sampler[] samplers;
        public int scene = -1;
        public Scene[] scenes;
        public Skin[] skins;
        public Texture[] textures;
        public JObject extensions;
        public JObject extras;
        public static glTF Parse(string json) => JsonConvert.DeserializeObject<glTF>(json);
        public static glTF Fetch(string url) => Parse(Web.UTF8StringFromWebRequest(url));
    }

    // https://github.com/KhronosGroup/glTF/blob/master/specification/2.0/schema/accessor.schema.json
    [Serializable]
    public class Accessor
    {
        public int bufferview = -1;
        public int byteOffset = -1;
        public int componentType = -1;
        public bool normalized;
        public int count;
        public string type;
        public float[] max;
        public float[] min;
        public AccessorSparse sparse;
        public string name;
        public JObject extensions;
        public JObject extras;
    }

    // https://github.com/KhronosGroup/glTF/blob/master/specification/2.0/schema/accessor.sparse.schema.json
    [Serializable]
    public class AccessorSparse
    {
        public int count = -1;
        public int[] indices;
        public int[] values;
        public JObject extensions;
        public JObject extras;
    }


    // https://github.com/KhronosGroup/glTF/blob/master/specification/2.0/schema/animation.schema.json
    [Serializable]
    public class Animation
    {
        public Channel[] channels;
        public Sampler[] samplers;
        public string name;
        public JObject extensions;
        public JObject extras;
    }

    // https://github.com/KhronosGroup/glTF/blob/master/specification/2.0/schema/animation.channel.target.schema.json
    [Serializable]
    public class Channel
    {
        public int sampler = -1;
        public int target = -1;
        public JObject extensions;
        public JObject extras;
    }

    // https://github.com/KhronosGroup/glTF/blob/master/specification/2.0/schema/camera.schema.json
    [Serializable]
    public class Camera
    {
        public Orthographic orthographic;
        public Perspective perspective;
        public string type;
        public string name;
        public JObject extensions;
        public JObject extras;
    }

    // https://github.com/KhronosGroup/glTF/blob/master/specification/2.0/schema/camera.orthographic.schema.json
    [Serializable]
    public class Orthographic
    {
        public float xmag;
        public float ymag;
        public float zfar;
        public float znear;
        public JObject extensions;
        public JObject extras;
    }

    // https://github.com/KhronosGroup/glTF/blob/master/specification/2.0/schema/camera.perspective.schema.json
    [Serializable]
    public class Perspective
    {
        public float aspectRatio;
        public float yfov;
        public float zfar;
        public float znear;
        public JObject extensions;
        public JObject extras;
    }

    // https://github.com/KhronosGroup/glTF/blob/master/specification/2.0/schema/image.schema.json
    [Serializable]
    public class Image
    {
        public string uri;
        public string type;
        public int bufferView;
        public string name;
        public JObject extensions;
        public JObject extras;
    }

    // https://github.com/KhronosGroup/glTF/blob/master/specification/2.0/schema/sampler.schema.json
    [Serializable]
    public class Sampler
    {
        public int magFilter = -1;
        public int minFilter = -1;
        public int wrapS = -1;
        public int wrapT = -1;
        public string name;
        public JObject extensions;
        public JObject extras;
    }

    // https://github.com/KhronosGroup/glTF/blob/master/specification/2.0/schema/skin.schema.json
    [Serializable]
    public class Skin
    {
        public int inverseBingMatrices = -1;
        public int skeleton = -1;
        public int[] joints;
        public string name;
        public JObject extensions;
        public JObject extras;
    }

    // https://github.com/KhronosGroup/glTF/blob/master/specification/2.0/schema/bufferView.schema.json
    [Serializable]
    public class BufferView
    {
        public int buffer = -1;
        public int byteOffset = -1;
        public int bufferview = -1;
        public int byteLength = -1;
        public int byteStride = -1;
        public int target = -1;
        public int componentType = -1;
        public int count = -1;
        public string type;
        public float[] max;
        public float[] min;
        public string name;
        public JObject extensions;
        public JObject extras;
    }

    // https://github.com/KhronosGroup/glTF/blob/master/specification/2.0/schema/buffer.schema.json
    [Serializable]
    public class Buffer
    {
        public string uri;
        public int byteLength = -1;
        public string name;
        public JObject extensions;
        public JObject extras;
    }

    // https://github.com/KhronosGroup/glTF/blob/master/specification/2.0/schema/mesh.primitive.schema.json
    [Serializable]
    public class MeshPrimitive
    {
        public MeshAttributes attributes;
        public int indices = -1;
        public int material = -1;
        public int mode = -1;
    }


    [Serializable]
    public class MeshAttributes
    {
        public int TEXCOORD_0 = -1;
        public int NORMAL = -1;
        public int TANGENT = -1;
        public int POSITION = -1;
    }

    [Serializable]
    public class Asset
    {
        public string copyright;
        public string generator;
        public string version;
        public string minVersion;
        public JObject extensions;
        public JObject extras;
    }

    [Serializable]
    public class Mesh
    {
        public MeshPrimitive[] primitives;
        public string name;
        public int material = -1;
    }

    [Serializable]
    public class Node
    {
        public int mesh = -1;
        public int[] children;
        public float[] translation;
        public float[] rotation;
        public string name;
    }

    [Serializable]
    public class Material
    {
        public string name;
        public PBRMetallicRoughness pbrMetallicRoughness;
        public bool doubleSided = false;
    }

    [Serializable]
    public class PBRMetallicRoughness
    {
        public float[] baseColorFactor;
        public BaseColorTexture baseColorTexture;
        public float metallicFactor;
        public float roughnessFactor;
    }

    [Serializable]
    public class BaseColorTexture
    {
        public int index = -1;
        public int textCoord = -1;
    }

    [Serializable]
    public class Scene
    {
        public string name;
        public int[] nodes;
    }

    [Serializable]
    public class Texture
    {
        public int source = -1;
        public int sampler = -1;
    }


}


