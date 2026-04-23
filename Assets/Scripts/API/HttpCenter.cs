using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class HttpCenter : MonoBehaviour
{
    public static HttpCenter Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<HttpCenter>();
            }
            return _instance;
        }
    }
    static HttpCenter _instance = null;
    public bool useMockData;
    private const string BaseUrl = "http://43.112.125.110:3000";

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            if (_instance != this)
            {
                DestroyImmediate(_instance.gameObject);
            }
        }
        
    }
    private void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        // GetAsync("/api/Health", (s) => {Debug.Log(s); }, (e) => { Debug.LogError(e);});
/*
        GetAsync("/api/games", (s) =>
        {
            Debug.Log(s);
            txt.text = s;
        }, (e) => { Debug.LogError(e); });*/

    }

    public bool isOtherBaseUrl = false;

    public string otherUrl = "";

    // 通用的 GET 请求
    // relativePath: 例如 "/api/Health"
    // queryParams: 可选的查询参数，例如 new Dictionary<string, string> { { "a", "1" }, { "b", "2" } }
    // onSuccess: 请求成功时回调，传回返回的文本
    // onError: 请求失败时回调，传回错误信息（可选）
    public IEnumerator Get(string relativePath, Dictionary<string, string> queryParams, Action<string> onSuccess, Action<string> onError = null,bool isToken=false)
    {
        // 模拟模式：直接返回假数据，不发起真实请求
        if (useMockData)
        {
            yield return new WaitForSeconds(0.1f); // 模拟网络延迟
            var mock = MockApiHandler.ProcessRequest("GET", relativePath, null, queryParams);
            if (!mock.isError)
                onSuccess?.Invoke(mock.responseText);
            else
                onError?.Invoke(mock.errorMessage);
            yield break;
        }
        string url = "";
        if (isOtherBaseUrl)
        {
            isOtherBaseUrl = false;
            // 确保路径以 / 开头，拼接成完整 URL
            string path = relativePath ?? "";
            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }
            url= otherUrl.TrimEnd('/') + path;
        }
        else
        {
            // 确保路径以 / 开头，拼接成完整 URL
            string path = relativePath ?? "";
            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }
            url = BaseUrl.TrimEnd('/') + path;
        }
       
       

        
        // 拼接查询参数
        if (queryParams != null && queryParams.Count > 0)
        {
            bool first = true;
            foreach (var kvp in queryParams)
            {
                url += first ? "?" : "&";
                first = false;
                url += UnityWebRequest.EscapeURL(kvp.Key) + "=" + UnityWebRequest.EscapeURL(kvp.Value);
            }
        }
        Debug.Log(url);

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // 设定头部信息，确保服务器按 JSON 处理
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            request.timeout = 30;
            if (isToken)
            {
                //这里加token处理
                Debug.Log("token已经注释，如需添加手动解析");
                //request.SetRequestHeader("Authorization", "Bearer " + ShareData.Instance.token);
            }
#if UNITY_2020_1_OR_NEWER
            yield return request.SendWebRequest();
#else
            yield return request.Send();
#endif

#if UNITY_2020_1_OR_NEWER
            bool isError = request.result != UnityWebRequest.Result.Success;
#else
            bool isError = request.isNetworkError || request.isHttpError;
