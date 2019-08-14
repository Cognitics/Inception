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

        #region Plane

        public static Vector4 Vec4(this Plane plane)
        {
            return new Vector4(plane.normal.x, plane.normal.y, plane.normal.z, plane.distance);
        }

        #endregion

        #region Bounds

        public static bool Contains (this Bounds bounds, Bounds other, bool skipY = false)
        {
            return bounds.min.x <= other.min.x && 
                   (skipY || bounds.min.y <= other.min.y) && 
                   bounds.min.z <= other.min.z && 
                   bounds.max.x >= other.max.x && 
                   (skipY || bounds.max.y >= other.max.y) && 
                   bounds.max.z >= other.max.z;
        }

        // NOTE: in Unity, GeometryUtility.TestPlanesAABB is a useful substitution for this
        public static int Intersects(this Bounds bounds, Plane[] frustum)
        {
            int result = 1; // inside

            for (int i = 0; i < 6; ++i)
            {
                var plane = frustum[i];

                Vector3 pv = new Vector3(plane.normal.x > 0f ? bounds.max.x : bounds.min.x, 
                                         plane.normal.y > 0f ? bounds.max.y : bounds.min.y, 
                                         plane.normal.z > 0f ? bounds.max.z : bounds.min.z);

                Vector3 nv = new Vector3(plane.normal.x < 0f ? bounds.max.x : bounds.min.x, 
                                         plane.normal.y < 0f ? bounds.max.y : bounds.min.y, 
                                         plane.normal.z < 0f ? bounds.max.z : bounds.min.z);

                float n = Vector4.Dot(new Vector4(pv.x, pv.y, pv.z, 1f), plane.Vec4());
                if (n < 0f)
                    return -1; // outside

                float m = Vector4.Dot(new Vector4(nv.x, nv.y, nv.z, 1f), plane.Vec4());
                if (m < 0f)
                    result = 0; // intersect
            }

            return result;
        }

        public static void Draw(this Bounds bounds, Color color, int drawMode = 0, float scalePad = 0f)
        {
            Vector3[] pts = new Vector3[8];
            Vector3 center = bounds.center;
            pts[0] = center + (new Vector3(bounds.min.x, bounds.max.y, bounds.min.z) - center) * (1f + scalePad);
            pts[1] = center + (new Vector3(bounds.max.x, bounds.max.y, bounds.min.z) - center) * (1f + scalePad);
            pts[2] = center + (new Vector3(bounds.max.x, bounds.max.y, bounds.max.z) - center) * (1f + scalePad);
            pts[3] = center + (new Vector3(bounds.min.x, bounds.max.y, bounds.max.z) - center) * (1f + scalePad);
            pts[4] = center + (new Vector3(bounds.min.x, bounds.min.y, bounds.min.z) - center) * (1f + scalePad);
            pts[5] = center + (new Vector3(bounds.max.x, bounds.min.y, bounds.min.z) - center) * (1f + scalePad);
            pts[6] = center + (new Vector3(bounds.max.x, bounds.min.y, bounds.max.z) - center) * (1f + scalePad);
            pts[7] = center + (new Vector3(bounds.min.x, bounds.min.y, bounds.max.z) - center) * (1f + scalePad);
            if (drawMode == 0)
            {
                Debug.DrawLine(pts[0], pts[1], color);
                Debug.DrawLine(pts[1], pts[2], color);
                Debug.DrawLine(pts[2], pts[3], color);
                Debug.DrawLine(pts[3], pts[0], color);
                Debug.DrawLine(pts[4], pts[5], color);
                Debug.DrawLine(pts[5], pts[6], color);
                Debug.DrawLine(pts[6], pts[7], color);
                Debug.DrawLine(pts[7], pts[4], color);
                Debug.DrawLine(pts[0], pts[4], color);
                Debug.DrawLine(pts[1], pts[5], color);
                Debug.DrawLine(pts[2], pts[6], color);
                Debug.DrawLine(pts[3], pts[7], color);
            }
            else
            {
                Debug.DrawLine((pts[0] + pts[4]) / 2, (pts[1] + pts[5]) / 2, color);
                Debug.DrawLine((pts[1] + pts[5]) / 2, (pts[2] + pts[6]) / 2, color);
                Debug.DrawLine((pts[2] + pts[6]) / 2, (pts[3] + pts[7]) / 2, color);
                Debug.DrawLine((pts[3] + pts[7]) / 2, (pts[0] + pts[4]) / 2, color);
            }
        }

        #endregion
    }
}
