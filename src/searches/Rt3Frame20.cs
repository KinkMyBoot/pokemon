using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;

using static SearchCommon;
using static RbyIGTChecker<Red>;

class Rt3Frame20
{
    const string State0 = "basesaves/red/manip/rt3f20t0.gqs";
    const string State1 = "basesaves/red/manip/rt3f20t1.gqs";
    const string State2 = "basesaves/red/manip/rt3f20t2.gqs";

    const string CurrentPath = "LLLLDDRRRUUULAURDDDDLLAL"
    + "RARRARRARRRUU"
    + "DDDDDDLLLLLLUUUUUUUUUUUUUUULLLLLLLLDDDRR"
    + "DADDDDDDDDDDDRRRRRRRRRRRRRRRR"
    + "RUUURRRRRDDRRRRRRUURARRDDDDDDDDLLLLDDDDDDDDDLLLLLLLLLLLLLLLLL"
    ;

    public static void Check()
    {
        RbyStrat pal;
        string state, path;

        state = State0;
        // pal = RbyStrat.Pal; path = CurrentPath + "LLLLLLRUUUUUUUUUAUUAUULUR";
        // pal = RbyStrat.Pal; path = CurrentPath + "LLLLLUUUUULAURUAUUAUUAUUU";
        // pal = RbyStrat.Pal; path = CurrentPath + "ALLLLLUUUUAUUAUUUALURUUUU";
        // pal = RbyStrat.Pal; path = CurrentPath + "LLLLUUUUUALLAUUAUUAURUUUU";
        // pal = RbyStrat.Pal; path = CurrentPath + "LLLLLAUUUUUUUUUUUUAUUUDUD";
        pal = RbyStrat.Pal; path = CurrentPath + "ALLLLLUUUURUULUAUUAUUAUUU";

        CheckIGT(state, new RbyIntroSequence(pal), path, "PARAS", 60);
    }

    public static void Search()
    {
        Search(new RbyIntroSequence(RbyStrat.Pal));
    }

    public static void Search(RbyIntroSequence intro, int numThreads = 16, int numFrames = 60)
    {
        StartWatch();
        Trace.WriteLine(intro);

        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];

        RbyMap moon1 = gb.Maps[59];
        RbyMap moon2 = gb.Maps[60];
        RbyMap moon3 = gb.Maps[61];

        gb.LoadState(State0);
        IGTResults states = Red.IGTCheckParallel(gbs, intro, numFrames, gb =>
            gb.Execute(SpacePath(CurrentPath),
                (moon1[ 5, 31], gb.PickupItem),
                (moon1[34, 31], gb.PickupItem),
                (moon1[35, 23], gb.PickupItem),
                (moon3[28,  5], gb.PickupItem),
                (moon1[ 2,  3], gb.PickupItem),
                (moon1[ 3,  2], gb.PickupItem)
            ) == gb.OverworldLoopAddress
        ).Purge();

        if(numThreads == 1) gb.Show();

        // for(int i = 0; i < states.Length; ++i) Trace.WriteLine(states[i].IGTFrame + " " + states[i].HRA + " " + states[i].HRS + " " + states[i].Divider + " " + states[i].Dsum);
        // Trace.WriteLine(states.TotalSuccesses + "/60 " + RNGSuccesses(states));
        // return;

