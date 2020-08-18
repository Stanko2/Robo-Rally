using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class TabUI : MonoBehaviour
{
    [FormerlySerializedAs("TabButtons")] public Button[] tabButtons;
    [FormerlySerializedAs("Tabs")] public GameObject[] tabs;
    [FormerlySerializedAs("SelectedColor")] public Color selectedColor;
    [FormerlySerializedAs("DeselectedColor")] public Color deselectedColor;

    // Start is called before the first frame update
    void Start()
    {
        Switch(0);
       /* for (int i = 0; i < TabButtons.Length; i++)
        {
            TabButtons[i].onClick.AddListener(() => Switch(i));
        }*/
    }
    public void Switch(int tabIndex){
        foreach (var tab in tabs)
        {
            tab.SetActive(false);
        }
        foreach (var button in tabButtons)
        {
            button.GetComponent<Image>().color = deselectedColor;
            button.GetComponentInChildren<Text>().color = Color.white;
        }
        tabs[tabIndex].SetActive(true);
        tabButtons[tabIndex].GetComponent<Image>().color = selectedColor;
        tabButtons[tabIndex].GetComponentInChildren<Text>().color = Color.black;
    }
}
