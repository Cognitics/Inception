using UnityEngine;
using UnityEngine.EventSystems;

namespace Cognitics.Unity.BlueMarble
{
    public class MapViewer : MonoBehaviour
    {
        public Camera Camera;
        public GameObject Model;

        private Vector3 StartDragPosition;
        private const float MinimumZoomDistance = .1f;
        private const float MaximumZoomDistance = 100f;
        private MeshFilter modelMesh;

        const float Scale = 1e-5f;
        GeocellTexture GeocellTexture;

        private void Start()
        {
            GeocellTexture = new GeocellTexture();
            Model.GetComponent<MeshRenderer>().material.SetTexture("_SelectionTex", GeocellTexture.Texture);
            modelMesh = Model.GetComponent<MeshFilter>();
        }

        void Update()
        {
            if(Input.touchCount == 1)
            {
                if (Input.GetTouch(0).phase == TouchPhase.Began)
                    BeginDragging();
                if (Input.GetTouch(0).phase == TouchPhase.Moved)
                    DoDragging();
                if (Input.GetTouch(0).phase == TouchPhase.Ended)
                    EndDragging();
            }
            if (Input.touchCount == 2)
            {
                Touch touch1 = Input.touches[0];
                Touch touch2 = Input.touches[1];
                float pinchDistanceDelta = 
                    Vector2.Distance(touch1.position, touch2.position) 
                    - Vector2.Distance(touch1.position - touch1.deltaPosition, touch2.position - touch2.deltaPosition);
                Zoom(-pinchDistanceDelta * .4f);
            }
            if(Input.touchCount == 0)
            {
                if (Input.GetKeyDown(KeyCode.Mouse1))
                    BeginDragging();
                if (Input.GetKey(KeyCode.Mouse1))
                    DoDragging();
                if (Input.GetKeyUp(KeyCode.Mouse1))
                    EndDragging();

                Zoom(-Input.mouseScrollDelta.y);
            }

            UpdateGeocellSelection();
            //Testing
            if(Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(0))
            {
                #region Wide Section
                /*
                CenterCameraOnLocation(Camera, new Vector3((float)((-18 + -8) * 0.5), (float)((54 + 52) * 0.5), 0.0f));
                FitBoundsToScreen(Camera, 52, -8, 54, -18);


                GeocellTexture.Set((float)52.5, (float)-8.5, GeocellColor);
                GeocellTexture.Set((float)52.5, (float)-9.5, GeocellColor);
                GeocellTexture.Set((float)52.5, (float)-10.5, GeocellColor);
                GeocellTexture.Set((float)52.5, (float)-11.5, GeocellColor);
                GeocellTexture.Set((float)52.5, (float)-12.5, GeocellColor);
                GeocellTexture.Set((float)52.5, (float)-13.5, GeocellColor);
                GeocellTexture.Set((float)52.5, (float)-14.5, GeocellColor);
                GeocellTexture.Set((float)52.5, (float)-15.5, GeocellColor);
                GeocellTexture.Set((float)52.5, (float)-16.5, GeocellColor);
                GeocellTexture.Set((float)52.5, (float)-17.5, GeocellColor);

                GeocellTexture.Set((float)53.5, (float)-8.5, GeocellColor);
                GeocellTexture.Set((float)53.5, (float)-9.5, GeocellColor);
                GeocellTexture.Set((float)53.5, (float)-10.5, GeocellColor);
                GeocellTexture.Set((float)53.5, (float)-11.5, GeocellColor);
                GeocellTexture.Set((float)53.5, (float)-12.5, GeocellColor);
                GeocellTexture.Set((float)53.5, (float)-13.5, GeocellColor);
                GeocellTexture.Set((float)53.5, (float)-14.5, GeocellColor);
                GeocellTexture.Set((float)53.5, (float)-15.5, GeocellColor);
                GeocellTexture.Set((float)53.5, (float)-16.5, GeocellColor);
                GeocellTexture.Set((float)53.5, (float)-17.5, GeocellColor);*/

                #endregion

                #region Tall Section

                CenterCameraOnLocation(Camera, new Vector3((float)((-10 + -8) * 0.5), (float)((56 + 52) * 0.5), 0.0f));
                FitBoundsToScreen(Camera, 52, -8, 56, -10);
                GeocellTexture.Set((float)52.5, (float)-8.5, GeocellColor);
                GeocellTexture.Set((float)52.5, (float)-9.5, GeocellColor);

                GeocellTexture.Set((float)53.5, (float)-8.5, GeocellColor);
                GeocellTexture.Set((float)53.5, (float)-9.5, GeocellColor);

                GeocellTexture.Set((float)54.5, (float)-8.5, GeocellColor);
                GeocellTexture.Set((float)54.5, (float)-9.5, GeocellColor);

                GeocellTexture.Set((float)55.5, (float)-8.5, GeocellColor);
                GeocellTexture.Set((float)55.5, (float)-9.5, GeocellColor);
                #endregion
            }
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

        private void BeginDragging()
        {
            if (EventSystem.current.currentSelectedGameObject != null)
                return;
            StartDragPosition = WorldPosition(Input.mousePosition);
            if (StartDragPosition == Vector3.zero)
                return;
        }

        private void DoDragging()
        {
            if (StartDragPosition == Vector3.zero)
                return;
            if (EventSystem.current.currentSelectedGameObject != null)
                return;
            Vector3 CurrentDragPosition = WorldPosition(Input.mousePosition);
            if (CurrentDragPosition.x == float.PositiveInfinity || StartDragPosition.x == float.PositiveInfinity)
                return; 
            Vector3 DragDelta = CurrentDragPosition - StartDragPosition;
            Vector3 currentPos = Camera.transform.position;
            Camera.transform.position -= DragDelta;
            if (!BoundMap())
            {
                Camera.transform.position = currentPos;
                StartDragPosition = WorldPosition(Input.mousePosition);
            }
        }

        private void EndDragging()
        {
            StartDragPosition = Vector3.positiveInfinity;
        }

        private Vector3 WorldPosition(Vector3 screenPosition)
        {
            Ray ray = Camera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit)) 
                return hit.collider.transform.TransformPoint(hit.point);
            else
                return Vector3.positiveInfinity;
        }

