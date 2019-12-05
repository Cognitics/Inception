using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System;

namespace Cognitics.Unity.BlueMarble
{
    [Serializable]
    public class ClickEvent : UnityEvent<GameObject, double, double> { }

    public class GlobeViewer : MonoBehaviour
    {
        public Camera Camera;
        public GameObject Model;
        public ClickEvent OnClick = new ClickEvent();

        private GameObject user;
        private GameObject uiControlsCanvas;
        private Vector3 StartDragPosition;
        private Vector3 CurrentDragPosition;
        private double prevLat, prevLon;

        private Vector3 northPole;
        private Vector3 southPole;
        

        const float MinimumZoomDistance = 64f;
        const float MaximumZoomDistance = 160f;

        const float Scale = 1e-5f;

        public GeocellTexture GeocellTexture;

        CoordinateSystems.WGS84Transform WGS84Transform = new CoordinateSystems.WGS84Transform();




        void Start()
        {
            WGS84Transform.GeodeticToECEF(90, 0, 0, out double northX, out double northZ, out double northY);
            WGS84Transform.GeodeticToECEF(-90, 0, 0, out double southX, out double southZ, out double southY);
            northPole = new Vector3((float)northX * Scale, (float)northY * Scale, (float)northZ * Scale);
            southPole = new Vector3((float)southX * Scale, (float)southY * Scale, (float)southZ * Scale);
            GeocellTexture = new GeocellTexture();
            Model.GetComponent<MeshRenderer>().material.SetTexture("_SelectionTex", GeocellTexture.Texture);
        }

        void Update()
        {
            if (IsPointerOverUIObject())
                return;
            if (Input.touchCount == 1)
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
                Zoom(pinchDistanceDelta * 0.001f);
            }

            if(Input.mouseScrollDelta.y != 0)
                Zoom(Input.mouseScrollDelta.y * 0.01f);

            if (Input.GetKeyDown(KeyCode.Mouse1))
                BeginDragging();
            if (Input.GetKey(KeyCode.Mouse1))
                DoDragging();
            if (Input.GetKeyUp(KeyCode.Mouse1))
                EndDragging();

