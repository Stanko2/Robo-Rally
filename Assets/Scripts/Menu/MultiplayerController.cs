using System;
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
    public Button joinButton;
    public TextBox nameTextBox;
    public TextBox maxPlayersTextBox;
    public Dropdown mapselect;
    private static NetworkManager network;
    public GameObject serverInfoPrefab;
    public Transform uiJoinPanel;
    public Toggle onlineMatch;
    public InputField JoinIDField;
    public GameObject lobbyPlayerPrefab;

    Dictionary<long, ServerConfig> _servers;
    public static MultiplayerController Instance;
    // Start is called before the first frame update
    private void Start()
    {
        joinButton.interactable = true;
        createButton.interactable = true;
        onlineMatch.interactable = !Menu.Local;
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
        SceneManager.LoadScene("Lobby");
    }

    private void Host()
    {
        createButton.interactable = false;
        joinButton.interactable = false;
        discovery.mapName = mapselect.options[mapselect.value].text;
        discovery.maxPlayers = maxPlayersTextBox.Value;
        discovery.serverName = nameTextBox.Value;
        if (onlineMatch.isOn)
        {
            HostOnlineMatch();
            return;
        }
        network.maxConnections = maxPlayersTextBox.Value;
        InvokeRepeating(nameof(UpdateServer), 0, 1);
        network.StartHost();
        discovery.AdvertiseServer();
    }
    
    void UpdateServer(){
        discovery.players = network.numPlayers;
    }

    public void RequestMatches()
    {
        for (int i = 0; i < uiJoinPanel.childCount; i++)
        {
            Destroy(uiJoinPanel.GetChild(i).gameObject);
        }
        Player.LocalPlayer.CmdGetAvailableMatches();
    }
    
    public void ShowAvailableMatches(IEnumerable<GameResponse> matches)
    {
        foreach (var match in matches)
        {
            var config = Instantiate(serverInfoPrefab, uiJoinPanel).GetComponent<ServerConfig>();
            config.info = match;
            config.Initialize();
        }
    }

    public void SpawnLobbyPlayer(Player player)
    {
        StartCoroutine(SpawnPlayer(player));
    }

    private IEnumerator SpawnPlayer(Player player)
    {
        yield return new WaitUntil(() => player.playerIndex != -1);
        var playerObject = Instantiate(lobbyPlayerPrefab);
        playerObject.name = $"Player {player.playerIndex}";
        player.lobbyPlayer = playerObject.GetComponent<LobbyPlayer>();
        player.lobbyPlayer.player = player;
        if (player.isLocalPlayer)
        {
            SceneManager.UnloadSceneAsync(1);
            var matchIdText = GameObject.Find("MatchIdText").GetComponent<Text>();
            matchIdText.text = $"Game code: {player.matchID}";
            player.lobbyPlayer.InitializeAuthority();
        }
        else
        {
            player.lobbyPlayer.Initialize();
        }
        player.lobbyPlayer.AssignTransform(player.playerIndex);
    }

    private void HostOnlineMatch()
    {
        Player.LocalPlayer.HostGame(new MatchSettings()
        {
            advertise = onlineMatch.isOn,
            map = mapselect.options[mapselect.value].text,
            maxPlayers = maxPlayersTextBox.Value,
            serverName = nameTextBox.Value
        });
        
    }
    
    public void JoinMatch()
    {
        joinButton.interactable = false;
        string id = JoinIDField.text.ToUpper();
        if (Matchmaker.ValidateID(id))
        {
            Player.LocalPlayer.JoinGame(id);
        }
        else
        {
            Debug.Log("Invalid matchId");
            joinButton.interactable = true;
        }
    }
    
    public void FinishedHost(bool success)
    {
        if (success)
        {
            SceneManager.LoadScene("Lobby", LoadSceneMode.Additive);
            Debug.Log("Hosted game");
        }
        else
        {
            joinButton.interactable = true;
            createButton.interactable = true;
        }
    }

    public void FinishedJoin(bool success)
    {
        if (success)
        {
            Debug.Log("Joined Game");
            SceneManager.LoadSceneAsync("Lobby", LoadSceneMode.Additive);
        }
        else
            Debug.Log("Match doesn't exist");

        joinButton.interactable = true;
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
