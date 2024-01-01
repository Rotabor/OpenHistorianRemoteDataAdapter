using System;
using System.Diagnostics;

namespace nsDataRemoting {

    /// <summary>
    /// Class to receive data using CallbackHnadler
    /// </summary>
    [Serializable]
    public class ValueResult {
        /// <summary>
        /// Index matches the index in the array of parameters passed to DataRemotingClient.CreateGroup
        /// </summary>
        public int Index { get; set; }
        /// <summary>
        /// Timestamp provided by the data source
        /// </summary>
        public DateTime Timestamp { get; set; }
        /// <summary>
        /// Value boxed in the object
        /// </summary>
        public object Value { get; set; }
        /// <summary>
        /// Status of the value
        /// </summary>
        public int Satus { get; set; }
    }

    public class CallbackHandler : MarshalByRefObject {
        /// <summary>
        /// EventHandler for the callback method
        /// </summary>
        static public EventHandler OnDataChangedEh;
        /// <summary>
        /// Remotely called callback method
        /// </summary>
        /// <param name="vra">Array of results</param>
        public void OnValueChanged(ValueResult[] vra) {
            try { if (OnDataChangedEh != null) OnDataChangedEh(vra, new EventArgs()); }
            catch (Exception e) { ErrorTraceEx(e, "OnValueChanged"); }
        }
        /// <summary>
        /// Extended error tracer
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <param name="msg">Additional message</param>
        static void ErrorTraceEx(Exception ex, string msg) {
            string exmsg = string.Concat($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {msg}: ",
                ex.Message, "\r\nSource: ", ex.Source, "\r\nMember: ", ex.TargetSite, "\r\nStack trace: ", ex.StackTrace);
            if (ex.InnerException != null) exmsg = String.Concat(exmsg, "\r\nInnerException: ", ex.InnerException.Message, "\r\nSource: ",
                ex.InnerException.Source, "\r\nMember: ", ex.InnerException.TargetSite, "\r\nStack trace: ", ex.InnerException.StackTrace);
            Trace.WriteLine(exmsg);
        }
    }
}
