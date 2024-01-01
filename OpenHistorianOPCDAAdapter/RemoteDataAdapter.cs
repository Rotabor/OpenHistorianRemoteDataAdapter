using GSF;
using GSF.Diagnostics;
using GSF.TimeSeries;
using GSF.TimeSeries.Adapters;
using GSF.TimeSeries.Statistics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using nsDataRemoting;
using System.Collections;
using System.Runtime.Remoting.Channels.Tcp;  // requires explicit 'System.Runtime.Remoting' assembly reference
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Remoting.Lifetime;
using System.Configuration; // requires explicit assembly reference
using System.Reflection;
using System.Text;
using System.Linq;

namespace nsOpenHistorianRemoteDataAdapter {
    /// <summary>
    /// Input adapter class
    /// </summary>
    [Description("DataRemoting: Get data from the remote data server")]
    public class RemoteDataAdapter : InputAdapterBase {

        IMeasurement[] _items; TcpChannel _tc; DataRemotingClient _client; Configuration _cfg;
        Timer _tmr; bool _connected, _reconreq;
        string _remotehost, _port; int _renewaltime;

        public RemoteDataAdapter() {
            // the only parameter to read is log's path
            _cfg = ConfigurationManager.OpenExeConfiguration(Assembly.GetExecutingAssembly().Location);
            // required connection string is 'remotehost=THEHOST;port=XXXXX'
            // optional 'renewaltime=YY', defaults is 30 s
            _renewaltime = MySponsor.RenewalTime; ParseConnectionString(); MySponsor.RenewalTime = _renewaltime;
            RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;
            RemotingConfiguration.ApplicationName = "DataRemote";
            RemotingConfiguration.RegisterActivatedServiceType(typeof(CallbackHandler));
            // the same port is used for inbound and outboun conctions
            ActivatedClientTypeEntry myActivatedClientTypeEntry =
                new ActivatedClientTypeEntry(typeof(DataRemotingClient),
                $"tcp://{_remotehost}:{_port}/{RemotingConfiguration.ApplicationName}");
            RemotingConfiguration.RegisterActivatedClientType(myActivatedClientTypeEntry);
            // callback to receive data
            CallbackHandler.OnDataChangedEh += OnDataChanged;
        }

        /// <summary>
        /// Parses the connection string. Absense of required parameters cause the exception
        /// </summary>
        void ParseConnectionString() {
            var settings = ConnectionString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .ToDictionary(i => i.Split('=')[0].Trim().ToUpper(), i => i.Split('=')[1].Trim());
            _remotehost = settings["REMOTEHOST"]; _port = settings["PORT"];
            { settings.TryGetValue("RENEWALTIME", out string s); if (int.TryParse(s, out int i)) _renewaltime = i; }
        }

        public override bool SupportsTemporalProcessing => false;

        protected override bool UseAsyncConnect => false;

        public override string GetShortStatus(int maxLength) {
            return ("Total sent measurements " + ProcessedMeasurements.ToString("N0")).CenterText(maxLength);
        }

        public override void Initialize() {
            bool result = false;
            try {
                // own log is used
                Trace.Listeners.Clear(); TraceInit(); base.Initialize(); App.TraceMsg($"Initialize with: {ConnectionString}"); ProcessingInterval = 0;
                OnStatusMessage(MessageLevel.Info, $"Initialising remote data source for device {Name}");
                StatisticsEngine.Register(this, "DataRemoting", "PMU");
                // get the list of parameters to request from the server
                _items = ParseOutputMeasurements(DataSource, false, $"FILTER ActiveMeasurements WHERE Device = '{Name}'");
                OnStatusMessage(MessageLevel.Info, $"Remote data source is initialized for device {Name}");
                result = IsConnected = true;
            }
            catch (Exception ex) { App.ErrorTraceEx(ex, "Initialize"); }
            if (!result) OnStatusMessage(MessageLevel.Info, $"Initialization failed");
            App.TraceMsg($"Initialize: {result}");
        }

        void TraceInit() {
            Trace.Listeners.Add(new TextWriterTraceListener($"{_cfg.AppSettings.Settings["logpath"].Value}\\RemoteDataAdapter_{Name}={DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt"));
            Trace.AutoFlush = true;
        }

