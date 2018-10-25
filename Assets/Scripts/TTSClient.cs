//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
//
// Microsoft Cognitive Services (formerly Project Oxford): https://www.microsoft.com/cognitive-services
//
// Microsoft Cognitive Services (formerly Project Oxford) GitHub:
// https://github.com/Microsoft/Cognitive-Speech-TTS
//
// Copyright (c) Microsoft Corporation
// All rights reserved.
//
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

// IMPORTANT: THIS CODE ONLY WORKS WITH THE .NET 4.6 SCRIPTING RUNTIME

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
// Note that Unity 2017.x doesn't recognize the namespace System.Net.Http by default.
// This is why we added mcs.rsp with "-r:System.Net.Http.dll" in it in the Assets folder.
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;

namespace CognitiveServicesTTS
{
    /// <summary>
    /// This class demonstrates how to get a valid O-auth token
    /// </summary>
    public class Authentication
    {
        private string AccessUri;
        private string apiKey;
        private string accessToken;
        private Timer accessTokenRenewer;

        private HttpClient client;

        //Access token expires every 10 minutes. Renew it every 9 minutes only.
        private const int RefreshTokenDuration = 9;

        public Authentication()
        {
            client = new HttpClient();
        }

        /// <summary>
        /// The Authenticate method needs to be called separately since it runs asynchronously
        /// and cannot be in the constructor, nor should it block the main Unity thread.
        /// </summary>
        /// <param name="issueTokenUri"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<string> Authenticate(string issueTokenUri, string apiKey)
        { 
            this.AccessUri = issueTokenUri;
            this.apiKey = apiKey;

            this.accessToken = await HttpClientPost(issueTokenUri, this.apiKey);

            // Renew the token every specfied minutes
            accessTokenRenewer = new Timer(new TimerCallback(OnTokenExpiredCallback),
                                           this,
                                           TimeSpan.FromMinutes(RefreshTokenDuration),
                                           TimeSpan.FromMilliseconds(-1));

            return accessToken;
        }

        public string GetAccessToken()
        {
            return this.accessToken;
        }

        private async void RenewAccessToken()
        {
            string newAccessToken = await HttpClientPost(AccessUri, this.apiKey);
            //swap the new token with old one
            //Note: the swap is thread unsafe
            this.accessToken = newAccessToken;
            Debug.Log(string.Format("Renewed token for user: {0} is: {1}",
                              this.apiKey,
                              this.accessToken));
        }

        private void OnTokenExpiredCallback(object stateInfo)
        {
            try
            {
                RenewAccessToken();
            }
            catch (Exception ex)
            {
                Debug.Log(string.Format("Failed renewing access token. Details: {0}", ex.Message));
            }
            finally
            {
                try
                {
                    accessTokenRenewer.Change(TimeSpan.FromMinutes(RefreshTokenDuration), TimeSpan.FromMilliseconds(-1));
                }
                catch (Exception ex)
                {
                    Debug.Log(string.Format("Failed to reschedule the timer to renew access token. Details: {0}", ex.Message));
                }
            }
        }

        /// <summary>
        /// Asynchronously calls the authentication service via HTTP POST to obtain 
        /// </summary>
        /// <param name="accessUri"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        private async Task<string> HttpClientPost(string accessUri, string apiKey)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, accessUri);
            request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
            request.Content = new StringContent("");

            HttpResponseMessage httpMsg = await client.SendAsync(request);
            Debug.Log($"Authentication Response status code: [{httpMsg.StatusCode}]");

