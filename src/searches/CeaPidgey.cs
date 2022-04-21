using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using static SearchCommon;

class CeaPidgey
{
    static RbyIntroSequence Intro = new RbyIntroSequence(RbyStrat.NoPalAB);
    const string PidgeyPath = "UUAURAURRURAU";
    const string State = "basesaves/blue/manip/pidgey.gqs";

    public static void Check()
    {
        // Check("UUUUUULLLLLUUUUUAURUUUUUURRURURRRRRUUUUUUUUAUUUUUUUUUUUUUUUUUUUUUUUULLLLLLLLDDDDDDDLLLLUUUUUUUUUUUUUALLLLLLDDDDDDDDDDDDDDDDDLDDLLLLLUUU"); // 53 4 3156 U,dl

        // Check("UUUUULLAULALLUUUUUUURAUUUUURRRRRRRURUUUUUUUAUUUUUUUUUUUUUUUUUUUUUAUUUUAULLLLLLLLDDDDDDDLLLLUUUUUUUUUUUUULLLLLLDDDDDDDDDDDDDDDLDDDDLLLLUULU"); // 54 2 3220 D,l
        // Check("UUUAUULALLLLUUUUAURUUUAUUUUURRRRURRRRUUUUUAUUUUUUUUUUUUUUUUUUUUAUUUUUUUULLLLLLLLDDDDDDDLLLLUUUUUUUUUUUUULLLLLLDDDDDDDDDDDLDDDDDDDDLLLLULUU"); // 54 2 3210 DU,d

        Check("UUUAUULALLLLUUUUURAUUAUUUUUURRRRRRURRUUUUUUUUUUUUUUUUUUUUUAUUUAUUUUUUUUULLLLLLLLDDDDDDDLLLLUUUUUUUUUUUUULLLLLLDDDDDDDDDDDDDDDDLDDDLLLLLUUU"); // 54 1 3234 DU,rl

        // Check("UUUULLLLLUUAURUUAUUUAUUUUAUURRRRURRRRUUUUUUUUUUUUUUUUAUUUUUUUUUUAUUUUUUULLLLLLLLDDADDDDDLLLLUUUUUUUUUUUUULLLLLLDDDDDDDDDDDDDDDDDDDLLLLLLUUAU"); // 54 3 3220 DD,r
        // Check("UUUULLLLLUUAURUUUAUUAUUUUAUURRRRURRURRUUUUUUUUUUUUUUUUAUUUUUUUUUUAUUUUUULLLLLLLLDDDDDDDLLLLUUUUUUUUUUUUULLLLLLDDDDADDDDDDDDDDDDDDDLLLLLLUUAU"); // 54 3 3218 DD,r
        // Check("UUUULLLLLUUAURUUUAUUAUUUUAUURRRRURRURRUUUUUUUUUUUUUUUUAUUUUUUUUUUAUUUUUULLLLLLLLDDDDDDDLLLLUUUUUUUUUUUUULLLLLLDADDDDDDDDDDDDDDDDDDLLLLLLUUAU"); // 54 3 3218 DD,r
    }

    public static int Check(string path, bool verbose = true)
    {
        int numThreads = 16;
        int numFrames = 60;

        BlueCb[] gbs = MultiThread.MakeThreads<BlueCb>(numThreads);
        BlueCb gb = gbs[0];
        if(numThreads == 1) gb.Record("test");

        gb.LoadState(State);

        int success = 0;
        int f = 0;

        bool CheckFrame(BlueCb gb)
        {
            bool ret = false;
            if(gb.Execute(RbyIGTChecker<BlueCb>.SpacePath(PidgeyPath)) == gb.WildEncounterAddress && gb.EnemyMon.Species.Name == "PIDGEY")
            {
                gb.Yoloball(0, Joypad.B);
                gb.ClearText(Joypad.A);
                gb.Press(Joypad.B);
                var npcTracker = new NpcTracker<BlueCb>(gb.CallbackHandler);
                int addr = gb.Execute(RbyIGTChecker<BlueCb>.SpacePath(path), (gb.Maps[51][25, 12], gb.PickupItem));
                string info = f + " " + npcTracker.GetMovement((50, 2), (51, 1), (51, 8));
                if(addr == gb.WildEncounterAddress && gb.EnemyMon.Species.Name == "CATERPIE" && gb.Yoloball())
                {
                    info += " success";
                    System.Threading.Interlocked.Increment(ref success);
                    ret = true;
                }
                else if(addr != gb.WildEncounterAddress) info += " no encounter";
                else if(gb.EnemyMon.Species.Name != "CATERPIE") info += " L" + gb.EnemyMon.Level + " " + gb.EnemyMon.Species.Name;
                else info += " yoloball fail";
                if(verbose) Trace.WriteLine(info + " " + gb.Tile);
            }
            f++;
            return ret;
        }

        if(verbose)
            gb.IGTCheck(Intro, numFrames, () => CheckFrame(gb));
        else
            BlueCb.IGTCheckParallel(gbs, Intro, numFrames, CheckFrame);
        Trace.WriteLine(success + "/" + numFrames);
        return success;
    }

