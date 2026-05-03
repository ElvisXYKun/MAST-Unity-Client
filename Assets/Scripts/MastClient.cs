using System;
using System.Collections.Generic;
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
    [Header("服务器连接配置")]
    public string serverIP = "192.168.1.100";
    public int serverPort = 8080;

    [Header("事件（Inspector 中连接）")]
    public UnityEvent<Vector3> OnPositionReceived;
    public UnityEvent<string> OnStatusChanged;

    [HideInInspector] public string debugLog = "等待启动...";

    private WebSocket _webSocket;

    void Start()
    {
        serverIP = PlayerPrefs.GetString("MAST_ServerIP", serverIP);
        ConnectToServer();
    }

    public async void ConnectToServer()
    {
        // 关闭旧连接
        if (_webSocket != null)
        {
            try { await _webSocket.Close(); } catch (Exception ex) { Debug.LogWarning("Close error: " + ex.Message); }
        }

        string url = $"ws://{serverIP}:{serverPort}/ws/tracking";
        AppendLog("Connecting: " + url);
        NotifyStatus("[..] Connecting: " + serverIP);

        _webSocket = new WebSocket(url);

        _webSocket.OnOpen += () =>
        {
            AppendLog("[OK] Connected");
            NotifyStatus("[OK] Connected: " + serverIP);
        };

        _webSocket.OnError += (e) =>
        {
            AppendLog("[ERR] Error: " + e);
            NotifyStatus("[ERR] " + e);
        };

        _webSocket.OnClose += (e) =>
        {
            AppendLog("Disconnected. Reconnect in 5s");
            NotifyStatus("[--] Disconnected. Reconnecting...");
            Invoke(nameof(ConnectToServer), 5f);
        };

        _webSocket.OnMessage += (bytes) =>
        {
            string json = System.Text.Encoding.UTF8.GetString(bytes);
            ProcessMessage(json);
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

            Vector3 pos = new Vector3(
                data.fused_position_unity[0],
                data.fused_position_unity[1],
                data.fused_position_unity[2]
            );
            UnityMainThreadDispatcher.Instance.Enqueue(() => OnPositionReceived?.Invoke(pos));
        }
        catch (Exception e)
        {
            AppendLog("[ERR] Parse failed: " + e.Message);
        }
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        _webSocket?.DispatchMessageQueue();
#endif
    }

    public void SetServerIP(string ip)
    {
        serverIP = ip.Trim();
        PlayerPrefs.SetString("MAST_ServerIP", serverIP);
        PlayerPrefs.Save();
    }

    void NotifyStatus(string msg)
    {
        UnityMainThreadDispatcher.Instance.Enqueue(() => OnStatusChanged?.Invoke(msg));
    }

    void AppendLog(string msg)
    {
        // 屏幕调试日志（最多保留 6 行）
        debugLog = System.DateTime.Now.ToString("HH:mm:ss") + " " + msg + "\n" + debugLog;
        var lines = debugLog.Split('\n');
        if (lines.Length > 6)
            debugLog = string.Join("\n", lines, 0, 6);

        // Editor 和开发包才输出 Console 日志，正式 Release 包自动屏蔽
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[MAST] " + msg);
#endif
    }

    async void OnApplicationQuit()
    {
        if (_webSocket != null)
        {
            try { await _webSocket.Close(); } catch { }
        }
    }
}
