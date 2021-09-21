﻿#if UNITY_EDITOR
using UnityEditor;
using FishNet.Editing;
#endif
using FishNet.Connection;
using FishNet.Managing.Client;
using FishNet.Managing.Server;
using FishNet.Managing.Timing;
using FishNet.Managing.Transporting;
using FishNet.Managing.Object;
using UnityEngine;
using FishNet.Managing.Scened;
using FishNet.Authenticating;

namespace FishNet.Managing
{
    //Set execution order to make sure this fires Awake first.
    [DefaultExecutionOrder(short.MinValue)]
    public class NetworkManager : MonoBehaviour
    {

        #region Public.
        /// <summary>
        /// True if server is active.
        /// </summary>
        public bool IsServer => ServerManager.Started;
        /// <summary>
        /// True if only the server is active.
        /// </summary>
        public bool IsServerOnly => (ServerManager.Started && !ClientManager.Started);
        /// <summary>
        /// True if the client is active and authenticated.
        /// </summary>
        public bool IsClient => ClientManager.Started;
        /// <summary>
        /// True if only the client is active and authenticated.
        /// </summary>
        public bool IsClientOnly => (!ServerManager.Started && ClientManager.Started);
        /// <summary>
        /// True if client and server are active.
        /// </summary>
        public bool IsHost => (ServerManager.Started && ClientManager.Started);
        /// <summary>
        /// NetworkServer for this NetworkManager.
        /// </summary>
        public ServerManager ServerManager { get; private set; } = null;
        /// <summary>
        /// NetworkClient for this NetworkManager.
        /// </summary>
        public ClientManager ClientManager { get; private set; } = null;
        /// <summary>
        /// TransportManager for this NetworkManager.
        /// </summary>
        public TransportManager TransportManager { get; private set; } = null;
        /// <summary>
        /// TimeManager for this NetworkManager.
        /// </summary>
        public TimeManager TimeManager { get; private set; } = null;
        /// <summary>
        /// SceneManager for this NetworkManager.
        /// </summary>
        public SceneManager SceneManager { get; private set; } = null;
        /// <summary>
        /// Authenticator for this NetworkManager. May be null if no Authenticator is used.
        /// </summary>
        public Authenticator Authenticator { get; private set; } = null;
        /// <summary>
        /// An empty connection. Passed around when a connection cannot be found to prevent object creation per not found case.
        /// </summary>
        public NetworkConnection EmptyConnection { get; private set; } = new NetworkConnection();
        #endregion

        #region Serialized.
        /// <summary>
        /// True to have your application run while in the background.
        /// </summary>
        [Tooltip("True to have your application run while in the background.")]
        [SerializeField]
        private bool _runInBackground = true;
        /// <summary>
        /// True to make this instance DontDestroyOnLoad. This is typical if you only want one NetworkManager.
        /// </summary>
        [Tooltip("True to make this instance DontDestroyOnLoad. This is typical if you only want one NetworkManager.")]
        [SerializeField]
        private bool _dontDestroyOnLoad = true;
        /// <summary>
        /// True to allow multiple NetworkManagers. When false any copies will be destroyed.
        /// </summary>
        [Tooltip("True to allow multiple NetworkManagers. When false any copies will be destroyed.")]
        [SerializeField]
        private bool _allowMultiple = false;
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("Collection to use for spawnable objects.")]
        [SerializeField]
        private PrefabObjects _spawnablePrefabs = null;
        /// <summary>
        /// Collection to use for spawnable objects.
        /// </summary>
        public PrefabObjects SpawnablePrefabs { get => _spawnablePrefabs; set => _spawnablePrefabs = value; }
        #endregion

        protected virtual void Awake()
        {
            if (WillBeDestroyed())
                return;

            SpawnablePrefabs.InitializePrefabRange(0);
            SetDontDestroyOnLoad();
            SetRunInBackground();
            EmptyConnection = new NetworkConnection();
            AddransportManager();
            AddServerAndClientManagers();
            AddTimeManager();
            AddSceneManager(); ;
            InitializeComponents();
        }

