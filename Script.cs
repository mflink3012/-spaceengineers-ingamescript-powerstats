  const string PROPERTY_NAME_TEXTSURFACE = "text-surface";
const int BATTERY_GRAPH_SIZE = 10;
const string BATTERY_GRAPH_INCREASE = ">";
const string BATTERY_GRAPH_DECREASE = "<";
const string BATTERY_GRAPH_FULL = "O";
const string BATTERY_GRAPH_EMPTY = "_";
List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
Dictionary<string, List<IMyPowerProducer>> producerMap = null;
TimeSpan time = new TimeSpan();
IMyTextSurface textSurface = null;
IMyTextSurface batterySurface = null;
string textSurfaceParentName = null;
Dictionary<String, String> props = null;
public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
    props = ReadProperties(Me.CustomData);
}
public void Save() {}
public void Main(string argument, UpdateType updateSource) {
    if (textSurface == null) {
        if (props.ContainsKey(PROPERTY_NAME_TEXTSURFACE)) {
            textSurfaceParentName = props[PROPERTY_NAME_TEXTSURFACE];
            textSurface = GridTerminalSystem.GetBlockWithName(textSurfaceParentName) as IMyTextSurface;
        }         if (textSurface == null) {
            textSurface = Me.GetSurface(0);
            batterySurface = Me.GetSurface(1);
            batterySurface.FontSize = 4.35f;
            batterySurface.Alignment = TextAlignment.CENTER;
        }         if (batterySurface == null) {
            batterySurface = textSurface;
        }         textSurface.ContentType = ContentType.TEXT_AND_IMAGE;
        batterySurface.ContentType = ContentType.TEXT_AND_IMAGE;
    }     if (props.ContainsKey(PROPERTY_NAME_TEXTSURFACE) && textSurface != Me.GetSurface(0)) {
        Echo($"Using text-surface at: '{textSurfaceParentName}'");
    } else if (props.ContainsKey(PROPERTY_NAME_TEXTSURFACE)) {
        Echo($"ERROR: No '{PROPERTY_NAME_TEXTSURFACE}' found with name '{props[PROPERTY_NAME_TEXTSURFACE]}'. Using text-surface from this programmable block.");
    } else {
        Echo($"WARNING: No '{PROPERTY_NAME_TEXTSURFACE}' defined in custom data. Using text-surface from this programmable block.");
    }
    textSurface.WriteText("");
    batterySurface.WriteText("");
    Print(textSurface, "POWER-STATS");
    time += Runtime.TimeSinceLastRun;
    if (producerMap == null || (time.Seconds % 2) == 0) {
        List<IMyPowerProducer> powerProducers = new List<IMyPowerProducer>();
        GridTerminalSystem.GetBlocksOfType(powerProducers);
        if (producerMap == null) {
            producerMap = new Dictionary<string, List<IMyPowerProducer>>();
        } else {
            producerMap.Clear();
        }
        batteries.Clear();
        foreach (IMyPowerProducer producer in powerProducers) {
            if (producer.CubeGrid != Me.CubeGrid) {
                continue;
            }
            if (producer is IMyBatteryBlock) {
                batteries.Add(producer as IMyBatteryBlock);
                continue;
            }
            string key = GetType(producer);
            List<IMyPowerProducer> producersOfSameType = null;
            try {
                producersOfSameType = producerMap[key];
            } catch (KeyNotFoundException) {
                producersOfSameType = new List<IMyPowerProducer>();
                producerMap.Add(key, producersOfSameType);
            }
            producersOfSameType.Add(producer);
        }
    }
    if (batteries.Count > 0) {
        float currentStoredPower = 0f;
        float maxStoredPower = 0f;
        float batteriesInput = 0f;
        float batteriesOutput = 0f;
        float batteriesMaxOutput = 0f;
        string batteryType = GetType(batteries[0]);
        foreach (IMyBatteryBlock battery in batteries) {
            currentStoredPower += battery.CurrentStoredPower;
            maxStoredPower += battery.MaxStoredPower;
            batteriesInput += battery.CurrentInput;
            batteriesOutput += battery.CurrentOutput;
            batteriesMaxOutput += battery.MaxOutput;
        }
        float powerTendency = batteriesInput - batteriesOutput;
        bool loading = (powerTendency > 0f);
        float powerLevelPercentage = currentStoredPower / maxStoredPower * 100;
        String batteryGraph = "";
        int batteryGraphFill = (int)powerLevelPercentage / BATTERY_GRAPH_SIZE;
        for (int i = 0; i < batteryGraphFill; ++i) {
            batteryGraph += BATTERY_GRAPH_FULL;
        }
        if (batteryGraphFill < BATTERY_GRAPH_SIZE) {
            if (loading) {
                batteryGraph += BATTERY_GRAPH_INCREASE;
            } else {
                batteryGraph += BATTERY_GRAPH_DECREASE;
            }
            for (int i = batteryGraphFill; i < BATTERY_GRAPH_SIZE - 1; ++i) {
                batteryGraph += BATTERY_GRAPH_EMPTY;
            }
        }         if (batterySurface == textSurface) {
            Print(batterySurface, $"[{batteryGraph}] {(int)powerLevelPercentage} % ({Math.Round(currentStoredPower,2)}/{Math.Round(maxStoredPower,2)} MWh)\n");
        } else {
            Print(batterySurface, $"[{batteryGraph}]\n{(int)powerLevelPercentage} %\n{Math.Round(currentStoredPower,2)}/{Math.Round(maxStoredPower,2)} MWh");
        }
        batteriesInput = (float)Math.Round(batteriesInput, 2);
        batteriesOutput = (float)Math.Round(batteriesOutput, 2);
        batteriesMaxOutput = (float)Math.Round(batteriesMaxOutput, 2);
        Print(textSurface, $"{batteries.Count} {batteryType} input: {batteriesInput} MW");
        Print(batteries.Count, batteryType, batteriesOutput, batteriesMaxOutput);
    } else {
        Print(textSurface, "No batteries found.");
    }
    if (producerMap.Count > 0) {
        foreach (KeyValuePair<string, List<IMyPowerProducer>> kvp in producerMap) {
            int count = kvp.Value.Count;
            string name = kvp.Key;
            float producerOutput = 0f;
            float producerMaxOutput = 0f;
            foreach (IMyPowerProducer powerProducer in kvp.Value) {
                producerOutput += powerProducer.CurrentOutput;
                producerMaxOutput += powerProducer.MaxOutput;
            }
            producerOutput = (float)Math.Round(producerOutput, 2);
            producerMaxOutput = (float)Math.Round(producerMaxOutput, 2);
            Print(count, name, producerOutput, producerMaxOutput);
        }
    } else {
        Print(textSurface, "No power-producers found.");
    }
}
private void Print(int count, string name, float currentOutput, float maxOutput, string unit = "MW") {
    Print(textSurface, $"{count} {name} output: {currentOutput}/{maxOutput} {unit}");
}
private void Print(IMyTextSurface surface, String text) {
    Echo(text);
    surface.WriteText($"{text}\n", true);
} private string GetType(IMyPowerProducer producer) {
    string typeString = producer.BlockDefinition.TypeIdString;
    if (typeString.StartsWith("MyObjectBuilder_")) {
        return typeString.Substring("MyObjectBuilder_".Length);
    }
    return typeString;
} private Dictionary<String, String> ReadProperties(string source) {
    Dictionary<String, String> result = new Dictionary<String, String>();
    string[] lines = source.Split('\n');
    string[] pair;     foreach (var line in lines)
    {
        pair = line.Split(new char[1] { '=' }, 2);
        if (pair.Length == 2) {
            result.Add(pair[0].ToLower(), pair[1]);
        } else {
            result.Add(pair[0].ToLower(), "");
        }
    }     return result;
}
