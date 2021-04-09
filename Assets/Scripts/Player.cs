using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class Player : NetworkBehaviour
{
    public static Player LocalPlayer;
    public bool ready;
    [SyncVar] public bool isHost = false;
    [SyncVar] public string matchID = String.Empty;
    [SyncVar] public int playerIndex = -1;
    private NetworkMatchChecker _networkMatchChecker;
    public LobbyPlayer lobbyPlayer;
    private string _playerName;

    public string PlayerName
    {
        get => _playerName;
        set
        {
            _playerName = value;
            lobbyPlayer.DisplayName(value);
            if(hasAuthority) CmdSendPlayerData(_playerName, SkinIndex);
        }
    }
    
    public int SkinIndex
    {
        get => _skinIndex;
        set
        {
            _skinIndex = value; 
            lobbyPlayer.DisplaySkin(value);
            if(hasAuthority) CmdSendPlayerData(PlayerName, _skinIndex);
        }
    }
    
    [ClientRpc]
    public void RpcUpdateIndex(int newValue)
    {
        Debug.Log($"{name}: Index Updated - new index:{newValue}");
        if (lobbyPlayer != null && newValue != -1)
        {
            lobbyPlayer.AssignTransform(newValue);
        }
    }

    [Command]
    private void CmdSendPlayerData(string playerName, int skin)
    {
        RpcUpdatePlayerData(playerName, skin);
    }

    [ClientRpc]
    private void RpcUpdatePlayerData(string playerName, int skin)
    {
        if(lobbyPlayer == null) CreateLobbyPlayer();
        if (!hasAuthority)
        {
            PlayerName = playerName;
            SkinIndex = skin;            
        }
    }

    [Command]
    public void CmdSetReady()
    {
        if (!ready)
        {
            Matchmaker.Instance.PlayerReady(this);
        }
    }
    
    private int _skinIndex;
    public bool cardsReady;
    public bool nextPhaseReady;
    private string _mapName;

    public override void OnStartClient()
    {
        DontDestroyOnLoad(gameObject);
        if(SceneManager.GetActiveScene().name == "Lobby")
            CreateLobbyPlayer();
        else
            SceneManager.sceneLoaded += (scene, loadSceneMode) => {if(scene.name == "Lobby") CreateLobbyPlayer();};
        base.OnStartClient();
    }

    public override void OnStartLocalPlayer()
    {
        LocalPlayer = this;
        SceneManager.sceneLoaded += OnGameLoaded; 
        base.OnStartLocalPlayer();
    }

    private void Start()
    {
        _networkMatchChecker = GetComponent<NetworkMatchChecker>();
    }

    public override void OnStopClient()
    {
        ClientDisconnect();
        base.OnStopClient();
    }

    [Command]
    public void CmdDisconnect()
    {
        ServerDisconnect();
    }
    
    public override void OnStopServer()
    {
        ServerDisconnect();
        base.OnStopServer();
        
    }
    private void ServerDisconnect()
    {
        Matchmaker.Instance.PlayerDisconnected(this);
        _networkMatchChecker.matchId = string.Empty.ToGuid();
        RpcPlayerDisconnected();
        ClientDisconnect();
    }

    [ClientRpc]
    private void RpcPlayerDisconnected()
    {
        ClientDisconnect();
            
        if (isLocalPlayer)
        {
            SceneManager.LoadScene("MultiplayerMenu");
        }
        //Destroy(gameObject);
    }

    private void ClientDisconnect()
    {
        if (hasAuthority) SceneManager.sceneLoaded -= OnGameLoaded;
        if (lobbyPlayer != null)
        {
            Destroy(lobbyPlayer.gameObject);
            lobbyPlayer = null;
        }
    }

    public void HostGame(MatchSettings settings)
    {
        string matchId = Matchmaker.GetRandomMatchId();
        settings.mapData = Saver.Load(settings.map);
        CmdHostGame(matchId, settings);
    }

    [Command]
    void CmdHostGame(string id, MatchSettings settings)
    {
        if (Matchmaker.Instance.HostGame(id, gameObject, settings))
        {
            matchID = id;
            isHost = true;
            playerIndex = 0;
            _networkMatchChecker.matchId = id.ToGuid();
            TargetHostGame(true);
        }
        else
        {
            Debug.Log("Error");
            TargetHostGame(false);
        }
    }

    [TargetRpc]
    private void TargetHostGame(bool success)
    {
        MultiplayerController.Instance.FinishedHost(success);
    }
    
    public void JoinGame(string id)
    {
        CmdJoinGame(id);
    }

    private void CreateLobbyPlayer()
    {
        MultiplayerController.Instance.SpawnLobbyPlayer(this);
    }

    [Command]
    public void CmdGetAvailableMatches()
    {
        List<GameResponse> available = new List<GameResponse>();
        foreach (var match in Matchmaker.Instance.Matches)
        {
            if (!match.Started && match.Settings.advertise && match.Players.Count < match.Settings.maxPlayers)
            {
                available.Add(match.ToGameResponse());
            }
        }
        TargetOnMatchesReceived(available.ToArray());
    }

    [TargetRpc]
    private void TargetOnMatchesReceived(GameResponse[] matches)
    {
        MultiplayerController.Instance.ShowAvailableMatches(matches);
    }
    
    [Command]
    private void CmdJoinGame(string id)
    {
        if (Matchmaker.Instance.JoinGame(id, gameObject, out int index))
        {
            playerIndex = index;
            matchID = id;
            isHost = false;
            _networkMatchChecker.matchId = id.ToGuid();
            TargetJoinGame(true, index);
            return;
        }

        Debug.Log($"Game with ID {id} doesn't exist");
        TargetJoinGame(false, index);
    }

    [TargetRpc]
    private void TargetJoinGame(bool success, int index)
    {
        playerIndex = index;
        MultiplayerController.Instance.FinishedJoin(success);
    }
    
    [ClientRpc]
    public void RpcOnStartGame(Match match)
    {
        if (hasAuthority)
        {
            
            GameController.GameControllerInitialized += (e) =>
            {
                e.Match = match;
                e.startButton.onClick.AddListener(SetCardsReady);
                SceneManager.sceneLoaded -= OnGameLoaded;
            };    
        }
    }

    private void OnGameLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log(scene.name);
        if (scene.name != "Main") return;
        CmdGameLoaded();
    }

    [Command]
    private void CmdGameLoaded()
    {
        var match = Matchmaker.Instance.Matches.Find(e => e.MatchId == matchID);
        Debug.Log("player loaded");
        match.PlayersReady++;
    }

    private void SetCardsReady(){
        if(cardsReady) return;
        GameController.CardInfo[] commands = new GameController.CardInfo[GameController.instance.slots.Length];
        for (int i = 0; i < commands.Length; i++)
        {
            commands[i] = GameController.instance.slots[i].card.command.ToCardInfo();
        }
        cardsReady = true;
        CmdClientReady(playerIndex, commands);
    }
    [Command]
    void CmdClientReady(int clientIndex, GameController.CardInfo[] cards){
        GameController.GetInstance(matchID).ClientReady(clientIndex, cards);
    }

    [Command]
    public void CmdNextPhase()
    {
        if(nextPhaseReady) return;
        nextPhaseReady = true;
    }
}