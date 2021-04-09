using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cards;
using Map;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;

public class Robot : NetworkBehaviour
{
    public static readonly Vector2[] directions = {Vector2.up, Vector2.right, Vector2.down, Vector2.left};
    public Queue<Command> commands;
    private Vector2 _pos = -Vector2.one;
    public Vector2 pos
    {
        get => _pos;
        private set
        {
            if (pos != -Vector2.one)
            {
                map[pos].robot = null;
                _pos = value;
                map[pos].robot = this;
                map[pos].OnRobotArrive();
            }
            else _pos = value;
            transform.position = 2 * new Vector3(pos.x, 0, pos.y);
        }
    }
    public int heading;
    public float moveTime;
    public bool idle = true;
    public bool moving;
    public bool invincible;
    public Map.Map map;
    [SyncVar]
    public int owningPlayerIndex;
    public float endMoveWaitTime = .5f;
    public Transform canvas;
    private Transform _camera;
    public TextMeshProUGUI nameText;
    [SyncVar(hook = "SetSkin")] public int skinIndex;
    private bool _dead;
    private Checkpoint _checkpoint;
    public int CheckpointsCount { get; private set; }
    public RobotHealth Health { get; private set; }
    
    public bool IsDead => _dead;
    [SyncVar(hook = "SetName")] public string RobotName;

    private void SetName(string old, string name)
    {
        nameText.text = name;
    }

    private void SetSkin(int old, int skin)
    {
        GetComponent<RobotSkin>().SetSkin(skin);
        Debug.Log("Skin set");
    }
    private bool IsOnlyRobotOnPosition
    {
        get
        {
            return GameController.instance.robots.Where(robot => robot != this).All(robot => robot.pos != pos);
        }
    }

    public override void OnStartAuthority()
    {
        nameText.transform.parent.GetComponent<Image>().color = Color.cyan;
        base.OnStartAuthority();
    }

    // Start is called before the first frame update
    public void Init()
    {
        if (Camera.main != null) _camera = Camera.main.transform;
        commands = new Queue<Command>();
        pos = map.start.coords;
        Health = GetComponent<RobotHealth>();
        CheckpointsCount = -1;
    }

    // Update is called once per frame
    void Update()
    {
        if (_camera != null)
        {
            Vector3 cameraToRobot = _camera.position - canvas.position;
            canvas.transform.rotation = Quaternion.Euler(-Vector3.Angle(Vector3.forward, cameraToRobot),0,0);    
        }
        
    }

    public void OnCheckpointArrive(Checkpoint checkpoint, bool finished)
    {
        _checkpoint = checkpoint;
        CheckpointsCount++;
        if(finished) Debug.Log("I won!!!");
    }
    public IEnumerator StartNext(){
        idle = false;
        if (!_dead)
        {
            var command = commands.Dequeue();
            switch (command.type)
            {
                case CommandType.Move1:
                    Move(pos + directions[heading]);
                    break;
                case CommandType.Move2:
                    Move(pos + directions[heading]);
                    yield return new WaitWhile(() => moving);
                    Move(pos + directions[heading]);
                    break;
                case CommandType.Move3:
                    Move(pos + directions[heading]);
                    yield return new WaitWhile(() => moving);
                    Move(pos + directions[heading]);
                    yield return new WaitWhile(() => moving);
                    Move(pos + directions[heading]);
                    break;
                case CommandType.Right:
                    Rotate(heading + 1);
                    break;
                case CommandType.Left:
                    Rotate(heading - 1);
                    break;
                case CommandType.Uturn:
                    Rotate(heading + 2);
                    break;
                case CommandType.Backup:
                    Move(pos - directions[heading], true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        yield return new WaitUntil(()=>!moving);
        idle = true;
    }

    public void UpdateInvincible()
    {
        if (invincible) invincible = !IsOnlyRobotOnPosition;
    }

    private void Rotate(int dir)
    {
        StartCoroutine(RotateToDir(dir));
    }

    [Client]
    private void Move(Vector2 target, bool backwards = false)
    {
        var canMove = backwards ? CanMove((heading + 2) % 4) : CanMove(heading);
        //Debug.Log(canMove);
        if (!canMove) return;
        StartCoroutine(MoveToPos(target));
        if(invincible) return;
        var pulling = GameController.instance.GetRobotAtPosition(target);
        if (pulling)
        {
            pulling.Move(target + directions[heading]);
        }
    }
    
    [Client]
    public bool CanMove(int direction){
        var movepos = pos + directions[direction];
        try
        {
            if (!invincible)
            {
                var r = GameController.instance.GetRobotAtPosition(movepos);
                if (r) return r.CanMove(direction);    
            }
            return !map[pos].walls[direction] && !map[movepos].walls[(direction+2) % 4];
        }
        catch(IndexOutOfRangeException){
            return false;
        }
    }

    public void Dead()
    {
        _dead = true;
    }

    public void Respawn()
    {
        _dead = false;
        pos = _checkpoint.coords;
        Health.Health = RobotHealth.MaxHealth;
    }
    public IEnumerator MoveToPos(Vector2 targetPos){
        moving = true;
        Vector2 m = targetPos - pos;
        Vector3 step = Vector3.Lerp(Vector3.zero, 2*new Vector3(m.x,0,m.y), moveTime*Time.fixedDeltaTime);
        
        for (var i = 0; i < moveTime/Time.fixedDeltaTime; i++)
        {
            transform.position += step;
            yield return new WaitForFixedUpdate();
        }
        yield return new WaitForSeconds(endMoveWaitTime);
        pos = targetPos;
        moving = false;
    }

    public IEnumerator RotateToDir(int targetHeading){
        moving = true;
        int rot = Mod(targetHeading,4);
        int deltaAngle = Mod(rot - heading, 4);
        float angleStep = Mathf.Lerp(heading*90, (heading + deltaAngle)*90, moveTime*Time.fixedDeltaTime) - 90*heading;
        if(deltaAngle > 2) angleStep = Mathf.Lerp(heading*90, (heading - Mod(- deltaAngle,4))*90, moveTime*Time.fixedDeltaTime) - 90*heading;
        for (int i = 0; i < moveTime/Time.fixedDeltaTime; i++)
        {
            transform.rotation *= Quaternion.Euler(new Vector3(0,angleStep, 0));
            yield return new WaitForFixedUpdate();
        }
        heading = rot;
       transform.eulerAngles = new Vector3(0,heading*90, 0);
        yield return new WaitForSeconds(endMoveWaitTime);
        moving = false;
    }

    public static int Mod(int x, int m) {
        return (x%m + m)%m;
    }
}
