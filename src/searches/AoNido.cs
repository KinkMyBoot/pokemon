using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;

using static SearchCommon;
using static RbyIGTChecker<Ao>;

class AoNido
{
    const string State = "basesaves/ao/nido718.gqs";
    const string Nido = "UDDDAULALLAUULALLLLLALLADDADLALLAUUAUDD";
    const string BasePath = "DRRUUURRRRRRRRRRRRRRRRRRRRRUR";
    const string BasePathToGirl = BasePath + "UUUUUUR";
    const string BasePathToSignL = BasePathToGirl + "UUUULUUUUUU";
    const string BasePathToSignR = BasePathToGirl + "UUUUUUUUUUL";
    static string[] PathToSign = { BasePathToSignL, BasePathToSignL, BasePathToSignL, BasePathToSignR, BasePathToSignR,
                                   BasePathToSignR, BasePathToSignL, BasePathToSignR, BasePathToSignL, BasePathToSignL };
    const string PathToGrass = "UUUUUUUUUUUUULLLUUUU";
    static string[] ForestPaths = { "", "",
        "UUURRRRUUUUUUUULLLLLUUUUUUURUUUUURRRRRRRRUUUUUUAUUUAUUUUUUUUUUUUUUUUAUUAUUUUUUULLLLLLLLLDDDDDADDLLLUUUUUUUUUUUUULLLLLLLDDDDDDDDDDDDDDDADDDDLLLLUUU",
        "UUURRRRUUUUUUUULLLLLUUUUUUURUUAUUURRRRRRRRUUUUUUUAUUUUUAUUUUUUUUUUAUUAUUUUUAUUUUULLLLLLLLLDDDDDDDLLLAUUUUUUUUUUUUULLLLLLLDDDDDDDADDDDDDDDDDDDLLLLUUU",
        "UUURRRRUUAUUUUUULLLLLUUUUUUURUUUUURRRRRRRAURUUAUUUUUUUUUUUUUUUUUAUUUUUUUUUAUUUUULLLLLLLLLADDDDDDDLLLUUUAUUUUUUUUUULLLLLLLDDDDDDDDDDDDDDDADDDDLLLLUUU",
        "UUURRRRUUUAUUUUULLLLLUUUUUUURUUUUURRRRRRRRUUUUUAUUUUAUUUUUUUUUUUUAUUUUUUUUUUUUUALLALLLLLLLDDDDDDDLLLUUUUUUUUUUUUULLLLLLLDDDDDDDADDDDDDDDDDDDLLLLUUU",
        "UUURRRRUUUUUUUULLLLLUUUUUUURUUUUURRRRRARRRUAUUUUUUUUUUUUAUUUAUUUUUUAUUAUUUUUUUUUULLLLLLALLLDDDDADDADLLLUUUUUUUUUUUUULLLLLLLDDDDDADDDDDDDDDDDDDDLLLLUUU",
        "UUURRRRUUUUUUUULLLLLUUUUUUURUUUUURRRRRRRRUUUUUAUUAUUUUUUUAUUAUUUUUUUUUUUAUUUUUUULLLLLLLLLDDDDADDADLLLAUUUUUUUUUUUUULLLLLLLDDDDADDDDDDDDDDDDDDDLLLLUUU",
        "UUURRRRAUUUUUUUULLLLLUUUUUUURUUUUURRRURUARRRRUAUUUUAUUUUAUUUUUAUUUUUUUUUUUUUUUUUULLLLLLLLALDDDDDDDLLLUUUUUUUUUUUUULLLLLLLDDDDDDDDDDDDDDDDDDDLLLLUUU",
        "UUURRRRUUUUUUUULLLLLUUUUUUURUUUUURRRRRRRRUUUAUUUUUUUAUUAUUUUUUUUUUUUUAUUAUUUUUUULLLLLLLALLDDDDADDDLLLUAUUUUAUUUUUUUULLLLLLLDDDDDDDDDDDDDDDDDDDLLLLUUU",
    };
    static int[][] IgnoreFrame = {
        new int[] {1, 16, 17},
        new int[] {1, 15, 16},
        new int[] {1, 14, 15},
        new int[] {1, 13, 14},
        new int[] {1, 12},
        new int[] {1, 10, 11, 12, 13},
        new int[] {1, 10, 11, 33},
        new int[] {1, 9, 10},
        new int[] {1, 8, 9},
        new int[] {1, 7, 8}
    };