            //UpdateGeocellSelection();
            UpdateCenterAndFit();
            if (Input.GetMouseButtonDown(0) || Input.touchCount == 1)
            {
                if (!Physics.Raycast(Camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
                    return;
                var hitpoint = hit.collider.transform.TransformPoint(hit.point);
                WGS84Transform.ECEFtoGeodetic(hitpoint.x / Scale, hitpoint.z / Scale, hitpoint.y / Scale, out double hitLat, out double hitLon, out double hitAlt);
                OnClick.Invoke(gameObject, hitLat, hitLon);
            }
        }

        float MousePosition()
        {
            if (Physics.Raycast(Camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit))
                return hit.collider.transform.TransformPoint(hit.point).x;
            else
                return Vector3.zero.x;
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
            WGS84Transform.ECEFtoGeodetic(x, y, z, out latitude, out longitude, out double altitude);
        }

        private Vector3 WorldPosition(Vector3 screenPosition)
        {
            Ray ray = Camera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
                return hit.collider.transform.TransformPoint(hit.point);
            else
                return Vector3.positiveInfinity;
        }

        public void Zoom(float factor)
        {
            var newPosition = Camera.transform.position + ((Model.transform.position - Camera.transform.position) * factor);
            float camDistance = Vector3.Distance(Model.transform.position, newPosition);
            if (camDistance < MinimumZoomDistance || camDistance > MaximumZoomDistance)
                return;
            Camera.transform.position = newPosition;
        }

        #region Dragging

        private void BeginDragging()
        {
            if(IsPointerOverUIObject())
                return;
            StartDragPosition = WorldPosition(Input.mousePosition);
            if (StartDragPosition.x != float.PositiveInfinity)
                StartDragPosition /= Scale;
        }

        private void DoDragging()
        {
            if(IsPointerOverUIObject())
                return;
            if (StartDragPosition.x == float.PositiveInfinity)
                return;
            CurrentDragPosition = WorldPosition(Input.mousePosition);
            if (CurrentDragPosition.x == float.PositiveInfinity)
                return;
            CurrentDragPosition /= Scale;
            WGS84Transform.ECEFtoGeodetic(CurrentDragPosition.x, CurrentDragPosition.z, CurrentDragPosition.y, out double currentLat, out double currentLon, out double currentAlt);
            WGS84Transform.ECEFtoGeodetic(StartDragPosition.x, StartDragPosition.z, StartDragPosition.y, out double startLat, out double startLon, out double startAlt);
            // start/current are swapped so that rotation is opposite the mouse movement

            if(IsPointInView(northPole))
            {
                if ((currentLat - startLat) < 0.0)
                    currentLat = startLat;
            }
            if(IsPointInView(southPole))
            {
                if ((currentLat - startLat) > 0.0)
                    currentLat = startLat;
            }
            RotateAround(Camera.transform, currentLat, currentLon, startLat, startLon);
        }

        private void EndDragging()
        {
            StartDragPosition = Vector3.positiveInfinity;
        }

        #endregion

        #region MoveToLocation

        public void RotateAround(Transform transform, float lat_rotation, float lon_rotation)
        {
            transform.RotateAround(Vector3.zero, -Vector3.up, lon_rotation);
            transform.RotateAround(Vector3.zero, transform.right, lat_rotation);
        }

        public void RotateAround(Transform transform, double lat_start, double lon_start, double lat_target, double lon_target)
        {
            // ???? something isn't right here
            //RotateAround(transform, (float)(lat_start - lat_target), (float)(lon_start - lon_target));
            RotateAround(transform, (float)(lat_target - lat_start), (float)(lon_target - lon_start));
        }

        float RotateStartTime = 0.0f;
        float RotateDuration = 0.0f;
        double RotateStartLatitude;
        double RotateStartLongitude;
        double RotateTargetLatitude;
        double RotateTargetLongitude;
        float CameraDistance;
        float CameraPreviousDistance;
        double min_lat, min_lon, max_lat, max_lon;

        public void CenterAndFit(double min_lat, double min_lon, double max_lat, double max_lon, float duration)
        {
            double center_lat = (min_lat + max_lat) / 2.0;
            double center_lon = (min_lon + max_lon) / 2.0;

            var ray = Camera.ViewportPointToRay(new Vector2(.5f, .5f));
            if (!Physics.Raycast(ray, out RaycastHit hit))
                return;
            var start_location = hit.collider.transform.TransformPoint(hit.point);
            start_location /= Scale;
            WGS84Transform.ECEFtoGeodetic(start_location.x, start_location.z, start_location.y, out double start_lat, out double start_lon, out double start_elev);
            this.min_lat = min_lat;
            this.min_lon = min_lon;
            this.max_lat = max_lat;
            this.max_lon = max_lon;
            RotateStartTime = Time.fixedTime;
            RotateDuration = duration;
            RotateStartLatitude = start_lat;
            RotateStartLongitude = start_lon;
            RotateTargetLatitude = center_lat;
            RotateTargetLongitude = center_lon;
            CameraDistance = CameraDistanceForBounds(min_lat, min_lon, max_lat, max_lon);
            CameraPreviousDistance = 0;
        }

        void UpdateCenterAndFit()
        {
            if (RotateDuration == 0.0f)
                return;
            float progress = (Time.fixedTime - RotateStartTime) / RotateDuration;
            double lat = RotateStartLatitude + ((RotateTargetLatitude - RotateStartLatitude) * progress);
            double lon = RotateStartLongitude + ((RotateTargetLongitude - RotateStartLongitude) * progress);
            if (Time.fixedTime - RotateStartTime > RotateDuration)
            {
                lat = RotateTargetLatitude;
                lon = RotateTargetLongitude;
                RotateDuration = 0.0f;
            }
            CenterCameraOnLocation(Camera, Scale, lat, lon);
            var stepDist = (CameraDistance * progress) - CameraPreviousDistance;
            Camera.transform.position = Vector3.MoveTowards(Camera.transform.position, Vector3.zero, stepDist);
            CameraPreviousDistance = (CameraDistance * progress);
        }

        public void CenterCameraOnLocation(double latitude, double longitude) => CenterCameraOnLocation(Camera, Scale, latitude, longitude);
        public void CenterCameraOnLocation(Camera camera, float scale, double latitude, double longitude)
        {
            var ray = camera.ViewportPointToRay(new Vector2(.5f, .5f));
            if (!Physics.Raycast(ray, out RaycastHit hit))
                return;
            var start_location = hit.collider.transform.TransformPoint(hit.point);
            start_location /= scale;
            WGS84Transform.ECEFtoGeodetic(start_location.x, start_location.z, start_location.y, out double start_lat, out double start_lon, out double start_elev);
            RotateAround(camera.transform, start_lat, start_lon, latitude, longitude);
        }

        public void FitBoundsToScreen(double min_lat, double min_lon, double max_lat, double max_lon) => FitBoundsToScreen(Camera, Scale, min_lat, min_lon, max_lat, max_lon);
        public void FitBoundsToScreen(Camera camera, float scale, double min_lat, double min_lon, double max_lat, double max_lon)
        {
            WGS84Transform.GeodeticToECEF(max_lat, min_lon, 0.0, out double topRightX, out double topRightZ, out double topRightY);
            WGS84Transform.GeodeticToECEF(min_lat, max_lon, 0.0, out double bottomLeftX, out double bottomLeftZ, out double bottomLeftY);

            WGS84Transform.GeodeticToECEF(max_lat, max_lon, 0.0, out double topLeftX, out double topLeftZ, out double topLeftY);
            WGS84Transform.GeodeticToECEF(min_lat, min_lon, 0.0, out double bottomRightX, out double bottomRightZ, out double bottomRightY);

            var topRight = new Vector3((float)topRightX * scale, (float)topRightY * scale, (float)topRightZ * scale);
            var bottomLeft = new Vector3((float)bottomLeftX * scale, (float)bottomLeftY * scale, (float)bottomLeftZ * scale);
            var topLeft = new Vector3((float)topLeftX * scale, (float)topLeftY * scale, (float)topLeftZ * scale);
            var bottomRight = new Vector3((float)bottomRightX * scale, (float)bottomRightY * scale, (float)bottomRightZ * scale);
            var centerPoint = new Vector3((topRight.x + bottomLeft.x) / 2, (topRight.y + bottomLeft.y) / 2, (topRight.z + bottomLeft.z) / 2);

            CenterCameraOnLocation(camera, scale, (max_lat + min_lat) / 2, (max_lon + min_lon) / 2);

            float width = Vector3.Distance(topLeft, topRight);
            float height = Vector3.Distance(topLeft, bottomLeft);
            
            if(width/height > camera.aspect)
            {
                var hFOV = Mathf.Rad2Deg * 2 * Mathf.Atan(Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad / 2) * camera.aspect);
                var dist = width * .5f / Mathf.Tan(hFOV * .5f * Mathf.Deg2Rad) + .4f;

                float distToMove = Vector3.Distance(camera.transform.position, centerPoint) - dist;
                camera.transform.position = Vector3.MoveTowards(camera.transform.position, Vector3.zero, distToMove);
            }
            else
            {
                var dist = height * .5f / Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad) + .4f;

                float distToMove = Vector3.Distance(camera.transform.position, centerPoint) - dist;
                camera.transform.position = Vector3.MoveTowards(camera.transform.position, Vector3.zero, distToMove);
            }
        }
        
