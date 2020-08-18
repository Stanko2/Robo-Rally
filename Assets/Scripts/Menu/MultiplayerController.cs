using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Mirror.Discovery;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class MultiplayerController : MonoBehaviour
{
    public ServerDiscovery discovery;
    public Button createButton;
    public TextBox nameTextBox;
    public TextBox maxPlayersTextBox;
    public Dropdown mapselect;
    public NetworkManager network;
    public GameObject serverInfoPrefab;
    public Transform uiJoinPanel;

    Dictionary<long, ServerConfig> _servers;
    public static MultiplayerController Instance;
    // Start is called before the first frame update
    void Start()
    {
        network = NetworkManager.singleton;
        discovery = network.GetComponent<ServerDiscovery>();
        if(Instance != null) Destroy(Instance.gameObject);
        string[] maps = Saver.ListSavedMaps();
        foreach (var map in maps)
        {
            mapselect.options.Add(new Dropdown.OptionData(map));
        }
        _servers = new Dictionary<long, ServerConfig>();
        DontDestroyOnLoad(gameObject);
        createButton.onClick.AddListener(Host);
        discovery.OnGameFound += OnServerFound;
        Instance = this;
    }
    public void Discover(){
        Debug.Log("Started Discovery");
        foreach(var i in _servers.Keys){
            Destroy(_servers[i].gameObject);
        }
        _servers.Clear();
        discovery.StartDiscovery();
    }
    public void Join(ServerConfig server){
        network.StartClient(server.info.uri);
        discovery.StopDiscovery();
        //SceneManager.LoadScene("Lobby");
    }

    private void Host(){
        network.StartHost();
        discovery.mapName = mapselect.options[mapselect.value].text;
        discovery.maxPlayers = maxPlayersTextBox.Value;
        discovery.serverName = nameTextBox.Value;
        network.maxConnections = maxPlayersTextBox.Value;
        InvokeRepeating(nameof(UpdateServer), 0, 1);
        //SceneManager.LoadScene("Lobby");
        discovery.AdvertiseServer();
        SceneManager.sceneLoaded += (scene, loadSceneMode) => {if(scene.name == "Lobby") LobbyLoaded();};
    }

    private void LobbyLoaded(){
        NetworkServer.SpawnObjects();
    }
    void UpdateServer(){
        discovery.players = network.numPlayers;
    }

    private void OnServerFound(GameResponse response){
        if(!_servers.ContainsKey(response.serverId))
        {
            var server = Instantiate(serverInfoPrefab, uiJoinPanel).GetComponent<ServerConfig>();
            server.info = response;
            _servers[response.serverId] = server;
            server.Initialize();
        }
        else
        {
            _servers[response.serverId].info.Players = response.Players;
        }
    }
}
