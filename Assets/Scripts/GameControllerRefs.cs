using System;
using Cards;
using UnityEngine;
using UnityEngine.UI;

public class GameControllerRefs : MonoBehaviour
{
    public CardSlot[] slots;
    public GameObject topUiPanel;
    public Button startButton;
    public Map.Map Map;
    private static GameControllerRefs instance;

    private void Awake()
    {
        instance = this;
    }

    public static void assignValues(GameController g)
    {
        if(instance == null)
        {
            Debug.LogError("Instance null, GameController wont work");
            return;
        }
        g.slots = instance.slots;
        g.map = instance.Map;
        g.topUiPanel = instance.topUiPanel;
        g.startButton = instance.startButton;
    }

    public static void assignValues(CommandDragDrop c)
    {
        if(instance == null) return;
        c.slots = instance.slots;
    }
}