            return await httpMsg.Content.ReadAsStringAsync();
        }
    }

    /// <summary>
    /// Generic event args
    /// </summary>
    /// <typeparam name="T">Any type T</typeparam>
    public class GenericEventArgs<T> : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenericEventArgs{T}" /> class.
        /// </summary>
        /// <param name="eventData">The event data.</param>
        public GenericEventArgs(T eventData)
        {
            this.EventData = eventData;
        }

        /// <summary>
        /// Gets the event data.
        /// </summary>
        public T EventData { get; private set; }
    }

    /// <summary>
    /// Gender of the voice.
    /// </summary>
    public enum Gender
    {
        Female,
        Male
    }

    /// <summary>
    /// Voice output formats.
    /// </summary>
    public enum AudioOutputFormat
    {
        /// <summary>
        /// raw-8khz-8bit-mono-mulaw request output audio format type.
        /// </summary>
        Raw8Khz8BitMonoMULaw,

        /// <summary>
        /// raw-16khz-16bit-mono-pcm request output audio format type.
        /// </summary>
        Raw16Khz16BitMonoPcm,

        /// <summary>
        /// riff-8khz-8bit-mono-mulaw request output audio format type.
        /// </summary>
        Riff8Khz8BitMonoMULaw,

        /// <summary>
        /// riff-16khz-16bit-mono-pcm request output audio format type.
        /// </summary>
        Riff16Khz16BitMonoPcm,

        // <summary>
        /// ssml-16khz-16bit-mono-silk request output audio format type.
        /// It is a SSML with audio segment, with audio compressed by SILK codec
        /// </summary>
        Ssml16Khz16BitMonoSilk,

        /// <summary>
        /// raw-16khz-16bit-mono-truesilk request output audio format type.
        /// Audio compressed by SILK codec
        /// </summary>
        Raw16Khz16BitMonoTrueSilk,

        /// <summary>
        /// ssml-16khz-16bit-mono-tts request output audio format type.
        /// It is a SSML with audio segment, and it needs tts engine to play out
        /// </summary>
        Ssml16Khz16BitMonoTts,

        /// <summary>
        /// audio-16khz-128kbitrate-mono-mp3 request output audio format type.
        /// </summary>
        Audio16Khz128KBitRateMonoMp3,

        /// <summary>
        /// audio-16khz-64kbitrate-mono-mp3 request output audio format type.
        /// </summary>
        Audio16Khz64KBitRateMonoMp3,

        /// <summary>
        /// audio-16khz-32kbitrate-mono-mp3 request output audio format type.
        /// </summary>
        Audio16Khz32KBitRateMonoMp3,

        /// <summary>
        /// audio-16khz-16kbps-mono-siren request output audio format type.
        /// </summary>
        Audio16Khz16KbpsMonoSiren,

        /// <summary>
        /// riff-16khz-16kbps-mono-siren request output audio format type.
        /// </summary>
        Riff16Khz16KbpsMonoSiren,

        /// <summary>
        /// raw-24khz-16bit-mono-truesilk request output audio format type.
        /// </summary>
        Raw24Khz16BitMonoTrueSilk,

        /// <summary>
        /// raw-24khz-16bit-mono-pcm request output audio format type.
        /// </summary>
        Raw24Khz16BitMonoPcm,

        /// <summary>
        /// riff-24khz-16bit-mono-pcm request output audio format type.
        /// </summary>
        Riff24Khz16BitMonoPcm,

        /// <summary>
        /// audio-24khz-48kbitrate-mono-mp3 request output audio format type.
        /// </summary>
        Audio24Khz48KBitRateMonoMp3,

        /// <summary>
        /// audio-24khz-96kbitrate-mono-mp3 request output audio format type.
        /// </summary>
        Audio24Khz96KBitRateMonoMp3,

        /// <summary>
        /// audio-24khz-160kbitrate-mono-mp3 request output audio format type.
        /// </summary>
        Audio24Khz160KBitRateMonoMp3
    }

    /// <summary>
    /// List of all voices currently implemented in this sample. This may not include all the
    /// voices supported by the Cognitive Services Text-to-Speech API. Please visit the following
    /// link to get the most up-to-date list of supported languages:
    /// https://docs.microsoft.com/en-us/azure/cognitive-services/speech/api-reference-rest/bingvoiceoutput
    /// Don't forget to edit ConvertVoiceNametoString() below if you add more values to this enum.
    /// </summary>
    public enum VoiceName
    {
        enAUCatherine,
        enAUHayleyRUS,
        enCALinda,
        enCAHeatherRUS,
        enGBSusanApollo,
        enGBHazelRUS,
        enGBGeorgeApollo,
        enIESean,
        enINHeeraApollo,
        enINPriyaRUS,
        enINRaviApollo,
        enUSZiraRUS,
        enUSJessaRUS,
        enUSBenjaminRUS,
        deATMichael,
        deCHKarsten,
        deDEHedda,
        deDEHeddaRUS,
        deDEStefanApollo,
        esESLauraApollo,
        esESHelenaRUS,
        esESPabloApollo,
        esMXHildaRUS,
        esMXRaulApollo,
        frCACaroline,
        frCAHarmonieRUS,
        frCHGuillaume,
        frFRJulieApollo,
        frFRHortenseRUS
    }

    /// <summary>
    /// Sample synthesize request
    /// </summary>
    public class Synthesize
    {
        /// <summary>
        /// Generates SSML.
        /// </summary>
        /// <param name="locale">The locale.</param>
        /// <param name="gender">The gender.</param>
        /// <param name="name">The voice name.</param>
        /// <param name="text">The text input.</param>
        private string GenerateSsml(string locale, string gender, VoiceName voicename, string text, int pitchdelta)
        {
            string voice = ConvertVoiceNametoString(voicename);

            XNamespace xmlns = "http://www.w3.org/2001/10/synthesis";
            var ssmlDoc = new XDocument(
                              new XElement(xmlns + "speak",
                                  new XAttribute("version", "1.0"),
                                  new XAttribute(XNamespace.Xml + "lang", locale), // was locked to "en-US"
                                  new XElement("voice",
                                      new XAttribute(XNamespace.Xml + "lang", locale),
                                      new XAttribute(XNamespace.Xml + "gender", gender),
                                      new XAttribute("name", voice),
                                      new XElement("prosody",
                                            new XAttribute("pitch", pitchdelta.ToString() + "Hz"),
                                                text))));

            return ssmlDoc.ToString();
        }

        private HttpClient client;
        private HttpClientHandler handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="Synthesize"/> class.
        /// </summary>
        public Synthesize()
        {
            var cookieContainer = new CookieContainer();
            handler = new HttpClientHandler() { CookieContainer = new CookieContainer(), UseProxy = false };
            client = new HttpClient(handler);
        }

        ~Synthesize()
        {
            client.Dispose();
            handler.Dispose();
        }

        /// <summary>
        /// Sends the specified text to be spoken to the TTS service and saves the response audio to a file.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task</returns>
        public async Task<Stream> Speak(CancellationToken cancellationToken, InputOptions inputOptions)
        {
            client.DefaultRequestHeaders.Clear();
            foreach (var header in inputOptions.Headers)
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }

            var genderValue = "";
            switch (inputOptions.VoiceType)
            {
                case Gender.Male:
                    genderValue = "Male";
                    break;

                case Gender.Female:
                default:
                    genderValue = "Female";
                    break;
            }

            var request = new HttpRequestMessage(HttpMethod.Post, inputOptions.RequestUri)
            {
                Content = new StringContent(GenerateSsml(inputOptions.Locale, genderValue, inputOptions.VoiceName, inputOptions.Text, inputOptions.PitchDelta))
            };

            var httpMsg = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
            Debug.Log($"Response status code: [{httpMsg.StatusCode}]");

            Stream httpStream = await httpMsg.Content.ReadAsStreamAsync();

            return httpStream;
        }

        /// <summary>
        /// Converts a specific VoioceName enum option into its string counterpart as expected
        /// by the API when building the SSML string that is sent to Cognitive Services.
        /// Make sure that each option in the enum is included in the switch below.
        /// </summary>
        /// <param name="voicename"></param>
        /// <returns></returns>
        public string ConvertVoiceNametoString(VoiceName voicename)
        {
            switch (voicename)
            {
                case VoiceName.enAUCatherine:
                    return "Microsoft Server Speech Text to Speech Voice (en-AU, Catherine)";
                case VoiceName.enAUHayleyRUS:
                    return "Microsoft Server Speech Text to Speech Voice (en-AU, HayleyRUS)";
                case VoiceName.enCALinda:
                    return "Microsoft Server Speech Text to Speech Voice (en-CA, Linda)";
                case VoiceName.enCAHeatherRUS:
                    return "Microsoft Server Speech Text to Speech Voice (en-CA, HeatherRUS)";
                case VoiceName.enGBSusanApollo:
                    return "Microsoft Server Speech Text to Speech Voice (en-GB, Susan, Apollo)";
                case VoiceName.enGBHazelRUS:
                    return "Microsoft Server Speech Text to Speech Voice (en-GB, HazelRUS)";
                case VoiceName.enGBGeorgeApollo:
                    return "Microsoft Server Speech Text to Speech Voice (en-GB, George, Apollo)";
                case VoiceName.enIESean:
                    return "Microsoft Server Speech Text to Speech Voice (en-IE, Sean)";
                case VoiceName.enINHeeraApollo:
                    return "Microsoft Server Speech Text to Speech Voice (en-IN, Heera, Apollo)";
                case VoiceName.enINPriyaRUS:
                    return "Microsoft Server Speech Text to Speech Voice (en-IN, PriyaRUS)";
                case VoiceName.enINRaviApollo:
                    return "Microsoft Server Speech Text to Speech Voice (en-IN, Ravi, Apollo)";
                case VoiceName.enUSZiraRUS:
                    return "Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)";
                case VoiceName.enUSJessaRUS:
                    return "Microsoft Server Speech Text to Speech Voice (en-US, JessaRUS)";
                case VoiceName.enUSBenjaminRUS:
                    return "Microsoft Server Speech Text to Speech Voice (en-US, BenjaminRUS)";
                case VoiceName.deATMichael:
                    return "Microsoft Server Speech Text to Speech Voice (de-AT, Michael)";
                case VoiceName.deCHKarsten:
                    return "Microsoft Server Speech Text to Speech Voice (de-CH, Karsten)";
                case VoiceName.deDEHedda:
                    return "Microsoft Server Speech Text to Speech Voice (de-DE, Hedda)";
                case VoiceName.deDEHeddaRUS:
                    return "Microsoft Server Speech Text to Speech Voice (de-DE, HeddaRUS)";
                case VoiceName.deDEStefanApollo:
                    return "Microsoft Server Speech Text to Speech Voice (de-DE, Stefan, Apollo)";
                case VoiceName.esESHelenaRUS:
                    return "Microsoft Server Speech Text to Speech Voice (es-ES, HelenaRUS)";
                case VoiceName.esESLauraApollo:
                    return "Microsoft Server Speech Text to Speech Voice (es-ES, Laura, Apollo)";
                case VoiceName.esESPabloApollo:
                    return "Microsoft Server Speech Text to Speech Voice (es-ES, Pablo, Apollo)";
                case VoiceName.esMXHildaRUS:
                    return "Microsoft Server Speech Text to Speech Voice (es-MX, HildaRUS)";
                case VoiceName.esMXRaulApollo:
                    return "Microsoft Server Speech Text to Speech Voice (es-MX, Raul, Apollo)";
                case VoiceName.frCACaroline:
                    return "Microsoft Server Speech Text to Speech Voice (fr-CA, Caroline)";
                case VoiceName.frCAHarmonieRUS:
                    return "Microsoft Server Speech Text to Speech Voice (fr-CA, HarmonieRUS)";
                case VoiceName.frCHGuillaume:
                    return "Microsoft Server Speech Text to Speech Voice (fr-CH, Guillaume)";
                case VoiceName.frFRJulieApollo:
                    return "Microsoft Server Speech Text to Speech Voice (fr-FR, Julie, Apollo)";
                case VoiceName.frFRHortenseRUS:
                    return "Microsoft Server Speech Text to Speech Voice (fr-FR, HortenseRUS)";
                default:
                    return "Microsoft Server Speech Text to Speech Voice (en-US, JessaRUS)";
            }
        }

        public string GetVoiceLocale(VoiceName voicename)
        {
            return ConvertVoiceNametoString(voicename).Substring(46, 5);
        }

        /// <summary>
        /// Inputs Options for the TTS Service.
        /// </summary>
        public class InputOptions
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Input"/> class.
            /// </summary>
            public InputOptions()
            {
                this.Locale = "en-us";
                this.VoiceName = VoiceName.enUSJessaRUS;
                // Default to Riff24Khz16BitMonoPcm output format.
                this.OutputFormat = AudioOutputFormat.Riff24Khz16BitMonoPcm;
                this.PitchDelta = 0;
            }

            /// <summary>
            /// Gets or sets the request URI.
            /// </summary>
            public Uri RequestUri { get; set; }

            /// <summary>
            /// Gets or sets the audio output format.
            /// </summary>
            public AudioOutputFormat OutputFormat { get; set; }

            /// <summary>
            /// Gets or sets the headers.
            /// </summary>
            public IEnumerable<KeyValuePair<string, string>> Headers
            {
                get
                {
                    List<KeyValuePair<string, string>> toReturn = new List<KeyValuePair<string, string>>();
                    toReturn.Add(new KeyValuePair<string, string>("Content-Type", "application/ssml+xml"));

                    string outputFormat;

                    switch (this.OutputFormat)
                    {
                        case AudioOutputFormat.Raw16Khz16BitMonoPcm:
                            outputFormat = "raw-16khz-16bit-mono-pcm";
                            break;

                        case AudioOutputFormat.Raw8Khz8BitMonoMULaw:
                            outputFormat = "raw-8khz-8bit-mono-mulaw";
                            break;

                        case AudioOutputFormat.Riff16Khz16BitMonoPcm:
                            outputFormat = "riff-16khz-16bit-mono-pcm";
                            break;

                        case AudioOutputFormat.Riff8Khz8BitMonoMULaw:
                            outputFormat = "riff-8khz-8bit-mono-mulaw";
                            break;

                        case AudioOutputFormat.Ssml16Khz16BitMonoSilk:
                            outputFormat = "ssml-16khz-16bit-mono-silk";
                            break;

                        case AudioOutputFormat.Raw16Khz16BitMonoTrueSilk:
                            outputFormat = "raw-16khz-16bit-mono-truesilk";
                            break;

                        case AudioOutputFormat.Ssml16Khz16BitMonoTts:
                            outputFormat = "ssml-16khz-16bit-mono-tts";
                            break;

                        case AudioOutputFormat.Audio16Khz128KBitRateMonoMp3:
                            outputFormat = "audio-16khz-128kbitrate-mono-mp3";
                            break;

                        case AudioOutputFormat.Audio16Khz64KBitRateMonoMp3:
                            outputFormat = "audio-16khz-64kbitrate-mono-mp3";
                            break;

                        case AudioOutputFormat.Audio16Khz32KBitRateMonoMp3:
                            outputFormat = "audio-16khz-32kbitrate-mono-mp3";
                            break;

                        case AudioOutputFormat.Audio16Khz16KbpsMonoSiren:
                            outputFormat = "audio-16khz-16kbps-mono-siren";
                            break;

                        case AudioOutputFormat.Riff16Khz16KbpsMonoSiren:
                            outputFormat = "riff-16khz-16kbps-mono-siren";
                            break;
                        case AudioOutputFormat.Raw24Khz16BitMonoPcm:
                            outputFormat = "raw-24khz-16bit-mono-pcm";
                            break;
                        case AudioOutputFormat.Riff24Khz16BitMonoPcm:
                            outputFormat = "riff-24khz-16bit-mono-pcm";
                            break;
                        case AudioOutputFormat.Audio24Khz48KBitRateMonoMp3:
                            outputFormat = "audio-24khz-48kbitrate-mono-mp3";
                            break;
                        case AudioOutputFormat.Audio24Khz96KBitRateMonoMp3:
                            outputFormat = "audio-24khz-96kbitrate-mono-mp3";
                            break;
                        case AudioOutputFormat.Audio24Khz160KBitRateMonoMp3:
                            outputFormat = "audio-24khz-160kbitrate-mono-mp3";
                            break;
                        default:
                            outputFormat = "riff-16khz-16bit-mono-pcm";
                            break;
                    }

                    toReturn.Add(new KeyValuePair<string, string>("X-Microsoft-OutputFormat", outputFormat));
                    // authorization Header
                    toReturn.Add(new KeyValuePair<string, string>("Authorization", this.AuthorizationToken));
                    // Refer to the doc
                    toReturn.Add(new KeyValuePair<string, string>("X-Search-AppId", "07D3234E49CE426DAA29772419F436CA"));
                    // Refer to the doc
                    toReturn.Add(new KeyValuePair<string, string>("X-Search-ClientID", "1ECFAE91408841A480F00935DC390960"));
                    // The software originating the request
                    toReturn.Add(new KeyValuePair<string, string>("User-Agent", "UnityTTSClient"));

                    return toReturn;
                }
                set
                {
                    Headers = value;
                }
            }

            /// <summary>
            /// Gets or sets the locale.
            /// </summary>
            public String Locale { get; set; }

            /// <summary>
            /// Gets or sets the type of the voice; male/female.
            /// </summary>
            public Gender VoiceType { get; set; }

            /// <summary>
            /// Gets or sets the name of the voice.
            /// </summary>
            public VoiceName VoiceName { get; set; }

            /// <summary>
            /// Gets or sets the pitch delta/modifier in Hz (plus/minus)
            /// </summary>
            public int PitchDelta { get; set; }

            /// <summary>
            /// Authorization Token.
            /// </summary>
            public string AuthorizationToken { get; set; }

            /// <summary>
            /// Gets or sets the text.
            /// </summary>
            public string Text { get; set; }
        }
    }
}