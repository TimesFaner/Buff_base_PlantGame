using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Mirror
{
    // a transport that can listen to multiple underlying transport at the same time
    [DisallowMultipleComponent]
    public class MultiplexTransport : Transport
    {
        public Transport[] transports;

        // multiplexed connectionId to (original connectionId, transport#)
        private readonly Dictionary<int, KeyValuePair<int, int>> multiplexedToOriginalId = new(100);

        // underlying transport connectionId to multiplexed connectionId lookup.
        //
        // originally we used a formula to map the connectionId:
        //   connectionId * transportAmount + transportId
        //
        // if we have 3 transports, then
        //   transport 0 will produce connection ids [0, 3, 6, 9, ...]
        //   transport 1 will produce connection ids [1, 4, 7, 10, ...]
        //   transport 2 will produce connection ids [2, 5, 8, 11, ...]
        //
        // however, some transports like kcp may give very large connectionIds.
        // if they are near int.max, then "* transprotAmount + transportIndex"
        // will overflow, resulting in connIds which can't be projected back.
        //   https://github.com/vis2k/Mirror/issues/3280
        //
        // instead, use a simple lookup with 0-indexed ids.
        // with initial capacity to avoid runtime allocations.

        // (original connectionId, transport#) to multiplexed connectionId
        private readonly Dictionary<KeyValuePair<int, int>, int> originalToMultiplexedId = new(100);

        private Transport available;

        // next multiplexed id counter. start at 1 because 0 is reserved for host.
        private int nextMultiplexedId = 1;

        ////////////////////////////////////////////////////////////////////////

        public void Awake()
        {
            if (transports == null || transports.Length == 0)
                Debug.LogError("[Multiplexer] Multiplex transport requires at least 1 underlying transport");
        }

        private void OnEnable()
        {
            foreach (var transport in transports)
                transport.enabled = true;
        }

        private void OnDisable()
        {
            foreach (var transport in transports)
                transport.enabled = false;
        }

        // add to bidirection lookup. returns the multiplexed connectionId.
        public int AddToLookup(int originalConnectionId, int transportIndex)
        {
            // add to both
            var pair = new KeyValuePair<int, int>(originalConnectionId, transportIndex);
            var multiplexedId = nextMultiplexedId++;

            originalToMultiplexedId[pair] = multiplexedId;
            multiplexedToOriginalId[multiplexedId] = pair;

            return multiplexedId;
        }

        public void RemoveFromLookup(int originalConnectionId, int transportIndex)
        {
            // remove from both
            var pair = new KeyValuePair<int, int>(originalConnectionId, transportIndex);
            var multiplexedId = originalToMultiplexedId[pair];

            originalToMultiplexedId.Remove(pair);
            multiplexedToOriginalId.Remove(multiplexedId);
        }

        public void OriginalId(int multiplexId, out int originalConnectionId, out int transportIndex)
        {
            var pair = multiplexedToOriginalId[multiplexId];
            originalConnectionId = pair.Key;
            transportIndex = pair.Value;
        }

        public int MultiplexId(int originalConnectionId, int transportIndex)
        {
            var pair = new KeyValuePair<int, int>(originalConnectionId, transportIndex);
            return originalToMultiplexedId[pair];
        }

        public override void ClientEarlyUpdate()
        {
            foreach (var transport in transports)
                transport.ClientEarlyUpdate();
        }

        public override void ServerEarlyUpdate()
        {
            foreach (var transport in transports)
                transport.ServerEarlyUpdate();
        }

        public override void ClientLateUpdate()
        {
            foreach (var transport in transports)
                transport.ClientLateUpdate();
        }

        public override void ServerLateUpdate()
        {
            foreach (var transport in transports)
                transport.ServerLateUpdate();
        }

        public override bool Available()
        {
            // available if any of the transports is available
            foreach (var transport in transports)
                if (transport.Available())
                    return true;

            return false;
        }

        public override int GetMaxPacketSize(int channelId = 0)
        {
            // finding the max packet size in a multiplex environment has to be
            // done very carefully:
            // * servers run multiple transports at the same time
            // * different clients run different transports
            // * there should only ever be ONE true max packet size for everyone,
            //   otherwise a spawn message might be sent to all tcp sockets, but
            //   be too big for some udp sockets. that would be a debugging
            //   nightmare and allow for possible exploits and players on
            //   different platforms seeing a different game state.
            // => the safest solution is to use the smallest max size for all
            //    transports. that will never fail.
            var mininumAllowedSize = int.MaxValue;
            foreach (var transport in transports)
            {
                var size = transport.GetMaxPacketSize(channelId);
                mininumAllowedSize = Mathf.Min(size, mininumAllowedSize);
            }

            return mininumAllowedSize;
        }

        public override void Shutdown()
        {
            foreach (var transport in transports)
                transport.Shutdown();
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            foreach (var transport in transports)
                builder.AppendLine(transport.ToString());

            return builder.ToString().Trim();
        }

        #region Client

        public override void ClientConnect(string address)
        {
            foreach (var transport in transports)
                if (transport.Available())
                {
                    available = transport;
                    transport.OnClientConnected = OnClientConnected;
                    transport.OnClientDataReceived = OnClientDataReceived;
                    transport.OnClientError = OnClientError;
                    transport.OnClientDisconnected = OnClientDisconnected;
                    transport.ClientConnect(address);
                    return;
                }

            throw new ArgumentException("[Multiplexer] No transport suitable for this platform");
        }

        public override void ClientConnect(Uri uri)
        {
            foreach (var transport in transports)
                if (transport.Available())
                    try
                    {
                        available = transport;
                        transport.OnClientConnected = OnClientConnected;
                        transport.OnClientDataReceived = OnClientDataReceived;
                        transport.OnClientError = OnClientError;
                        transport.OnClientDisconnected = OnClientDisconnected;
                        transport.ClientConnect(uri);
                        return;
                    }
                    catch (ArgumentException)
                    {
                        // transport does not support the schema, just move on to the next one
                    }

            throw new ArgumentException("[Multiplexer] No transport suitable for this platform");
        }

        public override bool ClientConnected()
        {
            return (object)available != null && available.ClientConnected();
        }

        public override void ClientDisconnect()
        {
            if ((object)available != null)
                available.ClientDisconnect();
        }

        public override void ClientSend(ArraySegment<byte> segment, int channelId)
        {
            available.ClientSend(segment, channelId);
        }

        #endregion

        #region Server

        private void AddServerCallbacks()
        {
            // all underlying transports should call the multiplex transport's events
            for (var i = 0; i < transports.Length; i++)
            {
                // this is required for the handlers, if I use i directly
                // then all the handlers will use the last i
                var transportIndex = i;
                var transport = transports[i];

                transport.OnServerConnected = originalConnectionId =>
                {
                    // invoke Multiplex event with multiplexed connectionId
                    var multiplexedId = AddToLookup(originalConnectionId, transportIndex);
                    OnServerConnected.Invoke(multiplexedId);
                };

                transport.OnServerDataReceived = (originalConnectionId, data, channel) =>
                {
                    // invoke Multiplex event with multiplexed connectionId
                    var multiplexedId = MultiplexId(originalConnectionId, transportIndex);
                    OnServerDataReceived.Invoke(multiplexedId, data, channel);
                };

                transport.OnServerError = (originalConnectionId, error, reason) =>
                {
                    // invoke Multiplex event with multiplexed connectionId
                    var multiplexedId = MultiplexId(originalConnectionId, transportIndex);
                    OnServerError.Invoke(multiplexedId, error, reason);
                };

                transport.OnServerDisconnected = originalConnectionId =>
                {
                    // invoke Multiplex event with multiplexed connectionId
                    var multiplexedId = MultiplexId(originalConnectionId, transportIndex);
                    OnServerDisconnected.Invoke(multiplexedId);
                    RemoveFromLookup(originalConnectionId, transportIndex);
                };
            }
        }

        // for now returns the first uri,
        // should we return all available uris?
        public override Uri ServerUri()
        {
            return transports[0].ServerUri();
        }

        public override bool ServerActive()
        {
            // avoid Linq.All allocations
            foreach (var transport in transports)
                if (!transport.ServerActive())
                    return false;

            return true;
        }

        public override string ServerGetClientAddress(int connectionId)
        {
            // convert multiplexed connectionId to original id & transport index
            OriginalId(connectionId, out var originalConnectionId, out var transportIndex);
            return transports[transportIndex].ServerGetClientAddress(originalConnectionId);
        }

        public override void ServerDisconnect(int connectionId)
        {
            // convert multiplexed connectionId to original id & transport index
            OriginalId(connectionId, out var originalConnectionId, out var transportIndex);
            transports[transportIndex].ServerDisconnect(originalConnectionId);
        }

        public override void ServerSend(int connectionId, ArraySegment<byte> segment, int channelId)
        {
            // convert multiplexed connectionId to original transport + connId
            OriginalId(connectionId, out var originalConnectionId, out var transportIndex);
            transports[transportIndex].ServerSend(originalConnectionId, segment, channelId);
        }

        public override void ServerStart()
        {
            AddServerCallbacks();

            foreach (var transport in transports)
                transport.ServerStart();
        }

        public override void ServerStop()
        {
            foreach (var transport in transports)
                transport.ServerStop();
        }

        #endregion
    }
}