using UnityEngine;

/// <summary>
/// 判定ポイントのコントローラー
/// マウス位置に基づいてリング上を移動する
/// </summary>
public class JudgmentPointController : MonoBehaviour
{
    [Header("Ring Settings")]
    [SerializeField] private float ringRadius = 5.0f;  // リングの半径

    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    // 現在の角度（度数法、0度=右、時計回りで増加）
    private float currentAngleDeg = 0f;

    void Update()
    {
        UpdatePositionFromMouse();
    }

    /// <summary>
    /// マウス位置からリング上の位置を計算して更新
    /// </summary>
    void UpdatePositionFromMouse()
    {
        // マウスのワールド座標を取得
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        // 画面中央（リングの中心）からマウスへの角度を計算
        // Atan2は-180～180度を返すので、0～360度に変換
        currentAngleDeg = Mathf.Atan2(mouseWorldPos.y, mouseWorldPos.x) * Mathf.Rad2Deg;

        // 0～360度の範囲に正規化
        if (currentAngleDeg < 0)
        {
            currentAngleDeg += 360f;
        }

        // リング上の位置を計算（極座標 → デカルト座標）
        float rad = currentAngleDeg * Mathf.Deg2Rad;
        float x = ringRadius * Mathf.Cos(rad);
        float y = ringRadius * Mathf.Sin(rad);

        // 判定ポイントの位置を更新
        transform.position = new Vector3(x, y, 0);

        // スプライトの回転を角度に合わせる（オプション）
        transform.rotation = Quaternion.Euler(0, 0, currentAngleDeg);
    }

    /// <summary>
    /// 現在の角度を取得（度数法）
    /// </summary>
    public float GetCurrentAngleDeg()
    {
        return currentAngleDeg;
    }

    /// <summary>
    /// リング半径を設定
    /// </summary>
    public void SetRingRadius(float radius)
    {
        ringRadius = radius;
    }

    /// <summary>
    /// 指定された角度との差分を計算（-180～180度の範囲）
    /// </summary>
    public float GetAngleDifference(float targetAngleDeg)
    {
        return Mathf.DeltaAngle(currentAngleDeg, targetAngleDeg);
    }

    /// <summary>
    /// 指定された角度範囲内にあるかをチェック
    /// </summary>
    public bool IsWithinAngleRange(float targetAngleDeg, float rangeInDeg)
    {
        float diff = Mathf.Abs(Mathf.DeltaAngle(currentAngleDeg, targetAngleDeg));
        return diff <= rangeInDeg;
    }

    /// <summary>
    /// 判定ポイントの表示/非表示を設定
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = visible;
        }
    }
}
