using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nsOpenHistorianRemoteDataAdapter {

    internal class App {

        public static void ErrorTraceEx(Exception ex, string msg) {
            string exmsg = string.Concat($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {msg}: ",
                ex.Message, "\r\nSource: ", ex.Source, "\r\nMember: ", ex.TargetSite, "\r\nStack trace: ", ex.StackTrace);
            if (ex.InnerException != null) exmsg = String.Concat(exmsg, "\r\nInnerException: ", ex.InnerException.Message, "\r\nSource: ",
                ex.InnerException.Source, "\r\nMember: ", ex.InnerException.TargetSite, "\r\nStack trace: ", ex.InnerException.StackTrace);
            Trace.WriteLine(exmsg);
        }

        public static void TraceMsg(string msg) {
            Trace.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {msg}");
        }
    }
}
