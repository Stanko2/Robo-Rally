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
        if (Application.isBatchMode)
        {
            NetworkManager.singleton.onlineScene = "MultiplayerMenu";
            NetworkManager.singleton.StartServer();
            Debug.Log("Server started");
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "");
            Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, "");
            Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, "");
            Application.logMessageReceived += (logstring, stack, type) =>
            {
                Console.WriteLine(logstring);
            };
            return;
        }
        
        mainPanel.GetComponent<Animation>().Play("UIShow");
        startButton.onClick.AddListener(AiStart);
        quitButton.onClick.AddListener(Quit);
        multiplayerButton.onClick.AddListener(Multiplayer);
        settingsButton.onClick.AddListener(OpenMapEditor);
        try
        {
            NetworkManager.singleton.StartClient();
            Transport.activeTransport.OnClientError.AddListener(FailedToConnect);
            Transport.activeTransport.OnClientConnected.AddListener(()=>cantConnectUI.SetActive(false));
        }
        catch (Exception e)
        {
            FailedToConnect(e);
        }
        
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
        NetworkManager.singleton.autoCreatePlayer = true;
        NetworkManager.singleton.ServerChangeScene("Main");
        SceneManager.sceneLoaded += (scene, loadSceneMode) => NetworkServer.SpawnObjects();
    }

    private void Multiplayer()
    {
        SceneManager.LoadSceneAsync("MultiplayerMenu",LoadSceneMode.Single).completed += e => 
             GameObject.Find("MultiplayerPanel").GetComponent<Animation>().Play("UIShow");
        mainPanel.GetComponent<Animation>().Play("UIClose");
        cantConnectUI.SetActive(false);
        Debug.Log(NetworkClient.active);
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
        Multiplayer();
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
