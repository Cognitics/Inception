
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cognitics.Unity.BlueMarble
{
    public class GlobeViewer : MonoBehaviour
    {
        public Camera Camera;
        public GameObject Model;

        private Vector3 DragStart;
        private Vector3 prevPosition;
        private Vector3 actualPos;
        private bool isTouch;
        private const float MIN_DISTANCE = 70f;
        private const float MAX_DISTANCE = 160f;
        private float cameraDistance;
        private float pinchDistance;
        private float pinchDistanceDelta;
        private bool isDragging;

        const float Scale = 1e-5f;

        GeocellTexture GeocellTexture;

        void Start()
        {
            GeocellTexture = new GeocellTexture();
            Model.GetComponent<MeshRenderer>().material.SetTexture("_SelectionTex", GeocellTexture.Texture);
            cameraDistance = Vector3.Distance(Camera.transform.position, Model.transform.position);
        }

        void Update()
        {
            isTouch = (Input.touchCount >= 1) ? true : false;
            if (isTouch)
                GetPinchInfo();

            CameraZoom();
            RotateGlobe();
            UpdateGeocellSelection();
        }

        //static Color32 GeocellColor = new Color32(255, 255, 0, 96);
        static Color32 GeocellColor = new Color32(255, 0, 255, 96);
        void UpdateGeocellSelection()
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (Input.GetMouseButton(0))
            {
                if (shift)
                {
                    if (Physics.Raycast(Camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
                    {
                        GeodeticForVector3(hit.collider.transform.TransformPoint(hit.point), out double latitude, out double longitude);
                        GeocellTexture.Set(latitude, longitude, GeocellColor);
                    }
                }
            }
            if (Input.GetMouseButtonDown(0))
            {
                if (Physics.Raycast(Camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
                {
                    GeodeticForVector3(hit.collider.transform.TransformPoint(hit.point), out double latitude, out double longitude);
                    bool is_set = GeocellTexture.Color(latitude, longitude).a != 0;
                    if(!ctrl)
                        GeocellTexture.Clear();
                    if (is_set)
                        GeocellTexture.Set(latitude, longitude, Color.clear);
                    else
                        GeocellTexture.Set(latitude, longitude, GeocellColor);
                    Debug.LogFormat("{0}, {1}", latitude, longitude);
                }
            }
        }

        void GeodeticForVector3(Vector3 position, out double latitude, out double longitude)
        {
            double x = position.x / Scale;
            double y = position.z / Scale;
            double z = position.y / Scale;
            WGS84.ConvertFromECEF(x, y, z, out latitude, out longitude, out double altitude);
        }

        private void RotateGlobe()
        {
            if (isTouch)
            {
                if (Input.touchCount == 2)
                    return;
                if (!isDragging && Input.GetTouch(0).phase == TouchPhase.Began)
                    InitDrag();
                if (isDragging && Input.GetTouch(0).phase == TouchPhase.Moved)
                    MoveCamera();
                if (isDragging && Input.GetTouch(0).phase == TouchPhase.Ended)
                    FinishDrag();
            }
            else
            {
                if (!isDragging && Input.GetKeyDown(KeyCode.Mouse1))
                    InitDrag();
                if (isDragging && Input.GetKey(KeyCode.Mouse1))
                    MoveCamera();
                if (isDragging && Input.GetKeyUp(KeyCode.Mouse1))
                    FinishDrag();
            }
        }

        private Vector3 GetCurrentMousePosition()
        {
            Ray ray = Camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
                return hit.collider.transform.TransformPoint(hit.point);
            else
                return Vector3.zero;
        }

        private void InitDrag()
        {
            if (EventSystem.current.currentSelectedGameObject != null)
                return;
            DragStart = GetCurrentMousePosition();
            prevPosition = DragStart;
            if (DragStart == Vector3.zero)
                return;
            isDragging = true;
        }

        private void MoveCamera()
        {
            if (!(actualPos == null))
                prevPosition = actualPos;
            if (DragStart == Vector3.zero || EventSystem.current.currentSelectedGameObject != null)
                return;
            actualPos = GetCurrentMousePosition();
            if (actualPos == Vector3.zero)
                return;
            Vector3 DragDelta = actualPos - DragStart;
            float rotation = Mathf.Sqrt(Mathf.Pow(DragDelta.x,2f) + Mathf.Pow(DragDelta.z,2f));
            if (actualPos.x > 0 && actualPos.z > 0)
            {
                if(DragDelta.x > 0)
                {
                    rotation *= -1;
                }
            }
            else if(actualPos.x > 0 && actualPos.z < 0)
            {
                if (DragDelta.x < 0)
                {
                    rotation *= -1;
                }
            }
            else if(actualPos.x < 0 && actualPos.z > 0)
            {
                if (DragDelta.x > 0)
                {
                    rotation *= -1;
                }
            }
            else
            {
                if (DragDelta.x < 0)
                {
                    rotation *= -1;
                }
            }
            Camera.transform.RotateAround(Vector3.zero, Vector3.up, rotation);
            Camera.transform.RotateAround(Model.transform.position, -Camera.transform.right, DragDelta.y);
        }

        private void FinishDrag()
        {
            isDragging = false;
        }

        private Vector2 ToLatLong(Vector3 pos, float sphereRadius)
        {
            float lat = Mathf.Acos(pos.y / sphereRadius);
            float lon = Mathf.Atan(pos.x / pos.z);
            return new Vector2(lat, lon);
        }

        private void GetPinchInfo()
        {
            if (Input.touchCount != 2)
                return;
            Touch touch1 = Input.touches[0];
            Touch touch2 = Input.touches[1];

            pinchDistance = Vector2.Distance(touch1.position, touch2.position);
            float prevDist = Vector2.Distance(touch1.position - touch1.deltaPosition, touch2.position - touch2.deltaPosition);

            pinchDistanceDelta = pinchDistance - prevDist;
        }
        
        private void CameraZoom()
        {
            if (isTouch)
            {
                if (Input.touchCount != 2)
                    return;
                Vector3 tempPosition = Camera.transform.position;
                Camera.transform.position += (Model.transform.position - Camera.transform.position) * pinchDistanceDelta * .001f;
                float camDistance = Vector3.Distance(Model.transform.position, Camera.transform.position);
                if (camDistance < MIN_DISTANCE || camDistance > MAX_DISTANCE)
                    Camera.transform.position = tempPosition;
            }
            else
            {
                Vector3 tempPosition = Camera.transform.position;
                Camera.transform.position += (Model.transform.position - Camera.transform.position) * Input.mouseScrollDelta.y / 10;
                float camDistance = Vector3.Distance(Model.transform.position, Camera.transform.position);
                if (camDistance < MIN_DISTANCE || camDistance > MAX_DISTANCE)
                    Camera.transform.position = tempPosition;
            }
        }
    }
}
