using UnityEngine;

public class Heal : MonoBehaviour
{
    public float heal = 50;
    public GameObject go = null;
    // Start is called before the first frame update
    void Awake()
    {
        this.go = this.gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter2D(Collider2D other)
    {

        PlayerInfo otherInfo = other.GetComponent<PlayerInfo>();
        if (otherInfo != null && otherInfo.isPlayer)
        {
            otherInfo.TakeDamage(-heal);
            GameObject.Destroy(this.go);
        }
    }
}