        private void Zoom(float factor)
        {
            float cameraDistance = Camera.orthographicSize + factor;
            cameraDistance = Mathf.Clamp(cameraDistance, MinimumZoomDistance, MaximumZoomDistance);
            Camera.orthographicSize = cameraDistance;
        }

        private bool BoundMap()
        {
            Vector2 lowerL = RectTransformUtility.WorldToScreenPoint(Camera, modelMesh.mesh.vertices[0]);
            Vector2 upperR = RectTransformUtility.WorldToScreenPoint(Camera, modelMesh.mesh.vertices[1]);
            Vector2 lowerR = RectTransformUtility.WorldToScreenPoint(Camera, modelMesh.mesh.vertices[2]);
            Vector2 upperL = RectTransformUtility.WorldToScreenPoint(Camera, modelMesh.mesh.vertices[3]);

            float x = Camera.pixelWidth / 2;
            float y = Camera.pixelHeight / 2;

            if (!(lowerL.x < x && lowerL.y < y))
                return false;
            if (!(upperR.x > x && upperR.y > y))
                return false;
            if (!(lowerR.x > x && lowerR.y < y))
                return false;
            if (!(upperL.x < x && upperL.y > y))
                return false;
            return true;
        }

        static void CenterCameraOnLocation(Camera camera, Vector3 location)
        {
            location.z = -1f;
            camera.transform.position = location;
        }

        static void FitBoundsToScreen(Camera camera, double min_lat, double min_lon, double max_lat, double max_lon)
        { 
            var topRight = new Vector3((float)min_lon, (float)max_lat, 0f);
            var bottomLeft = new Vector3((float)max_lon, (float)min_lat, 0f);
            var centerPoint = new Vector3((float)((max_lon + min_lon) * 0.5), (float)((max_lat + min_lat) * 0.5), 0.0f);
            var topCenter = (new Vector3(bottomLeft.x, topRight.y, 0f) + topRight) / 2;
            var leftCenter = (new Vector3(bottomLeft.x, topRight.y, 0f) + bottomLeft) / 2;
            float distToTop = Mathf.Abs(Vector3.Distance(centerPoint, topCenter));
            float distToSide = Mathf.Abs(Vector3.Distance(centerPoint, leftCenter));
            if (distToTop > distToSide)
                camera.orthographicSize = distToTop + .2f;
            else
                camera.orthographicSize = (distToSide / camera.aspect) + .2f;
        }


    }
}
