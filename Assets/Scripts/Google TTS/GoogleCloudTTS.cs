using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;

// Add this at the top of your file:
using System.IO;
[Serializable]
public class GoogleCloudConfig
{
    public string voiceName = "en-US-Wavenet-D";
    public string languageCode = "en-US";
    public AudioType audioType = AudioType.WAV; // Change to MP3 if needed
}

public class GoogleCloudTTS : MonoBehaviour
{
    public GoogleCloudConfig config;
    public AudioSource audioSource;

    private string accessToken;
    private GoogleCredential credentials;

    [Serializable]
    private class GoogleCredential
    {
        public string type;
        public string project_id;
        public string private_key_id;
        public string private_key;
        public string client_email;
        public string client_id;
        public string auth_uri;
        public string token_uri;
        public string auth_provider_x509_cert_url;
        public string client_x509_cert_url;
    }

    public void Speak(string message)
    {
        StartCoroutine(GenerateAndStreamAudio(message));
    }

    IEnumerator GenerateAndStreamAudio(string text)
    {
        if (credentials == null)
        {
            TextAsset jsonFile = Resources.Load<TextAsset>("service-account");
            credentials = JsonConvert.DeserializeObject<GoogleCredential>(jsonFile.text);
        }

        yield return StartCoroutine(GetAccessToken());

        string requestJson = JsonConvert.SerializeObject(new
        {
            input = new { text = text },
            voice = new
            {
                languageCode = config.languageCode,
                name = config.voiceName
            },
            audioConfig = new
            {
                audioEncoding = "LINEAR16" // Use "MP3" if you prefer AudioType.MPEG
            }
        });

        UnityWebRequest request = new UnityWebRequest("https://texttospeech.googleapis.com/v1/text:synthesize", UnityWebRequest.kHttpVerbPOST);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(requestJson);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + accessToken);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[Google TTS] Error: " + request.error);
            yield break;
        }

        var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(request.downloadHandler.text);
        string audioContent = response["audioContent"].ToString();
        byte[] audioBytes = Convert.FromBase64String(audioContent);

        string path = Application.persistentDataPath + "/tts_output.wav";
        System.IO.File.WriteAllBytes(path, audioBytes);
        using UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + path, config.audioType);
        yield return audioRequest.SendWebRequest();

        if (audioRequest.result == UnityWebRequest.Result.Success)
        {
            AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest);
            audioSource.clip = clip;
            audioSource.Play();
        }
        else
        {
            Debug.LogError("[Google TTS] Failed to load audio from file: " + audioRequest.error);
        }
    }

    IEnumerator GetAccessToken()
    {
        string jwt = CreateJWT();

        WWWForm form = new WWWForm();
        form.AddField("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer");
        form.AddField("assertion", jwt);

        UnityWebRequest tokenRequest = UnityWebRequest.Post(credentials.token_uri, form);
        yield return tokenRequest.SendWebRequest();

        if (tokenRequest.result == UnityWebRequest.Result.Success)
        {
            var tokenResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(tokenRequest.downloadHandler.text);
            accessToken = tokenResponse["access_token"].ToString();
        }
        else
        {
            Debug.LogError("[Google TTS] Token error: " + tokenRequest.error);
        }
    }

    string CreateJWT()
    {
        var header = new Dictionary<string, object>()
        {
            { "alg", "RS256" },
            { "typ", "JWT" }
        };

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = new Dictionary<string, object>()
        {
            { "iss", credentials.client_email },
            { "scope", "https://www.googleapis.com/auth/cloud-platform" },
            { "aud", credentials.token_uri },
            { "iat", now },
            { "exp", now + 3600 }
        };

        string headerEncoded = Base64UrlEncode(JsonConvert.SerializeObject(header));
        string payloadEncoded = Base64UrlEncode(JsonConvert.SerializeObject(payload));
        string unsignedToken = $"{headerEncoded}.{payloadEncoded}";

        // Sign with private key using BouncyCastle
        var rsaParams = GetBouncyCastleRSAParameters(credentials.private_key);
        using RSA rsa = RSA.Create();
        rsa.ImportParameters(rsaParams);

        byte[] signature = rsa.SignData(Encoding.UTF8.GetBytes(unsignedToken), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        string signatureEncoded = Base64UrlEncode(signature);

        return $"{unsignedToken}.{signatureEncoded}";
    }
    RSAParameters GetBouncyCastleRSAParameters(string privateKeyPEM)
    {
        privateKeyPEM = privateKeyPEM.Replace("-----BEGIN PRIVATE KEY-----", "")
            .Replace("-----END PRIVATE KEY-----", "")
            .Replace("\n", "").Replace("\r", "").Trim();

        byte[] keyData = Convert.FromBase64String(privateKeyPEM);

        AsymmetricKeyParameter keyParameter = PrivateKeyFactory.CreateKey(keyData);
        RsaPrivateCrtKeyParameters rsaParams = (RsaPrivateCrtKeyParameters)keyParameter;

        return new RSAParameters
        {
            Modulus = rsaParams.Modulus.ToByteArrayUnsigned(),
            Exponent = rsaParams.PublicExponent.ToByteArrayUnsigned(),
            D = rsaParams.Exponent.ToByteArrayUnsigned(),
            P = rsaParams.P.ToByteArrayUnsigned(),
            Q = rsaParams.Q.ToByteArrayUnsigned(),
            DP = rsaParams.DP.ToByteArrayUnsigned(),
            DQ = rsaParams.DQ.ToByteArrayUnsigned(),
            InverseQ = rsaParams.QInv.ToByteArrayUnsigned()
        };
    }


    byte[] DecodeRSAPrivateKey(string privateKey)
    {
        privateKey = privateKey.Replace("-----BEGIN PRIVATE KEY-----", "")
                               .Replace("-----END PRIVATE KEY-----", "")
                               .Replace("\n", "")
                               .Trim();
        return Convert.FromBase64String(privateKey);
    }

    string Base64UrlEncode(string input) => Base64UrlEncode(Encoding.UTF8.GetBytes(input));
    string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input).Replace("+", "-").Replace("/", "_").Replace("=", "");
}