        /// <summary>
        /// Initializes components. To be called after all components are added.
        /// </summary>
        private void InitializeComponents()
        {
            TimeManager.FirstInitialize(this);
            TimeManager.OnLateUpdate += TimeManager_OnLateUpdate;
            SceneManager.FirstInitialize(this);
            ServerManager.FirstInitialize(this);
            ClientManager.FirstInitialize(this);
        }
        /// <summary>
        /// Called when MonoBehaviours call LateUpdate.
        /// </summary>
        private void TimeManager_OnLateUpdate()
        {
            /* Some reason runinbackground becomes unset
            * or the setting goes ignored some times when it's set
            * in awake. Rather than try to fix or care why Unity
            * does this just set it in LateUpdate(or Update). */
            SetRunInBackground();

            /* Iterate outgoing fast as possible. It's the users
             * responsibility to use the tick events if they
             * wish to only send data on ticks. Data will however
             * only be read on ticks to maintain accurate
             * processing timings. */
            ServerManager.Objects.CheckDirtySyncTypes();
            /* Call these methods from here rather than
             * the transport manager so sync types
             * can be processed first. */
            TransportManager.IterateOutgoing(true);
            TransportManager.IterateOutgoing(false);
        }


        /// <summary>
        /// Returns if this NetworkManager can exist.
        /// </summary>
        /// <returns></returns>
        private bool WillBeDestroyed()
        {
            if (_allowMultiple)
                return false;

            //If here multiple are not allowed.
            //If found NetworkManager isn't this copy then return false.
            bool destroyThis = (InstanceFinder.NetworkManager != this);
            if (destroyThis)
            {
                Debug.Log($"NetworkManager on object {gameObject.name} is a duplicate and will be destroyed. If you wish to have multiple NetworkManagers enable 'Allow Multiple'.");
                Destroy(gameObject);
            }

            return destroyThis;
        }

        /// <summary>
        /// Sets DontDestroyOnLoad if configured to.
        /// </summary>
        private void SetDontDestroyOnLoad()
        {
            if (_dontDestroyOnLoad)
                DontDestroyOnLoad(this);
        }

        /// <summary>
        /// Sets Application.runInBackground to runInBackground.
        /// </summary>
        private void SetRunInBackground()
        {
            Application.runInBackground = _runInBackground;
        }

        /// <summary>
        /// Adds TransportManager.
        /// </summary>
        private void AddransportManager()
        {
            if (gameObject.TryGetComponent<TransportManager>(out TransportManager result))
                TransportManager = result;
            else
                TransportManager = gameObject.AddComponent<TransportManager>();
        }

        /// <summary>
        /// Adds TimeManager.
        /// </summary>
        private void AddTimeManager()
        {
            if (gameObject.TryGetComponent<TimeManager>(out TimeManager result))
                TimeManager = result;
            else
                TimeManager = gameObject.AddComponent<TimeManager>();
        }


        /// <summary>
        /// Adds SceneManager.
        /// </summary>
        private void AddSceneManager()
        {
            if (gameObject.TryGetComponent<SceneManager>(out SceneManager result))
                SceneManager = result;
            else
                SceneManager = gameObject.AddComponent<SceneManager>();
        }

        /// <summary>
        /// Adds and assigns NetworkServer and NetworkClient if they are not already setup.
        /// </summary>
        private void AddServerAndClientManagers()
        {
            //Add ServerManager if missing.
            if (gameObject.TryGetComponent<ServerManager>(out ServerManager sm))
                ServerManager = sm;
            else
                ServerManager = gameObject.AddComponent<ServerManager>();

            ClientManager = new ClientManager();
        }


        #region Editor.
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (SpawnablePrefabs == null)
                Reset();
        }
        protected virtual void Reset()
        {
            if (SpawnablePrefabs == null)
            {
                SpawnablePrefabs = DefaultPrefabsFinder.GetDefaultPrefabsFile(out _);
                //If found.
                if (SpawnablePrefabs != null)
                    Debug.Log($"NetworkManager on {gameObject.name} is using the default prefabs collection.");
            }
        }
#endif
        #endregion

    }


}