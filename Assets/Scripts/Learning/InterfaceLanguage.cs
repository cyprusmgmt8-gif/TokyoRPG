using UnityEngine;

/// <summary>
/// The language used for translations in the learning panel.
/// Change Language Code here when you add another language. Do not change the scripts.
/// </summary>
[CreateAssetMenu(fileName = "InterfaceLanguage", menuName = "Tokyo RPG/Interface Language")]
public class InterfaceLanguage : ScriptableObject
{
    [Tooltip("Current interface language. English is en.")]
    [SerializeField] string languageCode = "en";

    public string LanguageCode => string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode.Trim();
}
