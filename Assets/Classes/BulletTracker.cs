using System;
using UnityEngine;

public class BulletTracker
{
    public CooldownTracker cooldownTracker = null;
    public int max = 5;
    public int cur = 0;
    public int regen = 1;

    public BulletTracker(int max, float cooldown, int regen)
    {
        this.cooldownTracker = new CooldownTracker(cooldown);
        this.max = max;
        this.regen = regen;
        this.Reset();
    }
    public BulletTracker(int max, float cooldown) : this(max, cooldown, 1)
    {
    }

    public void Reset()
    {
        this.cur = this.max;
        this.cooldownTracker.Reset();
    }

    public void Tick(float deltaTime)
    {
        this.cooldownTracker.Tick(deltaTime);
        this.TryRegen();
    }

    public void TryRegen()
    {
        if (this.cur <= this.max && this.cooldownTracker.IsReady())
        {
            this.cur = Math.Min(this.cur + this.regen, this.max);
            this.cooldownTracker.StartCooldown();
        }
    }

    public int Take(int count)
    {
        int taken = Math.Min(this.cur, count);
        this.cur = Math.Max(this.cur - taken, 0);
        this.TryRegen();
        return taken;
    }
    public bool Take()
    {
        return this.Take(1) >= 1;
    }

    public float Percent()
    {
        return ((float)this.cur) / this.max;
    }
}
