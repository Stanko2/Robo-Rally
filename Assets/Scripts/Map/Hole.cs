using System.Linq;
using UnityEngine;

namespace Map
{
    public class Hole : MapTile
    {
        public Mesh[] holeMeshes;
        private bool[] _adjacentHoles;
        private MeshFilter _meshFilter;

        private void GetMesh()
        {
            var holes = _adjacentHoles.Count(a => a);
            switch (holes)
            {
                case 0:
                    _meshFilter.sharedMesh = holeMeshes[0];
                    break;
                case 1:
                    _meshFilter.sharedMesh = holeMeshes[1];
                    for (var i = 0; i < 4; i++)
                    {
                        if (!_adjacentHoles[i]) continue;
                        Direction = i;
                        break;
                    }
                    break;
                case 2:
                    if (_adjacentHoles[0] && _adjacentHoles[2])
                    {
                        _meshFilter.sharedMesh = holeMeshes[2];
                        Direction = 0;
                    }
                    else if (_adjacentHoles[1] && _adjacentHoles[3])
                    {
                        _meshFilter.sharedMesh = holeMeshes[2];
                        Direction = 1;
                    }
                    else
                    {
                        _meshFilter.sharedMesh = holeMeshes[3];
                        for (var i = 0; i < 4; i++)
                        {
                            if (_adjacentHoles[i] || !_adjacentHoles[(i + 1) % 4]) continue;
                            Direction = i;
                            break;
                        }
                    }
                    break;
                case 3:
                    _meshFilter.sharedMesh = holeMeshes[4];
                    for (var i = 0; i < 4; i++)
                    {
                        if (_adjacentHoles[i]) continue;
                        Direction = i;
                        break;
                    }
                    break;
                case 4:
                    _meshFilter.sharedMesh = holeMeshes[5];
                    break;
            }
        }

        public override void OnRobotArrive()
        {
            base.OnRobotArrive();
            robot.Dead();
        }

        protected override void Start()
        {
            base.Start();
            _meshFilter = GetComponent<MeshFilter>();
            //Map.MapInitialized += UpdateFromAdjacent;
            UpdateFromAdjacent();
        }

        private void UpdateFromAdjacent()
        {
            GetAdjacentHoles();
            GetMesh();
            //DestroyFloatingWalls();
            if(!Editing) return;
            foreach (var direction in Robot.directions)
            {
                Vector2 pos = coords + direction;
            
                if (pos.x < 0 || pos.x >= Map.width) continue;
                if (pos.y < 0 || pos.y >= Map.height) continue;
                if (!(Map[pos] is Hole)) continue;
                var hole = (Hole) Map[coords + direction];
                hole.GetAdjacentHoles();
                hole.GetMesh();
                //hole.DestroyFloatingWalls();
            }
        }

        private void DestroyFloatingWalls()
        {
            for (int i = 0; i < walls.Length; i++)
            {
                if (walls[i] && _adjacentHoles[i])
                {
                    Destroy(transform.GetChild(i).GetChild(0).gameObject);
                    walls[i] = false;
                }
            }
        }
        private void GetAdjacentHoles()
        {
            _adjacentHoles = new[] {false, false, false, false};
            for (var i = 0; i < Robot.directions.Length; i++)
            {
                var dir = Robot.directions[i];
                var pos = coords + dir;
                if (pos.x < 0 || pos.x >= Map.width) continue;
                if (pos.y < 0 || pos.y >= Map.height) continue;
                _adjacentHoles[i] = Map[pos] is Hole;
            }
        }
    }
}
