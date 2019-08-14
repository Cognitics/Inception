
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;  
using System.Collections.Generic;

public class ModeButton : MonoBehaviour
{
    private const int StateOverview = 0;
    private const int StateSelecting = 1;
    private const int StateDetail = 2;

    private int State = StateOverview;

    private Button Button;
    private Image ButtonImage;
    private Text ButtonText;
    private GameObject UserObject;
    private Camera Camera;
    private GameObject TerrainTester;
    private Cognitics.UnityCDB.SurfaceCollider SurfaceCollider;
    private bool justSelected = false;
    public bool isDetailMode = false;

    // TODO: this should only be visible if a database is loaded

    void Start()
    {
        Button = GetComponent<Button>();
        ButtonImage = GetComponent<Image>();
        ButtonText = GetComponentInChildren<Text>();
        Camera = Camera.main;
        UserObject = Camera.transform.parent.gameObject;
        TerrainTester = GameObject.Find("TerrainTester");
        SurfaceCollider = TerrainTester.GetComponent<Cognitics.UnityCDB.SurfaceCollider>();
    }

    void Update()
    {
        if (State != StateSelecting)
            return;
        if (justSelected)
        {
            justSelected = false;
            return;
        }
        if (Input.GetKeyUp(KeyCode.Mouse0))
            HandleSelect(Input.mousePosition, false);
        if ((Input.touchCount > 0) && (Input.GetTouch(0).phase == TouchPhase.Ended))
            HandleSelect(Input.GetTouch(0).position, true);
    }

    void UpdateState(int state)
    {
        State = state;
        ButtonImage.color = Button.colors.normalColor;
        switch (State)
        {
            case StateOverview:
                ButtonText.text = "Detail Mode";
                break;
            case StateSelecting:
                ButtonText.text = "Select Location";
                ButtonImage.color = Button.colors.pressedColor;
                break;
            case StateDetail:
                ButtonText.text = "Overview Mode";
                isDetailMode = true;
                break;
        }
    }

    public void OnClick()
    {
        if (!justSelected)
            justSelected = true;
        switch (State)
        {
            case StateOverview: UpdateState(StateSelecting); break;
            case StateSelecting: UpdateState(StateOverview); break;
            case StateDetail: SwitchToOverview(); break;
        }
    }

    private void SwitchToOverview()
    {
        UpdateState(StateOverview);
        SurfaceCollider.Database.SetLODBracketsForOverview();
        isDetailMode = false;
        Camera.farClipPlane = 50000.0f;
    }

    private bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    public void HandleSelect(Vector3 position, bool touch)
    {
        if (!touch && EventSystem.current.IsPointerOverGameObject())
        {
            UpdateState(StateOverview);
            return;
        }
        if(IsPointerOverUIObject())
        {
            UpdateState(StateOverview);
            return;
        }
        var location = TerrainLocationForPosition(position);
        if (float.IsInfinity(location.x))
        {
            UpdateState(StateOverview);
            return;
        }
        UpdateState(StateDetail);
        location.y += 10.0f;
        UserObject.transform.position = location;
        SurfaceCollider.Database.SetLODBracketsForDetail();
        Camera.farClipPlane = 5000.0f;
    }

    private Vector3 TerrainLocationForPosition(Vector3 position)
    {
        Ray ray = Camera.ScreenPointToRay(position);
        TerrainTester.transform.position = Camera.transform.position;
        while (true)
        {
            TerrainTester.transform.position += ray.direction * (Time.deltaTime * 15f);
            SurfaceCollider.TerrainElevationGetter();
            if (TerrainTester.transform.position.y < SurfaceCollider.minCameraElevation)
                break;
            if (Vector3.SqrMagnitude(TerrainTester.transform.position - Camera.transform.position) > (50000f * 50000f))
                return Vector3.positiveInfinity;
        }
        return TerrainTester.transform.position;
    }

}
