using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PlayerState
{
    public static string[] columns = new string[]
    {
        "frame",
        "position_x",
        "position_y",
        "move_dir_x",
        "move_dir_y",
        "last_movement_x",
        "last_movement_y",
        "delta_position_x",
        "delta_position_y",
        "shoot_cooldown",
        "dash_cooldown",
        "dash",
        "bullets",
        "health",
    };
    public int frame;
    public Vector2 position;
    public Vector2 moveDir;
    public Vector2 lastMovement;
    public Vector2 deltaPosition;
    public float shootCooldown;
    public float dashCooldown;
    public float dash;
    public int bullets;
    public float health;

    public Dictionary<string, float> ToDict()
    {
        return new Dictionary<string, float>()
        {
            { "frame", this.frame },
            { "position_x", this.position.x },
            { "position_y", this.position.y },
            { "move_dir_x", this.moveDir.x },
            { "move_dir_y", this.moveDir.y },
            { "last_movement_x", this.lastMovement.x },
            { "last_movement_y", this.lastMovement.y },
            { "delta_position_x", this.deltaPosition.x },
            { "delta_position_y", this.deltaPosition.y },
            { "shoot_cooldown", this.shootCooldown },
            { "dash_cooldown", this.dashCooldown },
            { "dash", this.dash },
            { "bullets", this.bullets },
            { "health", this.health },
        };
    }
    public float[] ToArray()
    {
        Dictionary<string, float> dict = this.ToDict();
        float[] array = new float[PlayerState.columns.Length];
        for (int i = 0; i < PlayerState.columns.Length; i++)
        {
            array[i] = dict[PlayerState.columns[i]];
        }
        return array;
    }
    public override string ToString()
    {
        float[] array = this.ToArray();
        return String.Join(",", array);
    }

    public static string ColumnString()
    {
        return String.Join(",", PlayerState.columns);
    }
}
