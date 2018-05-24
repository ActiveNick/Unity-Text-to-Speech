using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CognitiveServicesTTS;
using System.IO;
using System.Media;
using System.Threading;
using System.Net;
using System.Threading.Tasks;
#if !UNITY_WSA
using System.Security.Cryptography.X509Certificates;
#endif

// IMPORTANT: THIS CODE ONLY WORKS WITH THE .NET 4.6 SCRIPTING RUNTIME

public class SpeechManager : MonoBehaviour {

    [Tooltip("The audio source where speech will be played.")]
    public AudioSource audioSource = null;

    // Access token used to make calls against the Cognitive Services Speech API
    string accessToken;

#if !UNITY_WSA
    /// <summary>
    /// This class is required to circumvent a TLS bug in Unity, otherwise Unity will throw
    /// an error stating the certificate is invalid. This is supposed to be fixed in Unity
    /// 2018.2. Note that UWP doesn't have this bug, only Mono, hence the conditional code.
    /// </summary>
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

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

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
        // use the link above and go get your own. The free tier gives you 5,000 free API transactions / month.

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

        // For testing only, don't trigger playback on startup in normal circumstances
        //Speak("This is a sample message synthesized by Microsoft Cognitive Services using high quality voices. Feel free to make it say something more interesting.");
    }

    /// <summary>
    /// This method is called once by the Unity coroutine once the speech is successfully synthesized.
    /// It will then attempt to play that audio file.
    /// Note that the playback will fail if the output audio format is not pcm encoded.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="args">The <see cref="GenericEventArgs{Stream}"/> instance containing the event data.</param>
    //private void PlayAudio(object sender, GenericEventArgs<Stream> args)
    private void PlayAudio(Stream audioStream)
    {
        Console.WriteLine("Playing audio stream");

        // Play the audio using Unity AudioSource, allowing us to benefit from effects,
        // spatialization, mixing, etc.

        // Get the size of the original stream
        var size = audioStream.Length;

        // Don't playback if the stream is empty
        if (size > 0)
        {
            try
            {
                // Create buffer
                byte[] buffer = new byte[(int)size];

                buffer = ReadToEnd(audioStream);

                // Convert raw WAV data into Unity audio data
                int sampleCount = 0;
                int frequency = 0;
                var unityData = ToUnityAudio(buffer, out sampleCount, out frequency);

                // Convert to an audio clip
                var clip = ToClip("Speech", unityData, sampleCount, frequency);

                // Set the source on the audio clip
                audioSource.clip = clip;

                // Play audio
                audioSource.Play();
            }
            catch (Exception ex)
            {
                Debug.Log("An error occurred during audio stream conversion and playback." 
                           + Environment.NewLine + ex.Message);
            }
        }
    }

    /// <summary>
    /// Unity Coroutine that monitors the Task used to synthesize speech from a text string.
    /// Once completed, it starts audio playback using the assigned audio source.
    /// </summary>
    /// <param name="speakTask"></param>
    /// <returns></returns>
    private IEnumerator WaitAndPlayRoutine(Task<Stream> speakTask)
    {
        // Yield control back to the main thread as long as the task is still running
        while (!speakTask.IsCompleted)
        {
            yield return null;
        }

        // Get audio stream result send it to play TTS audio
        Stream resultStream = speakTask.Result;
        if (resultStream != null)
        {
            PlayAudio(resultStream);
        }
    }

    /// <summary>
    /// Converts a text string into synthesized speech using Microsoft Cognitive Services, then
    /// starts audio playback using the assigned audio source.
    /// </summary>
    /// <param name="message"></param>
    public void Speak(string message)
    {
        try
        {
            Console.WriteLine("Starting TTSSample request code execution.");
            // Synthesis endpoint for old Bing Speech API: https://speech.platform.bing.com/synthesize
            // For new unified SpeechService API: https://westus.tts.speech.microsoft.com/cognitiveservices/v1
            // Note: new unified SpeechService API synthesis endpoint is per region
            string requestUri = "https://westus.tts.speech.microsoft.com/cognitiveservices/v1";
            Synthesize cortana = new Synthesize();

            // Reuse Synthesize object to minimize latency
            Task<Stream> Speaking = cortana.Speak(CancellationToken.None, new Synthesize.InputOptions()
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
            });

            // We can't await the task without blocking the main Unity thread, so we'll call a coroutine to
            // monitor completion and play audio when it's ready.
            StartCoroutine(WaitAndPlayRoutine(Speaking));
        }
        catch (Exception ex)
        {
            Debug.Log("An error occurred when attempting to synthesize speech audio."
                       + Environment.NewLine + ex.Message);
        }
    }

    /// <summary>
    /// Reads a stream from beginning to end, returning an array of bytes
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static byte[] ReadToEnd(Stream stream)
    {
        long originalPosition = 0;

        if (stream.CanSeek)
        {
            originalPosition = stream.Position;
            stream.Position = 0;
        }

        try
        {
            byte[] readBuffer = new byte[4096];

            int totalBytesRead = 0;
            int bytesRead;

            while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
            {
                totalBytesRead += bytesRead;

                if (totalBytesRead == readBuffer.Length)
                {
                    int nextByte = stream.ReadByte();
                    if (nextByte != -1)
                    {
                        byte[] temp = new byte[readBuffer.Length * 2];
                        Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                        Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                        readBuffer = temp;
                        totalBytesRead++;
                    }
                }
            }

            byte[] buffer = readBuffer;
            if (readBuffer.Length != totalBytesRead)
            {
                buffer = new byte[totalBytesRead];
                Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
            }
            return buffer;
        }
        finally
        {
            if (stream.CanSeek)
            {
                stream.Position = originalPosition;
            }
        }
    }

    /// <summary>
    /// Converts two bytes to one float in the range -1 to 1.
    /// </summary>
    /// <param name="firstByte">The first byte.</param>
    /// <param name="secondByte"> The second byte.</param>
    /// <returns>The converted float.</returns>
    private static float BytesToFloat(byte firstByte, byte secondByte)
    {
        // Convert two bytes to one short (little endian)
        short s = (short)((secondByte << 8) | firstByte);

        // Convert to range from -1 to (just below) 1
        return s / 32768.0F;
    }

    /// <summary>
    /// Converts an array of bytes to an integer.
    /// </summary>
    /// <param name="bytes"> The byte array.</param>
    /// <param name="offset"> An offset to read from.</param>
    /// <returns>The converted int.</returns>
    private static int BytesToInt(byte[] bytes, int offset = 0)
    {
        int value = 0;
        for (int i = 0; i < 4; i++)
        {
            value |= ((int)bytes[offset + i]) << (i * 8);
        }
        return value;
    }

    /// <summary>
    /// Dynamically creates an <see cref="AudioClip"/> that represents raw Unity audio data.
    /// </summary>
    /// <param name="name"> The name of the dynamically generated clip.</param>
    /// <param name="audioData">Raw Unity audio data.</param>
    /// <param name="sampleCount">The number of samples in the audio data.</param>
    /// <param name="frequency">The frequency of the audio data.</param>
    /// <returns>The <see cref="AudioClip"/>.</returns>
    private static AudioClip ToClip(string name, float[] audioData, int sampleCount, int frequency)
    {
        var clip = AudioClip.Create(name, sampleCount, 1, frequency, false);
        clip.SetData(audioData, 0);
        return clip;
    }

    /// <summary>
    /// Converts raw WAV data into Unity formatted audio data.
    /// </summary>
    /// <param name="wavAudio">The raw WAV data.</param>
    /// <param name="sampleCount">The number of samples in the audio data.</param>
    /// <param name="frequency">The frequency of the audio data.</param>
    /// <returns>The Unity formatted audio data. </returns>
    private static float[] ToUnityAudio(byte[] wavAudio, out int sampleCount, out int frequency)
    {
        // Determine if mono or stereo
        int channelCount = wavAudio[22];  // Speech audio data is always mono but read actual header value for processing

        // Get the frequency
        frequency = BytesToInt(wavAudio, 24);

        // Get past all the other sub chunks to get to the data subchunk:
        int pos = 12; // First subchunk ID from 12 to 16

        // Keep iterating until we find the data chunk (i.e. 64 61 74 61 ...... (i.e. 100 97 116 97 in decimal))
        while (!(wavAudio[pos] == 100 && wavAudio[pos + 1] == 97 && wavAudio[pos + 2] == 116 && wavAudio[pos + 3] == 97))
        {
            pos += 4;
            int chunkSize = wavAudio[pos] + wavAudio[pos + 1] * 256 + wavAudio[pos + 2] * 65536 + wavAudio[pos + 3] * 16777216;
            pos += 4 + chunkSize;
        }
        pos += 8;

        // Pos is now positioned to start of actual sound data.
        sampleCount = (wavAudio.Length - pos) / 2;  // 2 bytes per sample (16 bit sound mono)
        if (channelCount == 2) { sampleCount /= 2; }  // 4 bytes per sample (16 bit stereo)

        // Allocate memory (supporting left channel only)
        var unityData = new float[sampleCount];

        // Write to double array/s:
        int i = 0;
        while (pos < wavAudio.Length)
        {
            unityData[i] = BytesToFloat(wavAudio[pos], wavAudio[pos + 1]);
            pos += 2;
            if (channelCount == 2)
            {
                pos += 2;
            }
            i++;
        }

        return unityData;
    }
}
