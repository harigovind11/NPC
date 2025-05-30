using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class VoiceSettings
{
    public float stability;
    public float similarity_boost;
}

[Serializable]
public class TTSData
{
    public string text;
    public string model_id;
    public VoiceSettings voice_settings;
}
public class Elevenlabs : MonoBehaviour
{
    public ElevenLabsConfig config;
    public AudioSource audioSource;

    public void Speak(string message)
    {
        StartCoroutine(GenerateAndStreamAudio(message));
    }

    public IEnumerator GenerateAndStreamAudio(string text)
    {
        string modelId = "eleven_multilingual_v2";
        string url = string.Format(config.ttsUrl, config.voiceId);

        TTSData ttsData = new TTSData
        {
            text = text.Trim(),
            model_id = modelId,
            voice_settings = new VoiceSettings
            {
                stability = 0.5f,
                similarity_boost = 0.8f
            }
        };

        string jsonData = JsonUtility.ToJson(ttsData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerAudioClip(new Uri(url), AudioType.MPEG);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("xi-api-key", config.apiKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[TTS] Error: " + request.error);
                yield break;
            }

            AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);
            if (audioClip != null)
            {
                audioSource.clip = audioClip;
                audioSource.Play();
            }
            else
            {
                Debug.LogWarning("[TTS] Failed to load audio clip. Retrying...");
                yield return StartCoroutine(GenerateAndStreamAudio(text));
            }
        }
    }
}
