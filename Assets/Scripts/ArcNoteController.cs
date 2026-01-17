using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 円弧状のノートコントローラー
/// 画面中央から放射状に外側へ移動するノート
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class ArcNoteController : MonoBehaviour
{
    // 自身の所属するプールを覚えておく変数
    private IObjectPool<ArcNoteController> _managedPool;

    // ノートの移動に関する変数
    private float targetBeat;           // 目標ビート（判定タイミング）
    private float appearTime;           // 出現から判定までの拍数
    private float angleDeg;             // ノートの中心角度（度数法）
    private float targetRadius;         // 目標半径（判定ライン）
    private float startRadius = 0.0f;   // 開始半径（中心）
    private bool _isJudged = false;     // 判定済みかどうか

    // 削除タイミング管理
    private bool reachedJudgmentLine = false;  // 判定ラインに到達したか
    private float deleteBeat = -1f;            // 削除すべきビート

    // ビジュアル管理
    private LineRenderer lineRenderer;

    [Header("Arc Settings")]
    [SerializeField] private float arcAngleSpan = 45f;  // 円弧の角度幅
    [SerializeField] private int arcSegments = 20;      // 円弧の分割数（滑らかさ）
    [SerializeField] private float lineWidth = 0.2f;    // 線の太さ
    [SerializeField] private Color arcColor = Color.cyan; // 円弧の色

    void Awake()
    {
        // LineRendererを取得
        lineRenderer = GetComponent<LineRenderer>();
        SetupLineRenderer();
    }

    /// <summary>
    /// LineRendererの初期設定
    /// </summary>
    void SetupLineRenderer()
    {
        if (lineRenderer == null) return;

        lineRenderer.loop = false;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.startColor = arcColor;
        lineRenderer.endColor = arcColor;
        lineRenderer.sortingOrder = 5;
    }

    /// <summary>
    /// プールをセット
    /// </summary>
    public void SetPool(IObjectPool<ArcNoteController> pool)
    {
        _managedPool = pool;
    }

    /// <summary>
    /// ノートの初期化
    /// </summary>
    /// <param name="targetBeat">目標ビート</param>
    /// <param name="appearTime">出現時間（拍数）</param>
    /// <param name="angle">角度（度数法）</param>
    /// <param name="radius">目標半径</param>
    public void Initialize(float targetBeat, float appearTime, float angle, float radius)
    {
        this.targetBeat = targetBeat;
        this.appearTime = appearTime;
        this.angleDeg = angle;
        this.targetRadius = radius;
        this._isJudged = false;

        // 削除タイミング管理をリセット
        this.reachedJudgmentLine = false;
        this.deleteBeat = -1f;

        // 見た目をリセット（表示状態に）
        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
        }

        // 初期位置を中心にセット
        transform.position = Vector3.zero;

        // 円弧を描画
        DrawArc(startRadius);
    }

    /// <summary>
    /// 毎フレーム更新（CRICueTestのUpdateAllNotesから呼ぶ）
    /// </summary>
    /// <param name="currentTotalBeat">現在の通算ビート</param>
    public void UpdatePositionByBeat(float currentTotalBeat)
    {
        // 判定ラインに到達済みなら位置更新をスキップ
        if (reachedJudgmentLine) return;

        // 進行度 (0.0 = 生成時, 1.0 = ジャストタイミング)
        float progress = 1.0f - (targetBeat - currentTotalBeat) / appearTime;
        progress = Mathf.Clamp01(progress);

        // 現在の半径を計算 (中心0から外側targetRadiusへ)
        float currentRadius = Mathf.Lerp(startRadius, targetRadius, progress);

        // 円弧を現在の半径で再描画
        DrawArc(currentRadius);
    }

    /// <summary>
    /// 円弧を描画
    /// </summary>
    /// <param name="radius">円弧の半径</param>
    void DrawArc(float radius)
    {
        if (lineRenderer == null) return;

        lineRenderer.positionCount = arcSegments + 1;

        // 円弧の開始角度と終了角度を計算
        // angleDegを中心に、arcAngleSpanの半分ずつ左右に広げる
        float startAngle = angleDeg - arcAngleSpan / 2f;
        float endAngle = angleDeg + arcAngleSpan / 2f;
        float angleStep = arcAngleSpan / arcSegments;

        for (int i = 0; i <= arcSegments; i++)
        {
            float angle = startAngle + (angleStep * i);
            float rad = angle * Mathf.Deg2Rad;
            float x = radius * Mathf.Cos(rad);
            float y = radius * Mathf.Sin(rad);

            lineRenderer.SetPosition(i, new Vector3(x, y, 0));
        }
    }

    /// <summary>
    /// 判定ライン到達時の処理（非表示にして削除タイミングを設定）
    /// </summary>
    /// <param name="currentBeat">現在のビート</param>
    /// <param name="delayInBeats">削除までの待機拍数</param>
    public void OnReachedJudgmentLine(float currentBeat, float delayInBeats)
    {
        reachedJudgmentLine = true;
        deleteBeat = currentBeat + delayInBeats;

        // 見た目を非表示に
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    /// <summary>
    /// 削除すべきかどうかを判定
    /// </summary>
    /// <param name="currentBeat">現在のビート</param>
    /// <returns>削除すべきならtrue</returns>
    public bool ShouldBeDeleted(float currentBeat)
    {
        // 削除タイミングが設定されていて、かつ現在のビートがそれを超えていたらtrue
        return deleteBeat >= 0 && currentBeat >= deleteBeat;
    }

    /// <summary>
    /// プールに返却
    /// </summary>
    public void ReturnToPool()
    {
        if (_managedPool != null)
        {
            _managedPool.Release(this);
        }
    }

    // 判定関連のメソッド
    public bool IsJudged()
    {
        return _isJudged;
    }

    public void SetJudged(bool judged)
    {
        _isJudged = judged;
    }

    public float GetTargetBeat()
    {
        return targetBeat;
    }

    public float GetAngleDeg()
    {
        return angleDeg;
    }

    /// <summary>
    /// 円弧の開始角度を取得
    /// </summary>
    public float GetStartAngleDeg()
    {
        return angleDeg - arcAngleSpan / 2f;
    }

    /// <summary>
    /// 円弧の終了角度を取得
    /// </summary>
    public float GetEndAngleDeg()
    {
        return angleDeg + arcAngleSpan / 2f;
    }

    /// <summary>
    /// 円弧の角度幅を設定
    /// </summary>
    public void SetArcAngleSpan(float span)
    {
        arcAngleSpan = span;
    }

    /// <summary>
    /// 現在の半径を取得（判定用）
    /// </summary>
    public float GetCurrentRadius(float currentTotalBeat)
    {
        // 進行度を計算
        float progress = 1.0f - (targetBeat - currentTotalBeat) / appearTime;
        progress = Mathf.Clamp01(progress);

        // 現在の半径
        return Mathf.Lerp(startRadius, targetRadius, progress);
    }

    /// <summary>
    /// 目標半径を取得
    /// </summary>
    public float GetTargetRadius()
    {
        return targetRadius;
    }

    // デバッグ用：targetBeatをオブジェクトのそばに表示
    private void OnGUI()
    {
        if (Camera.main == null) return;

        // ワールド座標をスクリーン座標に変換
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);

        // スクリーン座標系ではY軸が反転しているので補正
        screenPos.y = Screen.height - screenPos.y;

        // オブジェクトの少し上に表示するためのオフセット
        screenPos.y -= 30;

        // 文字色を黒に設定
        Color originalColor = GUI.color;
        GUI.color = Color.black;

        // targetBeatの値を表示
        string label = $"Beat: {targetBeat:F2}\nAngle: {angleDeg:F0}°";
        GUI.Label(new Rect(screenPos.x - 50, screenPos.y, 100, 40), label);

        // 色を元に戻す
        GUI.color = originalColor;
    }
}
