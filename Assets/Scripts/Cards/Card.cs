using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Cards
{
    public class Card : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler {
        public Command command;
        [FormerlySerializedAs("HighlightColor")] public Color highlightColor;
        public CommandDragDrop dragDrop;
        [FormerlySerializedAs("StartPos")] public Vector3 startPos;
        public bool placed = false;
        public Color Color{
            set => GetComponent<Image>().color = value;
        }
        public void OnPointerUp(PointerEventData data){
            if(dragDrop.isDragging) dragDrop.Drop(this);
        }
        public void OnPointerEnter(PointerEventData data)
        {
            GetComponent<Image>().color = !Input.GetKey(KeyCode.Mouse0) ? highlightColor : Color.white;
        }
        private void Start() {
            GetComponent<Image>().sprite = command.thumbnail;
            _rectTransform = GetComponent<RectTransform>();
            GetComponentInChildren<Text>().text = command.weight.ToString();
        }
        public void OnPointerClick(PointerEventData data){
            foreach (var slot in dragDrop.slots)
            {
                if(slot.card == null){
                    dragDrop.Assign(slot);
                    return;
                }
            }
        }
        public void OnPointerDown(PointerEventData data){
            GetComponent<Image>().color = Color.white;
            dragDrop.Drag(this);
        }
        RectTransform _rectTransform;
        private void Update() {
            if((!dragDrop.isDragging || dragDrop.dragging != this) && !placed){
                GetComponent<CanvasGroup>().blocksRaycasts = true;
                gameObject.layer = LayerMask.NameToLayer("UI");
                _rectTransform.anchoredPosition3D = Vector3.Lerp(_rectTransform.anchoredPosition3D, startPos, 5*Time.deltaTime);
                if(Vector3.Distance(_rectTransform.anchoredPosition3D, startPos) < .1f) _rectTransform.anchoredPosition3D = startPos;
            }
        }
        public void OnPointerExit(PointerEventData data){
            GetComponent<Image>().color = Color.white;
        }
    }
}