﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cards;
using Map;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Serialization;

public delegate void Init();
public class GameController : NetworkBehaviour
{

    public static event Init GameControllerInitialized;
    [FormerlySerializedAs("LocalPlayer")] public NetworkPlayer localPlayer;
    public CardSlot[] slots;
    public Command[] commandTemplates;
    public Robot[] robots;
    [FormerlySerializedAs("RobotPrefab")] public GameObject robotPrefab;
    [FormerlySerializedAs("CardPrefab")] public GameObject cardPrefab;
    [FormerlySerializedAs("TopUIPanel")] public GameObject topUiPanel;
    [FormerlySerializedAs("StartButton")] public Button startButton;
    [FormerlySerializedAs("ExecutionHighlight")] public Color executionHighlight;
    private List<Card> _cards;
    private CameraMover _mover;
    [FormerlySerializedAs("MapName")] public string mapName;
    public Map.Map map;
    public static GameController Instance;
    [FormerlySerializedAs("PlayersReady")] public int playersReady;
    [FormerlySerializedAs("CardStack")] public Stack<Command> cardStack;
    public static bool SinglePlayer = false;
    //TODO: Implement winning and restarting
    private void OnEnable() {
        // if(instance != null){
        //     Destroy(instance.gameObject);
        // }
        Instance = this;
        cardStack = new Stack<Command>();
        CardSlot.DragDrop = GetComponent<CommandDragDrop>();
        GameControllerInitialized?.Invoke();
    }
    public override void OnStartClient(){
        map.name = mapName;
        map.InitMap();
        if(!isServer) {
            robots = (Robot[])FindObjectsOfType(typeof(Robot));
            System.Array.Sort(robots, (a, b) => a.owningPlayerIndex.CompareTo(b.owningPlayerIndex));
        } 
        _mover = Camera.main.GetComponent<CameraMover>();
        _mover.localPlayer = robots[localPlayer.playerIndex].transform;
        foreach (var robot in robots)
        {   
            robot.transform.position = 2*new Vector3(map.startLocation.x,0,map.startLocation.y);
            robot.pos = map.startLocation;
            robot.map = map;   
            robot.Init();
        }
        localPlayer.CmdNextPhase();
    }
    // Start is called before the first frame update
    public override void OnStartServer()
    {
        robots = new Robot[NetworkManager.singleton.numPlayers];
        Command.Random = new System.Random(Random.Range(0,int.MaxValue));
        if (SinglePlayer)
        {
            localPlayer = (NetworkPlayer) FindObjectOfType(typeof(NetworkPlayer));
            NetworkPlayer.Players.Add(localPlayer.netIdentity);
        }
        for (int i = 0; i < NetworkManager.singleton.numPlayers; i++)
        {
            GameObject go = Instantiate(robotPrefab, 2*new Vector3(map.startLocation.x, 0, map.startLocation.y), Quaternion.identity);
            robots[i] = go.GetComponent<Robot>();
            if (!SinglePlayer)
            {
                NetworkServer.Spawn(go, NetworkPlayer.Players[i].connectionToClient);
                NetworkPlayer player = NetworkPlayer.Players[i].GetComponent<NetworkPlayer>();
                robots[i].owningPlayerIndex = i;
                robots[i].RobotName = player.playerName;
                robots[i].skinIndex = player.skinIndex;
            }
            else
            {
                NetworkServer.Spawn(go, localPlayer.connectionToClient);    
            }
        }
        StartCoroutine(AssignCards());        
    }

    public Robot GetRobotAtPosition(Vector2 pos)
    {
        return robots.FirstOrDefault(robot => robot.pos == pos);
    }
#region CardAssignment
    private IEnumerator AssignCards()
    {
        
        if (!SinglePlayer) yield return new WaitUntil(() => AllPlayersReady);
        else yield return null;
        
        if(cardStack.Count < NetworkPlayer.Players.Count * 9) ShuffleCards();
        for (int i = 0; i < NetworkPlayer.Players.Count; i++)
        {
            List<CardInfo> c = new List<CardInfo>();
            for (int j = 0; j < robots[i].GetComponent<RobotHealth>().Health + 4; j++)
            {
                c.Add(cardStack.Pop().ToCardInfo());
            }

            if (!SinglePlayer) TargetGetCards(NetworkPlayer.Players[i].connectionToClient, c.ToArray());
            else StartCoroutine(GetCards(c.Select(Command.FromCardInfo).ToArray()));
        }
    }
    private void ShuffleCards(){
        var cards = new List<Command>();
        foreach (var c in commandTemplates)
        {
            for (int i = 0; i < c.count; i++)
            {
                var command = Instantiate(c);
                command.GetWeight();
                cards.Add(command);
            }
        }
        while(cards.Count > 0){
            var index = Random.Range(0,cards.Count);
            cardStack.Push(cards[index]);
            cards.RemoveAt(index);
        }
    }
    public struct CardInfo{
        public int Weight;
        public CommandType Type;
    }

