﻿using FishNet.Managing;
using FishNet.Object;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FishNet.Connection
{

    public partial class NetworkConnection : IEquatable<NetworkConnection>
    {

        #region Public.
        /// <summary>
        /// Called after connection gains ownership of an object, and after the object has been added to Objects.
        /// </summary>
        public event Action<NetworkObject> OnObjectAdded;
        /// <summary>
        /// Called after connection loses ownership of an object, and after the object has been removed from Objects.
        /// </summary>
        public event Action<NetworkObject> OnObjectRemoved;
        /// <summary>
        /// NetworkManager managing this class.
        /// </summary>
        public NetworkManager NetworkManager { get; private set; } = null;
        /// <summary>
        /// True if this connection is authenticated.
        /// </summary>
        public bool Authenticated { get; private set; } = false;
        /// <summary>
        /// True if this connection is valid.
        /// </summary>
        public bool IsValid => (ClientId >= 0);
        /// <summary>
        /// Unique Id for this connection.
        /// </summary>
        public int ClientId = -1;
        /// <summary>
        /// Returns if this connection is for the local client.
        /// </summary>
        public bool IsLocalClient => (NetworkManager != null) ? (NetworkManager.ClientManager.Connection == this) : false;
        /// <summary>
        /// Objects owned by this connection. Only valid on owner and server.
        /// </summary>
        public HashSet<NetworkObject> Objects = new HashSet<NetworkObject>();
        /// <summary>
        /// Scenes this connection is in.
        /// </summary>
        public HashSet<Scene> Scenes { get; private set; } = new HashSet<Scene>();
        #endregion

        #region Internal.
        /// <summary>
        /// True if being disconnected.
        /// </summary>
        internal bool Disconnecting { get; private set; } = false;
        #endregion

        #region Private.
        /// <summary>
        /// True if connection has loaded start scenes.
        /// </summary>
        private bool _loadedStartScenes = false;
        #endregion

        #region Comparers.
        public override bool Equals(object obj)
        {
            return this.Equals(obj as NetworkConnection);
        }
        public bool Equals(NetworkConnection nc)
        {
            if (nc is null)
                return false;
            //If either is -1 Id.
            if (this.ClientId == -1 || nc.ClientId == -1)
                return false;
            if (System.Object.ReferenceEquals(this, nc))
                return true;

            return (this.ClientId == nc.ClientId);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public static bool operator ==(NetworkConnection a, NetworkConnection b)
        {   
            if (a is null && b is null)
                return true;
            if (a is null && !(b is null))
                return false;

            return (b == null) ? a.Equals(b) : b.Equals(a);
        }
        public static bool operator !=(NetworkConnection a, NetworkConnection b)
        {
            return !(a == b);
        }
        #endregion

        public NetworkConnection() { }
        public NetworkConnection(NetworkManager manager, int clientId)
        {
            Initialize(manager, clientId);
        }

        /// <summary>
        /// Initializes this for use.
        /// </summary>
        /// <param name="nm"></param>
        /// <param name="clientId"></param>
        private void Initialize(NetworkManager nm, int clientId)
        {
            NetworkManager = nm;
            ClientId = clientId;
            InitializeBuffer();
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        internal void Reset()
        {
            ClientId = -1;
            Objects.Clear();
            Authenticated = false;
            NetworkManager = null;
            _loadedStartScenes = false;
            UnsetDisconnecting();
            Scenes.Clear();
        }


        /// <summary>
        /// Disconnects this connection.
        /// </summary>
        /// <param name="immediately">True to disconnect immediately. False to send any pending data first.</param>
        public void Disconnect(bool immediately)
        {
            if (immediately)
            {
                NetworkManager.TransportManager.Transport.StopConnection(ClientId, true);
            }
            else
            {
                Disconnecting = true;
                ServerDirty();
            }
        }
        /// <summary>
        /// Unsets Disconnecting property.
        /// </summary>
        internal void UnsetDisconnecting()
        {
            Disconnecting = false;
        }

        /// <summary>
        /// Returns if just loaded start scenes and sets them as loaded if not.
        /// </summary>
        /// <returns></returns>
        internal bool SetLoadedStartScenes()
        {
            //Result becomes true if not yet loaded start scenes.
            bool result = (_loadedStartScenes) ? false : true;
            _loadedStartScenes = true;

            return result;
        }

        /// <summary>
        /// Sets connection as authenticated.
        /// </summary>
        internal void ConnectionAuthenticated()
        {
            Authenticated = true;
        }

        /// <summary>
        /// Adds to Objects owned by this connection.
        /// </summary>
        /// <param name="nob"></param>
        internal void AddObject(NetworkObject nob)
        {
            Objects.Add(nob);
            OnObjectAdded?.Invoke(nob);
        }

        /// <summary>
        /// Removes from Objects owned by this connection.
        /// </summary>
        /// <param name="nob"></param>
        internal void RemoveObject(NetworkObject nob)
        {
            Objects.Remove(nob);
            OnObjectRemoved?.Invoke(nob);
        }

        /// <summary>
        /// Adds a scene to this connections Scenes.
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        internal bool AddToScene(Scene scene)
        {
            return Scenes.Add(scene);
        }

        /// <summary>
        /// Removes a scene to this connections Scenes.
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        internal bool RemoveFromScene(Scene scene)
        {
            return Scenes.Remove(scene);
        }

    }


}