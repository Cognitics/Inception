using Cognitics.UnityCDB;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MagePanel : MonoBehaviour
{
    public GameObject MageObject;
    public Text LoginButtonText;
    public GameObject LoginScreen;
    public InputField UsernameText;
    public InputField PasswordText;
    public InputField DeviceText;
    public GameObject AddObservationButton;

    private MAGE MageScript;
    private bool checkLogin;

    void Start()
    {
        MageScript = MageObject.GetComponent<MAGE>();
        checkLogin = false;
    }

    void Update()
    {
        if (checkLogin)
            StartCoroutine(UpdateButtonText());
    }

    public IEnumerator UpdateButtonText()
    {
        LoginButtonText.text = "Logging into MAGE...";
        for(int i = 0; i < 15; ++i)
        {
            if(MageScript.SignedIn)
            {
                LoginButtonText.text = "Logged in as " + MageScript.Username;
                AddObservationButton.SetActive(true);
                yield break;
            }

            yield return new WaitForSecondsRealtime(1);
        }
        LoginButtonText.text = "Log in Failed";
        /*
        yield return new WaitForSecondsRealtime(5);
        if(MageScript == null)
            MageScript = MageObject.GetComponent<MAGE>();
        if (!MageScript.SignedIn)
            LoginButtonText.text = "Log in Failed";
        else
        {
            LoginButtonText.text = "Logged in as " + MageScript.Username;
            AddObservationButton.SetActive(true);
        }*/
        checkLogin = false;
    }

    public void OpenLoginScreen()
    {
        LoginScreen.SetActive(true);
        UsernameText.text = MageScript.Username;
        PasswordText.text = MageScript.Password;
        DeviceText.text = MageScript.Device;
    }

    public void CloseLoginScreen()
    {
        UsernameText.text = "";
        PasswordText.text = "";
        DeviceText.text = "";
        LoginScreen.SetActive(false);
    }

    public void PopulateMageObject()
    {
        MageScript.Username = UsernameText.text;
        MageScript.Password = PasswordText.text;
        MageScript.Device = DeviceText.text;
        checkLogin = true;
    }

    public void UpdateObservationButton()
    {
        //if (!MageScript.SignedIn)
        //    AddObservationButton.SetActive(false);
        //else
        //    AddObservationButton.SetActive(true);
    }
}
