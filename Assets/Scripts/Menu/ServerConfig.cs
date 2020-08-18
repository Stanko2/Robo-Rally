﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class ServerConfig : MonoBehaviour
{
    public GameResponse info;
    [FormerlySerializedAs("JoinButton")] public Button joinButton;
    [FormerlySerializedAs("NameText")] public Text nameText;
    [FormerlySerializedAs("PlayersText")] public Text playersText;
    [FormerlySerializedAs("MapText")] public Text mapText;
    [FormerlySerializedAs("SelectedColor")] public Color selectedColor;
    [FormerlySerializedAs("DeselectedColor")] public Color deselectedColor;
    MultiplayerController _controller;
    // Start is called before the first frame update
    public void Initialize()
    {
        _controller = MultiplayerController.Instance;
        mapText.text = info.MapName;
        nameText.text = info.ServerName;
        joinButton.onClick.AddListener(Join);
    }
    void Join(){
        _controller.Join(this);
    }

    // Update is called once per frame
    void Update()
    {
        playersText.text = info.Players.ToString() + " / " + info.MaxPlayers.ToString();
        joinButton.interactable = info.Players < info.MaxPlayers;
    }
}