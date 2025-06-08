using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class VoiceUtils
{
    [Serializable]
    public class WhisperResponse { public string text; }

    [Serializable]
    public class GeminiRequest
    {
        public GeminiContent[] contents;
        public GenerationConfig generationConfig;
    }

    [Serializable]
    public class GenerationConfig { public string response_mime_type; }

    [Serializable]
    public class GeminiContent
    {
        public string role;
        public GeminiPart[] parts;
    }

    [Serializable]
    public class GeminiPart { public string text; }

    [Serializable]
    public class GeminiResponseContainer
    {
        public Candidate[] candidates;
    }

    [Serializable]
    public class Candidate
    {
        public GeminiContent content;
    }

    [Serializable]
    public class GeminiFeedback
    {
        public int score;
        public string feedback;
        public string expression;
        public string suggestion;
    }

    public static void SaveClipToWav(AudioClip clip, string path)
    {
        if (clip == null || clip.samples == 0) return;

        byte[] wavBytes = WavUtility.FromAudioClip(clip);
        File.WriteAllBytes(path, wavBytes);
        Debug.Log("📁 Saved to " + path);
    }

    public static IEnumerator HandleSpeechToGemini(string path, string openAiApiKey, string geminiApiKey, Action<GeminiFeedback> onComplete, InterviewManager interviewManager)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            Debug.LogError("Invalid audio path.");
            yield break;
        }

        // Step 1: Whisper
        byte[] audioBytes = File.ReadAllBytes(path);
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", audioBytes, "recorded.wav", "audio/wav");
        form.AddField("model", "whisper-1");

        UnityWebRequest whisperReq = UnityWebRequest.Post("https://api.openai.com/v1/audio/transcriptions", form);
        whisperReq.SetRequestHeader("Authorization", "Bearer " + openAiApiKey);
        yield return whisperReq.SendWebRequest();

        if (whisperReq.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Whisper failed: " + whisperReq.error);
            yield break;
        }

        string whisperJson = whisperReq.downloadHandler.text;
        WhisperResponse whisperResponse = JsonUtility.FromJson<WhisperResponse>(whisperJson);
        if (string.IsNullOrWhiteSpace(whisperResponse?.text))
        {
            Debug.LogWarning("Whisper returned empty text.");
            yield break;
        }

        string userText = whisperResponse.text;
        Debug.Log("📝 Transcription: " + userText);

        // Step 2: Gemini
        GeminiRequest geminiRequest = new GeminiRequest
        {
            contents = new[]
            {
                new GeminiContent
                {
                    parts = new[]
                    {
                        new GeminiPart
                        {
                            // Updated prompt to request JSON directly in the response schema if possible, or ensure clean JSON output.
                            text =  $"You are an interviewer for {interviewManager.GetPositionName()} postion, you have ask the question to interviewee: " +
                                    $"Topic: {interviewManager.GetCurrentStageName()}, Question: {interviewManager.GetCurrentPrompt()}" +
                                    $"Evaluate the following job interview answer and provide feedback. Answer:\n\"{userText}\"\n" +
                                    $"Don't be too harsh. Be lenient!!!" +
                                    $"Output ONLY a JSON object with four keys: " +
                                    $"'score' (integer between 1 to 10, e.g., '5', '6'), " +
                                    $"'feedback' (string, e.g., 'confident', 'hesitant'), " +
                                    $"'expression' (string, e.g., 'clap', 'neutral', 'frown', 'good'), and " +
                                    $"'suggestion' (string, e.g., 'Speak more clearly.', 'Good points.'). Example: " +
                                    "{\"score\":\"8\", \"feedback\":\"confident\",\"expression\":\"clap\",\"suggestion\":\"Great answer!\"}"
                        }
                    }
                }
            },
            generationConfig = new GenerationConfig { response_mime_type = "application/json" }
        };


        string payload = JsonUtility.ToJson(geminiRequest);
        byte[] body = Encoding.UTF8.GetBytes(payload);

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-lite:generateContent?key={geminiApiKey}";
        UnityWebRequest geminiReq = new UnityWebRequest(url, "POST");
        geminiReq.uploadHandler = new UploadHandlerRaw(body);
        geminiReq.downloadHandler = new DownloadHandlerBuffer();
        geminiReq.SetRequestHeader("Content-Type", "application/json");
        yield return geminiReq.SendWebRequest();

        if (geminiReq.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Gemini failed: " + geminiReq.error);
            yield break;
        }

        string respText = geminiReq.downloadHandler.text;
        Debug.Log("🔁 Gemini raw response: " + respText);

        GeminiResponseContainer geminiResponseContainer = JsonUtility.FromJson<GeminiResponseContainer>(respText);

        if (geminiResponseContainer == null || geminiResponseContainer.candidates == null || geminiResponseContainer.candidates.Length == 0 ||
            geminiResponseContainer.candidates[0].content == null || geminiResponseContainer.candidates[0].content.parts == null ||
            geminiResponseContainer.candidates[0].content.parts.Length == 0)
        {
            Debug.LogError("❌ Failed to parse Gemini response structure or no content found. Raw: " + respText);
            // Fallback to manual extraction if direct parsing fails (e.g., if response_mime_type wasn't fully respected or output is wrapped)
            string extractedJsonFallback = ExtractJsonFromGemini(respText);
            if (string.IsNullOrEmpty(extractedJsonFallback))
            {
                Debug.LogError("❌ Fallback JSON extraction also failed.");
                yield break;
            }
            Debug.Log("🔁 Attempting fallback JSON parsing with: " + extractedJsonFallback);
            GeminiFeedback feedback = JsonUtility.FromJson<GeminiFeedback>(extractedJsonFallback);
            if (feedback == null || string.IsNullOrEmpty(feedback.expression))
            {
                Debug.LogError("❌ Fallback JSON parsing to GeminiFeedback failed.");
                yield break;
            }
            onComplete?.Invoke(feedback);
        }
        else
        {
            string feedbackJson = geminiResponseContainer.candidates[0].content.parts[0].text;
            GeminiFeedback feedback = JsonUtility.FromJson<GeminiFeedback>(feedbackJson);

            if (feedback == null || string.IsNullOrEmpty(feedback.expression))
            {
                Debug.LogError("❌ Failed to parse GeminiFeedback from candidates' part. JSON part: " + feedbackJson);
                // One more fallback: try to parse the whole raw response if it's just the JSON object
                GeminiFeedback feedbackAlt = JsonUtility.FromJson<GeminiFeedback>(respText);
                if (feedbackAlt != null && !string.IsNullOrEmpty(feedbackAlt.expression))
                {
                    onComplete?.Invoke(feedback);
                }
                else
                {
                    Debug.LogError("❌ All parsing attempts for Gemini feedback failed.");
                    yield break;
                }
            }
            else
            {
                onComplete?.Invoke(feedback);
            }
        }
    }

    private static string ExtractJsonFromGemini(string response)
    {
        // This is a basic extractor. Gemini might return JSON wrapped in ```json ... ``` or other text.
        int firstBrace = response.IndexOf('{');
        int lastBrace = response.LastIndexOf('}');

        if (firstBrace != -1 && lastBrace != -1 && lastBrace > firstBrace)
        {
            return response.Substring(firstBrace, lastBrace - firstBrace + 1);
        }
        Debug.LogError("❌ Failed to extract JSON from Gemini response using simple brace matching. Response: " + response);
        return null;
    }
}