#endif

            if (!isError)
            {
                // 将 Unicode 转义序列转换为中文
                string result = DecodeUnicode(request.downloadHandler.text);
                onSuccess?.Invoke(result);
            }
            else
            {
                onError?.Invoke(request.error);
            }
        }
    }

    // 通用的 POST 请求
    // relativePath: 例如 "/api/submit"
    // body: POST 请求体，例如 new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } }
    // onSuccess: 请求成功时回调，传回返回的文本
    // onError: 请求失败时回调，传回错误信息（可选）
    public IEnumerator Post(string relativePath, Dictionary<string, object> body, Action<string> onSuccess, Action<string> onError = null, bool isToken = false)
    {

        // 模拟模式：直接返回假数据，不发起真实请求
        if (useMockData)
        {
            yield return new WaitForSeconds(0.5f); // 模拟网络延迟
            var mock = MockApiHandler.ProcessRequest("POST", relativePath, null, null);
            if (!mock.isError)
                onSuccess?.Invoke(mock.responseText);
            else
                onError?.Invoke(mock.errorMessage);
            yield break;
        }
        // 确保路径以 / 开头，拼接成完整 URL
        string url = "";
        if (isOtherBaseUrl)
        {
            isOtherBaseUrl = false;
            string path = relativePath ?? "";
            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }

            url = otherUrl.TrimEnd('/') + path;
        }
        else
        {
            string path = relativePath ?? "";
            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }

            url = BaseUrl.TrimEnd('/') + path;
        }

        // 将请求体转换为 JSON 字符串
        string jsonBody = JsonConvert.SerializeObject(body);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            Debug.Log(jsonBody);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 30;
            // 设定头部信息，确保服务器按 JSON 处理
            //request.SetRequestHeader("Content-Type", "application/json");
            //request.SetRequestHeader("Accept", "application/json");
            if (isToken)
            {
                //这里加token处理
                Debug.Log("Jia token暂时注释");
                //request.SetRequestHeader("Authorization", "Bearer " + ShareData.Instance.token);
            }
#if UNITY_2020_1_OR_NEWER
            yield return request.SendWebRequest();
#else
            yield return request.Send();
#endif

#if UNITY_2020_1_OR_NEWER
            bool isError = request.result != UnityWebRequest.Result.Success;
#else
            bool isError = request.isNetworkError || request.isHttpError;
#endif

            if (!isError)
            {
                // 将 Unicode 转义序列转换为中文
                string result = DecodeUnicode(request.downloadHandler.text);
                onSuccess?.Invoke(result);
            }
            else
            {
                Debug.Log(url+"报错:"+ request.downloadHandler.text);
                onError?.Invoke(request.downloadHandler.text);
            }
        }
    }

    // 辅助：外部直接用 StartCoroutine(HttpCenter.Instance.Get(...)) 的便利方法（带查询参数）
    public void GetAsync(string relativePath, Dictionary<string, string> queryParams, Action<string> onSuccess, Action<string> onError = null,bool isToken=false)
    {
        StartCoroutine(Get(relativePath, queryParams, onSuccess, onError,isToken));
    }

    // 辅助：外部直接用 StartCoroutine(HttpCenter.Instance.Get(...)) 的便利方法（不带查询参数）
    public void GetAsync(string relativePath, Action<string> onSuccess, Action<string> onError = null,bool isToken=false)
    {
        StartCoroutine(Get(relativePath, null, onSuccess, onError,isToken));
    }

    // 辅助：外部直接用 StartCoroutine(HttpCenter.Instance.Post(...)) 的便利方法
    public void PostAsync(string relativePath, Dictionary<string, object> body, Action<string> onSuccess, Action<string> onError = null, bool isToken = false)
    {
        StartCoroutine(Post(relativePath, body, onSuccess, onError,isToken));
    }

    // 辅助：外部直接用 StartCoroutine(HttpCenter.Instance.DownloadImage(...)) 的便利方法
    public void DownloadImageAsync(string imageUrl, Action<Texture2D> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(DownloadImage(imageUrl, onSuccess, onError));
    }

    // 图片下载方法（只负责下载和回调）
    private IEnumerator DownloadImage(string imageUrl, Action<Texture2D> onSuccess, Action<string> onError = null)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            request.timeout = 30;
           // Debug.Log("开始下载图片：" + imageUrl);
            request.SendWebRequest();

            while (!request.isDone)
            {
                // 打印当前下载进度
                //Debug.Log(imageUrl + "下载进度: " + request.downloadProgress);
                yield return null;
            }

            if (!(request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError))
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                onSuccess?.Invoke(texture);
            }
            else
            {
                onError?.Invoke(request.error);
            }
        }
    }

    string DecodeUnicode(string unicodeString)
    {
        return System.Text.RegularExpressions.Regex.Unescape(unicodeString);
    }

    // 用于将字典转换为可序列化的JSON
    [Serializable]
    private class DictionaryWrapper : Dictionary<string, object>
    {
        public DictionaryWrapper(Dictionary<string, object> dictionary) : base(dictionary) { }
    }
}
