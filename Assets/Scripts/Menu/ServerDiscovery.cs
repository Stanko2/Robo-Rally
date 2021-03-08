using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Mirror.Discovery;
using Mirror;
using System.Net;
using System;
using UnityEngine.Serialization;

public delegate void GameFound(GameResponse response);
public class ServerDiscovery : NetworkDiscoveryBase<GameRequest, GameResponse>
{
    public event GameFound OnGameFound;
    public Transport transport;
    private long ServerId{get; set;}
    [HideInInspector]
    public string serverName;
    [HideInInspector]
    public int maxPlayers;
    [HideInInspector]
    public int players;
    [HideInInspector]
    public string mapName;
    // Start is called before the first frame update
    public override void Start()
    {
        ServerId = RandomLong();

        if (transport == null)
            transport = Transport.activeTransport;

        base.Start();
    }
    protected override GameResponse ProcessRequest(GameRequest request, IPEndPoint endPoint){
        return new GameResponse(){
            serverId = ServerId,
            uri = transport.ServerUri(),
            ServerName = this.serverName,
            MaxPlayers = this.maxPlayers,
            Players = this.players,
            MapName = this.mapName
        };
    }
    protected override GameRequest GetRequest() => new GameRequest();
    protected override void ProcessResponse(GameResponse response, IPEndPoint endpoint){
        response.EndPoint = endpoint;
        var realUri = new UriBuilder(response.uri)
            {
                Host = response.EndPoint.Address.ToString()
            };
            response.uri = realUri.Uri;
            OnGameFound?.Invoke(response);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}

public class GameRequest : ServerRequest {}
public class GameResponse : ServerResponse {
    public string ServerName;
    public int MaxPlayers;
    public int Players;
    public string MapName;
}
