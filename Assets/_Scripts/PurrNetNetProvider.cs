using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using MetaVoiceChat;
using MetaVoiceChat.NetProviders;
using MetaVoiceChat.Utils;
using PurrNet;
using PurrNet.Transports;
using UnityEngine;

namespace MetaVoiceChat.NetProviders.PurrNet
{
    [RequireComponent(typeof(MetaVc))]
    public class PurrNetNetProvider : NetworkBehaviour, INetProvider
    {
        #region Singleton
        public static PurrNetNetProvider LocalPlayerInstance { get; private set; }
        private readonly static List<PurrNetNetProvider> instances = new();
        public static IReadOnlyList<PurrNetNetProvider> Instances => instances;
        #endregion

        bool INetProvider.IsLocalPlayerDeafened => LocalPlayerInstance.MetaVc.isDeafened;

        public MetaVc MetaVc { get; private set; }

        protected override void OnSpawned()
        {
            #region Singleton
            if (isOwner)
            {
                LocalPlayerInstance = this;
            }

            instances.Add(this);
            MetaVc = GetComponent<MetaVc>();
            #endregion

            int maxBytes = 1024 - 13 - 4 - 8 - 1 - 2;
            MetaVc.StartClient(this, isOwner, maxBytes);

        }

        protected override void OnDespawned()
        {
            #region Singleton
            if (isOwner)
            {
                LocalPlayerInstance = null;
            }

            instances.Remove(this);
            if (MetaVc != null)
                MetaVc.StopClient();
        }

        void INetProvider.RelayFrame(int index, double timestamp, ReadOnlySpan<byte> data)
        {
            byte[] array = data.ToArray();
            float additionalLatency = Time.deltaTime;

            if (isServer)
            {
                RpcReceiveFrame(index, timestamp, additionalLatency, array);
            }
            else
            {
                CmdRelayFrame(index, timestamp, additionalLatency, array);
            }
        }

        [ServerRpc(channel: Channel.Unreliable)]
        private void CmdRelayFrame(int index, double timestamp, float additionalLatency, byte[] data)
        {
            additionalLatency += Time.deltaTime;
            RpcReceiveFrame(index, timestamp, additionalLatency, data);
        }


        [ObserversRpc(channel: Channel.Unreliable, excludeOwner: true)]
        private void RpcReceiveFrame(int index, double timestamp, float additionalLatency, byte[] data)
        {
            
            if (MetaVc == null)
                return;

            if (isOwner)
                return;

            if (isServer)
            {
                // Tirar Time.deltaTime para obter a latência real, já que o servidor não tem o atraso de enviar o comando para si mesmo
                float trueLatency = additionalLatency - Time.deltaTime;
                MetaVc.ReceiveFrame(index, timestamp, trueLatency, data);
            }
            else
            {
                MetaVc.ReceiveFrame(index, timestamp, additionalLatency, data);
            }
        }
    }
}
#endregion