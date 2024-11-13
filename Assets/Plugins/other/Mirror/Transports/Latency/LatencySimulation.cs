// wraps around a transport and adds latency/loss/scramble simulation.
//
// reliable: latency
// unreliable: latency, loss, scramble (unreliable isn't ordered so we scramble)
//
// IMPORTANT: use Time.unscaledTime instead of Time.time.
//            some games might have Time.timeScale modified.
//            see also: https://github.com/vis2k/Mirror/issues/2907

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;

namespace Mirror
{
    internal struct QueuedMessage
    {
        public int connectionId;
        public byte[] bytes;
        public double time;
    }

    [HelpURL("https://mirror-networking.gitbook.io/docs/transports/latency-simulaton-transport")]
    [DisallowMultipleComponent]
    public class LatencySimulation : Transport
    {
        public Transport wrap;

        [Header("Common")]
        [Tooltip("Jitter latency via perlin(Time * jitterSpeed) * jitter")]
        [FormerlySerializedAs("latencySpikeMultiplier")]
        [Range(0, 1)]
        public float jitter = 0.02f;

        [Tooltip("Jitter latency via perlin(Time * jitterSpeed) * jitter")]
        [FormerlySerializedAs("latencySpikeSpeedMultiplier")]
        public float jitterSpeed = 1;

        [Header("Reliable Messages")] [Tooltip("Reliable latency in milliseconds (1000 = 1 second)")] [Range(0, 10000)]
        public float reliableLatency = 100;
        // note: packet loss over reliable manifests itself in latency.
        //       don't need (and can't add) a loss option here.
        // note: reliable is ordered by definition. no need to scramble.

        [Header("Unreliable Messages")]
        [Tooltip(
            "Packet loss in %\n2% recommended for long term play testing, upto 5% for short bursts.\nAnything higher, or for a prolonged amount of time, suggests user has a connection fault.")]
        [Range(0, 100)]
        public float unreliableLoss = 2;

        [Tooltip(
            "Unreliable latency in milliseconds (1000 = 1 second) \n100ms recommended for long term play testing, upto 500ms for short bursts.\nAnything higher, or for a prolonged amount of time, suggests user has a connection fault.")]
        [Range(0, 10000)]
        public float unreliableLatency = 100;

        [Tooltip("Scramble % of unreliable messages, just like over the real network. Mirror unreliable is unordered.")]
        [Range(0, 100)]
        public float unreliableScramble = 2;

        // random
        // UnityEngine.Random.value is [0, 1] with both upper and lower bounds inclusive
        // but we need the upper bound to be exclusive, so using System.Random instead.
        // => NextDouble() is NEVER < 0 so loss=0 never drops!
        // => NextDouble() is ALWAYS < 1 so loss=1 always drops!
        private readonly Random random = new();

        // message queues
        // list so we can insert randomly (scramble)
        private readonly List<QueuedMessage> reliableClientToServer = new();
        private readonly List<QueuedMessage> reliableServerToClient = new();
        private readonly List<QueuedMessage> unreliableClientToServer = new();
        private readonly List<QueuedMessage> unreliableServerToClient = new();

        public void Awake()
        {
            if (wrap == null)
                throw new Exception("LatencySimulationTransport requires an underlying transport to wrap around.");
        }

        // forward enable/disable to the wrapped transport
        private void OnEnable()
        {
            wrap.enabled = true;
        }

        private void OnDisable()
        {
            wrap.enabled = false;
        }

        // noise function can be replaced if needed
        protected virtual float Noise(float time)
        {
            return Mathf.PerlinNoise(time, time);
        }

