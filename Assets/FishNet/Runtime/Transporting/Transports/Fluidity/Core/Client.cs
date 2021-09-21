using System;
using System.Collections.Concurrent;
using System.Threading;
using ENet;
using FishNet.Transporting;

namespace Fluidity.Client
{
    public class ClientSocket
    {
        ~ClientSocket()
        {
            _stopThread = true;
        }

        #region Public.
        /// <summary>
        /// Current ConnectionState.
        /// </summary>
        private LocalConnectionStates _connectionState = LocalConnectionStates.Stopped;
        /// <summary>
        /// Returns the current ConnectionState.
        /// </summary>
        /// <returns></returns>
        public LocalConnectionStates GetConnectionState()
        {
            return _connectionState;
        }
        /// <summary>
        /// Sets a new connection state.
        /// </summary>
        /// <param name="connectionState"></param>
        private void SetLocalConnectionState(LocalConnectionStates connectionState)
        {
            //If state hasn't changed.
            if (connectionState == _connectionState)
                return;

            _connectionState = connectionState;
            _transport?.HandleClientConnectionState(
                new ClientConnectionStateArgs(connectionState)
                );
        }
        #endregion

        #region Private.
        #region Configuration.
        /// <summary>
        /// Address to bind server to.
        /// </summary>
        private string _address = string.Empty;
        /// <summary>
        /// Port used by server.
        /// </summary>
        private ushort _port = 0;
        /// <summary>
        /// Number of configured channels.
        /// </summary>
        private int _channelsCount = 0;
        /// <summary>
        /// Poll timeout for socket.
        /// </summary>
        private int _pollTime = 0;
        /// <summary>
        /// MTU sizes for each channel.
        /// </summary>
        private int[] _mtus = new int[0];
        #endregion
        #region Queues.
        /// <summary>
        /// Changes to the sockets local connection state.
        /// </summary>
        private ConcurrentQueue<LocalConnectionStates> _localConnectionStates = new ConcurrentQueue<LocalConnectionStates>();
        /// <summary>
        /// Inbound messages which need to be handled.
        /// </summary>
        private ConcurrentQueue<IncomingPacket> _incoming = new ConcurrentQueue<IncomingPacket>();
        /// <summary>
        /// Outbound messages which need to be handled.
        /// </summary>
        private ConcurrentQueue<OutgoingPacket> _outgoing = new ConcurrentQueue<OutgoingPacket>();
        /// <summary>
        /// Commands which need to be handled.
        /// </summary>
        private ConcurrentQueue<CommandPacket> _commands = new ConcurrentQueue<CommandPacket>();
        /// <summary>
        /// ConnectionEvents which need to be handled.
        /// </summary>
        private ConcurrentQueue<ConnectionEvent> _connectionEvents = new ConcurrentQueue<ConnectionEvent>();
        #endregion
        /// <summary>
        /// True to stop the socket thread.
        /// </summary>
        private volatile bool _stopThread = false;
        /// <summary>
        /// True to dequeue Outgoing.
        /// </summary>
        private volatile bool _dequeueOutgoing = false;
        /// <summary>
        /// Buffer used to temporarily hold incoming data.
        /// </summary>
        private byte[] _incomingBuffer;
        /// <summary>
        /// Thread used for socket.
        /// </summary>
        private Thread _thread;
        /// <summary>
        /// Transport this belongs to.
        /// </summary>
        private Transport _transport;
        #endregion

        /// <summary>
        /// Initializes this for use.
        /// </summary>
        /// <param name="t"></param>
        public void Initialize(Transport t, ChannelData[] channelData)
        {
            _transport = t;

            //Set maximum MTU for each channel, and create byte buffer.
            int largestMtu = 0;
            _mtus = new int[channelData.Length];
            for (int i = 0; i < channelData.Length; i++)
            { 
                _mtus[i] = channelData[i].MaximumTransmissionUnit;
                largestMtu = Math.Max(largestMtu, _mtus[i]);
            }
            _incomingBuffer = new byte[largestMtu];
        }

