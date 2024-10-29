using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using NUnit.Framework;
using System.Xml;
using System;

public class CameraManager : MonoBehaviour
{
    public Camera bulletCam;
    public Camera obstacleCam;
    public Camera enemyCam;
    public Camera buffCam;

    public RenderTexture bulletTex;
    public RenderTexture obstacleTex;
    public RenderTexture enemyTex;
    public RenderTexture buffTex;

    public Shader shader;
    public PlayerInfo playerInfo;
    /*
    public List<Texture2D> bulletTexBuffer = new List<Texture2D>();
    public List<Texture2D> obstacleTexBuffer = new List<Texture2D>();
    public List<Texture2D> enemyTexBuffer = new List<Texture2D>();
    public List<Texture2D> buffTexBuffer = new List<Texture2D>();
    */
    //public List<int> frames = new List<int>();
    public Dictionary<string, Dictionary<int, Texture2D>> texBuffers = new Dictionary<string, Dictionary<int, Texture2D>>()
    {
        {"bullet",  new Dictionary<int, Texture2D>()},
        {"obstacle",  new Dictionary<int, Texture2D>()},
        {"enemy",  new Dictionary<int, Texture2D>()},
        {"buff",  new Dictionary<int, Texture2D>()},
    };

    // Start is called before the first frame update
    void Start()
    {
        bulletCam.targetTexture = bulletTex;
        enemyCam.targetTexture = enemyTex;
        buffCam.targetTexture = buffTex;
        obstacleCam.targetTexture = obstacleTex;
        bulletCam.SetReplacementShader(shader, string.Empty);
        enemyCam.SetReplacementShader(shader, string.Empty);
        buffCam.SetReplacementShader(shader, string.Empty);
        obstacleCam.SetReplacementShader(shader, string.Empty);

        if (this.playerInfo == null)
        {
            this.playerInfo = this.GetComponent<PlayerInfo>();
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (GameManager.singleton.ShouldSave())
        {
            //frames.Add(GameManager.singleton.frame);
            RenderAndBuffer(bulletCam, "bullet");
            RenderAndBuffer(enemyCam, "enemy");
            RenderAndBuffer(buffCam, "buff");
            RenderAndBuffer(obstacleCam, "obstacle");
        }
    }

    public string GetSaveDir(string name)
    {
        string dir = Path.Join(GameManager.singleton.GetTimestampPath(), playerInfo.team.ToString(), name);
        Directory.CreateDirectory(dir);
        return dir;
    }

    public void SaveBuffers()
    {
        foreach (KeyValuePair<string, Dictionary<int, Texture2D>> item in this.texBuffers)
        {
            this.SaveBuffer(item.Key);
        }
    }
    public void SaveBuffer(string name)
    {
        string dir = this.GetSaveDir(name);
        Dictionary<int, Texture2D> buffer = this.texBuffers[name];
        //int n = this.frames.Count;
        //Debug.Assert(n == buffer.Count);
        foreach (KeyValuePair<int, Texture2D> item in buffer)
        {
            this.SaveTexture(item.Value, dir, item.Key);
        }
    }
    void RenderAndBuffer(Camera cam, string name)
    {
        Dictionary<int, Texture2D> buffer = this.texBuffers[name];
        Texture2D tex = this.RenderCamera(cam);
        buffer[GameManager.singleton.frame] = tex;
    }
    void RenderAndSave(Camera cam, string name)
    {
        string dir = this.GetSaveDir(name);
        Texture2D tex = this.RenderCamera(cam);
        this.SaveTexture(tex, dir, GameManager.singleton.frame);
    }

    Texture2D RenderCamera(Camera cam)
    {
        RenderTexture rt = cam.targetTexture;
        RenderTexture.active = rt;
        cam.Render();
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.R8, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        return tex;
    }
    void SaveTexture(Texture2D tex, string dir, int frame)
    {
        string path = Path.Join(dir, frame + ".jpg");
        this.SaveTexture(tex, path);
    }
    void SaveTexture(Texture2D tex, string path)
    {
        byte[] bytes;
        bytes = ImageConversion.EncodeToJPG(tex);
        File.WriteAllBytes(path, bytes);
    }
}
