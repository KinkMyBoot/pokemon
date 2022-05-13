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
        RbyStrat pal;
        string state, path;

        state = Pidgey13;
        pal = RbyStrat.NoPal; path = "UUAUUAUUUUUUUUURUAUUUUUUUUAUURARRR" + "RUUUS_BUUL"; // lll 3591
        // pal = RbyStrat.NoPal; path = "UAUUAUUUUUUUUUURUAUUUUUUUAUUUARRRR" + "UUUS_BS_BS_BAU"; // eee 3593
        // pal = RbyStrat.NoPal; path = "UAUUAUUUUUUUUUURUAUUUUUUUAUUURARRR" + "UURUS_BUUL"; // eel 3593
        // pal = RbyStrat.NoPalAB; path = "UUUUUAUUUUUUUAURUUUAUUAUUUAUUURRRR" + "RRUUUUUS_BLLLLRR"; // 3592
        // pal = RbyStrat.PalHold; path = "UUAUUAUUUUAUUUAUUUUUUUURUUAUUURR" + "RRRRLLUUUS_BS_BAUU"; // 3592
        // pal = RbyStrat.PalHold; path = "UUUUUUUUUUUUUUAUUUUAURUUUUURR" + "RRRRSLLUUUUU"; // bad tile 3594

        // state = Pidgey14;
        // pal = RbyStrat.PalHold; path = "UUUUUUUUUUUUUUURUAUUAUUUUUAUUAR" + "RRDRRUUUUUURUUUDDDLLLLRR"; // bad tile 3595

        CheckIGT(state, new RbyIntroSequence(pal), path, "PIDGEY", 3600);
    }

    public static void Search()
    {
        for(RbyStrat pal = RbyStrat.NoPal; pal <= RbyStrat.PalHold; ++pal)
            Search(new RbyIntroSequence(pal), 16, 60, 57, 12);
    }

    public static void Search(RbyIntroSequence intro, int numThreads = 16, int numFrames = 60, int success = 55, int cost = 8)
    {
        StartWatch();
        Trace.WriteLine(intro);

        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];
        if(numThreads == 1) gb.Record("test");

        gb.LoadState(Pidgey13);
        IGTResults states = Red.IGTCheckParallel(gbs, intro, numFrames);
        // IGTResults states = Red.IGTCheckParallel(gbs, intro, numFrames, gb => gb.Execute(SpacePath("UAUUAUUUUUUUUUURUAUUUUUUUAUUURARRR")) == gb.OverworldLoopAddress).Purge();

        RbyMap forest = gb.Maps[51];
        RbyMap entrance = gb.Maps[47];
        RbyMap route2 = gb.Maps[13];
        Action actions = Action.Right | Action.Down | Action.Up | Action.Left | Action.A | Action.StartB;
        RbyTile startTile = gb.Tile;
        RbyTile[] endTiles = { route2[8, 7] };
        // RbyTile[] endTiles = { route2[8, 2] };
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, endTiles[0], actions);
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, entrance[5, 1], actions);
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, forest[1, 0], actions);
        forest[1, 1].GetEdge(0, Action.Up).NextTile = entrance[4, 7];
        entrance[4, 7].GetEdge(0, Action.Right).Cost = 0;
        entrance[5, 1].AddEdge(0, new Edge<RbyMap, RbyTile>() { Action = Action.Up, NextTile = route2[3, 11], NextEdgeset = 0, Cost = 0 });

        var parameters = new DFParameters<Red, RbyMap, RbyTile>()
        {
            MaxCost = cost,
            SuccessSS = success,
            // EndTiles = endTiles,
            EncounterCallback = gb => gb.EnemyMon.Species.Name == "PIDGEY" && gb.Yoloball() && gb.Tile.X >= 5,
            LogStart = "https://gunnermaniac.com/pokeworld?local=13#2/31/",
            FoundCallback = state =>
            {
                string failures = "";
                foreach(var i in state.IGT.IGTs)
                {
                    if(!i.Running && !i.Success)
                    {
                        gb.LoadState(i.State);
                        if(gb.EnemyMon.Species.Name != "PIDGEY") failures += " " + gb.EnemyMon.Species.Name;
                        else if(!gb.Yoloball()) failures += " yoloball";
                        else if(gb.Tile.X < 5) failures += " x=" + gb.Tile.X;
                    }
                }
                Trace.WriteLine(state.Log + " Captured: " + state.IGT.TotalSuccesses + " Failed: " + (state.IGT.TotalFailures - state.IGT.TotalRunning) + " NoEnc: " + state.IGT.TotalRunning + " Cost: " + state.WastedFrames + failures);
            }
        };

        DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states);
        Elapsed("search");
    }
}
