using UnityEditor;
using UnityEngine;

/// <summary>
/// Plays a beep during Play mode so the speaker can be checked.
/// Menu: Tokyo RPG / Audio / Play Test Tone.
/// The beep is not Japanese speech.
/// </summary>
public static class AudioTestMenu
{
    [MenuItem("Tokyo RPG/Audio/Play Test Tone")]
    static void PlayTestTone()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("Enter Play mode first. Tokyo RPG / Audio / Play Test Tone plays a beep, not Japanese speech.");
            return;
        }

        JapaneseAudioPlayer.Ensure().PlayTestTone();
        Debug.Log("Playing a test tone. This is not Japanese pronunciation.");
    }
}
