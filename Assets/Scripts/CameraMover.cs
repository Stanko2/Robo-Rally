using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class CameraMover : MonoBehaviour {
    
    [FormerlySerializedAs("FollowTarget")] public bool followTarget;
    [FormerlySerializedAs("RotSpeed")] public float rotSpeed = 15;
    [FormerlySerializedAs("MoveSpeed")] public float moveSpeed = 15;
    [FormerlySerializedAs("LocalPlayer")] public Transform localPlayer;
    public float minY = 5, maxY = 25;
    public float minRot=60, maxRot=90;
    public float zoomSpeed = 30;
    private Camera _camera;
    private Map.Map _map;
    private float _currentY;
    private Vector3 moveStartPos;
    private void Start()
    {
        _camera = GetComponent<Camera>();
        _map = FindObjectOfType<Map.Map>();
    }

    private void Update()
    {
        if(followTarget) Follow();
        else KeyboardMovement();
        transform.eulerAngles += new Vector3(0,Input.GetAxis("Rotate")*rotSpeed,0) * Time.deltaTime;
        Zoom();
        //ClampPosition();
        if (Input.GetKeyDown(KeyCode.M)) StartCoroutine(ShowPlayer());
    }
    
    private void Zoom()
    {
        var pos = transform.position;
        var zoom = Input.GetAxis("Mouse ScrollWheel");
        if (zoom < 0) _currentY += zoomSpeed * Time.deltaTime;
        else if (zoom > 0) _currentY -= zoomSpeed * Time.deltaTime;
        _currentY = Mathf.Clamp(_currentY, minY, maxY);
        transform.position = new Vector3(pos.x, _currentY, pos.z);
    }
    private void ClampPosition()
    {
        var rays = new[]
        {
            new Vector3(0, Screen.height / 2f), new Vector3(Screen.width / 2f, 0),
            new Vector3(Screen.width, Screen.height / 2f), new Vector3(Screen.width / 2f, Screen.height),
        };
        foreach (var pos in rays)
        {
            var ray = _camera.ScreenPointToRay(pos);
            //if(_map.MapBounds.IntersectRay(ray)) continue;
            Physics.Raycast(ray, out var hit, float.PositiveInfinity);
            if(hit.collider.CompareTag("Map")) continue;
            var deltapos = hit.point - _map.MapBounds.ClosestPoint(hit.point);
            deltapos.y = transform.position.y;
            transform.position -= deltapos;
        }
    }
    private void KeyboardMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3(horizontal, 0, vertical) * (rotSpeed * Time.deltaTime);
        if (Input.GetKey(KeyCode.Mouse2))
        {
            var currRot = Mathf.Lerp(minRot, maxRot, Input.mousePosition.y / Screen.height);
            transform.eulerAngles = new Vector3(currRot,0,0);
        }

        if (Input.GetKeyDown(KeyCode.Mouse0)) moveStartPos = _camera.ScreenToWorldPoint(Input.mousePosition);
        if (Input.GetKey(KeyCode.Mouse0))
        {
            movement = _camera.ScreenToWorldPoint(Input.mousePosition) - moveStartPos;
        }
        transform.position += movement;
    }
    private void Follow(){
        var position = transform.position;
        var position1 = localPlayer.position;
        var targetPos = new Vector3(position1.x, position.y, position1.z);
        transform.position = Vector3.Lerp(position, targetPos, Time.deltaTime);
    }

    private IEnumerator ShowPlayer()
    {
        Physics.Raycast(_camera.ScreenPointToRay(new Vector3(Screen.width / 2f, 0, Screen.height / 2f)), out var hit, float.PositiveInfinity);
        var position = transform.position;
        var deltaPos = Vector3.Lerp(Vector3.zero, (hit.point - localPlayer.position), Time.fixedDeltaTime);
        deltaPos.y = position.y;
        Debug.Log(deltaPos);
        for (int i = 0; i < 1f/Time.fixedDeltaTime; i++)
        {
            transform.position -= deltaPos;
            yield return new WaitForFixedUpdate();
        }
        // followTarget = true;
        // yield return new WaitForSeconds(2);
        // followTarget = false;
    }
}