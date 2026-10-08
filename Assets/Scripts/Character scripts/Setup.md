# Prologue scene transitions and the bar scene

The flow is Prologue_Club -> Prologue_Doctor -> Prologue_Bar. Each scene uses its own starting story node. Clicking to advance after the final line of the final node fades to black, opens the next Unity scene, then fades in. Clicking while text is still typing only completes that text, as before. Linked story nodes and choices are resolved before moving scenes. A choice with no Next Node ends its dialogue chain immediately; connect choices to their reply nodes if more dialogue should follow.

## Install: one new script, three code updates

Stop Play mode.

- ADD VNSceneTransition.cs to your normal scripts folder, outside Editor. You do not attach it anywhere; the dialogue manager creates it at runtime.
- REPLACE THE CONTENTS of your existing VNDialogueManager.cs, VNHeartMenu.cs and VNDoctorIntro.cs with the matching supplied versions. Keep original script files/.meta and their attached components.
- KEEP all other scripts, including the line-trigger VNStoryNode/VNSaveData/VNSaveSystem versions, the blur shader, menu art scripts and right-click deletion scripts.

This package retains the dialogue-line doctor fade, voice-stop behavior, menu deletion support, and load-error logging from the preceding updates.

## Make Prologue_Bar

1. Stop Play mode. Open your working Prologue_Doctor scene and save it.
2. Use File > Save As to create Assets/Scenes/Prologue_Bar.unity. Check the scene name at the top of the Hierarchy says Prologue_Bar before editing.
3. In this NEW bar scene, remove the EarRinging GameObject, including VNDoctorIntro and its AudioSource. Remove doctor-only speaker objects such as DR. Keep MC, the dialogue canvas/manager, KarmaManager, VNHeartMenuCanvas, Main Camera and EventSystem.
4. Keep RainAmbience if the bar should still hear rain; otherwise disable it in the bar scene. A bar music/ambience object can use its own 2D AudioSource.
5. Select ScenePicture, assign the first whole-scene bar sprite, and set its Image Color to white with full alpha. Keep the existing full-screen RectTransform.
6. In Project, create Dating Sim > VN Story Node and name the NEW asset Bar_Opening. Do not edit Doctor_Opening to become the bar: that would change the doctor scene too.
7. Add your bar script's lines to Bar_Opening. Use MC for the player speakerId. Add VNDialogueSpeaker profiles for the other bar speakers, with matching speakerIds and display names. They do not need VNCharacter controllers when the art is already in the whole-scene pictures.
8. Set a line's Scene Picture only when the whole-scene image should change. Leave it empty to keep the previous image. Assign the first bar picture on the first line too, so entering/replaying the node restores the correct art.
9. On the bar's VNDialogueManager, assign Starting Node = Bar_Opening and enable Play Starting Node On Start.
10. Leave the bar manager's NEW Next Scene Name field EMPTY for now. Set Scene Fade Duration = 0.75. These are scene-specific settings; do not Apply All on the manager prefab or dialogue canvas prefab.
11. Save the bar scene. Add it to File > Build Profiles > Scene List and check its box. Club and Doctor should remain checked too.
12. With the new story assets and pictures ready, run Tools > VN > Refresh Save Assets. Save the scene again.

## Connect the scenes

Open each Unity scene outside Play mode, select its VNDialogueManager INSTANCE in the Hierarchy, and edit the Next Scene section:

| Unity scene | Next Scene Name | Scene Fade Duration |
| --- | --- | --- |
| Prologue_Club | Prologue_Doctor | 0.75 |
| Prologue_Doctor | Prologue_Bar | 0.75 |
| Prologue_Bar | leave empty | 0.75 |

Save each scene individually. Do not apply these differing values to a shared prefab. This field is on the scene's dialogue manager, not on the story node. The Story Node Next Node field still connects dialogue assets within that scene.

On the final node in Club or Doctor, leave Continue Without Choice > Next Node empty if this is the end of that scene's chain. If there are choices, connect each to its intended branch and let its final node end the chain. The transition starts only after that chain ends.

## State and sound

PlayerName and the recorded choices already live across scenes in VNSaveSystem. This update also carries current karma into the next scene after its managers have awakened. Each scene still needs its own KarmaManager and VNDialogueManager and their UI references. Do not make the scene cameras, dialogue canvases or dialogue managers DontDestroyOnLoad for this setup.

The scene change uses Single mode: old scene objects are unloaded. The transition's temporary black canvas is the only object this script makes persistent, and it removes itself afterward. Old audio fades out, new audio fades in; the previous global volume is restored. The doctor timer waits during the transition, and the new dialogue begins after the fade-in. Menus cannot be opened during this short handoff. Loading an existing save still uses the existing save restoration path; it does not start a new dialogue over that save.

An old save made after the entire dialogue had already ended remains an ended-dialogue save; it does not automatically replay the new transition. To test this feature, play through the last line, or load a save made before the scene ended.

## Verify in Unity

- First test the bar directly: its first image, dialogue, speaker names, choices and save menu should work.
- Play Doctor and click past the final line: it should fade into Bar and begin Bar_Opening.
- Play Club through its end: it should go to Doctor; the blur/ringing should still end on the selected doctor line.
- Check player name and karma survive each handoff. Test a new save in Bar and load an earlier Club save.
- Confirm the Console has no duplicate Audio Listener or missing-scene messages.

C# compilation checked against Unity 6000.3.21f1. This package has not been run in your actual Unity Editor, so the scene transitions and retained state need the above Play-mode checks.

The karma transfer uses Unity's [sceneLoaded callback](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.SceneManager-sceneLoaded.html), which runs after scene objects' Awake and OnEnable calls and before Start.
