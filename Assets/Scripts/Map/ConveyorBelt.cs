using System;
using System.Collections;
using UnityEngine;

namespace Map
{
    public class ConveyorBelt : MapTile {
        [System.Serializable]
        public enum Type{Straight, Left, Right}
        public Type type;
        private MeshRenderer _renderer;
        private static readonly int MoveAmount = Shader.PropertyToID("MoveAmount");

        protected override void Start()
        {
            base.Start();
            _renderer = GetComponent<MeshRenderer>();
        }
    
        public IEnumerator Move()
        {
            MoveRobots();
            float currMove = 0;
            while (currMove < 1)
            {
                currMove += Time.deltaTime;
                _renderer.material.SetFloat(MoveAmount, currMove);
                yield return new WaitForEndOfFrame();
            }
            _renderer.material.SetFloat(MoveAmount, 1);
        }

        private void MoveRobots()
        {
            foreach (var robot in GameController.instance.robots)
            {
                if (robot.pos != coords) continue;
                if (robot.CanMove((Direction + 2) % 4))
                {
                    robot.StartCoroutine(robot.MoveToPos(robot.pos + Robot.directions[(Direction + 2) % 4]));
                    switch (type)
                    {
                        case Type.Left:
                            robot.StartCoroutine(robot.RotateToDir(robot.heading - 1));
                            break;
                        case Type.Right:
                            robot.StartCoroutine(robot.RotateToDir(robot.heading + 1));
                            break;
                        case Type.Straight:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }
    }
}