    [TargetRpc]
    void TargetGetCards(NetworkConnection conn, CardInfo[] cards)
    {
        var c = new Command[cards.Length];
        for (int i = 0; i < cards.Length; i++)
        {
            c[i] = Command.FromCardInfo(cards[i]);
        }
        StartCoroutine(GetCards(c));
        localPlayer.nextPhaseReady = false;

    }

#endregion
    public void ClientReady(int clientIndex, CardInfo[] c){
        playersReady ++;
        foreach (var command in c)
        {
            robots[clientIndex].commands.Enqueue(Command.FromCardInfo(command));
        }
        RpcAssignCommands(clientIndex, c);
        if(playersReady == NetworkManager.singleton.numPlayers) StartTurn();
    }
    [ClientRpc]
    void RpcAssignCommands(int robotIndex, CardInfo[] c){
        foreach (var command in c)
        {
            robots[robotIndex].commands.Enqueue(Command.FromCardInfo(command));
        }
    }
    
    private IEnumerator GetCards(Command[] commands){
        CardSlot.DragDrop.active = true;
        _cards = new List<Card>();
        for (var i = 0; i < commands.Length; i++)
        {
            var card = Instantiate(cardPrefab, topUiPanel.transform).GetComponent<Card>();
            card.command = commands[i];
            card.startPos = new Vector3(50 + 100*i, -70, 0);
            card.dragDrop = CardSlot.DragDrop;
            _cards.Add(card);
            yield return new WaitForSeconds(.2f);
        }
        //GetComponent<CommandDragDrop>().cards = _cards;
    }

    private void StartTurn(){
        StartCoroutine(NextTurn());
    }
    [ClientRpc]
    private void RpcNextTurn(int turn)
    {
        localPlayer.nextPhaseReady = false;
        Robot[] sorted = new Robot[robots.Length];
        System.Array.Copy(robots, sorted, robots.Length); 
        System.Array.Sort(sorted, (a, b) => b.commands.Peek().weight.CompareTo(a.commands.Peek().weight));
        slots[turn].card.Color = executionHighlight;
        if (turn > 0) slots[turn - 1].card.Color = Color.white;
        StartCoroutine(MoveRobots(sorted));
    }

    private IEnumerator MoveRobots(Robot[] sorted)
    {
        localPlayer.nextPhaseReady = false;
        foreach (var robot in sorted)
        {
            robot.StartCoroutine(robot.StartNext());
            yield return new WaitUntil(() => robot.idle);
        }
        localPlayer.CmdNextPhase();
    }

    [ClientRpc]
    void RpcUpdateMap()
    {
        localPlayer.nextPhaseReady = false;
        for (var i = 0; i < map.width; i++)
        {
            for (var j = 0; j < map.height; j++)
            {
                if (map.tiles[i, j] is ConveyorBelt)
                {
                    var cb = (ConveyorBelt) map.tiles[i, j];
                    cb.StartCoroutine(cb.Move());
                }
            }
        }

        foreach (var robot in robots)
        {
            robot.Health.Shoot();
            robot.UpdateInvincible();
        }
    }
    [ClientRpc]
    private void RpcEndTurn(){
        slots[4].card.Color = Color.white;
        foreach (var t in _cards)
        {
            Destroy(t.gameObject);
        }
        foreach (var slot in slots)
        {
            slot.card = null;
        }
        foreach (var robot in robots)
        {
            robot.commands = new Queue<Command>();
            if(robot.IsDead) robot.Respawn();
        }
        localPlayer.cardsReady = false;
        _mover.followTarget = false;
        localPlayer.CmdNextPhase();
    }

    [ClientRpc]
    private void RpcStartTurn()
    {
        _mover.followTarget = true;
        CardSlot.DragDrop.active = false;
    }

    private IEnumerator NextTurn(){
        RpcStartTurn();
        for (int turn = 0; turn < 5; turn++)
        {
            RpcNextTurn(turn);
            yield return new WaitUntil(() => AllPlayersReady);
            RpcUpdateMap();
            //yield return new WaitUntil(() => NextPhaseReady == NetworkManager.singleton.numPlayers);
            yield return new WaitForSeconds(2f);
        }
        playersReady = 0;
        RpcEndTurn();
        StartCoroutine(AssignCards());
    }

    private static bool AllPlayersReady
    {
        get
        {
            return NetworkPlayer.Players.All(player => player.GetComponent<NetworkPlayer>().nextPhaseReady);
        }
    }

    private bool AllSlotsFull{
        get
        {
            return slots.All(slot => slot.card);
        }
    }
    // Update is called once per frame
    private void Update()
    {
        startButton.interactable = AllSlotsFull && !localPlayer.cardsReady;
    }
}