
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
        if (Input.GetKeyDown(KeyCode.Mouse0))
            HandleSelect(Input.mousePosition);
        if ((Input.touchCount > 0) && (Input.GetTouch(0).phase == TouchPhase.Began))
            HandleSelect(Input.GetTouch(0).position);
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

    public void HandleSelect(Vector3 position)
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            UpdateState(StateOverview);
            return;
        }
        var location = TerrainLocationForPosition(position);
        if (location == Vector3.positiveInfinity)
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
