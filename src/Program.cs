#region Header
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using VRageMath;
using VRage.Game;
using VRage.Collections;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;

namespace SpaceEngineers.IngameScript.PowerStats
{
    public sealed class Program : MyGridProgram
    {
#endregion

        const String LCD_NAME = null;
        const int BATTERY_GRAPH_SIZE = 10;
        const string BATTERY_GRAPH_INCREASE = ">";
        const string BATTERY_GRAPH_DECREASE = "<";
        const string BATTERY_GRAPH_FULL = "O";
        const string BATTERY_GRAPH_EMPTY = "_";
        
        List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
        Dictionary<string, List<IMyPowerProducer>> producerMap = null;
        TimeSpan time = new TimeSpan();
        IMyTextSurface lcdOutput = null;
        
        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }
        
        public void Save() {}
        
        public void Main(string argument, UpdateType updateSource) {
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
            //GridTerminalSystem.GetBlocksOfType(windturbines, generator => generator.BlockDefinition.SubtypeName == "LargeBlockWindTurbine");
        
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
                    // Filter out docked grids
                    if (producer.CubeGrid != Me.CubeGrid) {
                        continue;
                    }
                    
                    // Separate batteries
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
                // Iterate over the batteries and sum their values
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
        
                if (powerLevelPercentage < 99.99f) {
                    if (loading) {
                        batteryGraph += BATTERY_GRAPH_INCREASE;
                    } else {
                        batteryGraph += BATTERY_GRAPH_DECREASE;
                    }
        
                    for (int i = batteryGraphFill; i < BATTERY_GRAPH_SIZE - 1; ++i) {
                        batteryGraph += BATTERY_GRAPH_EMPTY;
                    }
                }
        
                Print($"[{batteryGraph}] {(int)powerLevelPercentage} % ({Math.Round(currentStoredPower,2)}/{Math.Round(maxStoredPower,2)} MWh)\n");
        
                batteriesInput = (float)Math.Round(batteriesInput, 2);
                batteriesOutput = (float)Math.Round(batteriesOutput, 2);
                batteriesMaxOutput = (float)Math.Round(batteriesMaxOutput, 2);
        
                Print($"{batteries.Count} {batteryType} input: {batteriesInput} MW");
                Print(batteries.Count, batteryType, batteriesOutput, batteriesMaxOutput);
            } else {
                Print("No batteries found.");
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
                Print($"No power-producers found.");
            }
        }
        
        private void Print(int count, string name, float currentOutput, float maxOutput, string unit = "MW") {
            Print($"{count} {name} output: {currentOutput}/{maxOutput} {unit}");
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
        
        private string GetType(IMyPowerProducer producer) {
            //return producer.BlockDefinition.SubtypeName;
            string typeString = producer.BlockDefinition.TypeIdString;
        
            if (typeString.StartsWith("MyObjectBuilder_")) {
                return typeString.Substring("MyObjectBuilder_".Length);
            }
        
            return typeString;    
        }

#region Footer
    }
}
#endregion
