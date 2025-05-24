using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainUIScript : MonoBehaviour
{
    [SerializeField]private ProfileScript profileScript;
    [SerializeField] private GameObject startingScreenUI;
    [SerializeField] private GameObject waitingScreenUI;
    [SerializeField] private GameObject quickPlayUI;
    [SerializeField] private GameObject createRoomUI;
    [SerializeField] private GameObject findRoomUI;
    [SerializeField] private GameObject profileUI;
    [SerializeField] private GameObject[] currentMode1v1;
    [SerializeField] private GameObject[] currentMode2v2;
    [SerializeField] private GameObject currentModeYellow;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnQuickPlayButtonClicked()
    {
        quickPlayUI.SetActive(true);
    }
    public void OnCreateRoomButtonClicked()
    {
        createRoomUI.SetActive(true);
    }
    public void OnFindRoomButtonClicked()
    {
        findRoomUI.SetActive(true);
    }

    public void OnProfileButtonClicked()
    {
        profileUI.SetActive(true);
        gameObject.GetComponent<ProfileScript>().UpdateMatchCountText();
    }

    public void OnQuickPlayCloseButtonClicked()
    {
        quickPlayUI.SetActive(false);
    }
    public void OnCreateRoomCloseButtonClicked()
    {
        createRoomUI.SetActive(false);
    }
    public void OnFindRoomCloseButtonClicked()
    {
        findRoomUI.SetActive(false);
    }

    public void OnProfileCloseButtonClicked()
    {
        profileUI.SetActive(false);
        profileScript.ResetCardBackShowcase();
        profileScript.ResetProfilePicShowcase();
    }

    public void OpenWaitingScreenUI(string color, string playerCount, string joinCode)
    {
        if(startingScreenUI.activeSelf)startingScreenUI.SetActive(false);
        waitingScreenUI.SetActive(true);

        if(color == "red")
        {
            if(playerCount == "2")
            {
                currentMode1v1[0].SetActive(true);
            }
            else if(playerCount == "4")
            {
                currentMode2v2[0].SetActive(true);
            }
        }
        else if(color == "blue")
        {
            if (playerCount == "2")
            {
                currentMode1v1[1].SetActive(true);
                currentMode1v1[1].transform.GetChild(0).gameObject.GetComponent<Text>().text = joinCode;
            }
            else if (playerCount == "4")
            {
                currentMode2v2[1].SetActive(true);
                currentMode2v2[1].transform.GetChild(0).gameObject.GetComponent<Text>().text = joinCode;
            }
        }
        else if(color == "yellow")
        {
            currentModeYellow.SetActive(true);
            currentModeYellow.transform.GetChild(0).gameObject.GetComponent<Text>().text = joinCode;
        }
    }
}
