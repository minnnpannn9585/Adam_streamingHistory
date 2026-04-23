using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DanmakuManager : MonoBehaviour
{
    public static DanmakuManager Instance { get; private set; }

    [Header("弹幕配置")]
    public GameObject danmakuPrefab;       // 弹幕预制体
    public Transform danmakuContainer;     // 弹幕父容器（建议是一个全屏的空Image，Alpha设为0）
    public float danmakuSpeed = 200f;      // 弹幕滚动速度（像素/秒）
    public float danmakuSpacing = 50f;     // 弹幕上下间距
    public float danmakuLifetime = 8f;     // 弹幕存活时间（秒）

    // 弹幕数据结构
    public struct DanmakuData
    {
        public string userName;
        public string content;
    }

    // 弹幕队列
    private Queue<DanmakuData> danmakuQueue = new Queue<DanmakuData>();
    // 可用的弹幕轨道（Y轴位置）
    private List<float> availableTracks = new List<float>();
    // 当前正在显示的弹幕
    private List<GameObject> activeDanmakus = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 初始化弹幕轨道（根据容器高度自动计算）
        InitializeTracks();
    }

    private void Update()
    {
        // 处理队列中的弹幕
        if (danmakuQueue.Count > 0)
        {
            TrySpawnDanmaku();
        }

        // 更新所有活动弹幕的位置
        UpdateActiveDanmakus();
    }

    // ========== 外部调用API ==========
    /// <summary>
    /// 外部调用：向弹幕队列添加一条弹幕
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="content">弹幕内容</param>
    public void AddDanmaku(string userName, string content)
    {
        Debug.Log(userName);
        danmakuQueue.Enqueue(new DanmakuData { userName = userName, content = content });
    }

    // ========== 内部逻辑 ==========
    // 初始化轨道
    private void InitializeTracks()
    {
        if (danmakuContainer == null) return;

        float containerHeight = danmakuContainer.GetComponent<RectTransform>().rect.height;
        float prefabHeight = danmakuPrefab.GetComponent<RectTransform>().rect.height;

        // 计算可以放多少行弹幕
        int trackCount = Mathf.FloorToInt(containerHeight / (prefabHeight + danmakuSpacing));

        // 生成轨道Y坐标
        availableTracks.Clear();
        float startY = containerHeight / 2 - prefabHeight / 2; // 从顶部开始
        for (int i = 0; i < trackCount; i++)
        {
            availableTracks.Add(startY - i * (prefabHeight + danmakuSpacing));
        }
    }

    // 尝试生成一条弹幕
    private void TrySpawnDanmaku()
    {
        if (danmakuQueue.Count == 0 || availableTracks.Count == 0) return;

        // 取出队列中的第一条弹幕
        DanmakuData data = danmakuQueue.Dequeue();

        // 随机选择一个轨道
        int trackIndex = Random.Range(0, availableTracks.Count);
        float yPos = availableTracks[trackIndex];
        availableTracks.RemoveAt(trackIndex); // 暂时移除该轨道，避免重叠
        Debug.Log("生成弹幕实例");
        // 生成弹幕实例
        GameObject danmaku = Instantiate(danmakuPrefab, danmakuContainer);
        RectTransform rt = danmaku.GetComponent<RectTransform>();

        // 设置初始位置（容器右侧外部）
        float containerWidth = danmakuContainer.GetComponent<RectTransform>().rect.width;
        rt.anchoredPosition = new Vector2(containerWidth / 2 + rt.rect.width / 2, yPos);

        // 设置文本内容（第一个子对象是Text）
        Text textComponent = danmaku.transform.GetChild(0).GetComponent<Text>();
        if (textComponent != null)
        {
            textComponent.text = $"{data.userName}: {data.content}";
        }

        // 记录活动弹幕
        activeDanmakus.Add(danmaku);

        // 延迟回收轨道
        StartCoroutine(RecycleTrack(yPos, danmakuLifetime * 0.5f));
    }

    // 更新活动弹幕位置
    private void UpdateActiveDanmakus()
    {
        for (int i = activeDanmakus.Count - 1; i >= 0; i--)
        {
            GameObject danmaku = activeDanmakus[i];
            RectTransform rt = danmaku.GetComponent<RectTransform>();

            // 向左移动
            rt.anchoredPosition += Vector2.left * danmakuSpeed * Time.deltaTime;

            // 检查是否超出容器左侧
            float containerWidth = danmakuContainer.GetComponent<RectTransform>().rect.width;
            if (rt.anchoredPosition.x < -containerWidth / 2 - rt.rect.width / 2)
            {
                // 销毁弹幕
                Destroy(danmaku);
                activeDanmakus.RemoveAt(i);
            }
        }
    }

    // 回收轨道
    private System.Collections.IEnumerator RecycleTrack(float yPos, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!availableTracks.Contains(yPos))
        {
            availableTracks.Add(yPos);
        }
    }
}