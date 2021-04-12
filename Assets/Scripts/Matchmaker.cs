using System;
using System.Collections;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;


public struct MatchSettings
{
    public string map;
    public string serverName;
    public int maxPlayers;
    public bool advertise;
    public TileCollection mapData;
}

[Serializable]
public class Match
{
    public string MatchId;
    public MatchSettings Settings;
    public int PlayersReady;
    public bool Started;
    public SyncListGameObject Players = new SyncListGameObject();

    public Match(string id, GameObject Player, MatchSettings settings)
    {
        MatchId = id;
        Players.Add(Player);
        this.Settings = settings;
    }

    public GameResponse ToGameResponse()
    {
        return new GameResponse
        {
            matchId = MatchId,
            Players = Players.Count,
            MaxPlayers = Settings.maxPlayers,
            MapName = Settings.map,
            ServerName = Settings.serverName,
            uri = new Uri("about:blank"),
            serverId = Random.Range(Int32.MinValue, Int32.MaxValue),
        };
    }
    
    public override string ToString()
    {
        return $"Match {MatchId}: {Players.Count} players started: {Started}";
    }

    public Match() { }
}
[Serializable]    
public class SyncListGameObject : SyncList<GameObject>{ }
[Serializable]    
public class SyncListMatch : SyncList<Match>{ }

public class Matchmaker : NetworkBehaviour
{
    public static Matchmaker Instance;
    public SyncListMatch Matches = new SyncListMatch();
    public SyncListString Ids = new SyncListString();
    private const int MATCH_ID_LENGTH = 5;
    public GameObject gameControllerPrefab;
    
    private void Awake()
    {
        if(Instance != null) Destroy(gameObject);
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public bool HostGame(string id, GameObject hostPlayer, MatchSettings settings)
    {
        if (isClient && Matches.Count > 0) return false;
        if (!Ids.Contains(id))
        {
            Matches.Add(new Match(id, hostPlayer, settings));
            Ids.Add(id);
            Debug.Log($"Game Hosted: Current games {Matches.Count}");
            return true;
        }
        else
        {
            Debug.Log("Match Id already exist");
            return false;
        }
    }

    public bool JoinGame(string id, GameObject player, out int playerIndex)
    {
        if (Ids.Contains(id))
        {
            var match = Matches.Find(m => m.MatchId == id);
            match.Players.Add(player);
            playerIndex = match.Players.Count-1;
            return true;
        }

        playerIndex = -1;
        return false;
    }

    public void PlayerDisconnected(Player player)
    {
        if(player.matchID == String.Empty) return;
        var index = Matches.FindIndex(m => m.MatchId == player.matchID);
        if (player.ready) Matches[index].PlayersReady--;
        Debug.Log($"{Matches[0].MatchId}: {player.matchID}");
        if (Matches[index].Players.Count > 1)
        {
            Matches[index].Players.RemoveAt(player.playerIndex);
            for (var i = 0; i < Matches[index].Players.Count; i++)
            {
                var player1 = Matches[index].Players[i].GetComponent<Player>();
                player1.playerIndex = i;
                player1.RpcUpdateIndex(i);
            }

            Matches[index].Players[0].GetComponent<Player>().isHost = true;
            Debug.Log($"Player Disconnected: {Matches[index].Players.Count} players remaining in game");
        }
        else
        {
            Matches.RemoveAt(index);
            Ids.Remove(player.matchID);
            Debug.Log("No more players in Match. Terminating ...");
        }
        player.matchID = String.Empty;
        player.playerIndex = -1;
    }

    public static string GetRandomMatchId()
    {
        string id = String.Empty;
        for (int i = 0; i < MATCH_ID_LENGTH; i++)
        {
            id += (char)Random.Range(65, 65 + 26);
        }

        Debug.Log($"Id generated: {id}");
        return id;
    }

    public static bool ValidateID(string id)
    {
        return id.Length == MATCH_ID_LENGTH && id.All(i => i >= 65 && i <= 65 + 26);
    }

    public void PlayerReady(Player player)
    {
        var match = Matches.Find(e => e.MatchId == player.matchID);
        match.PlayersReady++;
        if (match.PlayersReady == match.Players.Count)
        {
            StartCoroutine(StartGame(match));
        }
    }

    private IEnumerator StartGame(Match match)
    {
        match.Started = true;
        match.PlayersReady = 0;
        foreach (var player in match.Players)
        {
            player.GetComponent<Player>().RpcOnStartGame(match);
        }

        if(!isClient) SceneManager.LoadScene("Main", LoadSceneMode.Additive);
        
        yield return new WaitUntil(() => match.PlayersReady == match.Players.Count);
        Debug.Log($"{match.Players.Count} clients loaded, spawning gameController ... ");
        
        GameObject go = Instantiate(gameControllerPrefab);
        go.GetComponent<NetworkMatchChecker>().matchId = match.MatchId.ToGuid();
        go.GetComponent<GameController>().GameControllerInitialized +=() =>
        {
            go.GetComponent<GameController>().Match = match;
        }; 
        NetworkServer.Spawn(go);
        
    }
}

public static class MatchExtensions
{
    public static Guid ToGuid(this string id)
    {
        MD5CryptoServiceProvider m = new MD5CryptoServiceProvider();
        byte[] inputbytes = Encoding.Default.GetBytes(id);
        byte[] hashBytes = m.ComputeHash(inputbytes);
        
        return new Guid(hashBytes);
    }
}