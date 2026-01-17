using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 円弧状のノートコントローラー
/// 画面中央から放射状に外側へ移動するノート
/// </summary>
public class ArcNoteController : MonoBehaviour
{
    // 自身の所属するプールを覚えておく変数
    private IObjectPool<ArcNoteController> _managedPool;

    // ノートの移動に関する変数
    private float targetBeat;           // 目標ビート（判定タイミング）
    private float appearTime;           // 出現から判定までの拍数
    private float angleDeg;             // ノートの角度（度数法）
    private float targetRadius;         // 目標半径（判定ライン）
    private float startRadius = 0.0f;   // 開始半径（中心）
    private bool _isJudged = false;     // 判定済みかどうか

    // 削除タイミング管理
    private bool reachedJudgmentLine = false;  // 判定ラインに到達したか
    private float deleteBeat = -1f;            // 削除すべきビート

    // ビジュアル管理
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        // SpriteRendererを取得
        spriteRenderer = GetComponent<SpriteRenderer>();
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
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        // 角度を元に回転をセット（スプライトの向きを合わせる）
        transform.rotation = Quaternion.Euler(0, 0, angleDeg);

        // 初期位置を中心にセット
        transform.position = Vector3.zero;
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

        // 極座標をXY座標に変換
        // x = r * cos(θ), y = r * sin(θ)
        float rad = angleDeg * Mathf.Deg2Rad;
        float x = currentRadius * Mathf.Cos(rad);
        float y = currentRadius * Mathf.Sin(rad);

        transform.position = new Vector3(x, y, 0);
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
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
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
