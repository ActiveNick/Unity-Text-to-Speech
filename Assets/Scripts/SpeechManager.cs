using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CognitiveServicesTTS;
using System.IO;
using System.Media;
using System.Threading;
using System.Net;
#if !UNITY_WSA
using System.Security.Cryptography.X509Certificates;
#endif

// IMPORTANT: THIS CODE ONLY WORKS WITH THE .NET 4.6 SCRIPTING RUNTIME

public class SpeechManager : MonoBehaviour {

    string accessToken;

#if !UNITY_WSA
    // This class is required to circumvent a TLS bug in Unity, otherwise Unity will throw
    // an error stating the certificate is invalid. This is supposed to be fixed in Unity
    // 2018.2. Note that UWP doesn't have this bug, only Mono, hence the conditional code.
    private class CustomCertificatePolicy : ICertificatePolicy
    {
        public bool CheckValidationResult(ServicePoint sp,
            X509Certificate certificate, WebRequest request, int error)
        {
            // We force Unity to always validate the certificate as "true".
            return true;
        }
    }
#endif

    // Use this for initialization
    void Start () {
#if !UNITY_WSA
        // Unity will complain that the following statement is deprecated, however it's still working :)
        ServicePointManager.CertificatePolicy = new CustomCertificatePolicy();

        // This 'workaround' seems to work for the .NET Storage SDK, but not here. Leaving this for clarity
        //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
#endif

        // FOR MORE INFO ON AUTHENTICATION AND HOW TO GET YOUR API KEY, PLEASE VISIT
        // https://docs.microsoft.com/en-us/azure/cognitive-services/speech/how-to/how-to-authentication
        Authentication auth = new Authentication("https://api.cognitive.microsoft.com/sts/v1.0/issueToken",
                                                 "4d5a1beefe364f8986d63a877ebd51d5"); // INSERT-YOUR-BING-SPEECH-API-KEY-HERE
        // Don't use the key above, it's mine and I reserve the right to invalidate it if/when I want, 
        // use the link abiove and go get your own. The free tier gives you 5,000 free API transactions / month.

        try
        {
            accessToken = auth.GetAccessToken();
            Debug.Log($"Token: {accessToken}\n");
        }
        catch (Exception ex)
        {
            Debug.Log("Failed authentication.");
            Debug.Log(ex.ToString());
            Debug.Log(ex.Message);
            return;
        }

        Speak("This is a test of the Cognitive Service text-to-speech API from within the Unity game engine.");
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    /// <summary>
    /// This method is called once the audio returned from the service.
    /// It will then attempt to play that audio file.
    /// Note that the playback will fail if the output audio format is not pcm encoded.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">The <see cref="GenericEventArgs{Stream}"/> instance containing the event data.</param>
    private static void PlayAudio(object sender, GenericEventArgs<Stream> args)
    {
        Console.WriteLine(args.EventData);

        // For SoundPlayer to be able to play the wav file, it has to be encoded in PCM.
        // Use output audio format AudioOutputFormat.Riff16Khz16BitMonoPcm to do that.
        SoundPlayer player = new SoundPlayer(args.EventData);
        player.PlaySync();
        args.EventData.Dispose();
    }

    /// <summary>
    /// Handler an error when a TTS request failed.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="GenericEventArgs{Exception}"/> instance containing the event data.</param>
    private static void ErrorHandler(object sender, GenericEventArgs<Exception> e)
    {
        Console.WriteLine("Unable to complete the TTS request: [{0}]", e.ToString());
    }

    public void Speak(string message)
    {
        Console.WriteLine("Starting TTSSample request code execution.");
        // Synthesis endpoint for old Bing Speech API: https://speech.platform.bing.com/synthesize
        // For new unified SpeechService API: https://westus.tts.speech.microsoft.com/cognitiveservices/v1
        // Note: new unified SpeechService API synthesis endpoint is per region
        string requestUri = "https://westus.tts.speech.microsoft.com/cognitiveservices/v1";
        var cortana = new Synthesize();

        cortana.OnAudioAvailable += PlayAudio;
        cortana.OnError += ErrorHandler;

        // Reuse Synthesize object to minimize latency
        cortana.Speak(CancellationToken.None, new Synthesize.InputOptions()
        {
            RequestUri = new Uri(requestUri),
            // Text to be spoken.
            Text = message,
            VoiceType = Gender.Female,
            // Refer to the documentation for complete list of supported locales.
            Locale = "en-US",
            // You can also customize the output voice. Refer to the documentation to view the different
            // voices that the TTS service can output.
            VoiceName = "Microsoft Server Speech Text to Speech Voice (en-US, Jessa24KRUS)",

            // Service can return audio in different output format.
            OutputFormat = AudioOutputFormat.Riff24Khz16BitMonoPcm,
            AuthorizationToken = "Bearer " + accessToken,
        }).Wait();
    }
}