        public float CameraDistanceForBounds(double min_lat, double min_lon, double max_lat, double max_lon)
        {
            float distToMove = 0f;
            WGS84Transform.GeodeticToECEF(max_lat, min_lon, 0.0, out double topRightX, out double topRightZ, out double topRightY);
            WGS84Transform.GeodeticToECEF(min_lat, max_lon, 0.0, out double bottomLeftX, out double bottomLeftZ, out double bottomLeftY);

            WGS84Transform.GeodeticToECEF(max_lat, max_lon, 0.0, out double topLeftX, out double topLeftZ, out double topLeftY);
            WGS84Transform.GeodeticToECEF(min_lat, min_lon, 0.0, out double bottomRightX, out double bottomRightZ, out double bottomRightY);

            var topRight = new Vector3((float)topRightX * Scale, (float)topRightY * Scale, (float)topRightZ * Scale);
            var bottomLeft = new Vector3((float)bottomLeftX * Scale, (float)bottomLeftY * Scale, (float)bottomLeftZ * Scale);
            var topLeft = new Vector3((float)topLeftX * Scale, (float)topLeftY * Scale, (float)topLeftZ * Scale);
            var bottomRight = new Vector3((float)bottomRightX * Scale, (float)bottomRightY * Scale, (float)bottomRightZ * Scale);

            if (!Physics.Raycast(Camera.ViewportPointToRay(new Vector2(.5f, .5f)), out RaycastHit hit))
                return -1;
            var hitPoint = hit.collider.transform.TransformPoint(hit.point);
            var heightAboveTerrain = Vector3.Distance(hitPoint, Camera.transform.position);

            float width = Vector3.Distance(topLeft, topRight);
            float height = Vector3.Distance(topLeft, bottomLeft);

            if (width / height > Camera.aspect)
            {
                var hFOV = Mathf.Rad2Deg * 2 * Mathf.Atan(Mathf.Tan(Camera.fieldOfView * Mathf.Deg2Rad / 2) * Camera.aspect);
                var dist = width * .5f / Mathf.Tan(hFOV * .5f * Mathf.Deg2Rad) + 2f;
                distToMove = heightAboveTerrain - dist;
            }
            else
            {
                var dist = height * .5f / Mathf.Tan(Camera.fieldOfView * .5f * Mathf.Deg2Rad) + 2f;
                distToMove = heightAboveTerrain - dist;
            }
            return distToMove;
        }

        public bool IsPointInView(Vector3 point)
        {
            Vector2 screenCoords = Camera.WorldToViewportPoint(point);
            if (screenCoords.x < 0 || screenCoords.y < 0 || screenCoords.x > 1 || screenCoords.y > 1)
                return false;
            if (!Physics.Raycast(Camera.ViewportPointToRay(screenCoords), out RaycastHit hit))
                return true;
            return false;
        }

        static private bool IsPointerOverUIObject()
        {
            if (EventSystem.current == null)
                return false;
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            return results.Count > 0;
        }

        #endregion

    }
}
