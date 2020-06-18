using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class POIButton : MonoBehaviour
{
    [HideInInspector] public string buttonText;
    [HideInInspector]public Vector3 locationPosition;
    private Button button;
    private GameObject user;
    private Vector3 offset = new Vector3(0, 200, 0);
    

    private void Awake()
    {
        user = GameObject.Find("UserObject");
        gameObject.GetComponentInChildren<Text>().text = buttonText;
        button = gameObject.GetComponent<Button>();
        button.onClick.AddListener(MoveUser);
    }

    void MoveUser()
    {
        user.transform.position = locationPosition + offset;
    }
}
