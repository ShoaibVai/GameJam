using System;
using UnityEngine;

namespace OMTK
{
    public class OMTKSystemTts
    {
        public bool IsReady { get; private set; }
        public bool IsFailed { get; private set; }
        public bool IsSpeaking { get; private set; }
        private float speakingUntil;

#if UNITY_ANDROID && !UNITY_EDITOR
        private sealed class OnInitListener : AndroidJavaProxy
        {
            private readonly OMTKSystemTts owner;

            public OnInitListener(OMTKSystemTts owner) : base("android.speech.tts.TextToSpeech$OnInitListener")
            {
                this.owner = owner;
            }

            public void onInit(int status)
            {
                owner.initStatus = status;
                owner.initDone = true;
            }
        }

        private AndroidJavaObject tts;
        private OnInitListener listener;
        private volatile bool initDone;
        private volatile int initStatus = -1;
        private float initAt;
        private bool initialized;
#endif

        public void Init()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (initialized)
            {
                return;
            }

            initialized = true;
            initAt = Time.unscaledTime;
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    listener = new OnInitListener(this);
                    tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, listener);
                }
            }
            catch (Exception exception)
            {
                IsFailed = true;
                Debug.LogWarning("[OMTK] System TTS unavailable: " + exception.Message);
            }
#else
            IsFailed = true;
#endif
        }

        public void Tick()
        {
            if (IsSpeaking && Time.unscaledTime >= speakingUntil)
            {
                IsSpeaking = false;
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            if (IsReady || IsFailed || tts == null)
            {
                return;
            }

            if (!initDone)
            {
                if (Time.unscaledTime > initAt + 12f)
                {
                    IsFailed = true;
                    Debug.LogWarning("[OMTK] System TTS initialization timed out.");
                }

                return;
            }

            if (initStatus != 0)
            {
                IsFailed = true;
                Debug.LogWarning("[OMTK] System TTS initialization failed: " + initStatus);
                return;
            }

            try
            {
                bool languageReady = false;
                using (AndroidJavaObject locale = new AndroidJavaObject("java.util.Locale", "en", "US"))
                {
                    int languageResult = tts.Call<int>("setLanguage", locale);
                    languageReady = languageResult != -1 && languageResult != -2;
                }

                if (!languageReady)
                {
                    using (AndroidJavaClass localeClass = new AndroidJavaClass("java.util.Locale"))
                    using (AndroidJavaObject defaultLocale = localeClass.CallStatic<AndroidJavaObject>("getDefault"))
                    {
                        int languageResult = tts.Call<int>("setLanguage", defaultLocale);
                        languageReady = languageResult != -1 && languageResult != -2;
                    }
                }

                if (!languageReady)
                {
                    IsFailed = true;
                    Debug.LogWarning("[OMTK] System TTS English voice unavailable.");
                    return;
                }

                tts.Call<int>("setSpeechRate", 0.95f);
                tts.Call<int>("setPitch", 1f);
                IsReady = true;
                Debug.Log("[OMTK] System TTS ready.");
            }
            catch (Exception exception)
            {
                IsFailed = true;
                Debug.LogWarning("[OMTK] System TTS setup failed: " + exception.Message);
            }
#endif
        }

        public bool Speak(string text)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!IsReady || tts == null || string.IsNullOrEmpty(text))
            {
                return false;
            }

            try
            {
                using (AndroidJavaObject bundle = new AndroidJavaObject("android.os.Bundle"))
                {
                    int result = tts.Call<int>("speak", text, 0, bundle, "omtk");
                    if (result == 0)
                    {
                        int words = Mathf.Max(1, text.Split(' ').Length);
                        speakingUntil = Time.unscaledTime + Mathf.Clamp(0.55f + words * 0.34f, 1f, 9f);
                        IsSpeaking = true;
                    }

                    return result == 0;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[OMTK] System TTS speak failed: " + exception.Message);
                return false;
            }
#else
            return false;
#endif
        }

        public void Stop()
        {
            IsSpeaking = false;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (tts == null)
            {
                return;
            }

            try
            {
                tts.Call<int>("stop");
            }
            catch
            {
            }
#endif
        }

        public void Shutdown()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (tts == null)
            {
                return;
            }

            try
            {
                tts.Call("shutdown");
                tts.Dispose();
            }
            catch
            {
            }

            tts = null;
            listener = null;
            IsReady = false;
#endif
        }
    }
}