        /// <summary>
        /// Starts the client connection.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="channelsCount"></param>
        /// <param name="pollTime"></param>
        public void StartConnection(string address, ushort port, byte channelsCount, int pollTime)
        {
            if (GetConnectionState() != LocalConnectionStates.Stopped || (_thread != null && _thread.IsAlive))
                return;

            SetLocalConnectionState(LocalConnectionStates.Starting);

            //Assign properties.
            _port = port;
            _address = address;
            _channelsCount = channelsCount;
            _pollTime = pollTime;

            ResetValues();

            _stopThread = false;
            _thread = new Thread(ThreadSocket);
            _thread.Start();
        }


        /// <summary>
        /// Stops the local socket.
        /// </summary>
        public void StopConnection()
        {
            if (GetConnectionState() == LocalConnectionStates.Stopped || GetConnectionState() == LocalConnectionStates.Stopping)
                return;

            SetLocalConnectionState(LocalConnectionStates.Stopping);
            _stopThread = true;
        }

        /// <summary>
        /// Resets values as though this is a new instance.
        /// </summary>
        private void ResetValues()
        {
            while (_localConnectionStates.TryDequeue(out _)) ;
            while (_incoming.TryDequeue(out _)) ;
            while (_outgoing.TryDequeue(out _)) ;
            while (_commands.TryDequeue(out _)) ;
            while (_connectionEvents.TryDequeue(out _)) ;
        }

        // This runs in a seperate thread, be careful accessing anything outside of it's thread
        // or you may get an AccessViolation/crash.
        private void ThreadSocket()
        {
            Address address = new Address();
            Peer client;
            Host socket;
            ENet.Event enetEvent;

            address.SetHost(_address);
            address.Port = _port;

            using (socket = new Host())
            {
                socket.Create();
                client = socket.Connect(address, _channelsCount);

                while (!_stopThread)
                {
                    //Actions issued to socket locally.
                    DequeueCommands();
                    //Packets to be sent out.
                    DequeueOutgoing(client);
                    //Check for events until there are none.
                    while (true)
                    {
                        // Any events happening?
                        if (socket.CheckEvents(out enetEvent) <= 0)
                        {
                            // If service time is met, break out of it.
                            if (socket.Service(_pollTime, out enetEvent) <= 0)
                                break;
                        }

                        HandleENetEvent(ref enetEvent);
                    }
                }

                /* Thread is ending. */
                client.Disconnect(0);
                socket.Flush();
            }

            _localConnectionStates.Enqueue(LocalConnectionStates.Stopped);
        }


        /// <summary>
        /// Dequeues and processes commands.
        /// </summary>
        private void DequeueCommands()
        {
            //Client doesn't use commands yet?
            while (_commands.TryDequeue(out _)) { }
        }

        /// <summary>
        /// Dequeues and processes outgoing.
        /// </summary>
        private void DequeueOutgoing(Peer client)
        {
            if (!_dequeueOutgoing)
                return;

            while (_outgoing.TryDequeue(out OutgoingPacket outgoing))
            {
                client.Send(outgoing.Channel, ref outgoing.Packet);
                outgoing.Packet.Dispose();
            }

            _dequeueOutgoing = true;
        }

        /// <summary>
        /// Handles an event received from ENet.
        /// </summary>
        private void HandleENetEvent(ref ENet.Event enetEvent)
        {
            switch (enetEvent.Type)
            {
                case ENet.EventType.None:
                default:
                    break;

                case ENet.EventType.Connect:
                    //Unfortunately PeerId is always 0 on client. It must be sent by server outside transport.
                    _connectionEvents.Enqueue(new ConnectionEvent()
                    {
                        Connected = true
                    });
                    break;
                case ENet.EventType.Disconnect:
                case ENet.EventType.Timeout:
                    _stopThread = true;
                    break;
                case ENet.EventType.Receive:
                    Packet packet = enetEvent.Packet;
                    //Packet wasn't sent; should never happen.
                    if (!packet.IsSet)
                        return;
                    //Packet size is larger than maximum bytes.
                    if (packet.Length > _mtus[enetEvent.ChannelID])
                    {
                        packet.Dispose();
                        return;
                    }
                    //Create and enqueue the packet.
                    IncomingPacket incoming = new IncomingPacket
                    {
                        Channel = enetEvent.ChannelID,
                        Packet = packet
                    };
                    _incoming.Enqueue(incoming);
                    break;
            }
        }

