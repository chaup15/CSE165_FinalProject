using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InterviewFlowManager : MonoBehaviour
{
    public InterviewManager interviewManager;
    public AudioSource ttsAudioSource;
    public Animator avatarAnimator;
    public VoiceInteractionManager voiceInteractionManager;
    public TMP_Text positionNameText; // UI Text to display position name
    public TMP_Text countdownText; // UI Text for countdown
    private string openAiApiKey;
    private string geminiApiKey;
    private bool isInterviewing = false;
    private bool isPositionNameSetting = false;

    void Start()
    {
        openAiApiKey = EnvironmentManager.OpenAIApiKey;
        geminiApiKey = EnvironmentManager.GeminiApiKey;
        positionNameText.text = "Position: \n" + interviewManager.GetPositionName();
    }

    public void StartInterview()
    {
        if (isInterviewing || isPositionNameSetting) return;
        StartCoroutine(InterviewLoop());
        isInterviewing = true;
        Debug.Log("🛑 Interview Start.");
    }
    public void StopInterview()
    {
        if (!isInterviewing) return;
        StopAllCoroutines();
        isInterviewing = false;
        interviewManager.ResetInterview();
        Debug.Log("🛑 Interview stopped.");
    }

    public void SetPositionNameSetting()
    {
        if (isPositionNameSetting || isInterviewing) return;
        StartCoroutine(SetPositionNameLoop());
        isPositionNameSetting = true;
        Debug.Log("🛑 Position name set to: " + interviewManager.GetPositionName());
    }

    public void StopPositionNameSetting()
    {
        if (!isPositionNameSetting) return;
        // StopAllCoroutines();
        isPositionNameSetting = false;
        Debug.Log("🛑 Position name setting stopped.");
    }

    private IEnumerator InterviewLoop()
    {

        int t = 0;
        while (!interviewManager.IsInterviewEnded())
        {

            string question = interviewManager.GetCurrentPrompt();
            Debug.Log($"🎤 Interviewer asks: {question}");

            // Step 1: Speak the question
            yield return TextToSpeechUtils.SpeakText(question, openAiApiKey, ttsAudioSource);

            // Step 2: 5s countdown before recording
            yield return new WaitForSeconds(1f);

            // Step 3: Start recording
            Debug.Log("⏺️ Recording started..." + (++t));
            voiceInteractionManager.StartRecording();

            // yield return new WaitForSeconds(60f); // or less if you want
            // yield return new WaitForSeconds(30f); // or less if you want
            float countdown = 30f;
            while (countdown > 0f)
            {
                countdownText.text = $"Time Left: {Mathf.CeilToInt(countdown)}s";
                yield return new WaitForSeconds(1f);
                countdown -= 1f;
            }
            countdownText.text = "Not Your Turn!"; // Clear after countdown


            // Step 4: Stop recording and handle STT + Gemini
            Debug.Log("⏹️ Recording stopped. Processing..." + (++t));
            voiceInteractionManager.StopRecordingWithCallback(OnGeminiFeedbackReceived);

            // Wait until processing is done (feedback callback sets a flag)
            yield return new WaitUntil(() => hasGeminiFeedback);
            hasGeminiFeedback = false;

            // Step 5: Advance to next stage
            interviewManager.AdvanceStage();
            yield return new WaitForSeconds(2f); // buffer between rounds
        }

        // Final message
        yield return TextToSpeechUtils.SpeakText("Thank you for participating in this interview!", openAiApiKey, ttsAudioSource);
        StopInterview();
    }

    private IEnumerator SetPositionNameLoop()
    {
        // 1. Speak prompt
        yield return TextToSpeechUtils.SpeakText("What job position are you applying?", openAiApiKey, ttsAudioSource);

        // 2. 1 second countdown
        yield return new WaitForSeconds(1f);

        // 3. Start recording
        voiceInteractionManager.StartRecording();
        float countdown = 4f;
        while (countdown > 0f)
        {
            countdownText.text = $"Time Left: {Mathf.CeilToInt(countdown)}s";
            yield return new WaitForSeconds(1f);
            countdown -= 1f;
        }
        countdownText.text = "Not Your Turn!"; // Clear after countdown

        // 4. Stop recording and get audio path
        voiceInteractionManager.StopRecording_save();
        string audioPath = Application.persistentDataPath + "/recorded.wav";

        // 5. Use Whisper to get STT text
        string sttText = null;
        bool done = false;
        yield return VoiceUtils.GetWhisperTranscription(audioPath, openAiApiKey, (result) =>
        {
            sttText = result;
            done = true;
        });
        yield return new WaitUntil(() => done);

        // 6. Set position name
        if (!string.IsNullOrWhiteSpace(sttText))
        {
            interviewManager.SetPositionName(sttText.Trim());
            Debug.Log("🛑 Position name set to: " + sttText.Trim());
            positionNameText.text = "Position: \n" + interviewManager.GetPositionName();
        }
        else
        {
            Debug.LogWarning("No position name detected from speech.");
        }
        
        StopPositionNameSetting();
    }

    // Gemini
    private bool hasGeminiFeedback = false;

    void OnGeminiFeedbackReceived(VoiceUtils.GeminiFeedback feedback)
    {
        Debug.Log($"✅ Gemini Feedback: {feedback.feedback} | Expression: {feedback.expression} | Suggestion: {feedback.suggestion} | Score: {feedback.score}");

        // 表情动画
        if (avatarAnimator != null && !string.IsNullOrEmpty(feedback.expression))
        {
            avatarAnimator.SetTrigger(feedback.expression);
            avatarAnimator.SetTrigger("neutral");
        }

        // Step 6: TTS播放
        StartCoroutine(PlaySuggestionAndContinue(feedback.suggestion));
    }

    IEnumerator PlaySuggestionAndContinue(string suggestion)
    {
        yield return TextToSpeechUtils.SpeakText(suggestion, openAiApiKey, ttsAudioSource);
        // ✅ 等待 TTS 播放完毕
        yield return new WaitWhile(() => ttsAudioSource.isPlaying);
        hasGeminiFeedback = true;
    }


}
