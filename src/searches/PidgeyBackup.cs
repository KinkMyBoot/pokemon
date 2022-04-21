using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;

using static SearchCommon;
using static RbyIGTChecker<Red>;

class PidgeyBackup
{
    const string Pidgey13 = "basesaves/red/manip/pidgey13.gqs";
    const string Pidgey14 = "basesaves/red/manip/pidgey14.gqs";

    public static void Check()
    {
        // 13
        // string pidgeybackup = "UUAUUUUUUAUUUUU" + "UUUAUUURU" + "UUUUARRRRR";
        // string pidgeybackup = "UUAUUUUUUAUUUUU" + "UUUAUUURU" + "UUUURARRRUUUUUR";
        // string pidgeybackup = "UUAUUUUUUUUUUU" + "UURUUUUU" + "UAUUURRRR";
        // string pidgeybackup = "UUAUUUUUUUUUUU" + "UURUUUUU" + "UUUURRRRU";
        string pidgeybackup = "UUUUUUUUUUUUU" + "UAUUUUAURU" + "UUUURR" + "RRRRSLLUUUUU"; // 3594/3600
        // string pidgeybackup = "UUUUUUUUUUUUU" + "URUAUUAUUU" + "UUUURR";
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.PalHold);
        CheckIGT(Pidgey13, intro, pidgeybackup, "PIDGEY", 3600);
        // 14
        // string pidgeybackup = "UUUUUUUUUUUUUU" + "URUAUUAUUU" + "UUAUUAR" + "RRDRRUUUUUURUUUDDDLLLLRR"; // 3595/3600
        // RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.PalHold);
        // CheckIGT(Pidgey14, intro, pidgeybackup, "PIDGEY", true);
    }

    public static void Search(int numThreads = 16, int numFrames = 60, int success = 55)
    {
        StartWatch();
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.PalHold);

        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];
        if(numThreads == 1) gb.Record("test");

        gb.LoadState(Pidgey14);
        IGTResults states = Red.IGTCheckParallel(gbs, intro, numFrames);
        // gb.Execute(SpacePath("UUUUUUUUUUUUU" + "UAUUUUAURU" + "UUUURR" + "RR")); // p13
        // gb.Execute(SpacePath("UUUUUUUUUUUUUU" + "URUAUUAUUU" + "UUAUUAR" + "RR")); // p14

        RbyMap forest = gb.Maps[51];
        RbyMap entrance = gb.Maps[47];
        RbyMap route2 = gb.Maps[13];
        Action actions = Action.Right | Action.Down | Action.Up | Action.Left | Action.A | Action.StartB;
        RbyTile startTile = gb.Tile;
        RbyTile[] endTiles = { route2[8, 7] };
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, endTiles[0], actions);
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, entrance[5, 1], actions);
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, forest[1, 0], actions);
        forest[1, 1].GetEdge(0, Action.Up).NextTile = entrance[4, 7];
        entrance[4, 7].GetEdge(0, Action.Right).Cost = 0;
        entrance[5, 1].AddEdge(0, new Edge<RbyMap, RbyTile>() { Action = Action.Up, NextTile = route2[3, 11], NextEdgeset = 0, Cost = 0 });
        // Pathfinding.DebugDrawEdges(gb, route2, 0);

        var parameters = new DFParameters<Red, RbyMap, RbyTile>()
        {
            MaxCost = 4,
            SuccessSS = success,
            EndTiles = endTiles,
            EncounterCallback = gb => gb.EnemyMon.Species.Name == "PIDGEY" && gb.Yoloball(),
            LogStart = "https://gunnermaniac.com/pokeworld?local=13#2/32/",
            FoundCallback = state =>
            {
                Trace.WriteLine(state.Log + " Captured: " + state.IGT.TotalSuccesses + " Failed: " + (state.IGT.TotalFailures - state.IGT.TotalRunning) + " NoEnc: " + state.IGT.TotalRunning + " Cost: " + state.WastedFrames);
            }
        };

        DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states);
        Elapsed("search");
    }
}
