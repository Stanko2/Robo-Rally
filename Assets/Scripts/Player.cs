using System;
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
            GameController.GameControllerInitialized += () =>
            {
                GameController.Instance.Match = match;
                GameController.Instance.startButton.onClick.AddListener(SetCardsReady);
            };    
        }
    }

    private void SetCardsReady(){
        if(cardsReady) return;
        GameController.CardInfo[] commands = new GameController.CardInfo[GameController.Instance.slots.Length];
        for (int i = 0; i < commands.Length; i++)
        {
            commands[i] = GameController.Instance.slots[i].card.command.ToCardInfo();
        }
        cardsReady = true;
        CmdClientReady(playerIndex, commands);
    }
    [Command]
    void CmdClientReady(int clientIndex, GameController.CardInfo[] cards){
        GameController.Instance.ClientReady(clientIndex, cards);
    }

    [Command]
    public void CmdNextPhase()
    {
        if(nextPhaseReady) return;
        nextPhaseReady = true;
    }
}