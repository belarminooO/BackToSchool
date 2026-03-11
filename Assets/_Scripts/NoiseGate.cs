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

        // Simplificar o cálculo do volume usando uma média móvel para suavizar as variações rápidas
        private float smoothedVolume = 0f;

        protected override void Filter(int index, ref float[] samples)
        {
            if (samples == null || (metaVc != null && metaVc.isInputMuted.Value))
            {
                currentVolumeDB = -80f;
                samples = null;
                return;
            }

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
