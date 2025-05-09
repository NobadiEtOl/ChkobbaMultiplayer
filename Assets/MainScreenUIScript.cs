using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainScreenUIScript : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuUI;
    [SerializeField] private GameObject quickPlayUI;
    [SerializeField] private GameObject createRoomUI;
    [SerializeField] private GameObject findRoomUI;
    [SerializeField] private GameObject profileUI;
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
    }
}
