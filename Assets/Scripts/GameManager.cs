using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;
using System.IO;
using System.Net.NetworkInformation;

public class GameManager : MonoBehaviour
{
    public static GameManager singleton = null;
    public GameObject welcomePanel = null;
    public GameObject winPanel = null;
    public GameObject heals = null;
    public Toggle dashToggle = null;
    public Toggle healToggle = null;
    public Toggle bounceToggle = null;
    public Toggle recordingToggle = null;
    public bool dashEnabled = true;
    public bool healEnabled = true;
    public bool bounceEnabled = false;
    public bool recordingEnabled = false;
    public TMPro.TMP_Text winText = null;
    public TMPro.TMP_Text dataText = null;
    public TMPro.TMP_InputField scenarioField = null;
    public string timestamp;
    public int frame = 0;
    public string dataPath;

    public bool active = false;
    // Start is called before the first frame update

    public PlayerController player1 = null;
    public PlayerController player2 = null;

    void Awake()
    {
        GameManager.singleton = this;
        this.welcomePanel.SetActive(true);
        this.winPanel.SetActive(false);
        this.timestamp = DateTime.UtcNow.ToUniversalTime().ToString("u").Replace(" ", "T").Replace(":", "-");
        this.frame = 0;
        this.dataPath = Path.Join(Application.dataPath, ".Data");
        Debug.Log(this.dataPath);
        Directory.CreateDirectory(this.dataPath);
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        this.dataText.text = this.dataPath;
        //this.active = true;
    }

    public void FixedUpdate()
    {
        if (this.active)
        {
            this.frame += 1;
        }
    }

    public bool ShouldSave()
    {
        return this.active && this.recordingEnabled;
    }

    public string GetOptionString()
    {
        List<string> options = new List<string>();
        if (dashEnabled) options.Add("dash");
        if (healEnabled) options.Add("heal");
        if (bounceEnabled) options.Add("bounce");
        return String.Join("_", options);
    }

    public string GetTimestampPath()
    {
        string folderName = this.timestamp.ToString();
        string scenarioName = this.scenarioField.text;
        folderName = String.Join("_", new String[] { folderName, scenarioName, this.GetOptionString() });
        return Path.Join(this.dataPath, folderName).ToString();
    }

    public string GetFramePath()
    {
        return Path.Join(this.GetTimestampPath(), this.frame.ToString()).ToString();
    }
    public string GetFramePath(string name)
    {
        return Path.Join(this.GetTimestampPath(), name, this.frame.ToString()).ToString();
    }

    public void StartGame()
    {
        this.dashEnabled = this.dashToggle.isOn;
        this.healEnabled = this.healToggle.isOn;
        this.bounceEnabled = this.bounceToggle.isOn;
        this.recordingEnabled = this.recordingToggle.isOn;

        this.heals.SetActive(this.healEnabled);

        this.frame = 0;
        this.welcomePanel.SetActive(false);
        this.active = true;
    }

    public void RestartGame()
    {
        this.winPanel.SetActive(false);
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    public void ShowWin(int team)
    {
        this.winText.text = "Player " + team + " wins!";
        this.winPanel.SetActive(true);
        this.active = false;
        if (this.recordingEnabled)
        {
            this.SavePlayerStates();
        }
    }

    public void SavePlayerStates()
    {
        SavePlayerStates(this.player1);
        SavePlayerStates(this.player2);
        player1.camManager.SaveBuffers();
        player2.camManager.SaveBuffers();
    }

    public void SavePlayerStates(PlayerController player)
    {
        int team = player.playerInfo.team;

        string dir = Path.Join(GameManager.singleton.GetTimestampPath(), team.ToString());
        Directory.CreateDirectory(dir);
        string path = Path.Join(dir, "states.csv");

        this.SavePlayerStates(player, path);
    }

    public void SavePlayerStates(PlayerController player, string path)
    {
        List<PlayerState> states = player.states;
        string[] lines = new string[states.Count + 1];
        lines[0] = PlayerState.ColumnString();
        int i = 1;
        foreach (PlayerState state in states)
        {
            lines[i] = state.ToString();
            ++i;
        }
        string csv = String.Join(
            Environment.NewLine,
            lines
        );
        System.IO.File.WriteAllText(path, csv);
    }
}
