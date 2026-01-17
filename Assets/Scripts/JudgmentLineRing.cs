using UnityEngine;

/// <summary>
/// リング状の判定ラインを描画するコンポーネント
/// LineRendererを使用して円形の判定ラインを表示します
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class JudgmentLineRing : MonoBehaviour
{
    [Header("Ring Settings")]
    [SerializeField] private float radius = 5.0f;          // リングの半径
    [SerializeField] private int segments = 100;           // リングの分割数（滑らかさ）
    [SerializeField] private float lineWidth = 0.1f;       // 線の太さ
    [SerializeField] private Color lineColor = Color.white; // 線の色

    private LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        SetupLineRenderer();
        DrawRing();
    }

    /// <summary>
    /// LineRendererの初期設定
    /// </summary>
    void SetupLineRenderer()
    {
        lineRenderer.loop = true; // ループさせて円を閉じる
        lineRenderer.useWorldSpace = false; // ローカル座標で描画
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        // マテリアルと色の設定
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;

        // 2Dゲーム用にZ座標を調整
        lineRenderer.sortingOrder = 10; // 他のオブジェクトより前面に表示
    }

    /// <summary>
    /// リングを描画
    /// </summary>
    void DrawRing()
    {
        lineRenderer.positionCount = segments;

        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = radius * Mathf.Cos(angle);
            float y = radius * Mathf.Sin(angle);

            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
        }
    }

    /// <summary>
    /// 半径を取得
    /// </summary>
    public float GetRadius()
    {
        return radius;
    }

    /// <summary>
    /// 半径を動的に変更
    /// </summary>
    public void SetRadius(float newRadius)
    {
        radius = newRadius;
        DrawRing();
    }

    /// <summary>
    /// 色を動的に変更
    /// </summary>
    public void SetColor(Color newColor)
    {
        lineColor = newColor;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
    }

#if UNITY_EDITOR
    // インスペクタで値を変更した時にリアルタイムで反映
    void OnValidate()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        SetupLineRenderer();
        DrawRing();
    }
#endif
}
