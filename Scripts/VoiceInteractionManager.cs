using System.IO;
using System;
using UnityEngine;

public class VoiceInteractionManager : MonoBehaviour
{
    private string openAiApiKey;
    private string geminiApiKey;
    public Animator avatarAnimator;

    private AudioClip recordedClip;
    private string wavPath;
    public KeyCode testKey = KeyCode.M;
    public KeyCode testKey2 = KeyCode.N;
    public string selectedMicrophoneDevice = null;
    private string[] micDevices;
    public int maxRecordingBufferSeconds = 300;

    public AudioSource ttsAudioSource;

    public InterviewManager interviewManager;

    void Start()
    {
        openAiApiKey = EnvironmentManager.OpenAIApiKey;
        geminiApiKey = EnvironmentManager.GeminiApiKey;
        wavPath = Path.Combine(Application.persistentDataPath, "recorded.wav");
        micDevices = Microphone.devices;

        if (micDevices.Length == 0)
        {
            Debug.LogError("No microphone devices found!");
        }

        if (string.IsNullOrEmpty(selectedMicrophoneDevice) || !IsMicrophoneDeviceAvailable(selectedMicrophoneDevice))
        {
            if (micDevices.Length > 0)
            {
                selectedMicrophoneDevice = micDevices[0];
                Debug.LogWarning($"Using default microphone: {selectedMicrophoneDevice}");
            }
        }
        else
        {
            Debug.Log($"Using microphone: {selectedMicrophoneDevice}");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(testKey)) StartRecording();
        if (Input.GetKeyDown(testKey2)) StopRecording();
    }

    public void StartRecording()
    {
        micDevices = Microphone.devices;
        if (Microphone.IsRecording(selectedMicrophoneDevice))
        {
            Debug.LogWarning("Already recording.");
            return;
        }

        if (recordedClip != null)
        {
            Destroy(recordedClip);
            recordedClip = null;
        }

        recordedClip = Microphone.Start(selectedMicrophoneDevice, false, maxRecordingBufferSeconds, 16000);
        Debug.Log("🎙️ Recording started...");
    }

    public void StopRecording()
    {
        if (!Microphone.IsRecording(selectedMicrophoneDevice) && recordedClip == null)
        {
            Debug.LogWarning("Not recording.");
            return;
        }

        int position = Microphone.IsRecording(selectedMicrophoneDevice) ? Microphone.GetPosition(selectedMicrophoneDevice) : recordedClip.samples;
        Microphone.End(selectedMicrophoneDevice);

        if (recordedClip == null) return;

        float[] audioData = new float[position * recordedClip.channels];
        recordedClip.GetData(audioData, 0);
        AudioClip trimmedClip = AudioClip.Create("TrimmedClip", position, recordedClip.channels, recordedClip.frequency, false);
        trimmedClip.SetData(audioData, 0);

        VoiceUtils.SaveClipToWav(trimmedClip, wavPath);
        StartCoroutine(VoiceUtils.HandleSpeechToGemini(wavPath, openAiApiKey, geminiApiKey, OnGeminiFeedbackReceived, interviewManager));

        Destroy(recordedClip);
        recordedClip = null;
        Destroy(trimmedClip);
    }

    void OnGeminiFeedbackReceived(VoiceUtils.GeminiFeedback feedback)
    {
        Debug.Log($"✅ Feedback: {feedback.feedback}, Expression: {feedback.expression}, Suggestion: {feedback.suggestion}");

        if (avatarAnimator != null)
        {
            avatarAnimator.SetTrigger(feedback.expression);
        }

        StartCoroutine(TextToSpeechUtils.SpeakText(feedback.suggestion, openAiApiKey, ttsAudioSource));
    }

    private bool IsMicrophoneDeviceAvailable(string deviceName)
    {
        if (string.IsNullOrEmpty(deviceName) || micDevices == null) return false;
        foreach (string device in micDevices)
        {
            if (device == deviceName) return true;
        }
        return false;
    }

    public void StopRecordingWithCallback(Action<VoiceUtils.GeminiFeedback> onComplete)
    {
        if (!Microphone.IsRecording(selectedMicrophoneDevice) && recordedClip == null)
        {
            Debug.LogWarning("Not recording.");
            return;
        }

        int position = Microphone.IsRecording(selectedMicrophoneDevice)
            ? Microphone.GetPosition(selectedMicrophoneDevice)
            : recordedClip.samples;
        Microphone.End(selectedMicrophoneDevice);

        if (recordedClip == null) return;

        float[] audioData = new float[position * recordedClip.channels];
        recordedClip.GetData(audioData, 0);
        AudioClip trimmedClip = AudioClip.Create("TrimmedClip", position, recordedClip.channels, recordedClip.frequency, false);
        trimmedClip.SetData(audioData, 0);

        VoiceUtils.SaveClipToWav(trimmedClip, wavPath);
        StartCoroutine(VoiceUtils.HandleSpeechToGemini(wavPath, openAiApiKey, geminiApiKey, onComplete, interviewManager));

        Destroy(recordedClip);
        recordedClip = null;
        Destroy(trimmedClip);
    }

}
