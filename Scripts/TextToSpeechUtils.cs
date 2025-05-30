using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class TextToSpeechUtils
{

    [Serializable]
    public class OpenAITTSRequest
    {
        public string model;
        public string input;
        public string voice;
        public string response_format;
    }
    public static IEnumerator SpeakText(string text, string apiKey, AudioSource audioSource)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.LogWarning("📭 No text provided for TTS.");
            yield break;
        }

        // ======  OpenAI Text-to-Speech API ======
        string url = "https://api.openai.com/v1/audio/speech";
        string voice = "alloy"; // alloy, echo, fable, onyx, nova, shimmer
        string model = "tts-1"; // or "tts-1-hd"
        string audioPath = Path.Combine(Application.persistentDataPath, "tts_output.mp3");

        OpenAITTSRequest payload = new OpenAITTSRequest
        {
            model = model,
            input = text,
            voice = voice,
            response_format = "mp3"
        };

        string jsonPayload = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        UnityWebRequest req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ TTS request failed: " + req.error);
            yield break;
        }

        byte[] mp3Data = req.downloadHandler.data;
        File.WriteAllBytes(audioPath, mp3Data);
        Debug.Log("🔊 TTS audio saved to: " + audioPath);

        // play audio
        using (UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + audioPath, AudioType.MPEG))
        {
            yield return audioRequest.SendWebRequest();

            if (audioRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("❌ Failed to load TTS audio: " + audioRequest.error);
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest);
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
}
