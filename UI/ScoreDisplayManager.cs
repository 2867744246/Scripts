using TMPro;
using UnityEngine;

/// <summary>
/// 实时得分显示管理器。
/// 在游戏 HUD 上实时显示当前得分，从 GameManager 获取分数数据。
/// </summary>
public class ScoreDisplayManager : MonoBehaviour
{
    [Header("UI 引用")]
    [Tooltip("显示当前得分的 TMP 文本组件")]
    public TMP_Text scoreText;

    [Header("显示设置")]
    [Tooltip("分数文字前缀")]
    public string scorePrefix = "得分：";

    [Tooltip("小数位数")]
    public int decimalPlaces = 0;

    /// <summary>
    /// 当前显示的分数（只读，用于外部调试）。
    /// </summary>
    public float CurrentDisplayScore { get; private set; }

    private void LateUpdate()
    {
        if (scoreText == null) return;
        if (GameManager.Instance == null) return;

        CurrentDisplayScore = GameManager.Instance.CurrentScore;
        scoreText.text = scorePrefix + CurrentDisplayScore.ToString($"F{decimalPlaces}");
    }
}