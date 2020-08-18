using UnityEngine;
using UnityEngine.Serialization;

public class CameraMover : MonoBehaviour {
    
    [FormerlySerializedAs("FollowTarget")] public bool followTarget;
    [FormerlySerializedAs("RotSpeed")] public float rotSpeed = 5;
    [FormerlySerializedAs("MoveSpeed")] public float moveSpeed = 5;
    [FormerlySerializedAs("LocalPlayer")] public Transform localPlayer;
    private void Update()
    {
        if(followTarget) Follow();
        else KeyboardMovement();
        transform.eulerAngles += new Vector3(0,Input.GetAxis("Rotate")*rotSpeed,0) * Time.deltaTime;
    }

    private void KeyboardMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3(horizontal, 0, vertical) * rotSpeed * Time.deltaTime;
        transform.position += movement;
    }
    private void Follow(){
        Vector3 targetPos = new Vector3(localPlayer.position.x, transform.position.y, localPlayer.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime);
    }
}