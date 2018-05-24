using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {

    public SpeechManager speech;
    public InputField input;

    public void SpeechPlayback()
    {
        string msg = input.text;
        speech.Speak(msg);
    }

    public void ClearText()
    {
        input.text = "";
    }
}
