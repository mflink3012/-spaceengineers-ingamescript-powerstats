const String LCD_NAME = null;
const int BATTERY_GRAPH_SIZE = 10;

/**

BETTER IDEA: Fetch all PowerProducers, iterate over them and count by name (not custom name!).
That would count all producers (also unknown to the script) and should make the code more generic.

**/

List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
List<IMySolarPanel> solarpanels = new List<IMySolarPanel>();
List<IMyPowerProducer> windturbines = new List<IMyPowerProducer>();
List<IMyReactor> reactors = new List<IMyReactor>();
TimeSpan time = new TimeSpan();
IMyTextSurface lcdOutput = null;

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

public void Save() {}

public void Main(string argument, UpdateType updateSource)
{
    if (lcdOutput == null) {
        if (LCD_NAME == null || LCD_NAME == "") {
            lcdOutput = Me.GetSurface(0);
        } else {
            lcdOutput = GridTerminalSystem.GetBlockWithName(LCD_NAME) as IMyTextSurface;
        }
    }

    ClearLCD();

    Print("POWER-STATS");

    time += Runtime.TimeSinceLastRun;

    // Fill a list with all batteries in the system
    if (batteries.Count < 1 || (time.Seconds % 2) == 0) {
        GridTerminalSystem.GetBlocksOfType(batteries);
    }

    if (solarpanels.Count < 1 || (time.Seconds % 2) == 0) {
        GridTerminalSystem.GetBlocksOfType(solarpanels);
    }

    if (windturbines.Count < 1 || (time.Seconds % 2) == 0) {
        GridTerminalSystem.GetBlocksOfType(windturbines, generator => generator.BlockDefinition.SubtypeName == "LargeBlockWindTurbine");
    }

    if (reactors.Count < 1 || (time.Seconds % 2) == 0) {
        GridTerminalSystem.GetBlocksOfType(reactors);
    }

    // Iterate over the batteries and sum their values
    float currentStoredPower = 0f;
    float maxStoredPower = 0f;
    float batteriesInput = 0f;
    float batteriesOutput = 0f;
    foreach (IMyBatteryBlock battery in batteries) {
        currentStoredPower += battery.CurrentStoredPower;
        maxStoredPower += battery.MaxStoredPower;
        batteriesInput += battery.CurrentInput;
        batteriesOutput += battery.CurrentOutput;
    }

    float solarpanelsOutput = 0f;
    float solarpanelsMaxOutput = 0f;
    foreach (IMyPowerProducer powerProducer in solarpanels) {
        solarpanelsOutput += powerProducer.CurrentOutput;
        solarpanelsMaxOutput += 0.16f; // powerProducer.MaxOutput;
    }

    float windturbinesOutput = 0f;
    float windturbinesMaxOutput = 0f;
    foreach (IMyPowerProducer powerProducer in windturbines) {
        windturbinesOutput += powerProducer.CurrentOutput;
        windturbinesMaxOutput += 0.4f; // powerProducer.MaxOutput;
    }

    float reactorsOutput = 0f;
    float reactorsMaxOutput = 0f;
    foreach (IMyPowerProducer powerProducer in reactors) {
        reactorsOutput += powerProducer.CurrentOutput;
        reactorsMaxOutput += powerProducer.MaxOutput; // 500 kW (Small) or 15 MW (Large)
    }

    float powerTendency = batteriesInput - batteriesOutput;
    bool loading = (powerTendency > 0f);
    float powerLevelPercentage = currentStoredPower / maxStoredPower * 100;

    String batteryGraph = "";
    int batteryGraphFill = (int)powerLevelPercentage / BATTERY_GRAPH_SIZE;
    for (int i = 0; i < batteryGraphFill; ++i) {
        batteryGraph += "O";
    }

    if (powerLevelPercentage < 99.99f) {
        if (loading) {
            batteryGraph += ">";
        } else {
            batteryGraph += "<";
        }

        for (int i = batteryGraphFill; i < BATTERY_GRAPH_SIZE - 1; ++i) {
            batteryGraph += "_";
        }
    }

    Print($"[{batteryGraph}] {(int)powerLevelPercentage} % ({Math.Round(currentStoredPower,2)}/{Math.Round(maxStoredPower,2)} MWh)\n");
    Print($"Batteries count: {batteries.Count}");
    Print($"Batteries input: {Math.Round(batteriesInput, 2)} MW");
    Print($"Batteries output: {Math.Round(batteriesOutput, 2)} MW");

    if (solarpanels.Count > 0) {
        Print($"Solar panels count: {solarpanels.Count}");
        Print($"Solar panels output: {Math.Round(solarpanelsOutput, 2)}/{Math.Round(solarpanelsMaxOutput, 2)} MW");
    } else {
        Echo($"Solar panels count: {solarpanels.Count}");
    }

    if (windturbines.Count > 0) {
        Print($"Wind turbines count: {windturbines.Count}");
        Print($"Wind turbines output: {Math.Round(windturbinesOutput, 2)}/{Math.Round(windturbinesMaxOutput, 2)} MW");
    } else {
        Echo($"Wind turbines count: {windturbines.Count}");
    }

    if (reactors.Count > 0) {
        Print($"Reactors count: {reactors.Count}");
        Print($"Reactors output: {Math.Round(reactorsOutput, 2)}/{Math.Round(reactorsMaxOutput, 2)} MW");
    } else {
        Echo($"Reactors count: {reactors.Count}");
    }
}

private void Print(String text) {
    Echo(text);
    WriteToLCD(text);
}

private void WriteToLCD(String text) {
    lcdOutput?.WriteText($"{text}\n", true);
}
    
private void ClearLCD() {
   lcdOutput?.WriteText("");
}