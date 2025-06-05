using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class EnvironmentManager : MonoBehaviour
{
    public static string OpenAIApiKey { get; private set; }
    public static string GeminiApiKey { get; private set; }

    void Awake()
    {
        string envFilePath = Path.Combine(Application.dataPath, "./CSE165_FinalProject/Scripts/", ".env"); // .env

        if (File.Exists(envFilePath))
        {
            Dictionary<string, string> envVariables = new Dictionary<string, string>();
            string[] lines = File.ReadAllLines(envFilePath);

            foreach (string line in lines)
            {
                string[] parts = line.Split('=');
                if (parts.Length == 2)
                {
                    envVariables[parts[0].Trim()] = parts[1].Trim();
                }
            }

            if (envVariables.ContainsKey("OPENAI_API_KEY"))
            {
                OpenAIApiKey = envVariables["OPENAI_API_KEY"];
                Debug.Log("OpenAI API Key loaded successfully.");
            }

            if (envVariables.ContainsKey("GEMINI_API_KEY"))
            {
                GeminiApiKey = envVariables["GEMINI_API_KEY"];
                Debug.Log("Gemini API Key loaded successfully.");
            }
        }
        else
        {
            Debug.LogError(".env file not found at: " + envFilePath);
        }
    }
}