using System;
using System.Collections.Specialized;

namespace Fisher.Utilities.CentralTraceListener.Model
{
    [Serializable]
    public class MessageLogItem
    {
        public long Id { get; set; }
        public int AppId { get; set; }
        public int ErrorNum { get; set; }
        public string HostName { get; set; }
        public string ExeName { get; set; }
        public DateTime LogDate { get; set; }
        public string Thread { get; set; }
        public string Level { get; set; }
        public string Logger { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
        public StringDictionary Attrs { get; set; }
        public MessageLogItem()
        {
            Attrs = new StringDictionary();
        }
    }
}
