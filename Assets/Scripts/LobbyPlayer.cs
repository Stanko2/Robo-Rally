using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class LobbyPlayer : NetworkBehaviour
{
    [FormerlySerializedAs("NameText")] public TextMeshProUGUI nameText;
    [FormerlySerializedAs("NameField")] public TMP_InputField nameField;
    [FormerlySerializedAs("CustomizeUI")] public GameObject customizeUi;
    public NetworkPlayer player;
    [FormerlySerializedAs("Canvas")] public Canvas canvas;
    public string playerName;
    [FormerlySerializedAs("Next")] public Button next;
    [FormerlySerializedAs("Prev")] public Button prev;
    public int skinIndex;
    Button _startButton;
    Button _exitButton;
    bool _ready = false;
    public override void OnStartClient(){
        canvas.worldCamera = Camera.main;
        customizeUi.SetActive(hasAuthority);
        nameText.gameObject.SetActive(!hasAuthority);
    }
    public override void OnStartAuthority(){
        customizeUi.SetActive(true);
        nameField.onEndEdit.AddListener((name) => CmdSendName(name, GetComponent<NetworkIdentity>()));
        _startButton = GameObject.Find("StartButton").GetComponent<Button>();
        _exitButton = GameObject.Find("ExitButton").GetComponent<Button>();
        _startButton.onClick.AddListener(Ready);
        _exitButton.onClick.AddListener(Leave);
        next.onClick.AddListener(() => CmdChangeSkin(skinIndex++,netIdentity));
        prev.onClick.AddListener(() => CmdChangeSkin(skinIndex--,netIdentity));
    }
    private void Update() {
        if(_startButton) _startButton.interactable = nameField.text.Length > 0 && !_ready;
    }
    void Leave(){
        GameObject go = NetworkManager.singleton.gameObject;
        NetworkManager.Shutdown();
        
        Destroy(go);
        Destroy(MultiplayerController.Instance.gameObject);
        SceneManager.LoadScene("Menu");
    }

    private void Ready(){
        _startButton.interactable = false;
        nameField.interactable = false;
        if(!_ready) player.CmdSetReady();
        _ready = true;
    }

    [Command]
    private void CmdSendName(string name, NetworkIdentity identity){
        RpcDisplayName(name, identity);
        playerName = name;
        player.playerName = playerName;
    }
    [ClientRpc]
    private void RpcDisplayName(string name, NetworkIdentity identity){
        identity.GetComponent<LobbyPlayer>().nameText.text = name;
        Debug.Log(name);
        playerName = name;
    }
    [Command]
    private void CmdChangeSkin(int skinIndex, NetworkIdentity identity)
    {
        RpcDisplaySkin(skinIndex, identity);
        player.skinIndex = skinIndex;
    }
    [ClientRpc]
    private void RpcDisplaySkin(int index, NetworkIdentity identity)
    {
        var skin = identity.GetComponent<RobotSkin>();
        skin.SetSkin(Robot.Mod(index, skin.skins.Length));
    }    
}
