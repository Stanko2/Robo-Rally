using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mirror;
using UnityEngine.Serialization;

public class Menu : MonoBehaviour
{
    [FormerlySerializedAs("StartButton")] public Button startButton;
    [FormerlySerializedAs("MultiplayerButton")] public Button multiplayerButton;
    [FormerlySerializedAs("SettingsButton")] public Button settingsButton;
    [FormerlySerializedAs("QuitButton")] public Button quitButton;
    [FormerlySerializedAs("MultiplayerPanel")] public GameObject multiplayerPanel;
    [FormerlySerializedAs("MainPanel")] public GameObject mainPanel;
    public static bool Local = false;

    public GameObject cantConnectUI;
    // Start is called before the first frame update
    private void Start()
    {
        mainPanel.GetComponent<Animation>().Play("UIShow");
        startButton.onClick.AddListener(AiStart);
        quitButton.onClick.AddListener(Quit);
        multiplayerButton.onClick.AddListener(Multiplayer);
        settingsButton.onClick.AddListener(OpenMapEditor);
        try
        {
            NetworkManager.singleton.StartClient();
            Transport.activeTransport.OnClientError.AddListener(FailedToConnect);
        }
        catch (Exception e)
        {
            FailedToConnect(e);
        }
        //SceneManager.sceneLoaded += (scene,loadSceneMode) => NetworkManager.singleton.ServerChangeScene(scene.name);
        
    }

    private void FailedToConnect(Exception e)
    {
        multiplayerButton.interactable = false;
        Local = true;
    }

    private void AiStart(){
        mainPanel.GetComponent<Animation>().Play("UIClose");
        NetworkManager.singleton.StartHost();
        GameController.SinglePlayer = true;
        SceneManager.LoadScene("Main");
        SceneManager.sceneLoaded += (scene, loadSceneMode) => NetworkServer.SpawnObjects();
    }

    private void Multiplayer()
    {
        NetworkManager.singleton.StartClient();
        SceneManager.LoadSceneAsync("MultiplayerMenu",LoadSceneMode.Additive).completed += e => 
            GameObject.Find("MultiplayerPanel").GetComponent<Animation>().Play("UIShow");
        mainPanel.GetComponent<Animation>().Play("UIClose");
    }

    public void JoinLocal()
    {
        NetworkManager.singleton.networkAddress = "localhost";
        NetworkManager.singleton.StartClient();
        multiplayerButton.interactable = true;     
        cantConnectUI.SetActive(false);
    }

    public void HostLocal()
    {
        NetworkManager.singleton.StartHost();
        multiplayerButton.interactable = true;
        cantConnectUI.SetActive(false);
    }
    
    private void Quit(){
        mainPanel.GetComponent<Animation>().Play("UIClose");
        Application.Quit();
    }

    private void OpenMapEditor(){
        mainPanel.GetComponent<Animation>().Play("UIClose");
        SceneManager.LoadScene("MapEditor");
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
