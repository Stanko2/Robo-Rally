using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class LobbyPlayer : MonoBehaviour
{
    [FormerlySerializedAs("NameText")] public TextMeshProUGUI nameText;
    [FormerlySerializedAs("NameField")] public TMP_InputField nameField;
    [FormerlySerializedAs("CustomizeUI")] public GameObject customizeUi;
    public Player player;
    [FormerlySerializedAs("Canvas")] public Canvas canvas;
    public string playerName;
    [FormerlySerializedAs("Next")] public Button next;
    [FormerlySerializedAs("Prev")] public Button prev;
    public int skinIndex;
    private Button _startButton;
    private Button _exitButton;
    private bool _ready = false;
    
    public void Initialize(){
        canvas.worldCamera = Camera.main;
        customizeUi.SetActive(false);
        nameText.gameObject.SetActive(true);
    }
    public void InitializeAuthority(){
        customizeUi.SetActive(true);
        nameText.gameObject.SetActive(false);
        nameField.onEndEdit.AddListener(name => SendName(name));
        _startButton = GameObject.Find("StartButton").GetComponent<Button>();
        _exitButton = GameObject.Find("ExitButton").GetComponent<Button>();
        
        _startButton.onClick.AddListener(Ready);
        _exitButton.onClick.AddListener(Leave);
        next.onClick.AddListener(() => ChangeSkin(skinIndex++));
        prev.onClick.AddListener(() => ChangeSkin(skinIndex--));
    }
    private void Update() {
        if(_startButton) _startButton.interactable = nameField.text.Length > 0 && !_ready;
    }
    
    public void AssignTransform(int index)
    {
        var slotTransform = GameObject.Find("PlayerSlots").transform.GetChild(index);
        transform.parent = slotTransform;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    private void Leave(){
        player.CmdDisconnect();
        _exitButton.interactable = false;
    }

    private void Ready(){
        _startButton.interactable = false;
        nameField.interactable = false;
        player.CmdSetReady();
    }
    
    private void SendName(string name)
    {
        player.PlayerName = name;
    }
    public void DisplayName(string name){
        nameText.text = name;
    }
    private void ChangeSkin(int skinIndex)
    {
        player.SkinIndex = skinIndex;
    }
    public void DisplaySkin(int index)
    {
        var skin = GetComponent<RobotSkin>();
        skin.SetSkin(Robot.Mod(index, skin.skins.Length));
    }    
}
