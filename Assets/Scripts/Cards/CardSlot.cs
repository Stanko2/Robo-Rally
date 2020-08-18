using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Cards
{
    public class CardSlot : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler {
        public Card card;
        public static CommandDragDrop DragDrop;
        [FormerlySerializedAs("HighlightColor")] public Color highlightColor;
        public bool mouseOver;
        public void OnPointerDown(PointerEventData data){
            if(card != null && DragDrop.active){
                DragDrop.Drag(card);
                card = null;
            }
            Debug.Log("drag");
        }
        public void OnPointerEnter(PointerEventData data){
            if(DragDrop.isDragging) GetComponent<Image>().color = highlightColor;
            mouseOver = true;
        }
        public void OnPointerExit(PointerEventData data){
            GetComponent<Image>().color = Color.white;
            mouseOver = false;
        }
    }
}