    public static void Check()
    {
        string path;
        RbyStrat pal;
        // 717
        // path = "LLLLLALLLALLLLDADDADLLUUAUUUDD"; pal = RbyStrat.NoPal;
        // path = "DDUAULLLALLAULALLLLLLADDADDLALLUAR"; pal = RbyStrat.NoPal;
        // path = "LDDLUAULALLLAULALDAULALDALLDDADLLU"; pal = RbyStrat.NoPalAB;
        // path = "LLADLDUUAULLALLALLAULALLADDDADDLLU"; pal = RbyStrat.Pal;
        // path = "LLALDDAUULLLAULALDLALLLADDADLLUUAD"; pal = RbyStrat.PalAB;
        // path = "LLDDALUAULLAULLALLAULALLADDADDDLLU"; pal = RbyStrat.PalRel;
        // 617
        // path = "LARDADDUALLUULLLLALLALLLDDDDALLAUUU"; pal = RbyStrat.NoPal; // 54 + 1
        // 718
        // path = "LRULLLLULLDLLLLS_BLLS_BDDDLLUU"; pal = RbyStrat.NoPal; // 56 + 1
        // path = "LRLLLULULLDLLLLS_BLLS_BDDDLLUU"; pal = RbyStrat.NoPal;
        // path = "LRDDAULALLAUULALLLLLALLADDADLALLAUUAUDD"; pal = RbyStrat.NoPal; // 57 + 1
        path = "UDDDAULALLAUULALLLLLALLADDADLALLAUUAUDD"; pal = RbyStrat.NoPal; // 57 + 1
        // path = "LADUDALUDALUULALLLLALS_BLALDADDALLLALU"; pal = RbyStrat.NoPal; // 56 + 1

        CheckIGT(new CheckIGTParameters() {
            StatePath = State, Intro = new RbyIntroSequence(pal), Path = path, CheckDV = true, NumFrames = 3600
        });
    }

    public static void CheckForest(int path, int numThreads = 32, int numFrames = 60)
    {
        StartWatch();
        Ao[] gbs = MultiThread.MakeThreads<Ao>(numThreads);
        gbs[0].LoadState(State);
        Trace.WriteLine(Ao.IGTCheckParallel(gbs, new RbyIntroSequence(), numFrames, gb => {
            return gb.Execute(SpacePath(Nido)) == gb.WildEncounterAddress && gb.Yoloball(0, Joypad.B) && Name(gb, path)
            && gb.Execute(SpacePath(PathToSign[path] + PathToGrass + ForestPaths[path]), (gb.Maps[51][25, 12], gb.PickupItem)) == gb.OverworldLoopAddress;
            // && gb.Execute(SpacePath(PathToSign[path] + PathToGrass + ForestPaths[path].Substring(0, ForestPaths[path].Length - 1) + "SLUU"), (gb.Maps[51][25, 12], gb.PickupItem), (gb.Maps[51][1, 19], gb.PickupItem)) == gb.OverworldLoopAddress;
        }).TotalSuccesses * 60.0f / numFrames);
        Elapsed();
    }

    public static void Search()
    {
        // Search(new RbyIntroSequence());
        SearchForest(9);
    }

    public static void Search(RbyIntroSequence intro, int numThreads = 10)
    {
        Trace.WriteLine(intro);
        StartWatch();

        Ao[] gbs = MultiThread.MakeThreads<Ao>(numThreads);
        Ao gb = gbs[0];
        if(numThreads == 1) gb.Record("test");

        gb.LoadState(State);
        IGTState state = gb.IGTCheck(intro, 1)[0];

        RbyMap viridian = gb.Maps[1];
        RbyMap route22 = gb.Maps[33];
        Action actions = Action.Right | Action.Down | Action.Up | Action.Left | Action.A | Action.StartB;
        RbyTile startTile = gb.Tile;
        RbyTile[] blocked = {
            viridian[8, 17], viridian[8, 18], viridian[8, 19], viridian[8, 20],
            route22[32, 14], route22[32, 15],
            route22[30, 10], route22[31, 10], route22[32, 10], route22[32, 9], route22[32, 8]
        };
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, route22[33, 11], actions, blocked);

        var parameters = new SFParameters<Ao, RbyMap, RbyTile>()
        {
            MaxCost = 210,//210 225 240
            EncounterCallback = gb => gb.EnemyMon.DVs.Attack == 15 && gb.EnemyMon.DVs.Defense >= 13 && gb.EnemyMon.DVs.Speed >= 14 && gb.EnemyMon.DVs.Special == 15 && gb.EnemyMon.Species.Name == "NIDORANM",
            LogStart = Link(startTile, ""),
            FoundCallback = (state, gb) =>
            {
                Trace.WriteLine(state.Log + " " + gb.EnemyMon.Species.Name + " L" + gb.EnemyMon.Level + " dvs: " + gb.EnemyMon.DVs + " cost: " + state.WastedFrames);
            }
        };

