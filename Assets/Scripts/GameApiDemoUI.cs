using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Net.WebSockets;
using System;
using System.Text;
using System.Threading;

// ========== WebSocket推送数据模型 ==========
[System.Serializable]
public class AnswerResultMessage
{
    public string type { get; set; }
    public string userId { get; set; }
    public string team { get; set; }
    public bool first { get; set; }
    public int questionIndex { get; set; }
    public TeamScores teamScores { get; set; }
    public string userAnswer { get; set; }
}

[System.Serializable]
public class TeamScores
{
    public int red { get; set; }
    public int blue { get; set; }
}

[System.Serializable]
public class ChatMessage
{
    public string type { get; set; }
    public string userId { get; set; }
    public string name { get; set; }
    public string text { get; set; }
    public string team { get; set; }
}

[System.Serializable]
public class UserJoinedMessage
{
    public string type { get; set; }
    public string userId { get; set; }
    public string team { get; set; }
}
[System.Serializable]
public class TeamMember
{
    public string userId { get; set; }
    public string name { get; set; }
}

[System.Serializable]
public class TeamsResponse
{
    public List<TeamMember> red { get; set; }
    public List<TeamMember> blue { get; set; }
}
// ========== 数据模型结束 ==========

[System.Serializable]
public class QuestionMeta
{
    public string category { get; set; }
    public string era { get; set; }
    public string source { get; set; }
}

[System.Serializable]
public class QuestionData
{
    public int index { get; set; }
    public string id { get; set; }
    public string question { get; set; }
    public QuestionMeta meta { get; set; }
}

[System.Serializable]
public class QuestionsResponse
{
    public List<QuestionData> questions { get; set; }
}

public class GameApiDemoUI : MonoBehaviour
{
    private static GameApiDemoUI _instance;

    public static GameApiDemoUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameApiDemoUI>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameApiDemoUI");
                    _instance = go.AddComponent<GameApiDemoUI>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    [Header("按钮引用")]
    public Button btnStart;
    public Button btnQuestions;
    public Button btnNext;
    public Button btnLeaderboard;
    public Button btnTeams;
    public Button btnEnd;

    [Header("模拟答题按钮")]
    public Button btnMockRedCorrect;
    public Button btnMockBlueCorrect;
    public Button btnMockNoAnswer;

    [Header("显示区域")]
    public Text responseText;

    [Header("服务配置")]
    public string serverHost = "43.112.125.110";
    public int serverPort = 3000;

    private ClientWebSocket _webSocket;
    private CancellationTokenSource _cancellationTokenSource;
    private bool gameStarted = false;

    void Start()
    {
        btnStart.onClick.AddListener(OnStartGame);
        btnQuestions.onClick.AddListener(OnGetQuestions);
        btnNext.onClick.AddListener(OnNextQuestion);
        btnLeaderboard.onClick.AddListener(OnGetLeaderboard);
        btnTeams.onClick.AddListener(OnGetTeams);
        btnEnd.onClick.AddListener(OnEndGame);

        btnMockRedCorrect.onClick.AddListener(() => OnMockAnswer("red", "RedPlayer1"));
        btnMockBlueCorrect.onClick.AddListener(() => OnMockAnswer("blue", "BluePlayer1"));
        btnMockNoAnswer.onClick.AddListener(OnMockNoAnswer);

        SetButtonsInteractable(false);
        btnStart.interactable = true;
        OnStartGame();
    }

    // ==========监听服务器==========
    private async void ConnectWebSocket()
    {
        if (_webSocket != null && _webSocket.State == WebSocketState.Open) return;
        CloseWebSocket();
        try
        {
            _webSocket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();
            string wsUrl = $"ws://{serverHost}:{serverPort}/ws";
            Uri serverUri = new Uri(wsUrl);

            ShowResponse("正在连接实时推送服务...", false);
            await _webSocket.ConnectAsync(serverUri, _cancellationTokenSource.Token);
            ShowResponse("实时推送服务连接成功！", false);

            // 开始接收消息循环
            ReceiveMessageLoop();
        }
        catch (Exception e)
        {
            ShowResponse($"实时推送服务连接失败：{e.Message}", true);
        }
    }

