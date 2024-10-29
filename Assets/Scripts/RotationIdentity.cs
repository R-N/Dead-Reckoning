using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationIdentity : MonoBehaviour
{
    // Start is called before the first frame update
    Transform trfm = null;
    void Start()
    {
        this.trfm = this.transform;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        this.trfm.rotation = Quaternion.identity;
    }
    void FixedUpdate()
    {
        this.trfm.rotation = Quaternion.identity;
    }
}