        // helper function to simulate latency
        private float SimulateLatency(int channeldId)
        {
            // spike over perlin noise.
            // no spikes isn't realistic.
            // sin is too predictable / no realistic.
            // perlin is still deterministic and random enough.
#if !UNITY_2020_3_OR_NEWER
            float spike = Noise((float)NetworkTime.localTime * jitterSpeed) * jitter;
#else
            var spike = Noise((float)Time.unscaledTimeAsDouble * jitterSpeed) * jitter;
#endif

            // base latency
            switch (channeldId)
            {
                case Channels.Reliable:
                    return reliableLatency / 1000 + spike;
                case Channels.Unreliable:
                    return unreliableLatency / 1000 + spike;
                default:
                    return 0;
            }
        }

        // helper function to simulate a send with latency/loss/scramble
        private void SimulateSend(
            int connectionId,
            ArraySegment<byte> segment,
            int channelId,
            float latency,
            List<QueuedMessage> reliableQueue,
            List<QueuedMessage> unreliableQueue)
        {
            // segment is only valid after returning. copy it.
            // (allocates for now. it's only for testing anyway.)
            var bytes = new byte[segment.Count];
            Buffer.BlockCopy(segment.Array, segment.Offset, bytes, 0, segment.Count);

            // enqueue message. send after latency interval.
            var message = new QueuedMessage
            {
                connectionId = connectionId,
                bytes = bytes,
#if !UNITY_2020_3_OR_NEWER
                time = NetworkTime.localTime + latency
#else
                time = Time.unscaledTimeAsDouble + latency
#endif
            };

            switch (channelId)
            {
                case Channels.Reliable:
                    // simulate latency
                    reliableQueue.Add(message);
                    break;
                case Channels.Unreliable:
                    // simulate packet loss
                    var drop = random.NextDouble() < unreliableLoss / 100;
                    if (!drop)
                    {
                        // simulate scramble (Random.Next is < max, so +1)
                        var scramble = random.NextDouble() < unreliableScramble / 100;
                        var last = unreliableQueue.Count;
                        var index = scramble ? random.Next(0, last + 1) : last;

                        // simulate latency
                        unreliableQueue.Insert(index, message);
                    }

                    break;
                default:
                    Debug.LogError($"{nameof(LatencySimulation)} unexpected channelId: {channelId}");
                    break;
            }
        }

        public override bool Available()
        {
            return wrap.Available();
        }

        public override void ClientConnect(string address)
        {
            wrap.OnClientConnected = OnClientConnected;
            wrap.OnClientDataReceived = OnClientDataReceived;
            wrap.OnClientError = OnClientError;
            wrap.OnClientDisconnected = OnClientDisconnected;
            wrap.ClientConnect(address);
        }

        public override void ClientConnect(Uri uri)
        {
            wrap.OnClientConnected = OnClientConnected;
            wrap.OnClientDataReceived = OnClientDataReceived;
            wrap.OnClientError = OnClientError;
            wrap.OnClientDisconnected = OnClientDisconnected;
            wrap.ClientConnect(uri);
        }

        public override bool ClientConnected()
        {
            return wrap.ClientConnected();
        }

        public override void ClientDisconnect()
        {
            wrap.ClientDisconnect();
            reliableClientToServer.Clear();
            unreliableClientToServer.Clear();
        }

        public override void ClientSend(ArraySegment<byte> segment, int channelId)
        {
            var latency = SimulateLatency(channelId);
            SimulateSend(0, segment, channelId, latency, reliableClientToServer, unreliableClientToServer);
        }

        public override Uri ServerUri()
        {
            return wrap.ServerUri();
        }

        public override bool ServerActive()
        {
            return wrap.ServerActive();
        }

        public override string ServerGetClientAddress(int connectionId)
        {
            return wrap.ServerGetClientAddress(connectionId);
        }

        public override void ServerDisconnect(int connectionId)
        {
            wrap.ServerDisconnect(connectionId);
        }

        public override void ServerSend(int connectionId, ArraySegment<byte> segment, int channelId)
        {
            var latency = SimulateLatency(channelId);
            SimulateSend(connectionId, segment, channelId, latency, reliableServerToClient, unreliableServerToClient);
        }

