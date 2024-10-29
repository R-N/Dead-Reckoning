using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlScheme : MonoBehaviour
{
    public string up = "w";
    public string down = "s";
    public string left = "a";
    public string right = "d";
    public string shoot = "space";
    public string dash = "left shift";

    public ControlScheme(
        string up,
        string down,
        string left,
        string right,
        string shoot,
        string dash
    ) {
        this.up = up;
        this.down = down;
        this.left = left;
        this.right = right;
        this.shoot = shoot;
        this.dash = dash;
    }

}
