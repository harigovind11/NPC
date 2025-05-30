using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class GeminiAPI : MonoBehaviour
{
    [Header("Gemini Settings")]
    //[SerializeField] private string apiKey = "YOUR_GEMINI_API_KEY"; // Replace this

    public GeminiConfig config;
    
    private static GeminiAPI _instance;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject); // Optional
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Call this from anywhere to send a prompt and receive a reply.
    /// </summary>
    public static void SendPrompt(string prompt, Action<string> onReplyReceived = null)
    {
        if (_instance == null)
        {
            Debug.LogError("[GeminiAPI] No instance found. Add GeminiAPI to the scene.");
            return;
        }

        _instance.StartCoroutine(_instance.SendPromptCoroutine(prompt, onReplyReceived));
    }

    private IEnumerator SendPromptCoroutine(string prompt, Action<string> onReplyReceived)
    {
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={config.apiKey}";


        string requestBody = $@"
        {{
            ""contents"": [
                {{
                    ""parts"": [
                        {{
                            ""text"": ""{EscapeJson(prompt)}""
                        }}
                    ]
                }}
            ]
        }}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log("[GeminiAPI] Sending request...");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log("[GeminiAPI] Raw response:\n" + json);

                GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(json);
                if (response != null &&
                    response.candidates != null &&
                    response.candidates.Length > 0 &&
                    response.candidates[0].content.parts.Length > 0)
                {
                    string reply = response.candidates[0].content.parts[0].text;
                    Debug.Log("[GeminiAPI] Gemini Reply: " + reply);
                    onReplyReceived?.Invoke(reply);
                }
                else
                {
                    Debug.LogWarning("[GeminiAPI] No valid reply from Gemini.");
                    onReplyReceived?.Invoke(null);
                }
            }
            else
            {
                Debug.LogError($"[GeminiAPI] Request failed: {request.responseCode} {request.error}\n{request.downloadHandler.text}");
                onReplyReceived?.Invoke(null);
            }
        }
    }

    private string EscapeJson(string str)
    {
        return str.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    // Response classes
    [Serializable]
    private class GeminiResponse
    {
        public Candidate[] candidates;
    }

    [Serializable]
    private class Candidate
    {
        public Content content;
    }

    [Serializable]
    private class Content
    {
        public Part[] parts;
    }

    [Serializable]
    private class Part
    {
        public string text;
    }
}
