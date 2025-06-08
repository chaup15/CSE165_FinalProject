using System.Collections;
using UnityEngine;

public class InterviewFlowManager : MonoBehaviour
{
    public InterviewManager interviewManager;
    public AudioSource ttsAudioSource;
    public Animator avatarAnimator;

    public VoiceInteractionManager voiceInteractionManager;

    private string openAiApiKey;
    private string geminiApiKey;
    private bool isInterviewing = false;

    void Start()
    {
        openAiApiKey = EnvironmentManager.OpenAIApiKey;
        geminiApiKey = EnvironmentManager.GeminiApiKey;
    }

    public void StartInterview()
    {
        if (isInterviewing) return;
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
            yield return new WaitForSeconds(5f); // or less if you want


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
        interviewManager.ResetInterview();
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
