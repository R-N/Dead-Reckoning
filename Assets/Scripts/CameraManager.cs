using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

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

    const int SlotsPerCamera = 4;
    const int MaxPendingWrites = 64;
    const int JpegQuality = 75;

    readonly object stateLock = new object();
    readonly object writerLock = new object();
    readonly object lifecycleLock = new object();

    CameraCapture[] captures;
    BlockingCollection<WriteJob> writeQueue;
    Thread writerThread;
    bool writerStarted;
    bool acceptingSubmissions;
    bool initialized;
    bool drained;
    bool buffersDisposed;
    int pendingRequests;
    int submissionsInProgress;
    bool faulted;
    bool faultLogged;
    string faultMessage;

    sealed class CaptureSlot
    {
        public NativeArray<byte> Data;
        public bool InFlight;
        public int Generation;
    }

    sealed class CameraCapture
    {
        public readonly Camera Camera;
        public readonly RenderTexture Target;
        public readonly string Name;
        public readonly int Width;
        public readonly int Height;
        public readonly CaptureSlot[] Slots;
        public int NextSlot;

        public CameraCapture(Camera camera, RenderTexture target, string name)
        {
            Camera = camera;
            Target = target;
            Name = name;
            Width = target.width;
            Height = target.height;
            Slots = new CaptureSlot[SlotsPerCamera];
            for (int i = 0; i < Slots.Length; ++i)
            {
                Slots[i] = new CaptureSlot();
            }
        }

        public void AllocateSlots()
        {
            int byteCount = checked(Width * Height);
            for (int i = 0; i < Slots.Length; ++i)
            {
                Slots[i].Data = new NativeArray<byte>(
                    byteCount,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory
                );
            }
        }

        public void DisposeSlots()
        {
            for (int i = 0; i < Slots.Length; ++i)
            {
                if (Slots[i].Data.IsCreated)
                {
                    Slots[i].Data.Dispose();
                }
            }
        }
    }

    sealed class WriteJob
    {
        public readonly string Path;
        public readonly byte[] Bytes;

        public WriteJob(string path, byte[] bytes)
        {
            Path = path;
            Bytes = bytes;
        }
    }

    void Start()
    {
        if (bulletCam == null || obstacleCam == null || enemyCam == null || buffCam == null ||
            bulletTex == null || obstacleTex == null || enemyTex == null || buffTex == null)
        {
            FailRecording("CameraManager cannot record: camera or render texture reference is missing.");
            ReportFaultIfNeeded();
            return;
        }

        bulletCam.targetTexture = bulletTex;
        enemyCam.targetTexture = enemyTex;
        buffCam.targetTexture = buffTex;
        obstacleCam.targetTexture = obstacleTex;
        bulletCam.SetReplacementShader(shader, string.Empty);
        enemyCam.SetReplacementShader(shader, string.Empty);
        buffCam.SetReplacementShader(shader, string.Empty);
        obstacleCam.SetReplacementShader(shader, string.Empty);

        if (playerInfo == null)
        {
            playerInfo = GetComponent<PlayerInfo>();
        }

        InitializeRecorder();
    }

    void FixedUpdate()
    {
        ReportFaultIfNeeded();

        if (GameManager.singleton.ShouldSave() && initialized && acceptingSubmissions)
        {
            SubmitCapture(captures[0]);
            SubmitCapture(captures[1]);
            SubmitCapture(captures[2]);
            SubmitCapture(captures[3]);
        }
    }

    public string GetSaveDir(string name)
    {
        string dir = Path.Join(GameManager.singleton.GetTimestampPath(), playerInfo.team.ToString(), name);
        Directory.CreateDirectory(dir);
        return dir;
    }

    public bool SaveBuffers()
    {
        StopAndDrain(false);
        ReportFaultIfNeeded();

        lock (stateLock)
        {
            return drained && !faulted;
        }
    }

    void InitializeRecorder()
    {
        if (playerInfo == null)
        {
            FailRecording("CameraManager cannot record: PlayerInfo is missing.");
            ReportFaultIfNeeded();
            return;
        }

        if (bulletCam == null || obstacleCam == null || enemyCam == null || buffCam == null ||
            bulletTex == null || obstacleTex == null || enemyTex == null || buffTex == null)
        {
            FailRecording("CameraManager cannot record: camera or render texture reference is missing.");
            ReportFaultIfNeeded();
            return;
        }

        if (!SystemInfo.supportsAsyncGPUReadback)
        {
            FailRecording("CameraManager cannot record: async GPU readback is unsupported on this platform; recording disabled.");
            ReportFaultIfNeeded();
            return;
        }

        captures = new CameraCapture[]
        {
            new CameraCapture(bulletCam, bulletTex, "bullet"),
            new CameraCapture(enemyCam, enemyTex, "enemy"),
            new CameraCapture(buffCam, buffTex, "buff"),
            new CameraCapture(obstacleCam, obstacleTex, "obstacle"),
        };

        try
        {
            for (int i = 0; i < captures.Length; ++i)
            {
                captures[i].AllocateSlots();
            }

            writeQueue = new BlockingCollection<WriteJob>(MaxPendingWrites);
            writerThread = new Thread(WriteFiles)
            {
                IsBackground = true,
                Name = "CameraManager file writer",
            };
            writerThread.Start();
            writerStarted = true;

            lock (stateLock)
            {
                acceptingSubmissions = true;
                initialized = true;
            }
        }
        catch (Exception exception)
        {
            DisposeWriter();
            DisposeCaptureBuffers();
            FailRecording("CameraManager could not initialize async recorder: " + exception.Message);
            ReportFaultIfNeeded();
        }
    }

    void SubmitCapture(CameraCapture capture)
    {
        CaptureSlot slot;
        int generation;
        if (!ReserveSlot(capture, out slot, out generation))
        {
            return;
        }

        int frame = GameManager.singleton.frame;
        string name = capture.Name;
        int width = capture.Width;
        int height = capture.Height;
        string path = null;

        try
        {
            bool canSubmit;
            lock (stateLock)
            {
                canSubmit = acceptingSubmissions && !faulted;
            }

            if (!canSubmit)
            {
                ReleaseSlot(slot, generation);
                return;
            }

            path = Path.Join(GetSaveDir(name), frame + ".jpg");
            capture.Camera.Render();

            lock (stateLock)
            {
                canSubmit = acceptingSubmissions && !faulted;
            }

            if (!canSubmit)
            {
                ReleaseSlot(slot, generation);
                return;
            }

            AsyncGPUReadback.RequestIntoNativeArray(
                ref slot.Data,
                capture.Target,
                0,
                GraphicsFormat.R8_UNorm,
                request => CompleteReadback(slot, generation, frame, name, path, width, height, request)
            );
        }
        catch (Exception exception)
        {
            ReleaseSlot(slot, generation);
            FailRecording("CameraManager async capture submission failed for " + name + " frame " + frame + ": " + exception.Message);
        }
        finally
        {
            lock (stateLock)
            {
                --submissionsInProgress;
                Monitor.PulseAll(stateLock);
            }
        }
    }

    bool ReserveSlot(CameraCapture capture, out CaptureSlot reserved, out int generation)
    {
        reserved = null;
        generation = 0;
        bool exhausted = false;

        lock (stateLock)
        {
            if (!acceptingSubmissions || faulted)
            {
                return false;
            }

            for (int i = 0; i < capture.Slots.Length; ++i)
            {
                int slotIndex = (capture.NextSlot + i) % capture.Slots.Length;
                CaptureSlot slot = capture.Slots[slotIndex];
                if (!slot.InFlight)
                {
                    slot.InFlight = true;
                    ++slot.Generation;
                    capture.NextSlot = (slotIndex + 1) % capture.Slots.Length;
                    ++pendingRequests;
                    ++submissionsInProgress;
                    reserved = slot;
                    generation = slot.Generation;
                    return true;
                }
            }

            exhausted = true;
        }

        if (exhausted)
        {
            FailRecording("CameraManager capture ring exhausted for " + capture.Name + "; recording stopped instead of reusing an in-flight readback buffer.");
            ReportFaultIfNeeded();
        }

        return false;
    }

    void CompleteReadback(
        CaptureSlot slot,
        int generation,
        int frame,
        string name,
        string path,
        int width,
        int height,
        AsyncGPUReadbackRequest request
    )
    {
        try
        {
            lock (stateLock)
            {
                if (!slot.InFlight || slot.Generation != generation)
                {
                    return;
                }
            }

            if (request.hasError)
            {
                FailRecording("CameraManager async GPU readback failed for " + name + " frame " + frame + "; recording stopped.");
                return;
            }

            NativeArray<byte> encoded = default(NativeArray<byte>);
            try
            {
                encoded = ImageConversion.EncodeNativeArrayToJPG(
                    slot.Data,
                    GraphicsFormat.R8_UNorm,
                    (uint)width,
                    (uint)height,
                    0,
                    JpegQuality
                );

                if (!encoded.IsCreated || encoded.Length == 0)
                {
                    FailRecording("CameraManager JPEG encoding returned no data for " + name + " frame " + frame + "; recording stopped.");
                    return;
                }

                byte[] bytes = new byte[encoded.Length];
                encoded.CopyTo(bytes);
                if (!EnqueueWrite(path, bytes))
                {
                    FailRecording("CameraManager file writer unavailable for " + path + "; recording stopped.");
                }
            }
            finally
            {
                if (encoded.IsCreated)
                {
                    encoded.Dispose();
                }
            }
        }
        catch (Exception exception)
        {
            FailRecording("CameraManager async completion failed for " + name + " frame " + frame + ": " + exception.Message);
        }
        finally
        {
            ReleaseSlot(slot, generation);
        }
    }

    bool EnqueueWrite(string path, byte[] bytes)
    {
        lock (writerLock)
        {
            if (writeQueue == null || writeQueue.IsAddingCompleted)
            {
                return false;
            }

            try
            {
                writeQueue.Add(new WriteJob(path, bytes));
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }

    void WriteFiles()
    {
        try
        {
            foreach (WriteJob job in writeQueue.GetConsumingEnumerable())
            {
                string tempPath = job.Path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    File.WriteAllBytes(tempPath, job.Bytes);
                    File.Move(tempPath, job.Path);
                }
                catch (Exception exception)
                {
                    try
                    {
                        if (File.Exists(tempPath))
                        {
                            File.Delete(tempPath);
                        }
                    }
                    catch (Exception cleanupException)
                    {
                        FailRecording("CameraManager failed deleting temporary file " + tempPath + ": " + cleanupException.Message);
                    }
                    FailRecording("CameraManager failed writing " + job.Path + ": " + exception.Message);
                }
            }
        }
        catch (Exception exception)
        {
            FailRecording("CameraManager file writer stopped unexpectedly: " + exception.Message);
        }
    }

    void ReleaseSlot(CaptureSlot slot, int generation)
    {
        lock (stateLock)
        {
            if (slot.InFlight && slot.Generation == generation)
            {
                slot.InFlight = false;
                --pendingRequests;
            }
            Monitor.PulseAll(stateLock);
        }
    }

    void StopAndDrain(bool disposeBuffers)
    {
        lock (lifecycleLock)
        {
            if (!drained)
            {
                lock (stateLock)
                {
                    acceptingSubmissions = false;
                    while (submissionsInProgress > 0)
                    {
                        Monitor.Wait(stateLock);
                    }
                }

                bool hasPendingRequests;
                lock (stateLock)
                {
                    hasPendingRequests = pendingRequests > 0;
                }

                if (hasPendingRequests)
                {
                    try
                    {
                        AsyncGPUReadback.WaitAllRequests();
                    }
                    catch (Exception exception)
                    {
                        FailRecording("CameraManager could not drain async GPU readbacks: " + exception.Message);
                    }
                }

                lock (stateLock)
                {
                    while (pendingRequests > 0)
                    {
                        Monitor.Wait(stateLock);
                    }
                }

                DisposeWriter();
                drained = true;
            }

            if (disposeBuffers && !buffersDisposed)
            {
                DisposeCaptureBuffers();
                buffersDisposed = true;
            }
        }
    }

    void DisposeWriter()
    {
        lock (writerLock)
        {
            if (writeQueue != null && !writeQueue.IsAddingCompleted)
            {
                writeQueue.CompleteAdding();
            }
        }

        if (writerThread != null && writerStarted && writerThread != Thread.CurrentThread)
        {
            writerThread.Join();
        }

        lock (writerLock)
        {
            if (writeQueue != null)
            {
                writeQueue.Dispose();
                writeQueue = null;
            }
            writerThread = null;
            writerStarted = false;
        }
    }

    void DisposeCaptureBuffers()
    {
        if (captures == null)
        {
            return;
        }

        for (int i = 0; i < captures.Length; ++i)
        {
            captures[i].DisposeSlots();
        }
    }

    void FailRecording(string message)
    {
        lock (stateLock)
        {
            acceptingSubmissions = false;
            if (!faulted)
            {
                faulted = true;
                faultMessage = message;
                faultLogged = false;
            }
        }
    }

    void ReportFaultIfNeeded()
    {
        string message = null;
        lock (stateLock)
        {
            if (faulted && !faultLogged)
            {
                faultLogged = true;
                message = faultMessage;
            }
        }

        if (message != null)
        {
            Debug.LogError(message, this);
        }
    }

    void OnDestroy()
    {
        StopAndDrain(true);
        ReportFaultIfNeeded();
    }
}
