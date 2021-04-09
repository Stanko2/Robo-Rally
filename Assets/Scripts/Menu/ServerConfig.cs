using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ServerConfig : MonoBehaviour
{
    public GameResponse info;
    [FormerlySerializedAs("JoinButton")] public Button joinButton;
    [FormerlySerializedAs("NameText")] public Text nameText;
    [FormerlySerializedAs("PlayersText")] public Text playersText;
    [FormerlySerializedAs("MapText")] public Text mapText;
    MultiplayerController _controller;
    // Start is called before the first frame update
    public void Initialize()
    {
        _controller = MultiplayerController.Instance;
        mapText.text = info.MapName;
        nameText.text = info.ServerName;
        joinButton.onClick.AddListener(Join);
    }
    void Join()
    {
        _controller.JoinIDField.text = info.matchId;
        _controller.JoinMatch();
    }

    // Update is called once per frame
    void Update()
    {
        playersText.text = info.Players + " / " + info.MaxPlayers;
        joinButton.interactable = info.Players < info.MaxPlayers;
    }
}
