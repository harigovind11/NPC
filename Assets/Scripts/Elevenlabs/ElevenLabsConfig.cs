using UnityEngine;


[CreateAssetMenu(fileName = "ElevenLabsConfig", menuName = "ElevenLabs/ElevenLabs Configuration")]
public class ElevenLabsConfig : ScriptableObject
{
    public string apiKey;
    public string voiceId;
    public string ttsUrl;

}