using System;
using UnityEngine;

/// <summary>
/// Streams audio recorded from the iPad microphone back to the server.
/// Uses 16kHz sample rate, 1 channel.
/// </summary>
[RequireComponent(typeof(MastClient))]
public class MicStreaming : MonoBehaviour
{
    [Header("Microphone Settings")]
    public int sampleRate = 16000;
    public int chunkLengthSeconds = 1;

    private string _micDevice;
    private AudioClip _micClip;
    private int _lastSamplePosition = 0;
    private MastClient _mastClient;
    private float _timer = 0f;

    void Start()
    {
        _mastClient = GetComponent<MastClient>();

        // Find the first microphone device available
        if (Microphone.devices.Length > 0)
        {
            _micDevice = Microphone.devices[0];
            // Start recording in a loop
            _micClip = Microphone.Start(_micDevice, true, 10, sampleRate);
        }
        else
        {
            Debug.LogWarning("[MAST Mic] No microphone found.");
        }
    }

    void Update()
    {
        if (_micClip == null || _mastClient == null) return;

        _timer += Time.deltaTime;
        if (_timer >= chunkLengthSeconds)
        {
            _timer = 0f;
            CaptureAndStreamChunk();
        }
    }

    void CaptureAndStreamChunk()
    {
        if (_micDevice == null) return;

        int currentPosition = Microphone.GetPosition(_micDevice);
        if (currentPosition < 0) return;

        // Calculate sample count
        int sampleCount = 0;
        if (currentPosition > _lastSamplePosition)
        {
            sampleCount = currentPosition - _lastSamplePosition;
        }
        else
        {
            // Wrapped around circular buffer
            sampleCount = (_micClip.samples - _lastSamplePosition) + currentPosition;
        }

        if (sampleCount <= 0) return;

        float[] samples = new float[sampleCount];
        _micClip.GetData(samples, _lastSamplePosition);
        _lastSamplePosition = currentPosition;

        // Convert float samples (-1.0 to 1.0) to 16-bit PCM bytes
        byte[] pcmData = ConvertToPCM16(samples);

        // Convert to Base64
        string base64Audio = Convert.ToBase64String(pcmData);

        // Create Payload
        var payload = new AudioPayload
        {
            type = "audio",
            data = base64Audio
        };

        string json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
        _mastClient.SendData(json);
    }

    byte[] ConvertToPCM16(float[] samples)
    {
        byte[] pcm = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            short value = (short)(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);
            byte[] bytes = BitConverter.GetBytes(value);
            pcm[i * 2] = bytes[0];
            pcm[i * 2 + 1] = bytes[1];
        }
        return pcm;
    }

    [Serializable]
    private class AudioPayload
    {
        public string type;
        public string data;
    }
}