    public static void CheckFile()
    {
        Paths paths = new Paths();
        foreach(string line in System.IO.File.ReadAllLines("paths.txt"))
        {
            string path = Regex.Match(line, @"/([LRUDSA_B]+)").Groups[1].Value;
            Trace.WriteLine(path);
            int success = Check(path, false);
            paths.Add(new Path(path, success));
        }
        paths.PrintAll("https://gunnermaniac.com/pokeworld?local=51#21/61/");
    }

    public static void Search(int numThreads = 10, int numFrames = 10)
    {
        StartWatch();

        Blue[] gbs = MultiThread.MakeThreads<Blue>(numThreads);
        Blue gb = gbs[0];
        if(numThreads == 1) gb.Record("test");

        gb.LoadState(State);
        // int[] framesToSearch = {1, 2, 45, 46, 49, 50, 51, 52, 55, 23};
        bool NoName(Blue gb)
        {
            gb.ClearText(Joypad.A);
            gb.Press(Joypad.B);
            return true;
        }
        IGTResults states = Blue.IGTCheckParallel(gbs, Intro, numFrames, gb =>
        {
            return gb.Execute(RbyIGTChecker<Blue>.SpacePath(PidgeyPath)) == gb.WildEncounterAddress && gb.Yoloball(0, Joypad.B) && NoName(gb);
            // && gb.Execute(RbyIGTChecker<Blue>.SpacePath("UUUUUULLLLLUUUUUAURUUUUUURRURURRRRRUUUUUUU")) == gb.OverworldLoopAddress;
        });

        RbyMap route2 = gb.Maps[13];
        RbyMap gate = gb.Maps[50];
        RbyMap forest = gb.Maps[51];
        forest.Sprites.Remove(25, 11);
        Action actions = Action.Right | Action.Down | Action.Up | Action.Left | Action.A | Action.StartB;
        RbyTile startTile = gb.Tile;
        RbyTile[] endTiles = { forest[1, 19] };
        RbyTile[] blockedTiles = {
            forest[26, 12],
            forest[16, 10], forest[18, 10],
            forest[16, 15], forest[18, 15],
            forest[11, 15], forest[12, 15],
            forest[11, 4], forest[12, 4],
            forest[6, 4], forest[8, 4],
            forest[6, 13], forest[8, 13],
            // forest[6, 15], forest[8, 15],
            forest[2, 19], forest[1, 18]
        };
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, endTiles[0], actions, blockedTiles);
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, gate[5, 1], actions, blockedTiles);
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, route2[3, 44], actions, blockedTiles);
        route2[3, 44].AddEdge(0, new Edge<RbyMap, RbyTile>() { Action = Action.Up, NextTile = gate[4, 7], NextEdgeset = 0, Cost = 0 });
        gate[4, 7].GetEdge(0, Action.Right).Cost = 0;
        gate[5, 1].AddEdge(0, new Edge<RbyMap, RbyTile>() { Action = Action.Up, NextTile = forest[17, 47], NextEdgeset = 0, Cost = 0 });
        forest[1, 19].RemoveEdge(0, Action.A);
        forest[25, 12].RemoveEdge(0, Action.A);
        forest[25, 13].RemoveEdge(0, Action.A);

        var parameters = new DFParameters<Blue,RbyMap,RbyTile>()
        {
            MaxCost = 16,
            SuccessSS = 8,
            // RNGSS = 52,
            EndTiles = endTiles,
            TileCallbacks = new (Tile<RbyMap, RbyTile>, Action<Blue>)[] { (forest[25, 12], gb => gb.PickupItem()) },
            EncounterCallback = gb => gb.Tile == endTiles[0] && gb.EnemyMon.Species.Name == "CATERPIE" && gb.Yoloball(),
            LogStart = "https://gunnermaniac.com/pokeworld?local=51#21/61/",
            FoundCallback = (state) =>
            {
                Trace.WriteLine(state.Log + " Captured: " + state.IGT.TotalSuccesses + " Failed: " + (state.IGT.TotalFailures - state.IGT.TotalRunning) + " NoEnc: " + state.IGT.TotalRunning + " Cost: " + state.WastedFrames);
            }
        };

        DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states, 0);
        Elapsed("search");
    }
}
