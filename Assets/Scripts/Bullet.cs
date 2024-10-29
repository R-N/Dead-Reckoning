using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public PlayerInfo playerInfo = null;
    public float damage = 20;
    public float lifetime = 5;
    public Controller controller = null;
    public bool bounce = false;

    // Start is called before the first frame update
    void Awake()
    {
        this.controller = this.GetComponent<Controller>();
        if (this.playerInfo == null)
        {
            this.playerInfo = this.GetComponent<PlayerInfo>();
        }
        this.bounce = GameManager.singleton.bounceEnabled;
    }

    private void FixedUpdate()
    {

        if (GameManager.singleton.active)
        {
            this.playerInfo.TakeDamage(this.playerInfo.maxHealth / this.lifetime * Time.fixedDeltaTime, false);
        }
    }


    private void OnCollisionEnter2D(Collision2D contact)
    {
        Collider2D other = contact.collider;
        if (other.tag == "obstacle")
        {
            if (this.bounce)
            {
                Vector2 moveDir = this.controller.moveDir;
                Vector2 normal = contact.contacts[0].normal;
                float dot = Vector2.Dot(moveDir, normal);
                Vector2 proj = normal * dot;
                Vector2 rej = moveDir - proj;
                Vector2 bounce = -proj + rej;
                this.controller.SetMoveDir(bounce);
                this.playerInfo.TakeDamage(0.5f * this.damage);
                this.controller.moveSpeed *= 0.5f;
            }
            else
            {
                this.playerInfo.TakeDamage(this.damage);
                this.controller.moveSpeed *= 0.5f;
            }
            return;
        }
        PlayerInfo otherInfo = other.GetComponent<PlayerInfo>();
        if (otherInfo != null)
        {
            if (otherInfo.team != this.playerInfo.team)
            {
                otherInfo.TakeDamage(this.damage);
                this.playerInfo.TakeDamage(this.damage);
                //GameObject.Destroy(this.gameObject);
                return;
            }
        }
    }

    public void SetTeam(int team)
    {
        this.playerInfo.SetTeam(team);
    }

    public void SetMoveDir(Vector2 moveDir)
    {
        this.controller.SetMoveDir(moveDir);
    }

    public void SetHealth(float health)
    {
        this.playerInfo.health = health;
        this.playerInfo.SetColor();
    }
    public void SetHealthPercent(float healthPercent)
    {
        this.SetHealth(healthPercent * this.playerInfo.health);
    }
}
