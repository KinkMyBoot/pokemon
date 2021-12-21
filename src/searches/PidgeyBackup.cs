using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;

using static SearchCommon;

class PidgeyBackup
{
    public static void Check()
    {
        // 13
        // string pidgeybackup = "UUAUUUUUUAUUUUU"+"UUUAUUURU"+"UUUUARRRRR";
        // string pidgeybackup = "UUAUUUUUUAUUUUU"+"UUUAUUURU"+"UUUURARRRUUUUUR";
        // string pidgeybackup = "UUAUUUUUUUUUUU"+"UURUUUUU"+"UAUUURRRR";
        // string pidgeybackup = "UUAUUUUUUUUUUU"+"UURUUUUU"+"UUUURRRRU";
        string pidgeybackup = "UUUUUUUUUUUUU"+"UAUUUUAURU"+"UUUURR"+"RRRRSLLUUUUU"; // 3594/3600
        // string pidgeybackup = "UUUUUUUUUUUUU"+"URUAUUAUUU"+"UUUURR";
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.PalHold);
        RbyIGTChecker<Red>.CheckIGT("basesaves/red/manip/pidgey13.gqs", intro, pidgeybackup, "PIDGEY", 3600);
        // 14
        // string pidgeybackup = "UUUUUUUUUUUUUU"+"URUAUUAUUU"+"UUAUUAR"+"RRDRRUUUUUURUUUDDDLLLLRR"; // 3595/3600
        // RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.PalHold);
        // RbyIGTChecker<Red>.CheckIGT("basesaves/red/manip/pidgey14.gqs", intro, pidgeybackup, "PIDGEY", true);
    }

    public static List<DFState<RbyMap,RbyTile>> Search(int numThreads = 15, int numFrames = 60, int success = -1)
    {
        StartWatch();
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.PalHold);

        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];
        if(numThreads == 1) gb.Record("test");
        Elapsed("threads");

        IGTResults states = new IGTResults(numFrames);
        gb.LoadState("basesaves/red/manip/pidgey14.gqs");
        gb.HardReset();
        intro.ExecuteUntilIGT(gb);
        byte[] igtState = gb.SaveState();

        MultiThread.For(states.Length, gbs, (gb, f) =>
        {
            gb.LoadState(igtState);
            gb.CpuWrite("wPlayTimeMinutes", 8);
            gb.CpuWrite("wPlayTimeSeconds", (byte)(f / 60));
            gb.CpuWrite("wPlayTimeFrames", (byte)(f % 60));
            // gb.CpuWrite("wPlayTimeSeconds", (byte)(f % 60));
            // gb.CpuWrite("wPlayTimeFrames", (byte)((f & 1) != 0 ? 37 : 17));
            // gb.CpuWrite("wPlayTimeFrames", (byte)((f & 1) != 0 ? 59 : 36));
            intro.ExecuteAfterIGT(gb);
            // gb.Execute(SpacePath("UUUUUUUUUUUUU"+"UAUUUUAURU"+"UUUURR"+"RR"));//p13
            // gb.Execute(SpacePath("UUUUUUUUUUUUUU"+"URUAUUAUUU"+"UUAUUAR"+"RR"));//p14

            states[f]=new IGTState(gb, false, f);
        });
        Elapsed("states");

        RbyMap forest = gb.Maps[51];
        RbyMap entrance = gb.Maps[47];
        RbyMap route2 = gb.Maps[13];
        Action actions = Action.Right | Action.Down | Action.Up | Action.Left | Action.A | Action.StartB;
        RbyTile startTile = gb.Tile;
        RbyTile[] endTiles = { route2[8, 7] };
        Pathfinding.GenerateEdges<RbyMap,RbyTile>(gb, 0, endTiles.First(), actions);
        Pathfinding.GenerateEdges<RbyMap,RbyTile>(gb, 0, entrance[5, 1], actions);
        Pathfinding.GenerateEdges<RbyMap,RbyTile>(gb, 0, forest[1, 0], actions);
        forest[1, 1].GetEdge(0, Action.Up).NextTile = entrance[4, 7];
        entrance[4, 7].GetEdge(0, Action.Right).Cost = 0;
        entrance[5, 1].AddEdge(0, new Edge<RbyMap,RbyTile>() { Action = Action.Up, NextTile = route2[3, 11], NextEdgeset = 0, Cost = 0 });
        // Pathfinding.DebugDrawEdges(gb, route2, 0);

        var results = new List<DFState<RbyMap,RbyTile>>();
        var parameters = new DFParameters<Red,RbyMap,RbyTile>()
        {
            MaxCost = 4,
            SuccessSS = success > 0 ? success : Math.Max(1, states.Length - 5),
            EndTiles = endTiles,
            EncounterCallback = gb =>
            {
                return gb.EnemyMon.Species.Name == "PIDGEY" && gb.Yoloball();
            },
            FoundCallback = state =>
            {
                results.Add(state);
                Trace.WriteLine(startTile.PokeworldLink + "/" + state.Log + " Captured: " + state.IGT.TotalSuccesses + " Failed: " + (state.IGT.TotalFailures - state.IGT.TotalOverworld) + " NoEnc: " + state.IGT.TotalOverworld + " Cost: " + state.WastedFrames);
            }
        };

        DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states);
        Elapsed("search");

        return results;
    }
}
