using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cards;
using Map;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Serialization;
using UnityEngine.SocialPlatforms.Impl;
using Random = UnityEngine.Random;

public delegate void Init();
public delegate void GameControllerInit();
public class GameController : NetworkBehaviour
{
    public Player localPlayer => Player.LocalPlayer;
    public event GameControllerInit GameControllerInitialized;
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
    [NonSerialized] public Match Match;
    public Map.Map map;

    public static GameController instance { get; private set; }
    private static Dictionary<string, GameController> instances;

    [Server]
    public static GameController GetInstance(string MatchId) => instances[MatchId];
    
    [FormerlySerializedAs("PlayersReady")] public int playersReady;
    [FormerlySerializedAs("CardStack")] public Stack<Command> cardStack;
    public static bool SinglePlayer = false;
    //TODO: Implement winning and restarting
    private void OnEnable() {
        GameControllerRefs.assignValues(this);

        instance = this;
        cardStack = new Stack<Command>();
        CardSlot.DragDrop = GetComponent<CommandDragDrop>();

    }

    public override void OnStartClient()
    {
        if(!isServer)
        {
            instance = this;
            localPlayer.InitializeGameController();
        }
        if (Camera.main != null) _mover = Camera.main.GetComponent<CameraMover>();
        
    }

    public void InitializeMatch(Match match)
    {
        Match = match;
        map.InitMap(match.Settings.mapData);
    }

    [ClientRpc]
    private void RpcInitRobots()
    {
        robots = (Robot[])FindObjectsOfType(typeof(Robot));
        Array.Sort(robots, (a, b) => a.owningPlayerIndex.CompareTo(b.owningPlayerIndex));
        _mover.localPlayer = robots[localPlayer.playerIndex].transform;
        foreach (var robot in robots)
        {
            robot.map = map;   
            robot.Init();
            robot.OnCheckpointArrive(map.start, false);
        }
        localPlayer.CmdNextPhase();
    }
    
    // Start is called before the first frame update
    public override void OnStartServer()
    {
        if(instances == null) instances = new Dictionary<string, GameController>();
        if (Match == null)
        {
            GameControllerInitialized?.Invoke();
            Debug.Log("Match was set to: " + Match);
        }
        instances[Match.MatchId] = this;
        map.InitMap(Match.Settings.mapData);
        if(isClient) Match = Matchmaker.Instance.Matches.Find(e => e.MatchId == Match.MatchId);
        var Players = SinglePlayer ? new SyncListGameObject() : Match.Players;
        if (SinglePlayer)
        {
            Match = new Match();
            Players.Add(localPlayer.gameObject);
        }
        robots = new Robot[Players.Count];
        Command.Random = new System.Random(Random.Range(0,int.MaxValue));
        for (int i = 0; i < Players.Count; i++)
        {
            GameObject go = Instantiate(robotPrefab, 2*new Vector3(map.start.coords.x, 0, map.start.coords.y), Quaternion.identity);
            robots[i] = go.GetComponent<Robot>();
            go.GetComponent<NetworkMatchChecker>().matchId = Match.MatchId.ToGuid();
            if (!SinglePlayer)
            {
                NetworkServer.Spawn(go, Players[i].GetComponent<NetworkIdentity>().connectionToClient);
                Player player = Players[i].GetComponent<Player>();
                robots[i].owningPlayerIndex = i;
                robots[i].RobotName = player.PlayerName;
                robots[i].skinIndex = player.SkinIndex;
            }
            else
            {
                NetworkServer.Spawn(go, localPlayer.connectionToClient);    
            }
        }
        RpcInitRobots();
        StartCoroutine(AssignCards());        
    }

    public Robot GetRobotAtPosition(Vector2 pos)
    {
        return robots.FirstOrDefault(robot => robot.pos == pos);
    }
#region CardAssignment
    [Server]
    private IEnumerator AssignCards()
    {
        if (!SinglePlayer) yield return new WaitUntil(() => AllPlayersReady(Match.MatchId));
        else yield return null;
        
        if(cardStack.Count < Match.Players.Count * 9) ShuffleCards();
        for (int i = 0; i < Match.Players.Count; i++)
        {
            List<CardInfo> c = new List<CardInfo>();
            for (int j = 0; j < robots[i].GetComponent<RobotHealth>().Health + 4; j++)
            {
                c.Add(cardStack.Pop().ToCardInfo());
            }

            if (!SinglePlayer) TargetGetCards(Match.Players[i].GetComponent<NetworkIdentity>().connectionToClient, c.ToArray());
            else StartCoroutine(GetCards(c.Select(Command.FromCardInfo).ToArray()));
        }
    }
    [Server]
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
    [Server]
    public void ClientReady(int clientIndex, CardInfo[] c){
        playersReady ++;
        foreach (var command in c)
        {
            robots[clientIndex].commands.Enqueue(Command.FromCardInfo(command));
        }
        RpcAssignCommands(clientIndex, c);
        if(playersReady == Match.Players.Count) StartTurn();
    }
    [ClientRpc]
    void RpcAssignCommands(int robotIndex, CardInfo[] c){
        foreach (var command in c)
        {
            robots[robotIndex].commands.Enqueue(Command.FromCardInfo(command));
        }
    }
    
