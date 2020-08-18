using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Map
{
    [Serializable]
    public class MapTile : MonoBehaviour{
        public static bool Editing;
        public Vector2 coords;
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
            Map = GameController.Instance.map;
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
    }
}