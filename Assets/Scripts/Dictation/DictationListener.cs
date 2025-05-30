using Oculus.Voice.Dictation;
using TMPro;
using UnityEngine;

public class DictationListener : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AppDictationExperience dictationExperience;
    [SerializeField] private TMP_Text uiText;
    
    private GoogleCloudTTS googleCloudTts;

    void OnEnable()
    {
        if (dictationExperience != null)
        {
            dictationExperience.DictationEvents.OnFullTranscription.AddListener(HandleTranscription);
        }
        else
        {
            Debug.LogError("[DictationListener] AppDictationExperience is not assigned.");
        }

        googleCloudTts = FindFirstObjectByType<GoogleCloudTTS>();
    }

    void OnDisable()
    {
        if (dictationExperience != null)
        {
            dictationExperience.DictationEvents.OnFullTranscription.RemoveListener(HandleTranscription);
        }
    }

    void HandleTranscription(string transcript)
    {
        Debug.Log("[DictationListener] Got transcript: " + transcript);

        if (uiText != null)
        {
            uiText.text += "\n<color=cyan><b>You:</b> " + transcript + "</color>";
        }

        GeminiAPI.SendPrompt(transcript, (reply) =>
        {
            if (!string.IsNullOrEmpty(reply))
            {
                Debug.Log("[GeminiAPI] Gemini says: " + reply);

                if (uiText != null)
                {
                    uiText.text += "\n<color=yellow><b>Gemini:</b> " + reply + "</color>";
                }

                if (googleCloudTts != null)
                {
                    googleCloudTts.Speak(reply);
                }
            }
            else
            {
                Debug.LogError("Gemini returned no reply.");
            }
        });
    }
}
