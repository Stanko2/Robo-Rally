using System;
using System.Collections.Generic;
using System.Reflection;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

namespace Map
{
    [Serializable]
    public class MapTile : MonoBehaviour{
        public static bool Editing;
        public static Transform PropertiesParent;
        public Vector2 coords;
        public static GameObject TextBoxPrefab;
        private int _direction;
        public int Direction
        {
            get => _direction;
            set
            {
                if (value > 3) _direction = 0;
                else if (value < 0) _direction = 3;
                else _direction = value;
                transform.eulerAngles = new Vector3(0,90*_direction,0);
            }
        }
        public int buildIndex;
        [NonSerialized]
        public GameController GameController;
        [FormerlySerializedAs("WallPrefab")] [SerializeField]
        protected GameObject wallPrefab;
        public bool[] walls;
        [FormerlySerializedAs("Robot")] public Robot robot;
        [NonSerialized]
        protected Map Map;
        protected virtual void Start()
        {
            Map = Editing ? transform.parent.GetComponent<Map>() : GameController.instance.map;
            var material = GetComponent<MeshRenderer>().material;
            GetComponent<MeshRenderer>().material = new Material(material);
            if(!Editing) GameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
            if (walls.Length <= 0) return;
            for (var i = 0; i < 4; i++)
            {
                if (!walls[i]) continue;
                var dir = Robot.directions[i] * .9f;
                var go = Instantiate(wallPrefab, transform.position + new Vector3(dir.x,0,dir.y), Quaternion.identity, transform.GetChild(i));
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
            }
        }

        public virtual void ShowTilePropertiesUi() { }

        public virtual void HidePropertiesUi()
        {
            for (var i = 0; i < PropertiesParent.childCount; i++)
            {
                var property = PropertiesParent.GetChild(i);
                Destroy(property.gameObject);
            }
        }
        public virtual void OnRobotArrive() { }

        public virtual void Rotate(){
            var newWalls = new bool[walls.Length];
            Direction++;
            for (int i = 0; i < walls.Length; i++)
            {
                newWalls[i] = walls[(i+1) % walls.Length];
                GameObject go = transform.GetChild(i).gameObject;
                go.SetActive(newWalls[i]);
            }
            walls = newWalls;
        }

        protected static TextBox ShowProperty(object instance, string propertyName)
        {
            var type = instance.GetType();
            var field = type.GetField(propertyName); 
            if(field == null) return null;
            var textBox = Instantiate(TextBoxPrefab, PropertiesParent).GetComponent<TextBox>();
            if (field.FieldType == typeof(int)) textBox.textBoxMode = TextBox.TextBoxMode.Int;
            else if (field.FieldType == typeof(string)) textBox.textBoxMode = TextBox.TextBoxMode.String;
            else if (field.FieldType == typeof(float)) textBox.textBoxMode = TextBox.TextBoxMode.Float;
            textBox.defaultValue = field.GetValue(instance).ToString();
            textBox.OnChangeValue += value => field.SetValue(instance,value);
            textBox.propertyName = propertyName;
            return textBox;
        }
    }
}