        public override void ServerStart()
        {
            wrap.OnServerConnected = OnServerConnected;
            wrap.OnServerDataReceived = OnServerDataReceived;
            wrap.OnServerError = OnServerError;
            wrap.OnServerDisconnected = OnServerDisconnected;
            wrap.ServerStart();
        }

        public override void ServerStop()
        {
            wrap.ServerStop();
            reliableServerToClient.Clear();
            unreliableServerToClient.Clear();
        }

        public override void ClientEarlyUpdate()
        {
            wrap.ClientEarlyUpdate();
        }

        public override void ServerEarlyUpdate()
        {
            wrap.ServerEarlyUpdate();
        }

        public override void ClientLateUpdate()
        {
            // flush reliable messages after latency.
            // need to iterate all, since queue isn't a sortedlist.
            for (var i = 0; i < reliableClientToServer.Count; ++i)
            {
                // message ready to be sent?
                var message = reliableClientToServer[i];
#if !UNITY_2020_3_OR_NEWER
                if (message.time <= NetworkTime.localTime)
#else
                if (message.time <= Time.unscaledTimeAsDouble)
#endif
                {
                    // send and eat
                    wrap.ClientSend(new ArraySegment<byte>(message.bytes));
                    reliableClientToServer.RemoveAt(i);
                    --i;
                }
            }

            // flush unreliable messages after latency.
            // need to iterate all, since queue isn't a sortedlist.
            for (var i = 0; i < unreliableClientToServer.Count; ++i)
            {
                // message ready to be sent?
                var message = unreliableClientToServer[i];
#if !UNITY_2020_3_OR_NEWER
                if (message.time <= NetworkTime.localTime)
#else
                if (message.time <= Time.unscaledTimeAsDouble)
#endif
                {
                    // send and eat
                    wrap.ClientSend(new ArraySegment<byte>(message.bytes));
                    unreliableClientToServer.RemoveAt(i);
                    --i;
                }
            }

            // update wrapped transport too
            wrap.ClientLateUpdate();
        }

        public override void ServerLateUpdate()
        {
            // flush reliable messages after latency.
            // need to iterate all, since queue isn't a sortedlist.
            for (var i = 0; i < reliableServerToClient.Count; ++i)
            {
                // message ready to be sent?
                var message = reliableServerToClient[i];
#if !UNITY_2020_3_OR_NEWER
                if (message.time <= NetworkTime.localTime)
#else
                if (message.time <= Time.unscaledTimeAsDouble)
#endif
                {
                    // send and eat
                    wrap.ServerSend(message.connectionId, new ArraySegment<byte>(message.bytes));
                    reliableServerToClient.RemoveAt(i);
                    --i;
                }
            }


            // flush unreliable messages after latency.
            // need to iterate all, since queue isn't a sortedlist.
            for (var i = 0; i < unreliableServerToClient.Count; ++i)
            {
                // message ready to be sent?
                var message = unreliableServerToClient[i];
#if !UNITY_2020_3_OR_NEWER
                if (message.time <= NetworkTime.localTime)
#else
                if (message.time <= Time.unscaledTimeAsDouble)
#endif
                {
                    // send and eat
                    wrap.ServerSend(message.connectionId, new ArraySegment<byte>(message.bytes));
                    unreliableServerToClient.RemoveAt(i);
                    --i;
                }
            }

            // update wrapped transport too
            wrap.ServerLateUpdate();
        }

        public override int GetBatchThreshold(int channelId)
        {
            return wrap.GetBatchThreshold(channelId);
        }

        public override int GetMaxPacketSize(int channelId = 0)
        {
            return wrap.GetMaxPacketSize(channelId);
        }

        public override void Shutdown()
        {
            wrap.Shutdown();
        }

        public override string ToString()
        {
            return $"{nameof(LatencySimulation)} {wrap}";
        }
    }
}