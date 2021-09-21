﻿using FishNet.Broadcast;
using FishNet.Managing;
using FishNet.Transporting;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FishNet.Connection
{
    public partial class NetworkConnection
    {

        #region Public.
        /// <summary>
        /// PacketBundles to send to this connection. An entry will be made for each channel.
        /// </summary>
        private List<PacketBundle> _toClientBundles = new List<PacketBundle>();
        #endregion

        #region Private.
        /// <summary>
        /// True if this object has been dirtied.
        /// </summary>
        private bool _serverDirtied = false;
        #endregion

        /// <summary>
        /// Initializes this script.
        /// </summary>
        private void InitializeBuffer()
        {
            int channels = NetworkManager.TransportManager.Transport.GetChannelCount();
            for (byte i = 0; i < channels; i++)
            {
                int mtu = NetworkManager.TransportManager.Transport.GetMTU(i);
                _toClientBundles.Add(new PacketBundle(mtu));
            }
        }


        /// <summary>
        /// Sends a broadcast to this connection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <param name="channel"></param>
        public void Broadcast<T>(T message, bool requireAuthenticated = true,  Channel channel = Channel.Reliable) where T : struct, IBroadcast
        {
            if (!IsValid)
                Debug.LogError($"Connection is not valid, cannot send broadcast.");
            else
                InstanceFinder.ServerManager.Broadcast<T>(this, message, requireAuthenticated,channel);
        }

        /// <summary>
        /// Sends data from the server to a client.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="segment"></param>
        /// <param name="connectionId"></param>
        internal void SendToClient(byte channel, ArraySegment<byte> segment)
        {
            //Cannot send data when disconnecting.
            if (Disconnecting)
                return;
            if (!IsValid)
                throw new ArgumentException($"NetworkConnection is not valid.");
            if (channel >= _toClientBundles.Count)
                throw new ArgumentException($"Channel {channel} is out of bounds.");

            _toClientBundles[channel].Write(segment);
            ServerDirty();
        }

        /// <summary>
        /// Returns a PacketBundle for a channel. ResetPackets must be called afterwards.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns>True if PacketBundle is valid on the index and contains data.</returns>
        internal bool GetPacketBundle(int channel, out PacketBundle packetBundle)
        {
            return PacketBundle.GetPacketBundle(channel, _toClientBundles, out packetBundle);
        }

        /// <summary>
        /// Indicates the server has data to send to this connection.
        /// </summary>
        private void ServerDirty()
        {
            bool wasDirty = _serverDirtied;
            _serverDirtied = true;

            //If not yet dirty then tell transport manager this is dirty.
            if (!wasDirty)
                NetworkManager.TransportManager.ServerDirty(this);
        }

        /// <summary>
        /// Resets that there is data to send.
        /// </summary>
        internal void ResetServerDirty()
        {
            _serverDirtied = false;
        }
    }


}