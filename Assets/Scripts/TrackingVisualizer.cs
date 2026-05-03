using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 3D 追踪目标可视化组件
/// 驱动球体位移 + LineRenderer 轨迹线 + TrailRenderer 拖尾
/// 挂载到 MastManager，将 TrackingTarget 球体拖入 trackingTarget 字段
/// </summary>
public class TrackingVisualizer : MonoBehaviour
{
    [Header("追踪目标（拖入场景中的 TrackingTarget 球体）")]
    public Transform trackingTarget;

    [Header("可视化参数")]
    [Tooltip("场景坐标缩放倍数（服务端单位：米，建议 2.0~5.0）")]
    public float sceneScale = 3.0f;

    [Tooltip("球体位移平滑速度，越大越贴近实时数据，越小越平滑")]
    public float moveSpeed = 12f;

    [Tooltip("轨迹线保留的最大历史点数")]
    public int maxTrailPoints = 300;

    private LineRenderer _lineRenderer;
    private Queue<Vector3> _trailPoints = new Queue<Vector3>();
    private Vector3 _targetPosition;
    private bool _hasData = false;

    void Start()
    {
        SetupLineRenderer();
        SetupTargetMaterial();
    }

    void SetupLineRenderer()
    {
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.startWidth = 0.03f;
        _lineRenderer.endWidth = 0.005f;

        // 创建渐变颜色（青色 → 深蓝色）
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0f, 0.9f, 1f), 0f),
                new GradientColorKey(new Color(0.1f, 0.2f, 0.8f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.1f, 1f)
            }
        );
        _lineRenderer.colorGradient = gradient;
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
    }

    void SetupTargetMaterial()
    {
        if (trackingTarget == null) return;
        // 给追踪球体一个醒目的发光蓝色材质
        var renderer = trackingTarget.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = new Color(0f, 0.7f, 1f);
            renderer.material.EnableKeyword("_EMISSION");
            renderer.material.SetColor("_EmissionColor", new Color(0f, 0.4f, 0.8f) * 2f);
        }
    }

    /// <summary>
    /// 由 MastClient.OnPositionReceived 事件驱动
    /// </summary>
    public void OnNewPosition(Vector3 position)
    {
        _targetPosition = position * sceneScale;
        _hasData = true;

        // 更新轨迹历史
        _trailPoints.Enqueue(_targetPosition);
        if (_trailPoints.Count > maxTrailPoints)
            _trailPoints.Dequeue();

        // 刷新 LineRenderer
        var points = new Vector3[_trailPoints.Count];
        _trailPoints.CopyTo(points, 0);
        _lineRenderer.positionCount = points.Length;
        _lineRenderer.SetPositions(points);
    }

    void Update()
    {
        // 平滑插值：避免数据帧之间球体跳变
        if (_hasData && trackingTarget != null)
        {
            trackingTarget.position = Vector3.Lerp(
                trackingTarget.position,
                _targetPosition,
                Time.deltaTime * moveSpeed
            );
        }
    }
}
