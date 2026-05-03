using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIPanel : MonoBehaviour
{
    [Header("UI 组件引用（拖拽连接）")]
    public TMP_Text statusText;
    public TMP_InputField ipInputField;
    public TMP_Text coordinateText;
    public TMP_Text debugText;
    public Button connectButton;

    private MastClient _mastClient;
    private Vector3 _latestPosition;
    private float _fpsTimer;
    private int _frameCount;
    private float _fps;

    void Start()
    {
        _mastClient = GetComponent<MastClient>();

        if (ipInputField != null)
            ipInputField.text = PlayerPrefs.GetString("MAST_ServerIP", "192.168.1.100");

        if (connectButton != null)
            connectButton.onClick.AddListener(OnConnectClicked);
    }

    void Update()
    {
        // FPS 计算
        _frameCount++;
        _fpsTimer += Time.deltaTime;
        if (_fpsTimer >= 1f)
        {
            _fps = _frameCount / _fpsTimer;
            _frameCount = 0;
            _fpsTimer = 0f;
        }

        // 更新坐标显示
        if (coordinateText != null)
        {
            coordinateText.text =
                "X: " + _latestPosition.x.ToString("F3") + "\n" +
                "Y: " + _latestPosition.y.ToString("F3") + "\n" +
                "Z: " + _latestPosition.z.ToString("F3") + "\n" +
                "FPS: " + _fps.ToString("F0");
        }

        // 更新调试日志
        if (debugText != null && _mastClient != null)
            debugText.text = _mastClient.debugLog;
    }

    void OnConnectClicked()
    {
        if (_mastClient == null || ipInputField == null) return;
        string ip = ipInputField.text.Trim();
        if (string.IsNullOrEmpty(ip)) return;
        _mastClient.SetServerIP(ip);
        _mastClient.ConnectToServer();
    }

    public void UpdateStatus(string status)
    {
        if (statusText != null)
            statusText.text = status;
    }

    public void UpdatePosition(Vector3 pos)
    {
        _latestPosition = pos;
    }
}
