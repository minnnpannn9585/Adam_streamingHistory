using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }
    private Coroutine autoEndCoroutine;   // 自动结束游戏的协程
    [Header("血量设置")]
    public int maxHealth = 100;          // 队伍最大血量
    public float baseDamage = 20f;       // 基础伤害系数 k
    public float timeLimitPerQuestion = 120f; // 每题时间限制（秒）

    [Header("引用")]
    public UIManager uiManager;           

    // 事件：血量变化时触发，供 UIManager 订阅
    public event Action<int, int> OnHealthChanged; // 参数：红队血量，蓝队血量
    private bool _isWaitingForNextQuestion;  // 防止多个下一题请求同时等待
    // 当前状态
    public bool IsGameActive { get; private set; }
    public int CurrentQuestionIndex { get; private set; } // 1-based
    public int TotalQuestions { get; private set; }

    private int redHealth;
    private int blueHealth;

    private float questionStartTime;      // 当前题开始时间（Time.time）
    private bool hasFirstAnswerInCurrentQuestion; // 当前题是否已有首个答对
    private List<QuestionData> questions;   // 存储题目列表
    private Coroutine countdownCoroutine;   // 倒计时协程引用

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        ResetGame();
    }
    public void ResetGameState()
    {
        // 停止所有协程
        StopCountdown();
        if (autoEndCoroutine != null)
        {
            StopCoroutine(autoEndCoroutine);
            autoEndCoroutine = null;
        }

        // 重置标志
        IsGameActive = false;
        CurrentQuestionIndex = 0;
        TotalQuestions = 0;
        redHealth = maxHealth;
        blueHealth = maxHealth;
        hasFirstAnswerInCurrentQuestion = false;
        _isWaitingForNextQuestion = false;
        questions?.Clear();

        // 重置UI
        OnHealthChanged?.Invoke(redHealth, blueHealth);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateLeaderboard(new List<UIManager.LeaderboardEntry>());
            UIManager.Instance.SetSubjectText("");
            UIManager.Instance.UpdateTimerDisplay(0);
        }
    }
    // 重置游戏
    private void ResetGame()
    {
        IsGameActive = false;
        CurrentQuestionIndex = 0;
        TotalQuestions = 0;
        redHealth = maxHealth;
        blueHealth = maxHealth;
        hasFirstAnswerInCurrentQuestion = false;
        _isWaitingForNextQuestion = false;            // 新增
        StopCountdown();
        OnHealthChanged?.Invoke(redHealth, blueHealth);
    }

    // 由 GameApiDemoUI 在开始游戏成功后调用
    public void GameStarted(string roundId, int questionsCount)
    {
        ResetGameState();
        IsGameActive = true;
        TotalQuestions = questionsCount;
        CurrentQuestionIndex = 1;          // 从第一题开始
        redHealth = maxHealth;
        blueHealth = maxHealth;
        hasFirstAnswerInCurrentQuestion = false;
        questionStartTime = Time.time;
        OnHealthChanged?.Invoke(redHealth, blueHealth);
       // uiManager.roundText.text = $"Round {roundId}";
        uiManager.QUESTIONText.text = $"QUESTION 1";

        // 启动倒计时
        StartCountdown();

        // 如果已经有题目列表，更新题目文本
        if (questions != null && questions.Count > 0)
        {
            UpdateCurrentQuestionText();
        }

        Debug.Log($"[Flow] 游戏开始，共 {questionsCount} 题");
    }

    // 存储题目
    public void SetQuestions(List<QuestionData> questionsList)
    {
        questions = questionsList;
        if (IsGameActive && CurrentQuestionIndex >= 1 && CurrentQuestionIndex <= questions.Count)
        {
            UpdateCurrentQuestionText();
        }
    }

    private void UpdateCurrentQuestionText()
    {
        if (questions != null && CurrentQuestionIndex >= 1 && CurrentQuestionIndex <= questions.Count)
        {
            string text = questions[CurrentQuestionIndex - 1].question;
            if (!string.IsNullOrEmpty(text) && UIManager.Instance != null)
            {
                UIManager.Instance.SetSubjectText(text);
            }
        }
    }

    // 由 GameApiDemoUI 在下一题成功后调用
    public void OnNextQuestion(int? nextIndex)
    {
        // 重置等待标志（因为已经进入下一题）
        _isWaitingForNextQuestion = false;

        if (!IsGameActive) return;

        if (nextIndex.HasValue)
        {
            CurrentQuestionIndex = nextIndex.Value;
            hasFirstAnswerInCurrentQuestion = false;
            questionStartTime = Time.time;
            UpdateCurrentQuestionText();

            // 重启倒计时
            StartCountdown();
            uiManager.QUESTIONText.text = $"QUESTION {CurrentQuestionIndex}";
            uiManager.SubjectImage.sprite = uiManager.list[CurrentQuestionIndex - 1];
            uiManager.roundText.text = $"ROUND {CurrentQuestionIndex}";
            Debug.Log($"[Flow] 进入第 {CurrentQuestionIndex} 题");
        }
        else
        {
            // 没有下一题，游戏结束
            Debug.Log("[Flow] 题目已用完，进行结算");
            CheckGameOver();
        }
    }

    //  GameApiDemoUI 在游戏结束后调用
    public void GameEnded()
    {

        IsGameActive = false;
        StopCountdown();
        _isWaitingForNextQuestion = false;          

        if (autoEndCoroutine != null)
        {
            StopCoroutine(autoEndCoroutine);
            autoEndCoroutine = null;
        }

        ResetGame();
        GameApiDemoUI.Instance.SetButtonsInteractable(false);
       
        GameApiDemoUI.Instance.btnStart.interactable = true;
        Debug.Log("[Flow] 游戏已结束");
        GameApiDemoUI.Instance.OnStartGame();
    }
    // 模拟用户答对（由模拟按钮调用）
    // team: "red" 或 "blue"
    // userName: 答对用户名（用于显示）
    // 模拟用户答对（由模拟按钮调用）
    public void OnAnswerCorrect(string team, string userName)
    {
        if (!IsGameActive)
        {
            Debug.LogWarning("[Flow] 游戏未激活，忽略答题");
            return;
        }

        // 构造一个假的 AnswerResultMessage
        var fakeMsg = new AnswerResultMessage
        {
            type = "answer_result",
            userId = userName,
            team = team,
            first = !hasFirstAnswerInCurrentQuestion, // 如果当前题还没有首答，则本次模拟算作首答
            questionIndex = CurrentQuestionIndex,
            teamScores = null, // 模拟时不需要
            userAnswer = "模拟答题" // 随意
        };

        // 调用统一处理
        OnAnswerResult(fakeMsg);
    }

    // 无人答对（由模拟按钮调用）
    public void OnNoAnswer()
    {
        if (!IsGameActive) return;

        // 如果已经在等待下一题，则忽略本次无人答对
        if (_isWaitingForNextQuestion)
        {
            Debug.LogWarning("[Flow] 已有下一题请求等待中，忽略无人答对");
            return;
        }

        // 停止倒计时
        StopCountdown();

        uiManager.ShowMessage("无人答对题目，进入下一题", 2f);

        _isWaitingForNextQuestion = true;
        StartCoroutine(DelayedNextQuestion(3f));
    }

    // ========== 倒计时相关 ==========
    private void StartCountdown()
    {
        // 停止之前的倒计时协程
        StopCountdown();

        if (!IsGameActive) return;

        countdownCoroutine = StartCoroutine(CountdownCoroutine());
    }

    private void StopCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
    }

    private IEnumerator CountdownCoroutine()
    {
        float remaining = timeLimitPerQuestion;

        // 倒计时循环，每秒更新 UI
        while (remaining > 0 && IsGameActive && !hasFirstAnswerInCurrentQuestion)
        {
            // 更新 UI 显示剩余时间
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateTimerDisplay(remaining);
            }

            yield return new WaitForSeconds(1f);

            // 重新计算剩余时间（防止因协程延迟导致误差）
            remaining = timeLimitPerQuestion - (Time.time - questionStartTime);
        }

        // 超时处理：如果仍然没有首答，且游戏仍然激活
        if (IsGameActive && !hasFirstAnswerInCurrentQuestion && remaining <= 0)
        {
            Debug.Log($"[Flow] 第 {CurrentQuestionIndex} 题超时，无人答对");
            uiManager.ShowMessage($"第 {CurrentQuestionIndex} 题超时，进入下一题", 2f);
            OnNoAnswer();
        }
        else
        {
            // 正常退出（例如已有首答），无需额外操作
            countdownCoroutine = null;
        }
    }

    private IEnumerator DelayedNextQuestion(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 调用下一题接口
        HttpCenter.Instance.PostAsync("/game/next-question", null,
            (response) => {
                var json = JObject.Parse(response);
                var nextIndex = json["nextIndex"]?.Value<int?>();
                OnNextQuestion(nextIndex);
                _isWaitingForNextQuestion = false;   // 请求完成，重置标志
            },
            (error) => {
                Debug.LogError("下一题失败：" + error);
                _isWaitingForNextQuestion = false;   // 请求失败也要重置
            });
    }

    // 检查游戏是否结束
    private void CheckGameOver()
    {
        if (!IsGameActive) return;

        string message;
        if (redHealth <= 0 && blueHealth <= 0)
            message = "平局！双方同时倒下";
        else if (redHealth <= 0)
            message = "蓝队胜利！";
        else if (blueHealth <= 0)
            message = "红队胜利！";
        else if (CurrentQuestionIndex >= TotalQuestions)
        {
            if (redHealth > blueHealth)
                message = "红队胜利！";
            else if (blueHealth > redHealth)
                message = "蓝队胜利！";
            else
                message = "平局！";
        }
        else
            return;

        // 显示结束面板
        uiManager.ShowGameOver(message);

        // 重置等待标志（游戏结束，不应再发起下一题）
        _isWaitingForNextQuestion = false;

        // 启动自动结束协程（10秒后调用结束游戏）
        if (autoEndCoroutine != null)
            StopCoroutine(autoEndCoroutine);
        autoEndCoroutine = StartCoroutine(AutoEndGameAfterDelay(10f));
    }
    private IEnumerator AutoEndGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        _isWaitingForNextQuestion = false;          

        HttpCenter.Instance.PostAsync("/game/end", null,
            (response) =>
            {
                Debug.Log("[Flow] 自动结束游戏成功");
                GameEnded();
                if (uiManager != null)
                    uiManager.HideInfoPanel();
            },
            (error) =>
            {
                Debug.LogError("[Flow] 自动结束游戏失败：" + error);
            });
    }
    // 刷新排行榜（调用 API 并更新 UI）
    private void RefreshLeaderboard()
    {
        if (!IsGameActive) return;
        HttpCenter.Instance.GetAsync("/game/leaderboard", (response) =>
        {
            UpdateLeaderboardFromJson(response);
        }, (error) => { Debug.LogError("刷新排行榜失败：" + error); });
    }
    /// <summary>
    /// 处理来自 WebSocket 的答对结果（包括模拟按钮构造的假消息）
    /// </summary>
    public void OnAnswerResult(AnswerResultMessage msg)
    {
        if (msg.first && hasFirstAnswerInCurrentQuestion) return;
        if (!IsGameActive)
        {
            Debug.LogWarning("[Flow] 游戏未激活，忽略答题结果");
            return;
        }

        // 显示弹幕
        if (DanmakuManager.Instance != null)
        {
            string displayName = msg.userId;
           // DanmakuManager.Instance.AddDanmaku(displayName, msg.userAnswer);
        }
       
        if (msg.first&& msg.questionIndex == CurrentQuestionIndex)
        {
            // 如果已经在等待下一题，则忽略本次首答（避免重复调用）
            if (_isWaitingForNextQuestion)
            {
                Debug.LogWarning("[Flow] 已有下一题请求等待中，忽略本次首答");
                return;
            }

            // 首个答对：停止倒计时，扣血，显示消息，延迟下一题
            StopCountdown();
            hasFirstAnswerInCurrentQuestion = true;

            // 计算伤害
            float elapsed = Time.time - questionStartTime;
            float tRatio = Mathf.Clamp01(elapsed / timeLimitPerQuestion);
            float damage = baseDamage * Mathf.Pow(1 - tRatio, 3);
            int damageInt = Mathf.RoundToInt(damage);

            // 扣减对方血量
            if (msg.team == "red")
            {
                blueHealth = Mathf.Max(0, blueHealth - damageInt);
                OnHealthChanged?.Invoke(redHealth, blueHealth);
                uiManager.ShowMessage($"观众 {msg.userId}（红队）答对！对蓝队造成 {damageInt} 点伤害！", 3f);
            }
            else if (msg.team == "blue")
            {
                redHealth = Mathf.Max(0, redHealth - damageInt);
                OnHealthChanged?.Invoke(redHealth, blueHealth);
                uiManager.ShowMessage($"观众 {msg.userId}（蓝队）答对了！对红队造成 {damageInt} 点伤害！", 3f);
            }
            else
            {
                Debug.LogWarning($"未知队伍: {msg.team}");
            }

            // 检查游戏是否结束（血量归零）
            if (redHealth <= 0 || blueHealth <= 0)
            {
                CheckGameOver();
                return;
            }

            // 启动延迟下一题（设置标志）
            _isWaitingForNextQuestion = true;
            StartCoroutine(DelayedNextQuestion(0));
        }
        else
        {
            // 非首个答对：仅显示提示
            uiManager.ShowMessage($"观众 {msg.userId}（{msg.team}队）答对了，但不是首个！", 2f);
        }

        // 刷新排行榜
        RefreshLeaderboard();
    }
    private void UpdateLeaderboardFromJson(string json)
    {
        var data = JObject.Parse(json);
        var users = data["users"] as JArray;
        if (users == null) return;

        var entries = new List<UIManager.LeaderboardEntry>();
        foreach (var u in users)
        {
            entries.Add(new UIManager.LeaderboardEntry
            {
                userId = u["userId"]?.ToString(),
                name = null, // 名字由 UIManager 从字典查找
                score = u["score"]?.Value<int>() ?? 0,
                team = u["team"]?.ToString()
            });
        }
        UIManager.Instance.UpdateLeaderboard(entries);
    }
}