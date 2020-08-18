using System;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float moveSpeed;
    private void FixedUpdate()
    {
        transform.position += -transform.up * (moveSpeed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        Destroy(gameObject);
        Debug.Log(other.gameObject);
        if (!other.CompareTag($"Robot")) return;
        other.GetComponent<IDamageable>().ApplyDamage();
    }
}