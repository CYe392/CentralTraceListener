using Fisher.Utilities.CentralTraceListener.Model;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Fisher.Utilities.CentralTraceListener.Helper
{
    public class ConfigureReader : IConfigureReader
    {
        public XDocument ReadConfig()
        {
            var configPath = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).FilePath;
            return XDocument.Load(configPath);
        }

        public ListenerSetting FindTraceListenerConfig(string listenerName, string traceSourceName = null)
        {
            var diagnosticsConfig = ReadConfig()?.FindSection("configuration", "system.diagnostics");
            if (diagnosticsConfig == null)
                return null;

            //Read SharedListeners tags
            var sharedListener = diagnosticsConfig.FindDescendant("sharedListeners")?.FindDescendantByName("add", listenerName);
            var switchFilterLevel = TraceLevel.Verbose;

            XElement listenerSection;
            if (string.IsNullOrEmpty(traceSourceName))
            {
                //Read <Listeners> under <Trace>
                listenerSection = diagnosticsConfig.FindSection("trace", "listeners");
            }
            else
            {
                //Read <Listeners> under <Sources>
                var sourceSection = diagnosticsConfig.FindSection("sources")?.FindElemntByName("source", traceSourceName);
                var switchName = sourceSection?.GetAttribute("switchName");

                if (sourceSection == null || string.IsNullOrEmpty(switchName))
                    return null;

                listenerSection = sourceSection.Element("listeners");

                //Read <switches> under <sources>
                var switchSetting = diagnosticsConfig.FindSection("switches")?.FindDescendantByName("add", switchName);
                switchFilterLevel = GetSwitchLevelFromXML(switchSetting);
            }

            var listener = XMLHelper.FindTraceListenerFromSection(listenerSection, listenerName);
            return CombineListenerSetting(listener, sharedListener, switchFilterLevel);
        }

        private ListenerSetting CombineListenerSetting(XElement listener, XElement sharedListener, TraceLevel switchFilterLevel = TraceLevel.Verbose)
        {
            if (listener == null)
                return null;

            //If the listener is overwritten, get attributes and filter from <Listeners> section
            //If the listener is referenced, get attributes and filter from <sharedListeners> section
            var listenerConfig = XMLHelper.HasAttribute(listener)
                                ? ConvertToListenerSetting(listener)
                                : ConvertToListenerSetting(sharedListener);

            //Compare switch level with listener filter level
            listenerConfig.Filter = CalcListenerFilter(switchFilterLevel, listenerConfig.Filter);
            return listenerConfig;
        }

        private TraceLevel GetSwitchLevelFromXML(XElement switchSection)
        {
            if (switchSection == null)
                return TraceLevel.Verbose;

            var switchFilterValue = switchSection.GetAttribute("value");
            //If value is int value of TraceLevel enum
            if (Regex.IsMatch(switchFilterValue, "^[0-4]$"))
            {
                return (TraceLevel)(int.Parse(switchFilterValue));
            }
            //If value is in string 
            return TypeConverter.ConvertStringToEnum(switchFilterValue, TraceLevel.Verbose);
        }

        private TraceFilter CalcListenerFilter(TraceLevel switchFilterLevel, TraceFilter listenerFilter)
        {
            SourceLevels convertedSourceLevel = switchFilterLevel == TraceLevel.Off
                                                ? SourceLevels.Off
                                                : (SourceLevels)((1 << (int)switchFilterLevel + 1) - 1);

            if (listenerFilter == null || listenerFilter is SourceFilter)
            {
                return new EventTypeFilter(convertedSourceLevel);
            }

            var listenerLevel = ((EventTypeFilter)listenerFilter).EventType;
            SourceLevels finalFilterLevel;

            if (convertedSourceLevel == SourceLevels.All)
                finalFilterLevel = listenerLevel;
            else if (listenerLevel == SourceLevels.All)
                finalFilterLevel = convertedSourceLevel;
            else
            {
                finalFilterLevel = (int)listenerLevel < (int)convertedSourceLevel
                                    ? listenerLevel
                                    : convertedSourceLevel;
            }

            return new EventTypeFilter(finalFilterLevel);
        }

        private ListenerSetting ConvertToListenerSetting(XElement element)
        {
            if (element == null)
                return null;

            var config = new ListenerSetting();

            config.Name = element.GetAttribute("name");
            config.Logger = element.GetAttribute("logger");
            config.CentralLogServiceUrl = element.GetAttribute("centralLogServiceUrl");

            var appId = element.GetAttribute("appId");
            config.AppId = TypeConverter.ConvertStringToInt(appId);

            var traceOutputOptions = element.GetAttribute("traceOutputOptions");
            config.AppendTraceOutputOption(traceOutputOptions);

            var filterElement = element.FindDescendant("filter");
            if (filterElement == null)
                return config;

            var filterType = filterElement.GetAttribute("type");
            var filterData = filterElement.GetAttribute("initializeData");

            if (filterType == typeof(EventTypeFilter).FullName)
            {
                config.Filter = new EventTypeFilter(TypeConverter.ConvertStringToEnum(filterData, SourceLevels.Verbose));
            }
            else if (filterType == typeof(SourceFilter).FullName)
            {
                config.Filter = new SourceFilter(filterData);
            }

            return config;
        }

    }
}
