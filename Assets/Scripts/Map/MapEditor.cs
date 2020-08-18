using System.Data;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Map
{
    public class MapEditor : MonoBehaviour {
        public Map map;
        public GameObject preview;
        [FormerlySerializedAs("Filename")] public InputField filename;
        public int width,height;
        public TileTemplate[] tileTemplates;
        MapTile _selected;
        int _direction;
        int _selectedIndex;
        private Camera _camera;
        // TODO: Make UI for Tile Properties
        private void Start() {
            _camera = Camera.main;
            map.templates = tileTemplates;
            MapTile.Editing = true;
            map.InitMap();
            preview.GetComponent<MeshRenderer>().material.color = Color.red;
            Select(0);
            // for (int i = 0; i < width; i++)
            // {
            //     for (int j = 0; j < height; j++)
            //     {
            //         map.tiles[i,j] = Instantiate(tileTemplates[0].tile, 2 * new Vector3(i,0,j),Quaternion.identity, transform).GetComponent<MapTile>();
            //         map.tiles[i,j].coords = new Vector2(i,j);
            //         map.tiles[i,j].walls = new bool[]{false, false, false, false};
            //     }
            // }
        }
        public void Save(){
            if(filename.text != ""){
                Saver.Save(TileCollection.Create(map), filename.text);
                SceneManager.LoadScene("Menu");
            }
        }
        public void Select(int index){
            tileTemplates[_selectedIndex].buildButton.GetComponent<RawImage>().color = Color.white;
            _selectedIndex = index;
            tileTemplates[index].buildButton.GetComponent<RawImage>().color = Color.red;
        }
        private void Update() {
            var ray = _camera.ScreenPointToRay(Input.mousePosition);
            if(_selected) _selected.gameObject.SetActive(true);
            if (!Physics.Raycast(ray, out var hit, Mathf.Infinity, LayerMask.GetMask("Default"))) return;
            _selected = hit.collider.GetComponent<MapTile>();
            Vector2 pos = _selected.coords;
            SetPreview();
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                Place(pos);
            }
            else if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                _direction++;
                _direction %= 4;
            }
        }

        private void Place(Vector2 pos)
        {
            if (!tileTemplates[_selectedIndex].directional)
            {
                Destroy(_selected.gameObject);
                var newtile = Instantiate(tileTemplates[_selectedIndex].tile, 2 * new Vector3(pos.x, 0, pos.y),
                    Quaternion.identity, map.transform).GetComponent<MapTile>();
                newtile.coords = pos;
                newtile.walls = new bool[4];
                newtile.Direction = _direction;
                map[pos] = newtile;
            }
            else
            {
                var rotatedDirection = Robot.Mod(_direction - _selected.Direction, 4);
                if (_selected.transform.GetChild(rotatedDirection).childCount > 0)
                {
                    Destroy(_selected.transform.GetChild(rotatedDirection).GetChild(0).gameObject);
                    _selected.walls[rotatedDirection] = false;
                }
                else
                {
                
                    var dir = Robot.directions[_direction] * .9f;
                    Instantiate(tileTemplates[_selectedIndex].tile,
                        _selected.transform.position + new Vector3(dir.x, .6f, dir.y),
                        Quaternion.Euler(0, 90 * _direction, 0), _selected.transform.GetChild(rotatedDirection));
                
                    _selected.walls[rotatedDirection] = true;
                }
            }
        }

        private void SetPreview()
        {
            if (!tileTemplates[_selectedIndex].directional)
            {
                _selected.gameObject.SetActive(false);
                var transform1 = _selected.transform;
                preview.transform.position = transform1.position;
                preview.transform.localScale = transform1.lossyScale;
                preview.transform.eulerAngles = new Vector3(0, 90 * _direction, 0);
            }
            else
            {
                var rotatedDirection = Robot.Mod(_direction - _selected.Direction, 4);
                var dir = Robot.directions[_direction] * .9f;
                preview.transform.position = _selected.transform.position + new Vector3(dir.x, .5f, dir.y);
                preview.transform.eulerAngles = new Vector3(0, 90 * _direction, 0);
                preview.transform.localScale = Vector3.one; //* .5f;
            }
            preview.GetComponent<MeshFilter>().sharedMesh = tileTemplates[_selectedIndex].tile.GetComponent<MeshFilter>().sharedMesh;
            preview.GetComponent<MeshRenderer>().sharedMaterials = tileTemplates[_selectedIndex].tile.GetComponent<MeshRenderer>().sharedMaterials;
        }
    }

    [System.Serializable]
    public class TileTemplate{
        public GameObject tile;
        [FormerlySerializedAs("BuildButton")] public Button buildButton;
        [FormerlySerializedAs("Directional")] public bool directional;
    }
}