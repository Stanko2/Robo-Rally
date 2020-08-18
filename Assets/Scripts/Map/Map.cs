using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Map
{
    public class Map : MonoBehaviour {
        [FormerlySerializedAs("Name")] public new string name;
        public event Init MapInitialized;
        public int width;
        public int height;
        public GameObject tilePrefab;
        public TileTemplate[] templates;
        public MapTile[,] tiles;
        [NonSerialized] private TileCollection _mapData;
        [FormerlySerializedAs("StartLocation")] public Vector2 startLocation;
        public void InitMap() {
            //Debug.Log("Map Loading");
            //Debug.Log(Name);
            if(Saver.Load(name) != null){
                _mapData = Saver.Load(name);
                FromTileCollection(_mapData);
            }
            else{
                Debug.Log(height);
                tiles = new MapTile[width, height];
                for (var i = 0; i < width; i++)
                {
                    for (var j = 0; j < height; j++)
                    {
                        tiles[i,j] = Instantiate(templates[0].tile, 2 * new Vector3(i,0,j),Quaternion.identity, transform).GetComponent<MapTile>();
                        Debug.Log(tiles[i,j]);
                        tiles[i,j].coords = new Vector2(i,j);
                        tiles[i, j].walls = new [] {false, false, false, false};
                    }
                }
            }
            Camera.main.transform.position = new Vector3(startLocation.x, 10, startLocation.y);
            MapInitialized?.Invoke();
        }
        private void FromTileCollection(TileCollection a){
            Debug.Log("Loaded");
            width = a.width;
            height = a.height;
            name = a.name;
            startLocation = a.StartLocation;
            tiles = new MapTile[width, height];
            int Checkpoints = 0;
            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    tiles[i, j] = Instantiate(templates[a.tiles[i + width * j].buildIndex].tile, 2 * new Vector3(i, 0, j),
                        Quaternion.identity, transform).GetComponent<MapTile>();
                    //Debug.Log(tiles[i,j]);
                    tiles[i,j].coords = new Vector2(i,j);
                    tiles[i,j].walls = a.tiles[i+width*j].walls;
                    tiles[i,j].Direction = a.tiles[i+width*j].direction;
                    if (tiles[i, j] is Checkpoint) Checkpoints++;
                }
            }

            Checkpoint.CheckpointCount = Checkpoints;
        }

        public MapTile this[Vector2 index]
        {
            get => tiles[(int) index.x, (int) index.y];
            set => tiles[(int) index.x, (int) index.y] = value;
        }
    }
}