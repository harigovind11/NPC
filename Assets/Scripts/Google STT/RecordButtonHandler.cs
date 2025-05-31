using UnityEngine;

public class RecordButtonHandler : MonoBehaviour
{
    public GoogleCloudSTT stt;

    public void StartRecord()
    {
        if (stt == null) return;

        if (stt.IsTTSPlaying())
        {
            stt.StopTTS();
        }

        stt.StartRecording();
    }

    public void StopRecord()
    {
        if (stt == null) return;

        stt.StopRecording();
    }
}