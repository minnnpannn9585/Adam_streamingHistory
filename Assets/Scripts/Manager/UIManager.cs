using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("血条 (Image 类型，宽度满值 500)")]
    public Image redHealthImage;
    public Image blueHealthImage;

    [Header("倒计时文本")]
    public Text timerText;                 // 显示剩余时间，格式 "00:30"
    [Header("回合文本")]
    public Text roundText; 
    [Header("当前题目文本")]
    public Text QUESTIONText;


    [Header("信息面板（复用）")]
    public GameObject infoPanel;           // 统一信息面板
    public Text infoText;                  // 面板上的文字
    public Button infoCloseButton;         // 关闭按钮（仅在游戏结束时需要显示）

    [Header("排行榜面板")]
    public Transform LeaderboardPanel;      // 排行榜容器（第一个子对象为标题，不可动）
    public GameObject LeaderboardPrefab;    // 预制体：三个子对象（Image, Name, Score）

    [Header("题目显示文本")]
    public Text SubjectText;
    public Image SubjectImage;

    [Header("队伍颜色")]
    public Color redTeamColor = Color.red;
    public Color blueTeamColor = Color.blue;

    public List<Sprite> list;
    // 记录血条原始宽度（满血时宽度）
    private float originalRedWidth;
    private float originalBlueWidth;

    private Coroutine autoHideCoroutine;   // 自动隐藏协程引用

    // 用户名字典（userId -> name），由外部调用 UpdateUserNames 更新
    private Dictionary<string, string> userNameDict = new Dictionary<string, string>();

    // 打字机协程
    private Coroutine typewriterCoroutine;

    // 排行榜条目数据结构（供外部传入）
    public class LeaderboardEntry
    {
        public string userId;
        public string name;      // 显示时优先用此字段，若为空则用 userId
        public int score;
        public string team;      // "red" 或 "blue"
    }
    public void Close()
    {
        Application.Quit();
    }
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.OnHealthChanged += UpdateHealthBars;

        // 记录血条初始宽度
        if (redHealthImage != null)
            originalRedWidth = redHealthImage.rectTransform.rect.width;
        if (blueHealthImage != null)
            originalBlueWidth = blueHealthImage.rectTransform.rect.width;

        // 初始化信息面板：隐藏 + 按钮监听
        if (infoPanel != null)
            infoPanel.SetActive(false);
        if (infoCloseButton != null)
            infoCloseButton.onClick.AddListener(HideInfoPanel);

        // 初始化排行榜：清除除第一个子对象外的所有条目
        ClearLeaderboard();
    }

    private void OnDestroy()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.OnHealthChanged -= UpdateHealthBars;
    }

    // ---------- 血条更新 ----------
    private void UpdateHealthBars(int redHealth, int blueHealth)
    {
        if (redHealthImage != null)
        {
            float redPercent = (float)redHealth / GameFlowManager.Instance.maxHealth;
            float newRedWidth = originalRedWidth * redPercent;
            SetImageWidth(redHealthImage, newRedWidth);
        }

        if (blueHealthImage != null)
        {
            float bluePercent = (float)blueHealth / GameFlowManager.Instance.maxHealth;
            float newBlueWidth = originalBlueWidth * bluePercent;
            SetImageWidth(blueHealthImage, newBlueWidth);
        }
    }

    private void SetImageWidth(Image img, float width)
    {
        RectTransform rt = img.rectTransform;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }

    // ---------- 倒计时显示 ----------
    public void UpdateTimerDisplay(float remainingSeconds)
    {
        if (timerText == null) return;

        // 确保剩余时间不小于0
        if (remainingSeconds < 0) remainingSeconds = 0;

        int minutes = Mathf.FloorToInt(remainingSeconds / 60);
        int seconds = Mathf.FloorToInt(remainingSeconds % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    // 修改 SetSubjectText 方法
    public void SetSubjectText(string text)
    {
        if (SubjectText == null) return;

        // 如果已有打字机协程在运行，先停止
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        // 启动新的打字机效果协程
        typewriterCoroutine = StartCoroutine(TypewriterEffect(text));
    }

    private IEnumerator TypewriterEffect(string fullText)
    {
        SubjectText.text = "";
        foreach (char c in fullText)
        {
            SubjectText.text += c;
            yield return new WaitForSeconds(0.01f);
        }
        typewriterCoroutine = null; // 完成后置空
    }

    // ---------- 用户名字典更新 ----------
    /// <summary>
    /// 更新用户 ID 到名字的映射（在获取队伍列表后调用）
    /// </summary>
    public void UpdateUserNames(List<(string userId, string name, string team)> users)
    {
        foreach (var u in users)
        {
            if (!userNameDict.ContainsKey(u.userId))
                userNameDict.Add(u.userId, u.name);
            else
                userNameDict[u.userId] = u.name;
        }
    }

    // ---------- 排行榜更新 ----------
    /// <summary>
    /// 更新排行榜显示
    /// </summary>
    /// <param name="entries">按分数降序排列的条目列表（如未排序，内部会重新排序）</param>
    public void UpdateLeaderboard(List<LeaderboardEntry> entries)
    {
        // 清除现有条目（保留第一个子对象）
        ClearLeaderboard();

        if (entries == null || entries.Count == 0) return;

        // 按分数降序排序（确保最高分在前）
        var sorted = entries.OrderByDescending(e => e.score).ToList();

        foreach (var entry in sorted)
        {
            // 实例化预制体
            GameObject item = Instantiate(LeaderboardPrefab, LeaderboardPanel);
            item.transform.SetAsLastSibling(); // 添加到末尾，保持顺序

            // 获取三个子对象（假设预制体结构固定：第0子对象是Image，第1是Name Text，第2是Score Text）
            Transform imgTransform = item.transform.GetChild(0);
            Transform nameTransform = item.transform.GetChild(1);
            Transform scoreTransform = item.transform.GetChild(2);

            // 设置队伍颜色
            Image teamImage = imgTransform.GetComponent<Image>();
            if (teamImage != null)
            {
                teamImage.color = entry.team == "red" ? redTeamColor : blueTeamColor;
            }

            // 设置玩家名字
            Text nameText = nameTransform.GetComponent<Text>();
            if (nameText != null)
            {
                // 优先使用传入的 name，若为空则从字典查找，若仍未找到则显示 userId
                string displayName = entry.name;
                if (string.IsNullOrEmpty(displayName) && userNameDict.ContainsKey(entry.userId))
                    displayName = userNameDict[entry.userId];
                if (string.IsNullOrEmpty(displayName))
                    displayName = entry.userId;
                nameText.text = displayName;
            }

            // 设置分数
            Text scoreText = scoreTransform.GetComponent<Text>();
            if (scoreText != null)
            {
                scoreText.text = entry.score.ToString();
            }
        }
    }

    /// <summary>
    /// 清除排行榜除第一个子对象（标题）外的所有条目
    /// </summary>
    private void ClearLeaderboard()
    {
        if (LeaderboardPanel == null) return;

        // 从后往前删除，避免索引变化
        for (int i = LeaderboardPanel.childCount - 1; i >= 1; i--)
        {
            Destroy(LeaderboardPanel.GetChild(i).gameObject);
        }
    }

    // ---------- 信息面板复用方法 ----------
    public void ShowMessage(string msg, float duration)
    {
        if (autoHideCoroutine != null)
            StopCoroutine(autoHideCoroutine);

        infoPanel.SetActive(true);
        infoText.text = msg;
        if (infoCloseButton != null)
            infoCloseButton.gameObject.SetActive(false);

        autoHideCoroutine = StartCoroutine(AutoHidePanel(duration));
    }

    private IEnumerator AutoHidePanel(float duration)
    {
        yield return new WaitForSeconds(duration);
        infoPanel.SetActive(false);
        autoHideCoroutine = null;
    }

    public void ShowGameOver(string msg)
    {
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }

        infoPanel.SetActive(true);
        infoText.text = msg;
        if (infoCloseButton != null)
            infoCloseButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// 由WebSocket推送调用，直接更新队伍分数并同步血条
    /// </summary>
    /// <param name="redScore">红队本轮得分（答对题数）</param>
    /// <param name="blueScore">蓝队本轮得分（答对题数）</param>
    public void UpdateTeamScores(int redScore, int blueScore)
    {
        // 调用现有的血条更新逻辑，将分数作为"血量"传递
        UpdateHealthBars(redScore, blueScore);
    }

    public void HideInfoPanel()
    {
        infoPanel.SetActive(false);
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
    }
}