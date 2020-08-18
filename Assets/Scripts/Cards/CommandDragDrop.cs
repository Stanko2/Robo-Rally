using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Cards
{
    public class CommandDragDrop : MonoBehaviour
    {
        [FormerlySerializedAs("IsDragging")] public bool isDragging;
        [FormerlySerializedAs("Dragging")] public Card dragging;
        public CardSlot[] slots;
        private int _draggingIndex;
        [HideInInspector]
        public bool active;

        [HideInInspector] public List<Card> cards;
        private void Start() {
            slots = (CardSlot[])FindObjectsOfType(typeof(CardSlot));
        }
        public void Drop(Card card)
        {
            card.GetComponent<Shadow>().effectDistance = Vector2.zero;
            foreach (var slot in slots)
            {
                if(slot.mouseOver && !slot.card) {
                    Assign(slot);
                    break;
                }
            }
        }
        public void Assign(CardSlot slot){
            slot.card = dragging;
            isDragging = false;
            dragging.transform.position = slot.transform.position;
            // dragging.startPos = cards[cards.Count - 1].startPos;
            // Debug.Log(_draggingIndex);
            // for (int i = cards.Count -1; i >= _draggingIndex; i--)
            // {
            //     cards[i].startPos = cards[i - 1].startPos;
            //     cards[i] = cards[i - 1];
            // }
            // cards[cards.Count - 1] = dragging;
            dragging.placed = true;
            dragging = null;
        }
        public void Drag(Card card)
        {
            if (!active) return;
            isDragging = true;
            dragging = card;
            _draggingIndex = cards.IndexOf(card);
            card.GetComponent<CanvasGroup>().blocksRaycasts = false;
            card.placed = false;
            card.GetComponent<Shadow>().effectDistance = new Vector2(5,-5);
            card.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }
        private void Update() {
            if(isDragging){
                if(Input.GetKey(KeyCode.Mouse0)) dragging.transform.position = Input.mousePosition;
                else{ isDragging = false; dragging = null; }
            }
        }
    }
}