    private async void ReceiveMessageLoop()
    {
        var buffer = new byte[1024 * 4];
        try
        {
            while (_webSocket != null && _webSocket.State == WebSocketState.Open && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    // 安全检查 UnityMainThreadDispatcher 是否存在
                    if (UnityMainThreadDispatcher.Instance != null)
                    {
                        UnityMainThreadDispatcher.Instance.Enqueue(() => ProcessWebSocketMessage(message));
                    }
                    else
                    {
                        Debug.LogWarning("UnityMainThreadDispatcher 未初始化，无法处理推送消息！请确保场景中挂接了该脚本。");
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            
        }
        catch (Exception e)
        {
            // 只有当对象还存在时才显示 UI 错误
            if (UnityMainThreadDispatcher.Instance != null && this != null)
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    if (this != null) // 再次检查脚本是否已被销毁
                    {
                        ShowResponse($"实时推送服务接收错误：{e.Message}", true);
                    }
                });
            }
            else
            {
                Debug.LogWarning($"WebSocket接收错误（对象已销毁）：{e.Message}");
            }
        }
    }

    private async void CloseWebSocket()
    {
        if (_webSocket != null && _webSocket.State == WebSocketState.Open)
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"关闭WebSocket时出错：{e.Message}");
            }
            finally
            {
                _webSocket?.Dispose();
                _webSocket = null;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }
    }

    private void ProcessWebSocketMessage(string json)
    {
        try
        {
            var msg = JObject.Parse(json);
            string msgType = msg["type"]?.ToString();

            switch (msgType)
            {
                case "answer_result":
                    var answerResult = msg.ToObject<AnswerResultMessage>();
                    HandleAnswerResult(answerResult);
                    break;
                case "chat_message":
                    var chatMsg = msg.ToObject<ChatMessage>();
                    HandleChatMessage(chatMsg);
                    break;
                case "user_joined":
                    var userJoined = msg.ToObject<UserJoinedMessage>();
                    HandleUserJoined(userJoined);
                    break;
                default:
                    Debug.Log($"收到未知类型推送：{msgType}，内容：{json}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"推送消息解析失败：{ex.Message}\n原始内容：{json}");
        }
    }

    private void HandleAnswerResult(AnswerResultMessage msg)
    {
        // 将推送消息转发给 GameFlowManager 统一处理（扣血、切换题目、刷新排行榜等）
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.OnAnswerResult(msg);
        }
        else
        {
            Debug.LogWarning("GameFlowManager 未初始化，无法处理答题结果！");
        }
    }

    private void HandleChatMessage(ChatMessage msg)
    {
        Debug.Log($"收到弹幕：[{msg.name}] {msg.text}"); if (DanmakuManager.Instance != null)
        {
            DanmakuManager.Instance.AddDanmaku(msg.name, msg.text);
        }
    }

    private void HandleUserJoined(UserJoinedMessage msg)
    {
        ShowResponse($"新用户加入{msg.team}队！用户ID：{msg.userId}", false);
        OnGetTeams();
    }
    // ========== WebSocket方法结束 ==========

    public void SetButtonsInteractable(bool enabled)
    {
        btnQuestions.interactable = enabled;
        btnNext.interactable = enabled;
        btnLeaderboard.interactable = enabled;
        btnTeams.interactable = enabled;
        btnEnd.interactable = enabled;
        btnMockRedCorrect.interactable = enabled;
        btnMockBlueCorrect.interactable = enabled;
        btnMockNoAnswer.interactable = enabled;
    }

    private void ShowResponse(string message, bool isError = false)
    {
        string prefix = isError ? "<color=red>[错误]</color>" : "<color=green>[响应]</color>";
        responseText.text = $"{prefix} {System.DateTime.Now:HH:mm:ss}\n{message}\n\n{responseText.text}";
    }

    private void OnError(string error)
    {
        ShowResponse(error, true);
    }

    private void RefreshLeaderboard()
    {
        if (!gameStarted) return;
        HttpCenter.Instance.GetAsync("/game/leaderboard", (response) =>
        {
            ShowResponse(response);
            UpdateLeaderboardUI(response);
        }, OnError);
    }

    private void UpdateLeaderboardUI(string json)
    {
        var data = JObject.Parse(json);
        var redScore = data["red"]?.Value<int>() ?? 0;
        var blueScore = data["blue"]?.Value<int>() ?? 0;
        var users = data["users"] as JArray;
        if (users == null) return;

        var entries = new List<UIManager.LeaderboardEntry>();
        foreach (var u in users)
        {
            entries.Add(new UIManager.LeaderboardEntry
            {
                userId = u["userId"]?.ToString(),
                name = null,
                score = u["score"]?.Value<int>() ?? 0,
                team = u["team"]?.ToString()
            });
        }
        UIManager.Instance.UpdateLeaderboard(entries);
    }

    private void UpdateUserNamesFromTeams(string json)
    {
        try
        {
            // 使用强类型模型直接反序列化，更安全
            var teamsResponse = JsonConvert.DeserializeObject<TeamsResponse>(json);

            if (teamsResponse == null)
            {
                ShowResponse("队伍列表解析失败：数据为空", true);
                return;
            }

            var userList = new List<(string userId, string name, string team)>();

            // 解析红队
            if (teamsResponse.red != null)
            {
                foreach (var member in teamsResponse.red)
                {
                    userList.Add((member.userId, member.name, "red"));
                }
            }

            // 解析蓝队
            if (teamsResponse.blue != null)
            {
                foreach (var member in teamsResponse.blue)
                {
                    userList.Add((member.userId, member.name, "blue"));
                }
            }

            // 更新 UIManager
            UIManager.Instance.UpdateUserNames(userList);
            ShowResponse($"成功获取队伍列表：红队 {teamsResponse.red?.Count ?? 0} 人，蓝队 {teamsResponse.blue?.Count ?? 0} 人");
        }
        catch (JsonException ex)
        {
            Debug.LogWarning($"队伍列表JSON解析失败: {ex.Message}\n原始数据: {json}");
            ShowResponse($"队伍列表解析失败，请检查服务端数据格式: {ex.Message}", true);
        }
    }

    public void OnStartGame()
    {
        //OnEndGame();
        btnStart.interactable = false;
        HttpCenter.Instance.PostAsync("/game/start", null, (response) =>
        {
            var json = JObject.Parse(response);
            bool ok = json["ok"]?.Value<bool>() ?? false;
            string errorCode = json["code"]?.ToString();

            if (ok)
            {
                gameStarted = true;
                btnStart.interactable = false;
                SetButtonsInteractable(true);
                ShowResponse(response);
               
                string roundId = json["roundId"]?.ToString();
                int qCount = json["questionsCount"]?.Value<int>() ?? 0;
                GameFlowManager.Instance.GameStarted(roundId, qCount);

                UIManager.Instance.UpdateLeaderboard(new List<UIManager.LeaderboardEntry>());
                OnGetLeaderboard();
                OnGetQuestions();
                OnGetTeams();
                RefreshLeaderboard();

                ConnectWebSocket();
            }
            else if (errorCode == "GAME_ACTIVE")
            {
                OnEndGame();
            }
            else
            {
                ShowResponse(response, true);
            }
        }, (response) =>
        {
            OnEndGame();
        });
    }

    private void OnGetQuestions()
    {
        HttpCenter.Instance.GetAsync("/game/questions", (response) =>
        {
            Debug.Log("[Questions RAW] " + response);
            ProcessQuestionsResponse(response);
        }, OnError);
    }

    private void ProcessQuestionsResponse(string response)
    {
        try
        {
            // 逻辑：匹配 "question":"..." 结构，并将内容中的 " 替换为 \"
            string cleanedResponse = System.Text.RegularExpressions.Regex.Replace(
                response,
                @"(?<=""question"":"")(.*?)(?="",""meta"")",
                m => m.Value.Replace("\"", "\\\"")
            );

            var questionsResponse = JsonConvert.DeserializeObject<QuestionsResponse>(cleanedResponse);

            if (questionsResponse?.questions != null)
            {
                GameFlowManager.Instance.SetQuestions(questionsResponse.questions);
                ShowResponse($"成功获取 {questionsResponse.questions.Count} 道题目");
                return;
            }
            else
            {
                ShowResponse("接口返回格式异常：questions 为空", true);
                return;
            }
        }
        catch (JsonException ex)
        {
            Debug.LogWarning($"JSON解析失败: {ex.Message}\n原始数据: {response}");
            ShowResponse($"JSON解析失败，请检查服务端数据格式: {ex.Message}", true);
        }
    }

    private void OnNextQuestion()
    {
        HttpCenter.Instance.PostAsync("/game/next-question", null, (response) =>
        {
            ShowResponse(response);
            var json = JObject.Parse(response);
            var nextIndex = json["nextIndex"]?.Value<int?>();
            GameFlowManager.Instance.OnNextQuestion(nextIndex);
        }, OnError);
    }

    private void OnGetLeaderboard()
    {
        HttpCenter.Instance.GetAsync("/game/leaderboard", (response) =>
        {
            ShowResponse(response);
            UpdateLeaderboardUI(response);
        }, OnError);
    }

    private void OnGetTeams()
    {
        HttpCenter.Instance.GetAsync("/game/teams", (response) =>
        {
            ShowResponse(response);
            UpdateUserNamesFromTeams(response); OnGetLeaderboard();
        }, OnError);
    }

    private void OnEndGame()
    {
        HttpCenter.Instance.PostAsync("/game/end", null, (response) =>
        {
            var json = JObject.Parse(response);
            bool ok = json["ok"]?.Value<bool>() ?? false;
            string errorCode = json["code"]?.ToString();

            if (ok || errorCode == "NO_GAME")
            {
                gameStarted = false;
                btnStart.interactable = true;
                SetButtonsInteractable(false);
                ShowResponse(response);
                GameFlowManager.Instance.GameEnded();
                UIManager.Instance.UpdateLeaderboard(new List<UIManager.LeaderboardEntry>());

                CloseWebSocket();
            }
            else
            {
                ShowResponse(response, true);
            }
        }, OnError);
    }

    private void OnMockAnswer(string team, string defaultUserName)
    {
        if (!gameStarted)
        {
            Debug.LogWarning("游戏未开始，无法模拟答题");
            return;
        }
        GameFlowManager.Instance.OnAnswerCorrect(team, defaultUserName);
        RefreshLeaderboard();
    }

    private void OnMockNoAnswer()
    {
        if (!gameStarted) return;
        GameFlowManager.Instance.OnNoAnswer();
    }

    private void OnDestroy()
    {
        CloseWebSocket();
    }
}