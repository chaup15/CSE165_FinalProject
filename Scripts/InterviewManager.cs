using System.Collections.Generic;
using UnityEngine;

public class InterviewManager : MonoBehaviour
{
    private string positionName;
    public class InterviewStage
    {
        public string Name;
        public string Prompt;

        public InterviewStage(string name, string prompt)
        {
            Name = name;
            Prompt = prompt;
        }
    }

    private List<InterviewStage> stages;
    private int currentStageIndex = 0;

    void Start()
    {
        positionName = "Software Engineer"; // Example position name, can be set dynamically
        stages = new List<InterviewStage>()
        {
            new InterviewStage("introduction", "Please introduce yourself and tell me why you're interested in this position."),
            new InterviewStage("project_experience", "Can you describe a project you've worked on recently and what your role was?"),
            new InterviewStage("technical_skills", "What technical skills do you have that are most relevant to this job?"),
            new InterviewStage("behavioral_question", "Tell me about a time you faced a challenge working in a team. How did you handle it?"),
            new InterviewStage("closing", "Do you have any questions for me?"),
            new InterviewStage("end", "Thank you for participating in the interview. We will get back to you soon.")
        };
    }

    public string GetCurrentPrompt()
    {
        return stages[currentStageIndex].Prompt;
    }

    public string GetCurrentStageName()
    {
        return stages[currentStageIndex].Name;
    }

    public void AdvanceStage()
    {
        if (currentStageIndex < stages.Count - 1)
        {
            currentStageIndex++;
        }
    }

    public bool IsInterviewEnded()
    {
        return currentStageIndex >= stages.Count - 1;
    }

    public void ResetInterview()
    {
        currentStageIndex = 0;
    }

    public string GetPositionName()
    {
        return positionName;
    }
    
    public void SetPositionName(string name)
    {
        positionName = name;
    }
}
