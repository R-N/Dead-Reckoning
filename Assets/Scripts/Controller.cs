using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public Rigidbody2D rb = null;
    public Transform trfm = null;
    public Vector2 moveDir = Vector2.zero;
    public float dashSpeed = 20;
    public float dash = 0;
    public Vector2 lastMovement;
    public Vector2 lastPosition;
    public Vector2 deltaPosition;
    // Start is called before the first frame update
    void Awake()
    {
        this.trfm = this.transform;
        this.rb = this.GetComponent<Rigidbody2D>();
    }


    public void FixedUpdate()
    {
        if (GameManager.singleton.active)
        {
            this.Move(this.moveDir);
        }
    }

    public void Move(Vector2? moveDir = null)
    {
        if (!moveDir.HasValue)
            moveDir = this.moveDir;
        float speed = this.dash > 0 ? this.dashSpeed : this.moveSpeed;
        Vector2 movement = moveDir.Value * speed * Time.fixedDeltaTime;
        if (this.dash > 0)
        {
            movement = Vector3.ClampMagnitude(movement, this.dash);
            this.dash = Mathf.Max(this.dash - movement.magnitude, 0);
        }
        Vector2 newPos = this.rb.position + movement;


        Debug.DrawRay(transform.position, this.lastMovement / Time.fixedDeltaTime, Color.green);
        Debug.DrawRay(transform.position, this.deltaPosition / Time.fixedDeltaTime, Color.yellow);
        this.rb.MovePosition(newPos);
        this.lastMovement = movement;
        this.deltaPosition = this.rb.position - this.lastPosition;
        this.lastPosition = this.rb.position;
        Debug.DrawRay(transform.position, this.lastMovement / Time.fixedDeltaTime, Color.red);
        Debug.DrawRay(transform.position, this.deltaPosition / Time.fixedDeltaTime, Color.magenta);
    }

    public void SetMoveDir(Vector2 moveDir)
    {
        if (this.dash >0)
        {
            return;
        }
        this.moveDir = moveDir;
    }
}