        SingleFrameSearch.StartSearch(gbs, parameters, startTile, 0, state);
        Elapsed("search");
    }

    public static void SearchForest(int path, int numThreads = 16, int numFrames = 60)
    {
        StartWatch();

        Ao[] gbs = MultiThread.MakeThreads<Ao>(numThreads);
        Ao gb = gbs[0];

        gb.LoadState(State);
        IGTResults states = Ao.IGTCheckParallel(gbs, new RbyIntroSequence(), numFrames, gb => {
            return gb.Execute(SpacePath(Nido)) == gb.WildEncounterAddress && gb.Yoloball(0, Joypad.B) && Name(gb, path);
        });
        foreach(int f in IgnoreFrame[path]) states[f].Success = false;
        states = states.Purge();

        string forced = "";
        // string forced = "UUURRRRAUUUUUUUULLLLLUUUUUUURUUUUU";

        states = Ao.IGTCheckParallel(gbs, states, gb => {
            // var npcTracker = new NpcTracker<Ao>(((Ao) gb).CallbackHandler);
            // gb.Execute(SpacePath(BasePathToGirl + "UUUU"));
            // if(npcTracker.GetMovement((1, 7)).StartsWith("R")) gb.Execute(SpacePath("LUUUUUU")); else gb.Execute(SpacePath("UUUUUUL"));
            gb.Execute(SpacePath(PathToSign[path] + PathToGrass + forced));
            // Trace.WriteLine(states2[k++].IGTFrame + " " + npcTracker.GetMovement((1, 1), (1, 7)));
            return false;
        });
        Elapsed();

        RbyMap route2 = gb.Maps[13];
        RbyMap gate = gb.Maps[50];
        RbyMap forest = gb.Maps[51];
        forest.Sprites.Remove(25, 11);
        Action actions = Action.Right | Action.Down | Action.Up | Action.Left | Action.A;
        RbyTile startTile = gb.Tile;
        RbyTile[] endTiles = { forest[2, 19] };
        RbyTile[] blockedTiles = {
            route2[5, 53], route2[4, 51], route2[5, 51], route2[6, 51], route2[7, 51],
            forest[26, 12],
            forest[17, 10], forest[18, 10],
            forest[11, 15], forest[12, 15],
            forest[11, 4], forest[12, 4],
            forest[7, 4], forest[8, 4],
        };
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, endTiles[0], actions, blockedTiles);
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, gate[5, 1], actions, blockedTiles);
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, route2[3, 44], actions, blockedTiles);
        route2[3, 44].AddEdge(0, new Edge<RbyMap, RbyTile>() { Action = Action.Up, NextTile = gate[4, 7], NextEdgeset = 0, Cost = 0 });
        gate[4, 7].GetEdge(0, Action.Right).Cost = 0;
        gate[5, 1].AddEdge(0, new Edge<RbyMap, RbyTile>() { Action = Action.Up, NextTile = forest[17, 47], NextEdgeset = 0, Cost = 0 });
        forest[25, 12].RemoveEdge(0, Action.A);
        forest[25, 13].RemoveEdge(0, Action.A);
        for(int y = 22; y <= 26; ++y) forest[25, y].RemoveEdge(0, Action.A);
        for(int y = 8; y <= 10; ++y) forest[13, y].RemoveEdge(0, Action.A);
        for(int y = 11; y <= 17; ++y) forest[6, y].RemoveEdge(0, Action.A);

        var parameters = new DFParameters<Ao, RbyMap, RbyTile>()
        {
            MaxCost = 20,
            SuccessSS = states.Length,
            EndTiles = endTiles,
            TileCallbacks = new (Tile<RbyMap, RbyTile>, Action<Ao>)[] { (forest[25, 12], gb => gb.PickupItem()) },
            LogStart = "https://gunnermaniac.com/pokeworld?local=51#17/66/" + forced,
        };
        parameters.FoundCallback = state =>
        {
            Trace.WriteLine(state.Log + " " + state.IGT.TotalRunning + "/" + states.Length + " c:" + state.WastedFrames + " t:" + TurnCount(state.Log));
            if(state.WastedFrames + 2 < parameters.MaxCost) parameters.MaxCost = state.WastedFrames + 2;
        };

        DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states, 0);
        Elapsed("search");
    }

    public static void Record(int path)
    {
        Ao gb = new Ao();
        gb.LoadState(State);
        gb.HardReset();
        gb.Record("test");
        var intro = new RbyIntroSequence();
        intro.ExecuteUntilIGT(gb);
        // gb.CpuWrite("wPlayTimeSeconds", 0);
        // gb.CpuWrite("wPlayTimeFrames", 49);
        intro.ExecuteAfterIGT(gb);
        gb.Execute(SpacePath(Nido));
        gb.Yoloball(0, Joypad.B);
        Name(gb, path);
        gb.Execute(SpacePath(PathToSign[path] + PathToGrass + ForestPaths[path]), (gb.Maps[51][25, 12], gb.PickupItem));
        gb.AdvanceFrames(500);
        gb.Dispose();
    }

    static bool Name(Rby gb, int path)
    {
        gb.ClearText(Joypad.B);
        gb.Press(Joypad.A);
        gb.RunUntil("_Joypad");
        gb.AdvanceFrames(1 + path);
        gb.Press(Joypad.A, Joypad.Start);
        return true;
    }

    public static bool CustomBall(Ao gb)
    {
        gb.ClearText(Joypad.B);
        gb.Press(Joypad.Right);
        gb.AdvanceFrames(0);
        gb.Press(Joypad.A);
        int ret = gb.Hold(Joypad.Up | Joypad.A, "ItemUseBall.captured", "ItemUseBall.failedToCapture");
        Trace.WriteLine(gb.CpuRead("hRandomAdd") + " " + gb.CpuRead("hRandomSub"));
        return ret == gb.SYM["ItemUseBall.captured"];
    }
}
