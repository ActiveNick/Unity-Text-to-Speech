using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CognitiveServicesTTS;
using System;
using System.Threading.Tasks;

public class UIManager : MonoBehaviour {

    public SpeechManager speech;
    public InputField input;
    public InputField pitch;
    public Toggle useSDK;
    public Dropdown voicelist;
    public GameObject shape;

    private void Start()
    {
        pitch.text = "0";

        List<string> voices = new List<string>();
        foreach (VoiceName voice in Enum.GetValues(typeof(VoiceName)))
        {
            voices.Add(voice.ToString());
        }
        voicelist.AddOptions(voices);
        voicelist.value = (int)VoiceName.enUSJessaRUS;
    }

    // The spinning cube is only used to verify that speech synthesis doesn't introduce
    // game loop blocking code.
    public void Update()
    {
        if (shape != null)
            shape.transform.Rotate(Vector3.up, 1);
    }

    /// <summary>
    /// Speech synthesis can be called via REST API or Speech Service SDK plugin for Unity
    /// </summary>
    public async void SpeechPlayback()
    {
        if (speech.isReady)
        {
            string msg = input.text;
            speech.voiceName = (VoiceName)voicelist.value;
            speech.VoicePitch = int.Parse(pitch.text);
            if (useSDK.isOn)
            {
                // Required to insure non-blocking code in the main Unity UI thread.
                await Task.Run(() => speech.SpeakWithSDKPlugin(msg));
            }
            else
            {
                // This code is non-blocking by default, no need to run in background
                speech.SpeakWithRESTAPI(msg);
            }
        } else
        {
            Debug.Log("SpeechManager is not ready. Wait until authentication has completed.");
        }
    }

    public void ClearText()
    {
        input.text = "";
    }
}
