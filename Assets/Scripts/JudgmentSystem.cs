using UnityEngine;

/// <summary>
/// リズムゲームの判定結果
/// </summary>
public enum JudgmentResult
{
    None,
    Perfect,
    Great,
    Good,
    Bad,
    Miss
}

/// <summary>
/// ノートの判定システム
/// ビート差に基づいてタイミング精度を評価
/// </summary>
public class JudgmentSystem : MonoBehaviour
{
    [Header("Judgment Timing Windows (in beats)")]
    [Tooltip("Perfect判定のタイミング許容範囲（拍数）")]
    [SerializeField] private float perfectWindow = 0.05f;

    [Tooltip("Great判定のタイミング許容範囲（拍数）")]
    [SerializeField] private float greatWindow = 0.1f;

    [Tooltip("Good判定のタイミング許容範囲（拍数）")]
    [SerializeField] private float goodWindow = 0.2f;

    [Tooltip("Bad判定のタイミング許容範囲（拍数）")]
    [SerializeField] private float badWindow = 0.3f;

    [Header("Judgment Feedback")]
    [Tooltip("最新の判定結果を表示")]
    [SerializeField] private JudgmentResult lastJudgment = JudgmentResult.None;

    /// <summary>
    /// ノートのタイミングを判定
    /// </summary>
    /// <param name="noteBeat">ノートの目標ビート</param>
    /// <param name="currentBeat">現在のビート</param>
    /// <returns>判定結果</returns>
    public JudgmentResult Judge(float noteBeat, float currentBeat)
    {
        float beatDifference = Mathf.Abs(noteBeat - currentBeat);

        JudgmentResult result;

        if (beatDifference <= perfectWindow)
        {
            result = JudgmentResult.Perfect;
        }
        else if (beatDifference <= greatWindow)
        {
            result = JudgmentResult.Great;
        }
        else if (beatDifference <= goodWindow)
        {
            result = JudgmentResult.Good;
        }
        else if (beatDifference <= badWindow)
        {
            result = JudgmentResult.Bad;
        }
        else
        {
            result = JudgmentResult.Miss;
        }

        lastJudgment = result;
        Debug.Log($"<color=yellow>Judgment: {result}</color> (Beat Difference: {beatDifference:F3})");

        return result;
    }

    /// <summary>
    /// 判定可能な範囲内かどうかをチェック
    /// </summary>
    /// <param name="noteBeat">ノートの目標ビート</param>
    /// <param name="currentBeat">現在のビート</param>
    /// <returns>判定可能ならtrue</returns>
    public bool IsInJudgmentRange(float noteBeat, float currentBeat)
    {
        float beatDifference = Mathf.Abs(noteBeat - currentBeat);
        return beatDifference <= badWindow;
    }

    /// <summary>
    /// ノートが判定範囲を過ぎたかどうかをチェック
    /// </summary>
    /// <param name="noteBeat">ノートの目標ビート</param>
    /// <param name="currentBeat">現在のビート</param>
    /// <returns>判定範囲を過ぎていればtrue</returns>
    public bool HasPassedJudgmentRange(float noteBeat, float currentBeat)
    {
        return (currentBeat - noteBeat) > badWindow;
    }
}
