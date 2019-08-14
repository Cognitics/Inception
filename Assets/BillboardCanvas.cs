using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardCanvas : MonoBehaviour
{
    [HideInInspector] public Camera Camera;
    public Color Color = Color.grey;
    [HideInInspector] public YemenHG YemenHG;

    public class Billboard
    {
        public GameObject GameObject;
        public Vector3 Position;
        public string Text;

    }

    public int CompareByDistance2(Billboard a, Billboard b)
    {
        var adist = (a.Position - Camera.transform.position).sqrMagnitude;
        var bdist = (b.Position - Camera.transform.position).sqrMagnitude;
        return adist.CompareTo(bdist);
    }

    public List<Billboard> Billboards = new List<Billboard>();

    void Awake()
    {
        Camera = Camera.main;
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -100;
    }

    void Update()
    {
        if (Time.frameCount % 60 == 0)
        {
            Billboards.Sort(CompareByDistance2);
            Billboards.Reverse();
            for (int i = 0; i < Billboards.Count; ++i)
                Billboards[i].GameObject.transform.SetSiblingIndex(i);
        }

        float mouseX = Input.GetMouseButtonDown(0) ? Input.mousePosition.x : float.NaN;
        float mouseY = Input.GetMouseButtonDown(0) ? Input.mousePosition.y : float.NaN;

        foreach (var billboard in Billboards)
        {
            var heading = billboard.Position - Camera.transform.position;
            var dot = Vector3.Dot(Camera.transform.forward, heading);

            var distance2 = (billboard.Position - Camera.transform.position).sqrMagnitude;
            const float maxReadableDistance = 1000.0f * 0.1f;
            const float maxReadableDistance2 = maxReadableDistance * maxReadableDistance;
            const float maxVisibleDistance = 5000.0f * 0.1f;
            const float maxVisibleDistance2 = maxVisibleDistance * maxVisibleDistance;

            billboard.GameObject.SetActive((dot > 0) || (distance2 > maxVisibleDistance2));

            billboard.GameObject.GetComponent<UnityEngine.UI.Text>().text = (distance2 < maxReadableDistance2) ? billboard.Text : "*";

            var position = Camera.WorldToScreenPoint(billboard.Position);
            position.x = Mathf.Max(position.x, 0.0f);
            position.x = Mathf.Min(position.x, Screen.width);
            position.y = Mathf.Max(position.y, 0.0f);
            position.y = Mathf.Min(position.y, Screen.height);
            billboard.GameObject.transform.position = position;

            if (float.IsNaN(mouseX))
                continue;

            const float dist = 10.0f;

            if (Mathf.Abs(mouseX - position.x) > dist)
                continue;
            if (Mathf.Abs(mouseY - position.y) > dist)
                continue;

            Debug.Log("[SELECTED] " + billboard.Text);
            YemenHG.SetTargetNode(billboard.Position);
        }

    }

    public void AddBillboard(Vector3 position, string text)
    {
        var billboard = new Billboard();
        billboard.Position = position;
        billboard.Text = text;
        billboard.GameObject = new GameObject(text);
        billboard.GameObject.transform.SetParent(transform);
        var uitext = billboard.GameObject.AddComponent<UnityEngine.UI.Text>();
        uitext.alignment = TextAnchor.MiddleCenter;
        uitext.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        uitext.fontSize = 20;
        uitext.fontStyle = FontStyle.Bold;
        uitext.color = Color;
        uitext.text = "*";
        var outline = billboard.GameObject.AddComponent<UnityEngine.UI.Outline>();
        Billboards.Add(billboard);
    }


}
