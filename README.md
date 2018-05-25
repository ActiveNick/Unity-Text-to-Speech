# Unity-Text-to-Speech
Sample app used to demonstrate the use of Microsoft Cognitive Services Text-to-Speech APIs from within Unity. These cloud-based APIs provide access to higher quality voices, providing consistency across all client platforms.

- **Unity version:** 2017.4.3f1
- **Target platforms tested:** Windows Desktop (standalone x64), UWP, Android

## Implementation Notes
- **THE CODE IN THIS SAMPLE APP ONLY WORKS WITH THE .NET 4.6 SCRIPTING RUNTIME**.
- This sample requires a Microsoft Cognitive Services Speech API key. FOR MORE INFO ON AUTHENTICATION AND HOW TO GET YOUR API KEY, [PLEASE VISIT THIS PAGE](https://docs.microsoft.com/en-us/azure/cognitive-services/speech/how-to/how-to-authentication). Don't use the key in this sample app, it's mine and I reserve the right to invalidate it if/when I want, use [this link](https://docs.microsoft.com/en-us/azure/cognitive-services/speech/how-to/how-to-authentication) and go get your own. The free tier gives you 5,000 free API transactions / month.
- The **CustomCertificatePolicy** class in SpeechManager.cs is required to circumvent a TLS bug in Unity, otherwise Unity will throw an error stating the certificate is invalid. This is supposed to be fixed in Unity 2018.2. Note that UWP doesn't have this bug, only Mono, hence the conditional code.
- **TTSClient.cs** contains a **VoiceName** enum with all the voices currently implemented in this sample. *This may not include all the voices supported by the Cognitive Services Text-to-Speech API*. [Please visit this page to get the most up-to-date list of supported languages](https://docs.microsoft.com/en-us/azure/cognitive-services/speech/api-reference-rest/bingvoiceoutput). Don't forget to edit **ConvertVoiceNametoString()** if you add more values to this enum to use more supported languages.

## Follow Me
* Twitter: [@ActiveNick](http://twitter.com/ActiveNick)
* Blog: [AgeofMobility.com](http://AgeofMobility.com)
* SlideShare: [http://www.slideshare.net/ActiveNick](http://www.slideshare.net/ActiveNick)

