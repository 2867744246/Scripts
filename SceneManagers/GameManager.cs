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
        {
            Instance = this;
            return;
        }

        if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 查找场景中的玩家车辆
        carController = FindObjectOfType<CarController>();
        if (carController != null)
        {
            startPositionX = carController.transform.position.x;
            // 订阅碰撞事件
            carController.onCollision += OnPlayerCollision;
        }
        else
        {
            Debug.LogWarning("GameManager: 场景中未找到 CarController。");
        }
    }

    private void Update()
    {
        if (currentState == GameState.Playing)
        {
            UpdateScore();
            CheckPauseInput();
        }
    }

    private void OnDestroy()
    {
        // 取消碰撞事件订阅
        if (carController != null)
        {
            carController.onCollision -= OnPlayerCollision;
        }
    }

    /// <summary>
    /// 更新分数，基于车辆行驶距离累加。
    /// </summary>
    private void UpdateScore()
    {
        if (carController == null) return;

        float currentX = carController.transform.position.x;
        distanceTraveled = Mathf.Max(0f, currentX - startPositionX);
        currentScore = distanceTraveled * scoreMultiplier;
    }

    /// <summary>
    /// 检测暂停输入（Esc 键）。
    /// </summary>
    private void CheckPauseInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PauseGame();
        }
    }

    /// <summary>
    /// 玩家车辆发生碰撞时触发结算。
    /// </summary>
    private void OnPlayerCollision()
    {
        if (currentState == GameState.Playing)
        {
            TriggerGameOver();
        }
    }

    /// <summary>
    /// 暂停游戏。
    /// </summary>
    public void PauseGame()
    {
        if (currentState != GameState.Playing) return;

        currentState = GameState.Paused;
        Time.timeScale = 0f;
        ShowPausePanel();
    }

    /// <summary>
    /// 恢复游戏。
    /// </summary>
    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;

        currentState = GameState.Playing;
        Time.timeScale = 1f;
        DestroyCurrentPanel();
    }

    /// <summary>
    /// 触发游戏结算。
    /// </summary>
    private void TriggerGameOver()
    {
        currentState = GameState.GameOver;
        Time.timeScale = 0f;
        ShowGameOverPanel();
    }

    /// <summary>
    /// 重新开始游戏（重载当前场景）。
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// 返回主界面。
    /// </summary>
    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(startSceneName);
    }

    /// <summary>
    /// 销毁当前动态面板。
    /// </summary>
    private void DestroyCurrentPanel()
    {
        if (uiCanvas != null)
        {
            Destroy(uiCanvas);
            uiCanvas = null;
        }
        currentPanel = null;
    }

    #region 动态 UI 生成

    /// <summary>
    /// 显示暂停面板。
    /// </summary>
    private void ShowPausePanel()
    {
        CreateUICanvas();
        currentPanel = CreatePanelBase("游戏暂停", 350f, 340f);

        // 继续游戏按钮
        CreateButton(currentPanel.transform, "继续游戏", new Vector2(0, 60f), () => ResumeGame());

        // 重新开始按钮
        CreateButton(currentPanel.transform, "重新开始", new Vector2(0, -30f), () =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        });

        // 返回主界面按钮
        CreateButton(currentPanel.transform, "返回主界面", new Vector2(0, -120f), ReturnToMainMenu);
    }

    /// <summary>
    /// 显示结算面板。
    /// </summary>
    private void ShowGameOverPanel()
    {
        CreateUICanvas();
        currentPanel = CreatePanelBase("游戏结束", 350f, 380f);

        // 显示得分
        string scoreStr = $"得分：{currentScore:F0}";
        CreateText(currentPanel.transform, scoreStr, 32, new Vector2(0, 30f));

        // 显示行驶距离
        string distanceStr = $"行驶距离：{distanceTraveled:F1} m";
        CreateText(currentPanel.transform, distanceStr, 22, new Vector2(0, -20f), new Color(0.8f, 0.8f, 0.8f, 1f));

        // 重新开始按钮
        CreateButton(currentPanel.transform, "重新开始", new Vector2(0, -90f), () =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        });

        // 返回主界面按钮
        CreateButton(currentPanel.transform, "返回主界面", new Vector2(0, -170f), ReturnToMainMenu);
    }

    /// <summary>
    /// 创建 UI Canvas，全屏覆盖。
    /// </summary>
    private void CreateUICanvas()
    {
        if (uiCanvas != null) return;

        uiCanvas = new GameObject("DynamicUICanvas");
        Canvas canvas = uiCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // 确保在最上层

        CanvasScaler scaler = uiCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        uiCanvas.AddComponent<GraphicRaycaster>();

        // 全屏半透明遮罩
        GameObject overlay = new GameObject("Overlay");
        overlay.transform.SetParent(uiCanvas.transform, false);
        Image overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.6f);
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// 创建面板基础背景与标题。
    /// </summary>
    /// <param name="title">面板标题。</param>
    /// <param name="width">面板宽度。</param>
    /// <param name="height">面板高度。</param>
    /// <returns>面板根 GameObject。</returns>
    private GameObject CreatePanelBase(string title, float width, float height)
    {
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(uiCanvas.transform, false);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(width, height);
        panelRect.anchoredPosition = Vector2.zero;

        // 标题文字
        CreateText(panel.transform, title, 40, new Vector2(0, height * 0.5f - 55f));

        return panel;
    }

    /// <summary>
    /// 创建 TMP 文本。
    /// </summary>
    /// <param name="parent">父节点 Transform。</param>
    /// <param name="text">文本内容。</param>
    /// <param name="fontSize">字号。</param>
    /// <param name="anchoredPosition">相对锚点的位置。</param>
    /// <param name="color">文本颜色。</param>
    /// <returns>创建的 TextMeshProUGUI 组件。</returns>
    private TextMeshProUGUI CreateText(Transform parent, string text, float fontSize, Vector2 anchoredPosition, Color? color = null)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = fontSize;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = color ?? Color.white;
        if (panelFont != null)
        {
            tmpText.font = panelFont;
        }

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(300f, 60f);
        textRect.anchoredPosition = anchoredPosition;

        return tmpText;
    }

    /// <summary>
    /// 创建按钮。
    /// </summary>
    /// <param name="parent">父节点 Transform。</param>
    /// <param name="label">按钮文字。</param>
    /// <param name="anchoredPosition">相对锚点的位置。</param>
    /// <param name="onClick">点击回调。</param>
    /// <returns>创建的 Button 组件。</returns>
    private Button CreateButton(Transform parent, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = new GameObject("Button_" + label);
        buttonObj.transform.SetParent(parent, false);

        Image btnImage = buttonObj.AddComponent<Image>();
        btnImage.color = new Color(0.9f, 0.55f, 0.2f, 1f);

        Button button = buttonObj.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        RectTransform btnRect = buttonObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.sizeDelta = new Vector2(220f, 50f);
        btnRect.anchoredPosition = anchoredPosition;

        // 按钮文字
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(buttonObj.transform, false);

        TextMeshProUGUI btnLabel = labelObj.AddComponent<TextMeshProUGUI>();
        btnLabel.text = label;
        btnLabel.fontSize = 24;
        btnLabel.alignment = TextAlignmentOptions.Center;
        btnLabel.color = Color.white;
        if (buttonFont != null)
        {
            btnLabel.font = buttonFont;
        }
        else if (panelFont != null)
        {
            btnLabel.font = panelFont;
        }

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return button;
    }

    #endregion
}