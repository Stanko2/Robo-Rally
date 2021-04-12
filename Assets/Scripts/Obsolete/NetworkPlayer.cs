using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class NetworkPlayer : NetworkBehaviour {
    public LobbyPlayer player;
    [FormerlySerializedAs("PlayerSlotPrefab")] public GameObject playerSlotPrefab;
    [FormerlySerializedAs("PlayerIndex")] public int playerIndex;
    static int _playersReady;
    private static string _mapName;
    [FormerlySerializedAs("CardsReady")] public bool cardsReady;
    [FormerlySerializedAs("NextPhaseReady")] public bool nextPhaseReady;
    public static List<NetworkIdentity> Players;
    public static NetworkPlayer LocalPlayer;
    private static List<NetworkIdentity> _playerSlots;
    public int skinIndex;
    public string playerName;
    
    public override void OnStartLocalPlayer()
    {
        LocalPlayer = this;
        if(isServer){
            Players = new List<NetworkIdentity>();
            _playerSlots = new List<NetworkIdentity>();
            _mapName = MultiplayerController.Instance.discovery.mapName;
            SceneManager.sceneLoaded += (scene, loadSceneMode) =>
            {
                if(scene.name == "Lobby") LobbyLoaded();
                if(scene.name == "Main" && GameController.SinglePlayer) GameController.instance.startButton.onClick.AddListener(SetCardsReady);
            };
            
        }
        else LobbyLoaded();
    }
    public override void OnStartClient(){
        DontDestroyOnLoad(gameObject);
    }

    private void LobbyLoaded(){
        CmdAddPlayer(netIdentity, !isServer);
    }

    [Command]
    public void CmdSetReady(){
        _playersReady++;
        if (_playersReady != NetworkManager.singleton.numPlayers) return;
        NetworkManager.singleton.ServerChangeScene("Main");
        SceneManager.sceneLoaded += (scene, loadSceneMode) => {
            NetworkServer.SpawnObjects();
        };
        RpcStartGame();
    }
    [ClientRpc]
    private void RpcStartGame()
    {
        SceneManager.LoadScene("Main");
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
        GameController.instance.ClientReady(clientIndex, cards);
    }

    [Command]
    public void CmdNextPhase()
    {
        if(nextPhaseReady) return;
        nextPhaseReady = true;
    }
    
    [Command]
    private void CmdAddPlayer(NetworkIdentity player, bool sendMap){
        NetworkPlayer p = player.GetComponent<NetworkPlayer>();
        p.playerIndex = NetworkManager.singleton.numPlayers - 1;
//        Debug.Log(p.playerIndex);
        // Transform slotTransform = GameObject.Find("PlayerSlots").transform.GetChild(p.PlayerIndex);
        var go = Instantiate(playerSlotPrefab);
        go.name = $"Player {p.playerIndex}";
        NetworkServer.Spawn(go, player.connectionToClient);
        //go.GetComponent<LobbyPlayer>().player = this;
        p.player = go.GetComponent<LobbyPlayer>();
        _playerSlots.Add(go.GetComponent<NetworkIdentity>());
        Players.Add(player);
        RpcShowPlayer(p.playerIndex, go.GetComponent<NetworkIdentity>());
        GetSkins(player);
        TargetAssignExistingTransforms(player.connectionToClient, _playerSlots.GetRange(0,_playerSlots.Count - 1).ToArray());
        if(sendMap) TargetOnMapReceive(player.connectionToClient, Saver.Load(_mapName));
    }

    private void GetSkins(NetworkIdentity p)
    {
        var skins = new int[_playerSlots.Count];
        var names = new string[_playerSlots.Count];
        for (var i = 0; i < _playerSlots.Count; i++)
        {
            var a = _playerSlots[i];
            var player = a.GetComponent<LobbyPlayer>();
            names[i] = player.playerName;
            skins[i] = player.skinIndex;
        }

        TargetAssignNamesAndSkins(p.connectionToClient, _playerSlots.ToArray(), names, skins);
    }

    [TargetRpc]
    void TargetAssignNamesAndSkins(NetworkConnection conn, NetworkIdentity[] p,string[] names, int[] skins)
    {
        for (var i = 0; i < p.Length; i++)
        {
            var lobby = p[i].GetComponent<LobbyPlayer>();
            lobby.playerName = names[i];
            lobby.skinIndex = skins[i];
        }
    }
    [TargetRpc]
    void TargetAssignExistingTransforms(NetworkConnection conn, NetworkIdentity[] players){
        for (int i = 0; i < players.Length; i++)
        {
            AssignTransform(i, players[i].gameObject);
        }
    }
    [TargetRpc]
    void TargetOnMapReceive(NetworkConnection conn, TileCollection mapData){
        _mapName = $"temp_{Random.Range(0,10000)}";
        Saver.Save(mapData, _mapName);
    }
    [ClientRpc]
    private void RpcShowPlayer(int playerIndex, NetworkIdentity identity)
    {
        if (isLocalPlayer){ 
            this.playerIndex = playerIndex;
            player = identity.GetComponent<LobbyPlayer>();
            //player.player = this;
            SceneManager.sceneLoaded += (scene,loadSceneMode) => {
                if(scene.name == "Main"){
                    Debug.Log("Scene Loaded");
                    // GameController.GameControllerInitialized += () => 
                    // {
                    //     //GameController.Instance.localPlayer = this;
                    //     
                    //     e.startButton.onClick.AddListener(SetCardsReady);
                    // };
                }
            };
        }
        AssignTransform(playerIndex, identity.gameObject);
    }

    private static void AssignTransform(int playerIndex, GameObject go)
    {
        var slotTransform = GameObject.Find("PlayerSlots").transform.GetChild(playerIndex);
        go.transform.parent = slotTransform;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
    }
}