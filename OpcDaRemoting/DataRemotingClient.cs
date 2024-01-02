using System;
using System.Diagnostics;
using System.Runtime.Remoting.Lifetime;

namespace nsDataRemoting {
    /// <summary>
    /// Leasing sponsor
    /// </summary>
    public class MySponsor : MarshalByRefObject, ISponsor {
        /// <summary>
        /// Watchdog is set true every renew request
        /// </summary>
        public static bool Watchdog = true;
        /// <summary>
        /// Renewal time for servers's DataRemotingClient, defauls is 30 s
        /// </summary>
        public static int RenewalTime = 30;
        /// <summary>
        /// Sets infinite lifetime for the sponsor
        /// </summary>
        /// <returns></returns>
        public override object InitializeLifetimeService() => null;
        /// <summary>
        /// Renewal method called for servers's DataRemotingClient 
        /// </summary>
        /// <param name="lease">not used</param>
        /// <returns></returns>
        public TimeSpan Renewal(ILease lease) { Watchdog = true; return TimeSpan.FromSeconds(RenewalTime); }
    }
    /// <summary>
    /// Remote object dummy, not intended for local use
    /// Only requested public methods should be declared with no implementation
    /// </summary>
    public class DataRemotingClient : MarshalByRefObject {
        /// <summary>
        /// Called by adapters' Initialize method
        /// </summary>
        /// <returns>True for successful initialization</returns>
        public bool Initialize() => false;
        /// <summary>
        /// Called by adapters' AttemptConnection method
        /// </summary>
        /// <returns>True for successful connection</returns>
        public bool Connect() => false;
        /// <summary>
        /// Called by adapters' OnConnected method 
        /// </summary>
        /// <param name="sa">Array of required parameters</param>
        /// <returns>True for successful subscription</returns>
        public bool CreateGroup(string[] sa) => false;
        /// <summary>
        /// Called by adapters' AttemptDisconnection method 
        /// </summary>
        /// <returns>True for successful disconnection</returns>
        public bool Disconnect() => false;
        /// <summary>
        /// Called by adapters' Dispose method. To release any unmanaged resource used on the server side 
        /// </summary>
        public void Dispose() { }
    }
}