        protected override void AttemptConnection() {
            if (Trace.Listeners.Count == 0) TraceInit();
            App.TraceMsg($"AttemptConnection"); bool result = false;
            try {
                var port = Convert.ToInt32(_port);
                _tc = new TcpChannel(new Hashtable() { ["port"] = port }, null, new BinaryServerFormatterSinkProvider() { TypeFilterLevel = TypeFilterLevel.Full });
                ChannelServices.RegisterChannel(_tc, false); _client = new DataRemotingClient();
                if (_client.Initialize()) {
                    // watchdog timer
                    _tmr = new Timer(OnFire, null, _renewaltime * 2200, _renewaltime * 1100);
                    ((ILease)_client.GetLifetimeService()).Register(new MySponsor());
                    if (result = _client?.Connect() ?? false) OnStatusMessage(MessageLevel.Info, $"Remote data adapter connected to {Name}");
                    else OnStatusMessage(MessageLevel.Info, $"Connection to {Name} remote data adapter failed");
                }
                else OnStatusMessage(MessageLevel.Info, $"Connection to {Name} remote data adapter failed");
            }
            catch (Exception ex) { App.ErrorTraceEx(ex, "AttemptConnection"); }
            _connected = IsConnected = result; App.TraceMsg($"AttemptConnection: {result}"); if (!result) throw new Exception();
        }

        protected override void OnConnected(){
            App.TraceMsg($"OnConnected"); bool result = false;
            try {
                var measurements = DataSource.Tables["ActiveMeasurements"];
                if (result = _client?.CreateGroup(Array.ConvertAll(_items, i => measurements.Select($"ID = '{i.Key}'")[0]["SignalReference"].ToNonNullString())) ?? false)
                    OnStatusMessage(MessageLevel.Info, "Remote data subscribed");
                else OnStatusMessage(MessageLevel.Info, $"Subscription failed");
            }
            catch (Exception ex) { App.ErrorTraceEx(ex, "OnConnected"); }
            IsConnected = result; App.TraceMsg($"OnConnected: {result}"); if (!result) throw new Exception();
        }

        protected override void Dispose(bool disposing) {
            App.TraceMsg($"Dispose");
            try { base.Dispose(true); _tmr?.Dispose(); _client?.Dispose(); }
            catch (Exception ex) { App.ErrorTraceEx(ex, "Dispose"); }
            App.TraceMsg($"Dispose: done"); Trace.Close(); Trace.Listeners.Clear();
        }

        protected override void AttemptDisconnection() {
            App.TraceMsg($"AttemptDisconnection");
            bool result = false;
            try {
                try { ChannelServices.UnregisterChannel(_tc); } catch { }
                if (IsConnected) { _client?.Disconnect(); result = true; }
            }
            catch (Exception ex) { App.ErrorTraceEx(ex, "AttemptDisconnection"); }
            if (!result) OnStatusMessage(MessageLevel.Info, $"Disconnection failed");
            App.TraceMsg($"AttemptDisconnection: {result}");
        }

        public void OnDataChanged(object o, EventArgs ea) {
            try {
                var vra = o as ValueResult[];
                List<IMeasurement> measurements = new List<IMeasurement>();
                foreach (var vri in vra) {
                    var measurement = Measurement.Clone(_items[vri.Index], Convert.ToDouble(vri.Value), vri.Timestamp.ToUniversalTime().Ticks);
                    measurement.StateFlags = vri.Satus >= 192 ? MeasurementStateFlags.Normal : vri.Satus >= 20 ? MeasurementStateFlags.SuspectData : MeasurementStateFlags.BadData;
                    measurements.Add(measurement);
                }
                OnNewMeasurements(measurements);
            }
            catch (Exception ex) { App.ErrorTraceEx(ex, "OnDataChanged"); }
        }
        /// <summary>
        /// Watchdog timer fired
        /// </summary>
        /// <param name="o"></param>
        void OnFire(object o) {
            if (_connected = MySponsor.Watchdog) MySponsor.Watchdog = false;
            else {
                // to process reconnection TBD
            }
        }
        /// <summary>
        /// Adds server's connection state to adapter's status
        /// </summary>
        public override string Status {
            get {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(base.Status);
                stringBuilder.AppendFormat("   Remote server connected: {0}", _connected);
                stringBuilder.AppendLine();
                return stringBuilder.ToString();
            }
        }
    }
}
