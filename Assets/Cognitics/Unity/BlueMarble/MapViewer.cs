
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cognitics.Unity.BlueMarble
{
    public class MapViewer : MonoBehaviour
    {
        public Camera Camera;
        public GameObject Model;

        private bool isDragging;
        private Vector3 DragStart;
        private bool isTouch;
        private const float MIN_DISTANCE = -10f;
        private const float MAX_DISTANCE = -160f;
        private float cameraDistance;
        private float pinchDistance;
        private float pinchDistanceDelta;
        private MeshFilter modelMesh;

        const float Scale = 1e-5f;
        GeocellTexture GeocellTexture;

        private void Start()
        {
            cameraDistance = Camera.transform.position.z;
            GeocellTexture = new GeocellTexture();
            Model.GetComponent<MeshRenderer>().material.SetTexture("_SelectionTex", GeocellTexture.Texture);
            modelMesh = Model.GetComponent<MeshFilter>();
        }

        void Update()
        {
            isTouch = (Input.touchCount >= 1) ? true : false;
            if (isTouch)
                GetPinchInfo();
            CameraZoom();
            CameraMovement();
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
                        var point = hit.collider.transform.TransformPoint(hit.point);
                        GeocellTexture.Set(point.y, point.x, GeocellColor);
                    }
                }
            }
            if (Input.GetMouseButtonDown(0))
            {
                if (Physics.Raycast(Camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
                {
                    var point = hit.collider.transform.TransformPoint(hit.point);
                    bool is_set = GeocellTexture.Color(point.y, point.x).a != 0;
                    if(!ctrl)
                        GeocellTexture.Clear();
                    if (is_set)
                        GeocellTexture.Set(point.y, point.x, Color.clear);
                    else
                        GeocellTexture.Set(point.y, point.x, GeocellColor);
                    Debug.LogFormat("{0}, {1}", point.y, point.x);
                }
            }
        }

        private void InitDrag()
        {
            if (EventSystem.current.currentSelectedGameObject != null)
                return;
            DragStart = GetCurrentMousePosition();
            if (DragStart == Vector3.zero)
                return;
            isDragging = true;
        }

        private void MoveCamera()
        {
            if (DragStart == Vector3.zero)
                return;
            if (EventSystem.current.currentSelectedGameObject != null)
                return;
            Vector3 actualPos = GetCurrentMousePosition();
            if (actualPos == Vector3.zero)
                return;
            Vector3 DragDelta = actualPos - DragStart;
            Vector3 currentPos = Camera.transform.position;
            Camera.transform.position -= DragDelta;
            if (!BoundMap())
            {
                Camera.transform.position = currentPos;
                DragStart = GetCurrentMousePosition();
            }
        }

        private void FinishDrag()
        {
            isDragging = false;
        }

        private Vector3 GetCurrentMousePosition()
        {
            Ray ray = Camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
                return hit.collider.transform.TransformPoint(hit.point);
            else
                return Vector3.zero;
        }
        
        private void CameraMovement()
        {
            if (isTouch)
            {
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
                cameraDistance += pinchDistanceDelta * .4f;
                cameraDistance = Mathf.Clamp(cameraDistance, MAX_DISTANCE, MIN_DISTANCE);
                Camera.transform.position = new Vector3(Camera.transform.position.x, Camera.transform.position.y, cameraDistance);
                pinchDistance = pinchDistanceDelta = 0f;
            }
            else
            {
                cameraDistance += Input.mouseScrollDelta.y * 10;
                cameraDistance = Mathf.Clamp(cameraDistance, MAX_DISTANCE, MIN_DISTANCE);
                Camera.transform.position = new Vector3(Camera.transform.position.x, Camera.transform.position.y, cameraDistance);
            }
        }

        private bool BoundMap()
        {
            Vector2 lowerLeft = RectTransformUtility.WorldToScreenPoint(Camera, modelMesh.mesh.vertices[0]);
            Vector2 upperRight = RectTransformUtility.WorldToScreenPoint(Camera, modelMesh.mesh.vertices[1]);
            Vector2 lowerRight = RectTransformUtility.WorldToScreenPoint(Camera, modelMesh.mesh.vertices[2]);
            Vector2 upperLeft = RectTransformUtility.WorldToScreenPoint(Camera, modelMesh.mesh.vertices[3]);

            float x = Camera.pixelWidth / 2;
            float y = Camera.pixelHeight / 2;

            if (!(lowerLeft.x < x && lowerLeft.y < y))
                return false;
            if (!(upperRight.x > x && upperRight.y > y))
                return false;
            if (!(lowerRight.x > x && lowerRight.y < y))
                return false;
            if (!(upperLeft.x < x && upperLeft.y > y))
                return false;
            return true;
        }
    }
}
