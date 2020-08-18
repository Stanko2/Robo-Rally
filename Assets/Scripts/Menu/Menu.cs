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
    // Start is called before the first frame update
    private void Start()
    {
        mainPanel.GetComponent<Animation>().Play("UIShow");
        startButton.onClick.AddListener(AiStart);
        quitButton.onClick.AddListener(Quit);
        multiplayerButton.onClick.AddListener(Multiplayer);
        settingsButton.onClick.AddListener(OpenMapEditor);
        //SceneManager.sceneLoaded += (scene,loadSceneMode) => NetworkManager.singleton.ServerChangeScene(scene.name);
    }

    private void AiStart(){
        mainPanel.GetComponent<Animation>().Play("UIClose");
        NetworkManager.singleton.StartHost();
        GameController.SinglePlayer = true;
        SceneManager.LoadScene("Main");
        SceneManager.sceneLoaded += (scene, loadSceneMode) => NetworkServer.SpawnObjects();
    }

    private void Multiplayer(){
        mainPanel.GetComponent<Animation>().Play("UIClose");
        multiplayerPanel.GetComponent<Animation>().Play("UIShow");
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
