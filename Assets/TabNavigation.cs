using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TabNavigation : MonoBehaviour
{
    public InputField UserName;
    public InputField Password;
    public InputField Device;

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            if (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() == null)
                return;
            if (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() == UserName)
                EventSystem.current.SetSelectedGameObject(Password.gameObject);
            else if (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() == Password)
                EventSystem.current.SetSelectedGameObject(Device.gameObject);
            else if (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() == Device)
                EventSystem.current.SetSelectedGameObject(UserName.gameObject);
        }

        
    }
}
