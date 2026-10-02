using UnityEngine;

[CreateAssetMenu(fileName = "NewStoryNode", menuName = "DatingSim/Story Node")]
public class StoryNode : ScriptableObject
{
    [Header("Dialogue Flow")]
    [TextArea(3, 5)] public string[] storyLines; // Lines of text you click through sequentially

    [Header("Choices at the end of text")]
    public string leftButtonText;
    public int leftButtonPoints;
    public StoryNode leftNextNode; // The next story block if they choose left

    [Space]
    public string rightButtonText;
    public int rightButtonPoints;
    public StoryNode rightNextNode; // The next story block if they choose right
}