        /// <summary>
        /// Allows for Outgoing queue to be iterated.
        /// </summary>
        public void IterateOutgoing()
        {
            _dequeueOutgoing = true;
        }

        /// <summary>
        /// Iterates the Incoming queue.
        /// </summary>
        /// <param name="transport"></param>
        public void IterateIncoming()
        {
            /* Run local connection states first so we can begin
            * to read for data at the start of the frame, as that's
            * where incoming is read. */
            while (_localConnectionStates.TryDequeue(out LocalConnectionStates state))
                SetLocalConnectionState(state);

            //Stopped or trying to stop.
            if (GetConnectionState() == LocalConnectionStates.Stopped || GetConnectionState() == LocalConnectionStates.Stopping)
                return;

            /* Commands. */
            //Client doesn't use commands yet?
            while (_commands.TryDequeue(out CommandPacket commandPacket)) { }

            /* ConnectionEvents. */
            while (_connectionEvents.TryDequeue(out ConnectionEvent connectionEvent))
            {
                if (connectionEvent.Connected)
                    SetLocalConnectionState(LocalConnectionStates.Started);
                else
                    SetLocalConnectionState(LocalConnectionStates.Stopped);
            }

            /* Incoming. */
            bool started = (GetConnectionState() == LocalConnectionStates.Started);
            ClientReceivedDataArgs dataArgs = new ClientReceivedDataArgs();
            while (_incoming.TryDequeue(out IncomingPacket incoming))
            {
                //Only process data if started. Otherwise processing will be skipped and packet will be disposed of.
                if (started)
                {
                    //Copy to byte buffer and make arraysegment out of it.
                    incoming.Packet.CopyTo(_incomingBuffer, 0);
                    ArraySegment<byte> data = new ArraySegment<byte>(_incomingBuffer, 0, incoming.Packet.Length);
                    //Tell generic transport to handle packet.
                    dataArgs.Data = data;
                    dataArgs.Channel = (Channel)incoming.Channel;
                    _transport.HandleClientReceivedDataArgs(dataArgs);
                    //_transport.HandleClientReceivedDataArgs(
                        //new ClientReceivedDataArgs(data, (Channel)incoming.Channel)
                        //);
                }
                incoming.Packet.Dispose();
            }
        }

        /// <summary>
        /// Sends a packet to a single, or all clients.
        /// </summary>
        /// <param name="connectionId">Client to send packet to. Use -1 to send to all clients.</param>
        /// <param name="channelId"></param>
        /// <param name="segment"></param>
        public void SendToServer(byte channelId, ArraySegment<byte> segment, ChannelData[] channels)
        {
            //Out of bounds for channels.
            if (channelId < 0 || channelId >= channels.Length)
                return;
            //Not started, cannot send.
            if (GetConnectionState() != LocalConnectionStates.Started)
                return;

            Packet enetPacket = default;
            PacketFlags flags = (PacketFlags)channels[channelId].ChannelType;
            // Create the packet.
            enetPacket.Create(segment.Array, segment.Offset, segment.Offset + segment.Count, flags);

            //ConnectionId isn't used from client to server.
            OutgoingPacket outgoing = new OutgoingPacket
            {
                Channel = channelId,
                Packet = enetPacket
            };
            _outgoing.Enqueue(outgoing);
        }


    }
}
