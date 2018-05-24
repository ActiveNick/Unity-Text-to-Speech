using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CognitiveServicesTTS;
using System;

public class UIManager : MonoBehaviour {

    public SpeechManager speech;
    public InputField input;
    public Dropdown voicelist;

    private void Start()
    {
        List<string> voices = new List<string>();
        foreach (VoiceName voice in Enum.GetValues(typeof(VoiceName)))
        {
            voices.Add(voice.ToString());
        }
        voicelist.AddOptions(voices);
        voicelist.value = (int)VoiceName.enUSJessaRUS;
    }

    public void SpeechPlayback()
    {
        string msg = input.text;
        speech.voiceName = (VoiceName)voicelist.value;
        speech.Speak(msg);
    }

    public void ClearText()
    {
        input.text = "";
    }
}
