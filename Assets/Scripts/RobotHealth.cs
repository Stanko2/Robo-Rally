using System;
using UnityEngine;

public interface IDamageable
{
    int Health { get; set; }
    void ApplyDamage(int damage = 1);
    void Heal(int healthRegenerated);
}

public class RobotHealth : MonoBehaviour, IDamageable
{
    public const int MaxHealth = 5;
    public int Health { get; set; }
    private Robot _robot;
    public GameObject bulletPrefab;
    public Transform shootTransform;
    
    private void Start()
    {
        _robot = GetComponent<Robot>();
        Health = MaxHealth;
    }

    public void Heal(int healthRegenerated)
    {
        Health += healthRegenerated;
        if (Health > MaxHealth) Health = MaxHealth;
    }

    public void Shoot()
    {
        if(_robot.IsDead) return;
        if (Physics.Raycast(shootTransform.position, -shootTransform.up, out var hit, float.PositiveInfinity))
        {
            if (hit.collider.CompareTag("Robot"))
            {
                Instantiate(bulletPrefab, shootTransform.position, shootTransform.rotation);
            }
        }
    }
    
    public void ApplyDamage(int damage = 1)
    {
        if(_robot.invincible) return;
        Health-= damage;
        if (Health <= 0)
        {
            Health = MaxHealth;
            _robot.Dead();
        }
    }
}