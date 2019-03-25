using UnityEngine;

namespace Cognitics.UnityCDB
{
    public static class ExtensionMethods
    {
        #region GameObject

        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (component == null)
                component = gameObject.AddComponent<T>();

            return component;
        }

        #endregion

        #region Transform

        public static void FromMatrix4x4(this Transform transform, Matrix4x4 m)
        {
            transform.localScale = m.GetScale();
            transform.localRotation = m.GetRotation();
            transform.localPosition = m.GetPosition();
        }

        #endregion

        #region Matrix4x4

        public static Quaternion GetRotation(this Matrix4x4 m)
        {
            float qw = 0.5f * Mathf.Sqrt(1f + m.m00 + m.m11 + m.m22);
            float w = 4f * qw;
            float qx = (m.m21 - m.m12) / w;
            float qy = (m.m02 - m.m20) / w;
            float qz = (m.m10 - m.m01) / w;
            return new Quaternion(qx, qy, qz, qw);
        }

        public static Vector3 GetPosition(this Matrix4x4 m)
        {
            float x = m.m03;
            float y = m.m13;
            float z = m.m23;
            return new Vector3(x, y, z);
        }

        public static Vector3 GetScale(this Matrix4x4 m)
        {
            float x = Mathf.Sqrt(m.m00 * m.m00 + m.m01 * m.m01 + m.m02 * m.m02);
            float y = Mathf.Sqrt(m.m10 * m.m10 + m.m11 * m.m11 + m.m12 * m.m12);
            float z = Mathf.Sqrt(m.m20 * m.m20 + m.m21 * m.m21 + m.m22 * m.m22);
            return new Vector3(x, y, z);
        }

        #endregion
    }
}
