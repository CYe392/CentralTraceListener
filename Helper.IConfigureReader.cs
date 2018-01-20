using System.Xml.Linq;
using Fisher.Utilities.CentralTraceListener.Model;

namespace Fisher.Utilities.CentralTraceListener.Helper
{
    public interface IConfigureReader
    {
        ListenerSetting FindTraceListenerConfig(string listenerName, string traceSourceName = null);
        XDocument ReadConfig();
    }
}
