using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PlayerInfo : MonoBehaviour
{
    public Transform trfm = null;
    public bool isPlayer = true;
    public int team = 1;
    public Color baseColor = Color.white;
    public Color color = Color.white;
    public LayerMask layer = 1;
    public SpriteRenderer rend = null;
    public GameObject go = null;
    public float colorRate = 0.2f;
    public float baseColorValue = 0.7f;

    public float health = 100.0f;
    public float maxHealth = 100.0f;

    public string layerPrefix = "player";
    public Dictionary<int, Color> teamColors = new Dictionary<int, Color>()
    {
        {1, Color.blue},
        {2, Color.red},
    };
    // Start is called before the first frame update
    void Awake()
    {
        this.trfm = this.transform;
        this.rend = this.GetComponent<SpriteRenderer>();
        this.go = this.gameObject;
        this.health = this.maxHealth;
        this.go.tag = this.layerPrefix;
        this.SetTeam(this.team);
    }

    public void SetTeam(int team)
    {
        this.team = team;
        this.SetColor(this.teamColors[team], true);
        this.SetLayer(this.layerPrefix + "_" + team);
    }

    public void SetLayer(string layerName)
    {
        this.layer = LayerMask.NameToLayer(layerName);
        this.go.layer = this.layer;
    }

    public void SetColor(Color color, bool immediate)
    {
        this.baseColor = color;
        this.color = Color.Lerp(Color.white, color, this.colorValue);
        if (immediate)
        {
            this.rend.color = this.color;
        }
    }

    public void SetColor(Color color)
    {
        this.SetColor(color, false);
    }
    public void SetColor()
    {
        this.SetColor(this.baseColor, false);
    }

    void LateUpdate()
    {
        this.rend.color = Color.Lerp(this.rend.color, this.color, this.colorRate);
    }

    public float colorValue
    {
        get { return this.baseColorValue * this.health / this.maxHealth; }
    }
    public void TakeDamage(float damage, bool animate)
    {
        this.health = Math.Min(Math.Max(0, this.health - damage), this.maxHealth);
        if (animate)
        {
            this.SetColor(this.baseColor);
            this.rend.color = Color.white;
        }
        if (this.health <= 0)
        {
            if (this.isPlayer)
            {
                GameManager.singleton.ShowWin(this.team == 1 ? 2 : 1);
            }
            GameObject.Destroy(this.go);
        }
    }
    public void TakeDamage(float damage)
    {
        this.TakeDamage(damage, true);
    }

    public void TakeDamagePercent(float percent)
    {
        this.TakeDamage(percent * this.health);
    }

}
