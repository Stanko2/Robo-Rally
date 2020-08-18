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
        protected override void Start()
        {
            base.Start();
            _renderer = GetComponent<MeshRenderer>();
        }
    
        public IEnumerator Move(){
            MoveRobots();
            _renderer.material.SetFloat("StartTime", Time.time);
            _renderer.material.SetInt("Move",1);
            // if(Robot != null) yield return new WaitWhile(()=> Robot.moving);
            yield return new WaitForSeconds(1);
            _renderer.material.SetInt("Move",0);
        }

        private void MoveRobots()
        {
            foreach (var robot in GameController.Instance.robots)
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