        Action actions = Action.Right | Action.Down | Action.Up | Action.Left | Action.A;
        RbyTile startTile = gb.Tile;
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 3, moon3[28, 5], actions, moon1[35, 21], moon2[18, 10], moon2[19, 10], moon2[20, 10], moon2[21, 10], moon2[22, 10], moon2[23, 10], moon2[24, 10]);
        moon1[35, 23].RemoveEdge(3, Action.A);
        moon1[34, 10].RemoveEdge(3, Action.Left);
        moon1[34, 9].RemoveEdge(3, Action.Left);
        moon1[34, 8].RemoveEdge(3, Action.Left);
        moon1[26, 3].RemoveEdge(3, Action.Down);
        moon1[27, 3].RemoveEdge(3, Action.Down);
        moon1[28, 3].RemoveEdge(3, Action.Down);
        moon1[24, 3].RemoveEdge(3, Action.Left);
        moon3[28, 6].GetEdge(3, Action.Left).Cost = 0;
        moon3[28, 6].RemoveEdge(3, Action.Up);
        moon3[27, 6].RemoveEdge(3, Action.Right);
        moon3[27, 5].RemoveEdge(3, Action.A);
        moon3[27, 5].GetEdge(3, Action.Right).NextEdgeset = 4;
        moon1.Sprites.Remove(2, 2);
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 4, moon1[2, 2], actions, moon1[3, 3], moon2[18, 10], moon2[19, 10], moon2[20, 10], moon2[21, 10], moon2[22, 10], moon2[23, 10], moon2[24, 10]);
        moon3[28, 5].RemoveEdge(4, Action.A);
        for(int x = 10; x <= 11; ++x) for(int y = 4; y <= 10; ++y) { moon1[x, y].RemoveEdge(4, Action.Left); moon1[x, y].RemoveEdge(4, Action.A); }
        for(int x = 3; x <= 9; ++x) for(int y = 9; y <= 10; ++y) { moon1[x, y].RemoveEdge(4, Action.Left); }
        moon1[4, 2].RemoveEdge(4, Action.A);
        moon1[4, 2].GetEdge(4, Action.Left).NextEdgeset = 5;
        moon1[2, 4].RemoveEdge(4, Action.A);
        moon1[2, 4].GetEdge(4, Action.Up).NextEdgeset = 5;
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 5,  moon3[10, 17], actions, moon3[33, 23], moon3[34, 23], moon3[35, 23], moon3[36, 23]);
        moon1[3, 2].RemoveEdge(5, Action.A);
        moon1[2, 3].RemoveEdge(5, Action.A);

        Paths results = new Paths();
        var parameters = new DFParameters<Red, RbyMap, RbyTile>()
        {
            SuccessSS = 54,
            // RNGSS = 55,
            // RNGRange = 3,
            MaxCost = 80,
            // MaxTurns = 8,
            EncounterCallback = gb => gb.EnemyMon.Species.Name == "PARAS" && gb.Yoloball(),// && gb.Tile.X <= 11 && gb.Tile.Y <= 21,
            // EndTiles = new RbyTile[] { moon3[28, 5] }, EndEdgeSet = 4,
            // EndTiles = new RbyTile[] { moon2[18, 11] }, EndEdgeSet = 4,
            // EndTiles = new RbyTile[] { moon1[17, 12], moon1[16, 11] }, EndEdgeSet = 4,
            // EndTiles = new RbyTile[] { moon1[3, 3] }, EndEdgeSet = 5,
            // EndTiles = new RbyTile[] { moon2[5, 6], moon2[6, 5] }, EndEdgeSet = 5,
            // EndTiles = new RbyTile[] { moon3[15, 31] }, EndEdgeSet = 5,
            // EndTiles = new RbyTile[] { moon3[10, 17] }, EndEdgeSet = 5,
            TileCallbacks = new (Tile<RbyMap, RbyTile>, Action<Red>)[] {
                (moon1[5, 31], gb => gb.PickupItem()),
                (moon1[34, 31], gb => gb.PickupItem()),
                (moon1[35, 23], gb => gb.PickupItem()),
                (moon3[28, 5], gb => gb.PickupItem()),
                (moon1[2, 3], gb => gb.PickupItem()),
                (moon1[3, 2], gb => gb.PickupItem()),
            },
            LogStart = startTile.PokeworldLink + "/",
            FoundCallback = state =>
            {
                Path p = new Path(state.Log, state.IGT.TotalSuccesses, state.WastedFrames, RNGSuccesses(state.IGT));
                Trace.WriteLine(p);
                results.Add(p);
            }
        };

        DepthFirstSearch.StartSearch(gbs, parameters, startTile, 5, states);
        Elapsed("search");
    }

    static string RNGSuccesses(IGTResults igt)
    {
        return "[" + igt.RDivSuccesses() + "] " + igt.RNGSuccesses(0) + "-" + igt.RNGSuccesses(1) + "-" + igt.RNGSuccesses(2) + "--" + igt.RNGSuccesses(3) + "-" + igt.RNGSuccesses(4) + "-" + igt.RNGSuccesses(5);
    }
}
