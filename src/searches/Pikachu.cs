using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using static SearchCommon;
using static RbyIGTChecker<Red>;

class Pikachu
{
    const string State = "basesaves/red/manip/pikachu.gqs";

    public static void Check()
    {
        // string path = "RUS_BRRRUS_BUUUUAUUUUAU";//56
        string path = "RAUURRRUUUUUS_BS_BUAUUU";//58
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.Pal);
        CheckIGT(State, intro, path, "PIKACHU", 60, true, true);
    }

    public static void Search()
    {
        for(RbyStrat pal = RbyStrat.NoPal; pal <= RbyStrat.PalRel; ++pal)
            Pikachu.Search(new RbyIntroSequence(pal));
    }

    public static void Search(RbyIntroSequence intro, int numThreads = 16)
    {
        Trace.WriteLine(intro);
        StartWatch();

        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];
        if(numThreads == 1) gb.Record("test");

        gb.LoadState(State);
        IGTState state = gb.IGTCheck(intro, 1)[0];

        RbyMap forest = gb.Maps[51];
        forest.Sprites.Remove(25, 11);
        Action actions = Action.Right | Action.Up | Action.A | Action.StartB;
        RbyTile startTile = gb.Tile;
        RbyTile[] endTiles = { forest[25, 19] };
        RbyTile[] blockedTiles = {
            forest[26, 12],
            forest[16, 10], forest[18, 10],
            forest[16, 15], forest[18, 15],
            forest[11, 15], forest[12, 15],
            forest[11, 6], forest[12, 6],
            forest[6, 6], forest[8, 6],
            forest[6, 15], forest[8, 15],
            forest[2, 19], forest[1, 18]
        };
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, endTiles[0], actions, blockedTiles);
        forest[25, 12].RemoveEdge(0, Action.A);
        forest[25, 13].RemoveEdge(0, Action.A);

        var parameters = new SFParameters<Red, RbyMap, RbyTile>()
        {
            MaxCost = 300,
            EndTiles = endTiles,
            // TileCallback = (forest[25, 12], gb => gb.PickupItem()),
            EncounterCallback = gb => gb.EnemyMon.Species.Name == "PIKACHU" && gb.EnemyMon.Level == 5
                && gb.EnemyMon.DVs.Attack >= 10 && gb.EnemyMon.DVs.Defense >= 0 && gb.EnemyMon.DVs.Speed >= 10 && gb.EnemyMon.DVs.Special >= 14
                ,
            LogStart = startTile.PokeworldLink + "/",
            FoundCallback = (state, gb) =>
            {
                bool yoloball = gb.Yoloball();
                gb.LoadState(state.IGT.State);
                bool selectball = gb.Selectball();
                Trace.WriteLine(state.Log + " " + gb.EnemyMon.Species.Name + " L" + gb.EnemyMon.Level + " dvs: " + gb.EnemyMon.DVs + " cost: " + state.WastedFrames + " yb: " + yoloball + " sb: " + selectball);
            }
        };

        SingleFrameSearch.StartSearch(gbs, parameters, startTile, 0, state);
        Elapsed("search");
    }
}
