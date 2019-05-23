# Unity-Text-to-Speech
Sample app used to demonstrate the use of [Microsoft Cognitive Services Speech Service](https://docs.microsoft.com/azure/cognitive-services/speech-service/) [Text-to-Speech (TTS) APIs](https://azure.microsoft.com/services/cognitive-services/text-to-speech/) from within the Unity game engine. These cloud-based APIs provide access to higher quality voices, providing consistency across all client platforms. Check out the [Text-to-Speech Overview page](https://azure.microsoft.com/services/cognitive-services/text-to-speech/) to try out & hear a sample of these voices.

This sample provides a self-contained **SpeechManager** component that is easy to reuse in your own Unity projects. Given that Cognitive Services are cloud APIs, they are therefore not available when offline. It is recommended to fallback on local platform-specific Text-to-Speech APIs when offline.

The code in this sample demonstrates two ways to call the Speech Synthesis service. The first makes use of the [Text-to-Speech REST API endpoint](https://docs.microsoft.com/azure/cognitive-services/speech-service/rest-apis). When running the sample, make sure the checkbox "Use SDK Plugin" is unchecked to use this method. The second approach uses the new Cognitive Services Speech SDK for Unity, which features a plugin for Windows Desktop, UWP and Android, available as part of of a [Unity package](https://aka.ms/csspeech/unitypackage). **IMPORTANT: The plugin is not included with this repo and you must import it in your project or the sample won't run. [You can download the Unity package from here](https://aka.ms/csspeech/unitypackage)**.

- **Unity version:** 2018.3.14f1
- **Target platforms tested (REST API):** Unity Editor, Windows Desktop (standalone x64), UWP, Android, iOS
- **Target platforms tested (Plugin):** Unity Editor

## Implementation Notes
- **THE CODE IN THIS SAMPLE APP ONLY WORKS WITH THE .NET 4.6 SCRIPTING RUNTIME**. Additionally, there seems to be an issue [with the use of HttpClient in Unity 2018.1](https://forum.unity.com/threads/httpclient-not-available-in-2018-1-with-net-4-x.532684/).
- This sample requires a Microsoft Cognitive Services Speech API key. FOR MORE INFO ON AUTHENTICATION AND HOW TO GET YOUR API KEY, [PLEASE VISIT THIS PAGE](https://docs.microsoft.com/azure/cognitive-services/speech/how-to/how-to-authentication). Don't use the key in this sample app, it's mine and I reserve the right to invalidate it if/when I want, use [this link](https://docs.microsoft.com/azure/cognitive-services/speech/how-to/how-to-authentication) and go get your own. The free tier gives you 5,000 free API transactions / month.
- The **CustomCertificatePolicy** class in SpeechManager.cs is required to circumvent a TLS bug in Unity 2018.1, otherwise Unity will throw an error stating the certificate is invalid. This temporary workaround simply bypasses certificate validation. This has been fixed in Unity 2018.2+ and will be removed after more testing is done. Note that UWP doesn't have this bug, only Mono, hence the conditional code.
- **TTSClient.cs** contains a **VoiceName** enum with all the voices currently implemented in this sample. *This may not include all the voices supported by the Cognitive Services Text-to-Speech API*. [Please visit this page to get the most up-to-date list of supported languages](https://docs.microsoft.com/azure/cognitive-services/speech/api-reference-rest/bingvoiceoutput). Don't forget to edit **ConvertVoiceNametoString()** if you add more values to this enum to use more supported languages.
- To change the pitch of the voice playback, use a delta value in Hz. Default is 0 (zero). Typical accepted delta changes can range from -10 to 10. *Note: This currently only works with the REST API*.

## Follow Me
* Twitter: [@ActiveNick](http://twitter.com/ActiveNick)
* SlideShare: [http://www.slideshare.net/ActiveNick](http://www.slideshare.net/ActiveNick)

