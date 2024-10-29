using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    public ControlScheme controlScheme = null;
    public Controller controller = null;
    public GameObject bulletPrefab = null;
    public Transform trfm = null;
    public PlayerInfo playerInfo = null;
    public CooldownTracker shootCooldown = new CooldownTracker(0.25f);
    public BulletTracker bullets = new BulletTracker(5, 0.8f);
    public CooldownTracker dashCooldown = new CooldownTracker(2);
    public float dashDistance = 10;
    public float maxRecoil = 10;
    public CameraManager camManager = null;

    public List<PlayerState> states = new List<PlayerState>();
    void Awake()
    {
        this.trfm = this.transform;
        this.controller = this.GetComponent<Controller>(); 
        this.playerInfo = this.GetComponent<PlayerInfo>();
        this.camManager = this.GetComponent<CameraManager>();
        if (this.controlScheme == null)
        {
            this.controlScheme = this.GetComponent<ControlScheme>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.singleton.active && this.controlScheme != null)
        {
            float moveY = 0;
            float moveX = 0;
            if (Input.GetKey(this.controlScheme.up))
                moveY += 1.0f;
            if (Input.GetKey(this.controlScheme.down))
                moveY -= 1.0f;
            if (Input.GetKey(this.controlScheme.left))
                moveX -= 1.0f;
            if (Input.GetKey(this.controlScheme.right))
                moveX += 1.0f;
            Vector2 moveDir = new Vector2(moveX, moveY).normalized;
            this.controller.SetMoveDir(moveDir);
            if (this.shootCooldown.IsReady() && Input.GetKey(this.controlScheme.shoot))
            {
                this.Shoot();
            }
            if (GameManager.singleton.dashEnabled && this.dashCooldown.IsReady() && Input.GetKeyDown(this.controlScheme.dash) && moveDir != Vector2.zero)
            {
                this.Dash();
            }
        }
    }

    public void FixedUpdate()
    {
        if (GameManager.singleton.active)
        {
            this.shootCooldown.Tick(Time.fixedDeltaTime);
            this.bullets.Tick(Time.fixedDeltaTime);
            this.dashCooldown.Tick(Time.fixedDeltaTime);
            this.controller.Move();
            if (GameManager.singleton.ShouldSave())
            {
                this.states.Add(this.GetState());
            }
        }
    }

    public PlayerInfo FindClosestTarget()
    {
        GameObject[] others = GameObject.FindGameObjectsWithTag("player");
        PlayerInfo closestTarget = null;
        float minDist = float.PositiveInfinity;
        foreach (GameObject other in others)
        {
            PlayerInfo otherInfo = other.GetComponent<PlayerInfo>();
            if (otherInfo.isPlayer && otherInfo.team != this.playerInfo.team)
            {
                float dist = Vector3.Distance(otherInfo.trfm.position, this.trfm.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestTarget = otherInfo;
                }
            }
        }
        return closestTarget;
    }

    public void Shoot()
    {
        if(this.bullets.cur <= 0)
        {
            return;
        }
        PlayerInfo closestTarget = this.FindClosestTarget();
        if (closestTarget != null)
        {
            float bulletPercent = this.bullets.Percent();
            if (!this.bullets.Take())
            {
                return;
            }
            Bullet bullet = GameObject.Instantiate(bulletPrefab, this.trfm.position, this.trfm.rotation).GetComponent<Bullet>();
            bullet.SetHealthPercent(bulletPercent);
            bullet.SetTeam(this.playerInfo.team);
            Vector3 targetDir = (closestTarget.trfm.position - this.trfm.position).normalized;
            float recoil = this.maxRecoil * (1f-bulletPercent);
            recoil = UnityEngine.Random.Range(0.5f * recoil, recoil);
            float sign = UnityEngine.Random.Range(0, 2);
            recoil = sign >= 0.5f ? recoil : -recoil;
            targetDir = Quaternion.AngleAxis(recoil, Vector3.forward) * targetDir;
            bullet.SetMoveDir(new Vector2(targetDir.x, targetDir.y));
            this.shootCooldown.StartCooldown();
        }

    }

    public void Dash()
    {
        this.controller.dash = this.dashDistance;
        this.dashCooldown.StartCooldown();
    }

    public PlayerState GetState()
    {
        return new PlayerState()
        {
            frame = GameManager.singleton.frame,
            position = new Vector2(this.trfm.position.x, this.trfm.position.y),
            moveDir = this.controller.moveDir,
            lastMovement = this.controller.lastMovement,
            deltaPosition = this.controller.deltaPosition,
            shootCooldown=this.shootCooldown.timer,
            dashCooldown=this.dashCooldown.timer,
            dash=this.controller.dash,
            bullets=this.bullets.cur,
            health=this.playerInfo.health
        };
    }

    public Dictionary<string, float> GetStateDict()
    {
        return this.GetState().ToDict();
    }
}
