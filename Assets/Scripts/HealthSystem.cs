using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthSystem
{
    public event EventHandler onHealthChanged;
    public event EventHandler onDead;
    public int health { get; private set; }
    public int maxHealth { get; private set; }
    public HealthSystem(int maxHealth)
    {
        this.maxHealth = maxHealth;
        health = maxHealth;
    }
    public int GetHealth()
    {
        return health;
    }
    public void SetHealth(int value)
    {
        this.health = value;
        if (onHealthChanged != null) onHealthChanged(this, EventArgs.Empty);
    }
    public float GetHealthPercent()
    {
        return (float)health / maxHealth;
    }
    public void Damage(int damageAmount)
    {
        health -= damageAmount;
        if (health < 0) health = 0;
        if (onHealthChanged != null) onHealthChanged(this, EventArgs.Empty);
        if (health <= 0)
        {
            Die();
        }
    }
    public void Heal(int healAmount)
    {
        health += healAmount;
        if (health > maxHealth) health = maxHealth;
        if (onHealthChanged != null) onHealthChanged(this, EventArgs.Empty);
    }
    public void Die()
    {
        if (onDead != null) onDead(this, EventArgs.Empty);
    }
    public bool IsDead()
    {
        return health <= 0;
    }
}
