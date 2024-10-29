using System;
using UnityEngine;

public class CooldownTracker 
{
    public float timer = 0;
    public float cooldown = 0;

    public CooldownTracker(float cooldown)
    {
        this.cooldown = cooldown;
        this.Reset();
    }

    public void Tick(float deltaTime)
    {
        this.timer = Mathf.Max(this.timer - deltaTime, 0);
    }

    public void StartCooldown()
    {
        this.timer = cooldown;
    }

    public bool IsReady()
    {
        return this.timer <= 0;
    }

    public void Reset()
    {
        this.timer = 0;
    }
}
