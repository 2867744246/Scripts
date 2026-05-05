using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 游戏状态枚举。
/// </summary>
public enum GameState
{
    Playing,
    Paused,
    GameOver
}

/// <summary>
/// 游戏管理器 - 管理游戏生命周期。
/// 负责计分、暂停/恢复、碰撞结算、动态生成暂停与结算面板。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("计分设置")]
    [Tooltip("距离转换分数的倍率（分数 = 行驶距离 × 倍率）")]
    [SerializeField] private float scoreMultiplier = 10f;

    [Header("场景名称")]
    [Tooltip("开始界面的场景名称")]
    [SerializeField] private string startSceneName = "StartScene";

    [Header("面板样式")]
    [Tooltip("面板默认字体，为空则使用 TMP 默认字体")]
    [SerializeField] private TMP_FontAsset panelFont;

    [Tooltip("按钮字体，为空则使用面板字体")]
    [SerializeField] private TMP_FontAsset buttonFont;

    // 游戏状态
    private GameState currentState = GameState.Playing;

    // 车辆引用
    private CarController carController;

    // 计分数据
    private float startPositionX;
    private float currentScore;
    private float distanceTraveled;

    // 动态生成的 UI
    private GameObject uiCanvas;
    private GameObject currentPanel;

    /// <summary>
    /// 当前游戏状态。
    /// </summary>
    public GameState CurrentState => currentState;

    /// <summary>
    /// 当前得分（只读）。
    /// </summary>
    public float CurrentScore => currentScore;

    private void Awake()
    {
        if (Instance == null)
