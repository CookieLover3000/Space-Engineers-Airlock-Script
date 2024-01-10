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
using static IngameScript.Program.Airlock;

namespace IngameScript
{
    partial class Program
    {
        public enum CycleState
        {
            Init,
            CycleOut,
            CycleIn,
            DoneOut,
            DoneIn
        }
        public enum PressureState
        {
            Depressurizing,
            Depressurized,
            Pressurizing,
            Pressurized,
            Init
        }
        public class Airlock
        {

            public int ID { get; set; }
            private List<IMyDoor> innerDoors;
            private List<IMyDoor> outerDoors;
            private List<IMyAirVent> vents;
            private List<IMyGasTank> oxygenTanks;
            private List<IMyGasGenerator> oxygenGenerators;
            private IMyGasTank specialTank;

            private Program _program;
            public CycleState cyclestate;
            public PressureState pressurestate;

            public Airlock(List<IMyDoor> innerDoors, List<IMyDoor> outerDoors, List<IMyAirVent> vents, IMyGasTank specialTank, int id, Program program)
            {
                ID = id;
                this.innerDoors = innerDoors;
                this.outerDoors = outerDoors;
                this.vents = vents;
                this.specialTank = specialTank;
                this._program = program;
                oxygenTanks = new List<IMyGasTank>();
                _program.GridTerminalSystem.GetBlocksOfType<IMyGasTank>(oxygenTanks, x => x.DetailedInfo.Split(' ')[1] == "Oxygen");
                oxygenGenerators = new List<IMyGasGenerator>();
                _program.GridTerminalSystem.GetBlocksOfType(oxygenGenerators);

                _program.Echo($"Added Airlock {id}");

                cyclestate = CycleState.Init;
                pressurestate = PressureState.Init;

            }

            public void Cycle()
            {
                _program.Echo($"Cycling Airlock {ID}");
                if (cyclestate == CycleState.DoneOut)
                    CycleIn();
                else if (cyclestate == CycleState.DoneIn)
                    CycleOut();
                else if (cyclestate == CycleState.Init && vents.All(vent => vent.Status == VentStatus.Pressurized))
                    CycleOut();
                else if (cyclestate == CycleState.Init)
                    CycleIn();
            }

            public void CycleIn()
            {
                foreach (var door in outerDoors)
                {
                    door.CloseDoor();
                }
                cyclestate = CycleState.CycleIn;
                if (outerDoors[0].Status == DoorStatus.Closed)
                {
                    foreach (var door in outerDoors)
                    {
                        door.Enabled = false;
                    }
                    try
                    {
                        Pressurize();
                    }
                    catch
                    {
                        _program.Echo("Cannot pressurize, opening airlock.");
                        pressurestate = PressureState.Pressurized;
                    }
                }

                if (pressurestate == PressureState.Pressurized)
                {
                    foreach (var door in innerDoors)
                    {
                        door.Enabled = true;
                        door.OpenDoor();
                    }
                    cyclestate = CycleState.DoneIn;
                }
            }

            public void CycleOut()
            {
                foreach (IMyDoor door in innerDoors)
                {
                    door.CloseDoor();
                }
                cyclestate = CycleState.CycleOut;
                    if (innerDoors[0].Status == DoorStatus.Closed)
                    {

                        foreach (var door in innerDoors)
                            door.Enabled = false;
                        foreach (var door in outerDoors)
                            door.Enabled = false;
                        try
                        {
                            Depressurize();
                        }
                        catch
                        {
                            _program.Echo("Cannot Depressurize, opening airlock.");
                            pressurestate = PressureState.Depressurized;
                        }
                    }
                    if (pressurestate == PressureState.Depressurized)
                    {
                        foreach (var door in outerDoors)
                        {
                            door.Enabled = true;
                            door.OpenDoor();
                        }
                        cyclestate = CycleState.DoneOut;
                    }
            }

            private void Pressurize()
            {
                _program.GridTerminalSystem.GetBlocksOfType<IMyGasTank>(oxygenTanks, x => x.DetailedInfo.Split(' ')[1] == "Oxygen");
                _program.GridTerminalSystem.GetBlocksOfType(oxygenGenerators);

                if (pressurestate != PressureState.Pressurizing)
                {
                    pressurestate = PressureState.Pressurizing;

                    foreach (var tank in oxygenTanks)
                        tank.Enabled = false;
                    foreach (var generator in oxygenGenerators)
                        generator.Enabled = false;
                    specialTank.Enabled = true;
                    foreach (var vent in vents)
                        vent.Depressurize = false;
                }
                if (specialTank.FilledRatio < 0.000001f)
                {
                    _program.Echo($"{specialTank} is empty, enabling other tanks");
                    foreach (var tank in oxygenTanks)
                        tank.Enabled = true;
                }

                if (vents[0].Status == VentStatus.Pressurized)
                {
                    pressurestate = PressureState.Pressurized;

                    foreach (var tank in oxygenTanks)
                        tank.Enabled = true;
                    foreach (var generator in oxygenGenerators)
                        generator.Enabled = true;
                    specialTank.Enabled = false;
                }
            }

            private void Depressurize()
            {
                _program.GridTerminalSystem.GetBlocksOfType<IMyGasTank>(oxygenTanks, x => x.DetailedInfo.Split(' ')[1] == "Oxygen");
                _program.GridTerminalSystem.GetBlocksOfType(oxygenGenerators);

                pressurestate = PressureState.Depressurizing;

                if (vents[0].GetOxygenLevel() < 0.000001f)
                    pressurestate = PressureState.Depressurized;

                // Disable oxygen tanks
                foreach (var tank in oxygenTanks)
                    tank.Enabled = false;
                // Disable o2/h2 generators
                foreach (var generator in oxygenGenerators)
                    generator.Enabled = false;
                // Enable the Special tank
                specialTank.Enabled = true;
                // Make the vents in the airlock depressurize.
                foreach (var vent in vents)
                    vent.Depressurize = true;

                if (pressurestate == PressureState.Depressurized)
                {
                    pressurestate = PressureState.Depressurized;

                    // Enable all tanks
                    foreach (var tank in oxygenTanks)
                        tank.Enabled = true;
                    // Enable the Generators
                    foreach (var generator in oxygenGenerators)
                        generator.Enabled = true;
                    // Disable the Special Tank
                    specialTank.Enabled = false;
                }
                if (specialTank.FilledRatio > 0.999999f)
                {
                    _program.Echo($"{specialTank} is full, Opening Airlock without depressurizing.");
                    pressurestate = PressureState.Depressurized;
                }
            }
        }
    }
}
