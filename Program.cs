using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;
using static IngameScript.Program;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        string openingID = "[";
        string closingID = "]";
        string ID = "Airlock";
        string inID = "Inner";
        string outID = "Outer";
        float fillRatio = 0.2f;

        //==============================//
        //   DON'T TOUCH BEYOND THIS POINT   //
        //==============================//


        MyCommandLine _commandLine = new MyCommandLine();
        Dictionary<string, Action> _commands = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);

        List<Airlock> Airlocks = new List<Airlock>();
        List<Airlock> AirlocksInProgress = new List<Airlock>();

        bool cycling = false;

        IMyGasTank specialTank;

        private static int currentPosition = 0;
        private static char movingChar = '•';
        public Program()
        {
            _commands["add"] = CreateAirlock;
            _commands["Cycle"] = Cycle;
            _commands["delete"] = DeleteAirlocks;
            _commands["Cycle Out"] = CycleOut;
            _commands["Cycle in"] = CycleIn;
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            CreateAirlock();
        }

        public void Main(string argument, UpdateType updateSource)
        {
            IsWorking();
            ParseData(argument);
            Cycling();
            FillTank();
        }
        public void ParseData(String argument) // Parse the arguments that are given to the programmable blcok.
        {
            if (_commandLine.TryParse(argument))
            {
                Action commandAction;

                // Retrieve the first argument. Switches are ignored.
                string command = _commandLine.Argument(0);

                // Now we must validate that the first argument is actually specified,
                // then attempt to find the matching command delegate.
                if (command == null)
                {
                    Echo("No command specified");
                }
                else if (_commands.TryGetValue(_commandLine.Argument(0), out commandAction))
                {
                    // We have found a command. Invoke it.
                    commandAction();
                }
                else
                {
                    Echo($"Unknown command {command}");
                }
            }
        }

        public void CreateAirlock()
        {
            int i = 1;
            int y = -1;

            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();

            IMyDoor door;
            IMyAirVent vent;

            GridTerminalSystem.SearchBlocksOfName(openingID + ID, blocks);
            foreach (IMyTerminalBlock block in blocks)
            {
                if (block is IMyGasTank)
                {
                }
            }
            for (i = 1; i <= 99; i++)
            {
                List<IMyDoor> innerDoors = new List<IMyDoor>();
                List<IMyDoor> outerDoors = new List<IMyDoor>();
                List<IMyAirVent> vents = new List<IMyAirVent>();

                foreach (IMyTerminalBlock block in blocks)
                {
                    if (block.CustomName.Contains(i.ToString() + closingID))
                    {
                        bool airlockExists = Airlocks.Any(airlock => airlock.ID == i);
                        if (airlockExists)
                        {
                            Echo($"Airlock {i} already exists. As such this block will be ignored.");
                        }
                        else
                        {
                            y = i;
                            if (block is IMyDoor)
                            {
                                door = block as IMyDoor;
                                if (door.CustomName.Contains(openingID + inID + closingID))
                                {
                                    innerDoors.Add(door);
                                }
                                else if (door.CustomName.Contains(openingID + outID + closingID))
                                {
                                    outerDoors.Add(door);
                                }
                            }
                            else if (block is IMyAirVent)
                            {
                                vent = block as IMyAirVent;
                                vents.Add(vent);
                            }
                        }
                    }
                }
                if (innerDoors.Count > 0 && outerDoors.Count > 0 && vents.Count > 0)
                {
                    Airlocks.Add(new Airlock(innerDoors, outerDoors, vents, specialTank, y, this));
                }
            }
        }


        public void DeleteAirlocks()
        {
            Airlocks.Clear();
            Runtime.UpdateFrequency = UpdateFrequency.None;
        }

        public void Cycle()
        {
            cycling = true;
            string argument = _commandLine.Argument(1);
            int airlockID = -1;

            bool areEqual = string.Equals(argument, "all", StringComparison.OrdinalIgnoreCase);
            if (areEqual)
            {
                foreach (Airlock airlock in Airlocks)
                { airlock.Cycle(); }
            }

            try
            {
                int.TryParse(argument, out airlockID);
            }
            catch
            {
                Echo($"Cannot find airlock {argument}");
            }
            Airlock specificAirlock = Airlocks.FirstOrDefault(airlock => airlock.ID == airlockID);
            if (specificAirlock != null)
            {
                specificAirlock.Cycle();
            }
            else
                Echo($"Cannot find Airlock {argument}");
        }

        public void CycleOut()
        {
            cycling = true;
            string argument = _commandLine.Argument(1);
            int airlockID = -1;

            try
            {
                // Convert string to int.
                int.TryParse(argument, out airlockID);
            }
            catch
            {
                Echo($"Cannot find airlock {argument}");
            }
            Airlock specificAirlock = Airlocks.FirstOrDefault(airlock => airlock.ID == airlockID);
            if (specificAirlock != null)
            {
                specificAirlock.CycleOut();
            }
            else
                Echo($"Cannot find Airlock {argument}");
        }

        public void CycleIn()
        {
            cycling = true;
            string argument = _commandLine.Argument(1);
            int airlockID = -1;

            try
            {
                // Convert string to int.
                int.TryParse(argument, out airlockID);
            }
            catch
            {
                Echo($"Cannot find airlock {argument}");
            }
            Airlock specificAirlock = Airlocks.FirstOrDefault(airlock => airlock.ID == airlockID);
            if (specificAirlock != null)
            {
                specificAirlock.CycleIn();
            }
            else
                Echo($"Cannot find Airlock {argument}");
        }
        public void Cycling()
        {
            if (cycling)
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
                List<Airlock> airlocksToRemove = new List<Airlock>();

                foreach (Airlock airlock in Airlocks)
                {
                    if (airlock.cyclestate == CycleState.CycleIn || airlock.cyclestate == CycleState.CycleOut)
                    {
                        AirlocksInProgress.Add(airlock);
                    }
                }

                foreach (Airlock airlock in AirlocksInProgress)
                {
                    if (airlock.cyclestate == CycleState.CycleIn)
                        airlock.CycleIn();
                    else if (airlock.cyclestate == CycleState.CycleOut)
                        airlock.CycleOut();
                    else if ((airlock.cyclestate == CycleState.DoneOut) || (airlock.cyclestate == CycleState.DoneIn))
                        airlocksToRemove.Add(airlock);
                }

                foreach (Airlock airlockToRemove in airlocksToRemove)
                {
                    AirlocksInProgress.Remove(airlockToRemove);
                }

                if (AirlocksInProgress.Count == 0)
                {
                    Runtime.UpdateFrequency = UpdateFrequency.Update100;
                    cycling = false;
                }
            }
        }

        public void FillTank()
        {
            if (!cycling)
            {
                if (specialTank.FilledRatio < fillRatio)
                {

                    specialTank.Enabled = true;
                }
                else
                    specialTank.Enabled = false;
            }
        }

        static string UpdateLoadingString(string x)
        {
            char[] charArray = x.ToCharArray();

            // Clear the previous position of the moving character
            charArray[currentPosition] = '-';

            // Move to the next position
            currentPosition = (currentPosition + 1) % x.Length;

            // Set the moving character in the new position
            charArray[currentPosition] = movingChar;

            return new string(charArray);
        }

        void IsWorking()
        {
            string baseString = "---------------------------------------------";
            string movingString = UpdateLoadingString(baseString);

            Echo(movingString);
            Echo("Script is running");
            Echo(movingString);
        }
    }
}