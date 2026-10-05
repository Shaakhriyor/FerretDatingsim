using UnityEngine;

[CreateAssetMenu(fileName = "NewStoryPhase", menuName = "DatingSim/Story Phase")]
public class StoryPhase : ScriptableObject
{
    [Header("Question / Scenario")]
    [TextArea(3, 5)] public string scenarioText; // What is happening in this scene?

    [Header("Option A (Usually Good)")]
    [TextArea(2, 3)] public string optionAText;
    public int optionAPoints;

    [Header("Option B (Usually Bad)")]
    [TextArea(2, 3)] public string optionBText;
    public int optionBPoints;
}