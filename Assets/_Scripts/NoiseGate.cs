using System;
using MetaVoiceChat.Input;
using UnityEngine;
using UnityEngine.Rendering;

namespace MetaVoiceChat.AudioFilters
{
    public class NoiseGate : VcInputFilter
    {
        [Header("MetaVc")]
        public MetaVc metaVc;

        [Header("Threshold (-80 to 0)")]
        [SerializeField] public float decibelThreshold = -30f;

        [Header("Monitor")]
        [SerializeField] public float currentVolumeDB = -80f;

        public bool HasReceivedSamples { get; private set; }
        public bool HasFreshSamples => HasReceivedSamples && Time.unscaledTime - _lastSampleTime <= 0.25f;

        // Simplificar o cálculo do volume usando uma média móvel para suavizar as variações rápidas
        private float smoothedVolume = 0f;
        private float _lastSampleTime = float.NegativeInfinity;

        private void Awake()
        {
            ResetMonitoring();
        }

        private void OnEnable()
        {
            ResetMonitoring();
        }

        private void ResetMonitoring()
        {
            currentVolumeDB = -80f;
            smoothedVolume = 0f;
            _lastSampleTime = float.NegativeInfinity;
            HasReceivedSamples = false;
        }

        protected override void Filter(int index, ref float[] samples)
        {
            if (samples == null || samples.Length == 0 || (metaVc != null && metaVc.isInputMuted.Value))
            {
                ResetMonitoring();
                samples = null;
                return;
            }

            HasReceivedSamples = true;
            _lastSampleTime = Time.unscaledTime;

            float sum = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                sum += samples[i] * samples[i];
            }

            float volume = Mathf.Sqrt(sum / samples.Length);

            smoothedVolume = Mathf.Lerp(smoothedVolume, volume, 0.2f);

            currentVolumeDB = smoothedVolume > 0.0001f ? 20f * Mathf.Log10(smoothedVolume) : -80f;

            if (volume < Mathf.Pow(10, decibelThreshold / 20))
            {

                samples = null;
            }
        }
    }
}
