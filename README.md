# Unity-Text-to-Speech
Sample app used to demonstrate the use of Microsoft Cognitive Services Text-to-Speech APIs from within Unity.

- **Unity version:** 2017.4.3f1

## Implementation Notes
- **THE CODE IN THIS SAMPLE APP ONLY WORKS WITH THE .NET 4.6 SCRIPTING RUNTIME**.
- This sample requires a Microsoft Cognitive Services Speech API key. FOR MORE INFO ON AUTHENTICATION AND HOW TO GET YOUR API KEY, [PLEASE VISIT THIS PAGE](https://docs.microsoft.com/en-us/azure/cognitive-services/speech/how-to/how-to-authentication). Don't use the key in this sample app, it's mine and I reserve the right to invalidate it if/when I want, use [this link](https://docs.microsoft.com/en-us/azure/cognitive-services/speech/how-to/how-to-authentication) and go get your own. The free tier gives you 5,000 free API transactions / month.
- The **CustomCertificatePolicy** class in SpeechManager.cs is required to circumvent a TLS bug in Unity, otherwise Unity will throw an error stating the certificate is invalid. This is supposed to be fixed in Unity 2018.2. Note that UWP doesn't have this bug, only Mono, hence the conditional code.

## Follow Me
* Twitter: [@ActiveNick](http://twitter.com/ActiveNick)
* Blog: [AgeofMobility.com](http://AgeofMobility.com)
* SlideShare: [http://www.slideshare.net/ActiveNick](http://www.slideshare.net/ActiveNick)

