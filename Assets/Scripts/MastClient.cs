using System;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Events;
using NativeWebSocket;
using Newtonsoft.Json;

[Serializable]
public class TrackingData
{
    public string type;
    public float timestamp;
    public string sensor;
    public float[] fused_position_unity;
}

public class MastClient : MonoBehaviour
{
    [Header("Server Settings")]
    public string serverIP = "192.168.1.100";
    public int serverPort = 8080;

    [Header("Events")]
    public UnityEvent<Vector3> OnPositionReceived;
    public UnityEvent<string> OnStatusChanged;

    [HideInInspector] public string debugLog = "Waiting...";

    private WebSocket _webSocket;
    private readonly ConcurrentQueue<Vector3> _posQueue = new ConcurrentQueue<Vector3>();
    private readonly ConcurrentQueue<string>  _statusQueue = new ConcurrentQueue<string>();
    private bool _pendingReconnect = false;
    private float _reconnectCountdown = -1f;

    void Start()
    {
        serverIP = PlayerPrefs.GetString("MAST_ServerIP", serverIP);
        ConnectToServer();
    }

    void Update()
    {
        // 1. Process position updates
        while (_posQueue.TryDequeue(out Vector3 pos))
            OnPositionReceived?.Invoke(pos);

        // 2. Process status text updates
        while (_statusQueue.TryDequeue(out string status))
            OnStatusChanged?.Invoke(status);

        // 3. Process reconnect countdown
        if (_pendingReconnect)
        {
            _pendingReconnect = false;
            _reconnectCountdown = 5f;
        }
        if (_reconnectCountdown > 0f)
        {
            _reconnectCountdown -= Time.deltaTime;
            if (_reconnectCountdown <= 0f)
            {
                _reconnectCountdown = -1f;
                ConnectToServer();
            }
        }

        // 4. NativeWebSocket dispatch queue
#if !UNITY_WEBGL || UNITY_EDITOR
        _webSocket?.DispatchMessageQueue();
#endif
    }

    public async void ConnectToServer()
    {
        _reconnectCountdown = -1f;
        _pendingReconnect = false;

        if (_webSocket != null)
        {
            try { await _webSocket.Close(); } catch { }
        }

        string url = $"ws://{serverIP}:{serverPort}/ws/tracking";
        AppendLog("Connecting: " + url);
        _statusQueue.Enqueue("[..] Connecting: " + serverIP);

        _webSocket = new WebSocket(url);

        _webSocket.OnOpen += () =>
        {
            AppendLog("[OK] Connected");
            _statusQueue.Enqueue("[OK] Connected: " + serverIP);
        };

        _webSocket.OnError += (e) =>
        {
            AppendLog("[ERR] " + e);
            _statusQueue.Enqueue("[ERR] " + e);
        };

        _webSocket.OnClose += (e) =>
        {
            AppendLog("Disconnected. Reconnect in 5s");
            _statusQueue.Enqueue("[--] Disconnected. Reconnecting...");
            _pendingReconnect = true;
        };

        _webSocket.OnMessage += (bytes) =>
        {
            ProcessMessage(System.Text.Encoding.UTF8.GetString(bytes));
        };

        await _webSocket.Connect();
    }

    void ProcessMessage(string json)
    {
        try
        {
            var data = JsonConvert.DeserializeObject<TrackingData>(json);
            if (data == null || data.type == "ping") return;
            if (data.fused_position_unity == null || data.fused_position_unity.Length < 3) return;

            _posQueue.Enqueue(new Vector3(
                data.fused_position_unity[0],
                data.fused_position_unity[1],
                data.fused_position_unity[2]
            ));
        }
        catch (Exception e)
        {
            AppendLog("[ERR] Parse: " + e.Message);
        }
    }

    public void SetServerIP(string ip)
    {
        serverIP = ip.Trim();
        PlayerPrefs.SetString("MAST_ServerIP", serverIP);
        PlayerPrefs.Save();
        AppendLog("Set Server IP: " + serverIP);
    }

    public async void SendData(string message)
    {
        if (_webSocket != null && _webSocket.State == WebSocketState.Open)
        {
            try
            {
                await _webSocket.SendText(message);
            }
            catch (Exception e)
            {
                AppendLog("[ERR] SendData: " + e.Message);
            }
        }
    }

    void AppendLog(string msg)
    {
        debugLog = DateTime.Now.ToString("HH:mm:ss") + " " + msg + "\n" + debugLog;
        var lines = debugLog.Split('\n');
        if (lines.Length > 6)
            debugLog = string.Join("\n", lines, 0, 6);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[MAST] " + msg);
#endif
    }

    async void OnApplicationQuit()
    {
        if (_webSocket != null)
            try { await _webSocket.Close(); } catch { }
    }
}
