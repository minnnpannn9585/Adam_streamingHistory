using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class MockApiHandler
{
    private class GameState
    {
        public bool isActive;
        public string roundId;
        public List<Question> questions;
        public int currentQuestionIndex; // 1-based
        public int redScore;
        public int blueScore;
        public Dictionary<string, UserStats> userStats; // userId -> stats
        public List<UserInfo> redTeamUsers;
        public List<UserInfo> blueTeamUsers;

        public GameState()
        {
            isActive = false;
            roundId = null;
            questions = new List<Question>();
            currentQuestionIndex = 0;
            redScore = 0;
            blueScore = 0;
            userStats = new Dictionary<string, UserStats>();
            redTeamUsers = new List<UserInfo>();
            blueTeamUsers = new List<UserInfo>();
        }

        public void ResetForNewRound()
        {
            roundId = GenerateRoundId();
            currentQuestionIndex = 1;
            redScore = 0;
            blueScore = 0;
            userStats.Clear();
            // 初始化队伍成员，使用与模拟按钮一致的用户名
            redTeamUsers = new List<UserInfo>
            {
                new UserInfo { userId = "RedPlayer1", name = "RedPlayer1" },
                new UserInfo { userId = "RedPlayer2", name = "RedPlayer2" }
            };
            blueTeamUsers = new List<UserInfo>
            {
                new UserInfo { userId = "BluePlayer1", name = "BluePlayer1" },
                new UserInfo { userId = "BluePlayer2", name = "BluePlayer2" }
            };
            // 初始化用户统计
            foreach (var u in redTeamUsers)
                userStats[u.userId] = new UserStats { userId = u.userId, name = u.name, team = "red", score = 0 };
            foreach (var u in blueTeamUsers)
                userStats[u.userId] = new UserStats { userId = u.userId, name = u.name, team = "blue", score = 0 };
        }
    }
    [System.Serializable]
    public class QuestionData
    {
        [JsonProperty("id")]
        public string id;

        [JsonProperty("question")]
        public string question;

        [JsonProperty("meta")]
        public object meta; // 可根据需要定义更具体的类

    }
    private class Question
    {
        public string id;
        public string question;
        public string answer;
        public JObject meta;
    }

    private class UserInfo
    {
        public string userId;
        public string name;
    }

    private class UserStats
    {
        public string userId;
        public string name;
        public string team;
        public int score;
    }

    private static GameState _state = new GameState();

    static MockApiHandler()
    {
        LoadQuestions();
    }

    private static void LoadQuestions()
    {
        // 你提供的 10 道东亚历史题数据（完整 JSON）
        var rawJson = @"{
          ""id"": ""set_1758681686"",
          ""name"": ""IHBB Questions (categorized by region)"",
          ""items"": [
            {
              ""id"": ""4145814520837557632_70977"",
              ""question"": ""Empire that traded with China along the Silk Road, delivering coins and medallions depicting Maximian and Marcus Aurelius."",
              ""answer"": ""Roman Empire ("",
              ""aliases"": [],
              ""meta"": { ""category"": ""East Asia"", ""era"": """", ""source"": """" }
            },
            {
              ""id"": ""8256485734730678398_71068"",
              ""question"": ""Country that received the port of Qingdao [[CHING-DOW]] in the treaty, triggering the May 4th Movement in another country."",
              ""answer"": ""Empire of Japan"",
              ""aliases"": [ ""Nippon"" ],
              ""meta"": { ""category"": ""East Asia"", ""era"": """", ""source"": """" }
            },
            {
              ""id"": ""2634099948783167717_70929"",
              ""question"": ""1449 “Crisis” where over 200,000 Ming soldiers were killed and the Zhengtong Emperor was captured by a small Mongol cavalry force."",
              ""answer"": ""Tumu Crisis (or Battle of Tumu"",
              ""aliases"": [],
              ""meta"": { ""category"": ""East Asia"", ""era"": """", ""source"": """" }
            },
            {
              ""id"": ""3934984331681327949_71064"",
              ""question"": ""Military position first held by Minamoto no Yoritomo [[mih-nah-MOH-toh noh toh- ree-TOH-moh]], making him the de facto head of Japan."",
              ""answer"": ""Shogun"",
              ""aliases"": [ ""Sei-i Taishōgun"" ],
              ""meta"": { ""category"": ""East Asia"", ""era"": """", ""source"": """" }
            },
            {
              ""id"": ""5807627153844267730_70907"",
              ""question"": ""This dynasty’s founder, Nurhaci created the Eight Banners Army. A century later, its Kangxi [kahng-­‐ shee] Emperor welcomed Jesuit missionaries. For the point, name this Chinese dynasty founded by Manchu invaders after the fall of the Ming."",
              ""answer"": ""Qing [cheeng] Dynasty ("",
              ""aliases"": [],
              ""meta"": { ""category"": ""East Asia"", ""era"": """", ""source"": """" }
            },
            {
              ""id"": ""172771011519092164_70910"",
              ""question"": ""Sima Yan established the Jin dynasty during this period by overthrowing the Cao Cao-­‐founded Wei. The collapse of the Han dynasty led to this strife-­‐filled period of Chinese history. For ten points, what is this period named for the number of major combatants?"",
              ""answer"": ""Three Kingdoms period BONUS: The tumult of the Three Kingdoms period essentially started in 184 AD with this peasant revolt, led by Zhang Jiao. What is this revolt whose name includes both a color and a piece of headwear? Yellow Turban Rebellion Page 3"",
              ""aliases"": [],
              ""meta"": { ""category"": ""East Asia"", ""era"": """", ""source"": """" }
            },
            {
              ""id"": ""8563557077310448171_70906"",
              ""question"": ""This region was one of the leading producers of green-­‐glazed celadon during the Goryeo dynasty. A later king of this region’s Choson dynasty is credited with inventing its modern alphabet, hangul. For the point, name this peninsula, ruled in the 15th century by King Sejong."",
              ""answer"": ""Korean Peninsula ("",
              ""aliases"": [],
              ""meta"": { ""category"": ""East Asia"", ""era"": """", ""source"": """" }
            },
            {
              ""id"": ""7893811237934152811_70906"",
              ""question"": ""One of these events was the brightest observed celestial event in history and occurred in 1006 AD. Another of these events in 1054 AD was recorded by Chinese astronomers and led to the creation of the Crab nebula. For the point, name these events in which stars violently explode."",
              ""answer"": ""supernova (do not"",
              ""aliases"": [],
              ""meta"": { ""category"": ""East Asia"", ""era"": """", ""source"": """" }
            },
            {
              ""id"": ""6383316799072349306_70968"",
              ""question"": ""This country’s holiest shrine is rebuilt every 20 years. In 1995, a doomsday cult carried out a sarin attack in this nation’s largest city. The Ise [ee-­‐say] and Yasukuni shrines can be found in, for ten points, what Asian country where Shinto developed in the 8th century and is practiced in Tokyo?"",
              ""answer"": ""Japan BONUS: In Japan, Shinbutsu-­‐shugo is a syncretism, or mixed religion, of aspects of Shinto with which other world religion, commonly practiced in Thailand and Tibet? Buddhism Page 4"",
              ""aliases"": [],
              ""meta"": { ""category"": ""East Asia"", ""era"": """", ""source"": """" }
            },
            {
              ""id"": ""8516328565041028796_70899"",
              ""question"": ""After the Battle of the Allia, one of these structures was built and named for Servius Tullius. That structure was surpassed by the Aurelian line, built in the 3rd century AD. For ten points, name this type of defensive structure that was built by the Qin Dynasty to guard the northern border of China."",
              ""answer"": ""walls ("",
              ""aliases"": [],
              ""meta"": { ""category"": ""East Asia"", ""era"": """", ""source"": """" }
            }
          ]
        }";

        var obj = JObject.Parse(rawJson);
        var items = obj["items"] as JArray;
        if (items != null)
        {
            _state.questions = items.Select(item => new Question
            {
                id = item["id"]?.ToString(),
                question = item["question"]?.ToString(),
                answer = item["answer"]?.ToString(),
                meta = item["meta"] as JObject
            }).ToList();
        }
    }

    /// <summary>
    /// 记录答题结果
    /// </summary>
    /// <param name="team">红队 "red" 或蓝队 "blue"</param>
    /// <param name="userName">答对用户名</param>
    /// <param name="addTeamScore">是否增加队伍分数（首个答对）</param>
    public static void RecordAnswer(string team, string userName, bool addTeamScore)
    {
        if (!_state.isActive) return;

        // 根据用户名查找对应用户（优先精确匹配）
        UserStats targetUser = null;
        var userList = (team == "red") ? _state.redTeamUsers : _state.blueTeamUsers;
        foreach (var u in userList)
        {
            if (u.name == userName)
            {
                targetUser = _state.userStats.ContainsKey(u.userId) ? _state.userStats[u.userId] : null;
                break;
            }
        }
        // 如果找不到，则随机选一个（避免空指针）
        if (targetUser == null && userList.Count > 0)
        {
            var rand = new System.Random();
            var randomUser = userList[rand.Next(userList.Count)];
            targetUser = _state.userStats.ContainsKey(randomUser.userId) ? _state.userStats[randomUser.userId] : null;
        }

        if (targetUser != null)
        {
            // 增加个人分数
            targetUser.score++;
        }
        else
        {
            Debug.LogWarning($"[Mock] 找不到用户 {userName}，无法记录个人分数");
        }

        // 增加队伍分数（仅首个答对）
        if (addTeamScore)
        {
            if (team == "red")
                _state.redScore++;
            else if (team == "blue")
                _state.blueScore++;
        }

        Debug.Log($"[Mock] 记录答题：{team}队 {userName}，加队伍分={addTeamScore}，当前个人分={targetUser?.score}");
    }

    // 保留旧方法以兼容外部调用（但内部改为调用新方法）
    public static void RecordFirstAnswer(string team, string userName)
    {
        RecordAnswer(team, userName, true);
    }

    // 处理 HTTP 请求
    public static (string responseText, bool isError, string errorMessage) ProcessRequest(
        string method, string path, Dictionary<string, object> body = null, Dictionary<string, string> queryParams = null)
    {
        path = path.TrimEnd('/');

        // 健康检查
        if (method == "GET" && path == "/health")
            return (JObject.FromObject(new { ok = true }).ToString(), false, null);

        // 开始游戏
        if (method == "POST" && path == "/game/start")
        {
            if (_state.isActive)
                return (JObject.FromObject(new { error = "Game already active", code = "GAME_ACTIVE" }).ToString(), true, "Game already active");

            _state.isActive = true;
            _state.ResetForNewRound();

            var response = new { ok = true, roundId = _state.roundId, questionsCount = _state.questions.Count };
            return (JObject.FromObject(response).ToString(), false, null);
        }

        // 获取题目列表
        if (method == "GET" && path == "/game/questions")
        {
            if (!_state.isActive)
                return (JObject.FromObject(new { error = "No active game", code = "NO_GAME" }).ToString(), true, "No active game");

            var questionsForClient = _state.questions.Select((q, index) => new
            {
                index = index + 1,
                id = q.id,
                question = q.question,
                meta = q.meta
            }).ToList();

            var response = new { questions = questionsForClient };
            return (JObject.FromObject(response).ToString(), false, null);
        }

        // 下一题
        if (method == "POST" && path == "/game/next-question")
        {
            if (!_state.isActive)
                return (JObject.FromObject(new { error = "No active game", code = "NO_GAME" }).ToString(), true, "No active game");

            if (_state.currentQuestionIndex > _state.questions.Count)
                return (JObject.FromObject(new { error = "No more questions", code = "NO_MORE_QUESTIONS" }).ToString(), true, "No more questions");

            // 增加索引（不再自动模拟答题）
            _state.currentQuestionIndex++;

            int? nextIndex = _state.currentQuestionIndex <= _state.questions.Count ? (int?)_state.currentQuestionIndex : null;
            var response = new { ok = true, nextIndex };
            return (JObject.FromObject(response).ToString(), false, null);
        }

        // 获取排行榜
        if (method == "GET" && path == "/game/leaderboard")
        {
            if (!_state.isActive)
                return (JObject.FromObject(new { error = "No active game", code = "NO_GAME" }).ToString(), true, "No active game");

            var users = _state.userStats.Values
                .OrderByDescending(u => u.score)
                .Select(u => new { userId = u.userId, score = u.score, team = u.team })
                .ToList();

            var response = new { red = _state.redScore, blue = _state.blueScore, users };
            return (JObject.FromObject(response).ToString(), false, null);
        }

        // 获取队伍成员
        if (method == "GET" && path == "/game/teams")
        {
            var redMembers = _state.redTeamUsers.Select(u => new { userId = u.userId, name = u.name }).ToList();
            var blueMembers = _state.blueTeamUsers.Select(u => new { userId = u.userId, name = u.name }).ToList();
            var response = new { red = redMembers, blue = blueMembers };
            return (JObject.FromObject(response).ToString(), false, null);
        }

        // 结束游戏
        if (method == "POST" && path == "/game/end")
        {
            if (!_state.isActive)
                return (JObject.FromObject(new { error = "No active game", code = "NO_GAME" }).ToString(), true, "No active game");

            _state.isActive = false;
            return (JObject.FromObject(new { ok = true }).ToString(), false, null);
        }

        return (JObject.FromObject(new { error = "Not found", code = "NOT_FOUND" }).ToString(), true, "Not found");
    }

    private static string GenerateRoundId()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var random = new System.Random();
        return new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
    }
}