    [Client]
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
        List<Robot> activeRobots = robots.ToList().FindAll(e => !e.IsDead);
        Robot[] sorted = new Robot[activeRobots.Count];
        Array.Copy(activeRobots.ToArray(), sorted, activeRobots.Count); 
        Array.Sort(sorted, (a, b) => b.commands.Peek().weight.CompareTo(a.commands.Peek().weight));
        slots[turn].card.Color = executionHighlight;
        if (turn > 0) slots[turn - 1].card.Color = Color.white;
        StartCoroutine(MoveRobots(sorted));
    }

    [Client]
    private IEnumerator MoveRobots(IEnumerable<Robot> sorted)
    {
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
        StartCoroutine(MapUpdate());
    }
    [Client]
    private IEnumerator MapUpdate()
    {
        if (robots.Any(e => map[e.pos] is ConveyorBelt))
        {
            MoveConveyorBelts();
            yield return new WaitForSeconds(1f);
        }

        foreach (var robot in robots)
        {
            robot.Health.Shoot();
            robot.UpdateInvincible();
        }
        localPlayer.CmdNextPhase();
    }

    [Client]
    private void MoveConveyorBelts()
    {
        for (var i = 0; i < map.width; i++)
        {
            for (var j = 0; j < map.height; j++)
            {
                if (!(map.tiles[i, j] is ConveyorBelt)) continue;
                var cb = (ConveyorBelt) map.tiles[i, j];
                cb.StartCoroutine(cb.Move());
            }
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

    [Server]
    private IEnumerator NextTurn(){
        RpcStartTurn();
        for (int turn = 0; turn < 5; turn++)
        {
            SetAllPlayersUnready();
            yield return new WaitUntil(() => AllPlayersUnready(Match.MatchId));
            RpcNextTurn(turn);
            Debug.Log($"Turn {turn}: robots moved");
            yield return new WaitUntil(() => AllPlayersReady(Match.MatchId));
            SetAllPlayersUnready();
            yield return new WaitUntil(() => AllPlayersUnready(Match.MatchId));
            RpcUpdateMap();
            yield return new WaitForSeconds(CalculateWaitTime());
            Debug.Log($"Turn {turn}: Map updated");
            yield return new WaitUntil(() => AllPlayersReady(Match.MatchId));
        }
        playersReady = 0;
        RpcEndTurn();
        StartCoroutine(AssignCards());
    }

    private float CalculateWaitTime()
    {
        float waitTime = .2f;
        if (robots.Any(robot => map[robot.pos] is ConveyorBelt))
        {
            waitTime += 1;
        }
        
        return waitTime;
    }
    
    private void SetAllPlayersUnready()
    {
        foreach (var player in Match.Players)
        {
            player.GetComponent<Player>().nextPhaseReady = false;
        }
    }

    private static bool AllPlayersReady(string matchID)
    {
        return GetInstance(matchID).Match.Players.All(e=>e.GetComponent<Player>().nextPhaseReady);
    }

    private static bool AllPlayersUnready(string matchID)
    {
        return GetInstance(matchID).Match.Players.All(e=>!e.GetComponent<Player>().nextPhaseReady);
    }
    
    private bool AllSlotsFull{
        get
        {
            return slots.All(slot => slot.card);
        }
    }
    
    public void PlayerDisconnect(Robot robot)
    {
        List<Robot> r = robots.ToList();
        r.Remove(robot);
        robots = r.ToArray();
    }

    // Update is called once per frame
    private void Update()
    {
        if (Application.isBatchMode) return;
        startButton.interactable = AllSlotsFull && !localPlayer.cardsReady;
        List<Vector2> activeCanvasPositions = new List<Vector2>();
        var localRobot = robots.First(e => e.hasAuthority);
        activeCanvasPositions.Add(localRobot.pos);
        localRobot.canvas.gameObject.SetActive(true);
        foreach (var robot in robots)
        {
            if (activeCanvasPositions.Contains(robot.pos)) continue;
            robot.canvas.gameObject.SetActive(true);
            activeCanvasPositions.Add(robot.pos);
        }
    }
}
