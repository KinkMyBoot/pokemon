using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using static SearchCommon;

class CeaPidgey
{
    static RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.NoPalAB);
    static string pidgeypath = "UUAURAURRURAU";

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

        IGTResults states = new IGTResults(numFrames);
        gb.LoadState("basesaves/blue/manip/pidgey.gqs");
        gb.HardReset();
        intro.ExecuteUntilIGT(gb);
        byte[] igtState = gb.SaveState();

        int success = 0;

        void CheckFrame(BlueCb gb, int f, ref int success, bool verbose)
        {
            gb.LoadState(igtState);
            gb.CpuWrite("wPlayTimeSeconds", (byte) (f / 60));
            gb.CpuWrite("wPlayTimeFrames", (byte) (f % 60));
            intro.ExecuteAfterIGT(gb);
            int address = gb.Execute(RbyIGTChecker<BlueCb>.SpacePath(pidgeypath));
            if(address != gb.SYM["CalcStats"]) return;
            if(gb.EnemyMon.Species.Name != "PIDGEY") return;
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
            }
            else if(addr != gb.WildEncounterAddress) info += " no encounter";
            else if(gb.EnemyMon.Species.Name != "CATERPIE") info += " L" + gb.EnemyMon.Level + " " + gb.EnemyMon.Species.Name;
            else info += " yoloball fail";
            if(verbose) Trace.WriteLine(info + " " + gb.Tile);
        }

        if(verbose)
            for(int f = 0; f < states.Length; ++f) CheckFrame(gb, f, ref success, true);
        else
            MultiThread.For(states.Length, gbs, (gb, f) => CheckFrame(gb, f, ref success, false));
        Trace.WriteLine(success + "/" + numFrames);
        return success;
    }

    public static void CheckFile()
    {
        string[] lines = File.ReadAllLines("paths.txt");
        List<Display> display = new List<Display>();
        foreach(string line in lines)
        {
            string path = Regex.Match(line, @"/([LRUDSA_B]+)").Groups[1].Value;
            Trace.WriteLine(path);
            int success = Check(path, false);
            display.Add(new Display(path, success));
        }
        Display.PrintAll(display, "https://gunnermaniac.com/pokeworld?local=51#21/61/");
    }

    public static void Search(int numThreads = 10, int numFrames = 10)
    {
        StartWatch();

        Blue[] gbs = MultiThread.MakeThreads<Blue>(numThreads);
        Blue gb = gbs[0];
        if(numThreads == 1) gb.Record("test");
        Elapsed("threads");

        IGTResults states = new IGTResults(numFrames);
        gb.LoadState("basesaves/blue/manip/pidgey.gqs");
        gb.HardReset();
        intro.ExecuteUntilIGT(gb);
        byte[] igtState = gb.SaveState();

        // int[] framesToSearch = {1, 2, 45, 46, 49, 50, 51, 52, 55, 23};

        MultiThread.For(states.Length, gbs, (gb, it) =>
        {
            int f = it;
            if(f >= 36) f++;
            if(f >= 37) f++;
            if(f >= 47) f++;

            gb.LoadState(igtState);
            gb.CpuWrite("wPlayTimeSeconds", (byte) (f / 60));
            gb.CpuWrite("wPlayTimeFrames", (byte) (f % 60));
            intro.ExecuteAfterIGT(gb);
            gb.Execute(RbyIGTChecker<Blue>.SpacePath(pidgeypath));
            gb.Yoloball(0, Joypad.B);
            gb.ClearText(Joypad.A);
            gb.Press(Joypad.B);
            gb.Execute(RbyIGTChecker<Blue>.SpacePath("UUUUUULLLLLUUUUUAURUUUUUURRURURRRRRUUUUUUU"));

            states[it] = new IGTState(gb, false, f);
        });
        Elapsed("states");

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
            TileCallback = (forest[25, 12], gb =>
                gb.PickupItem()
            ),
            EncounterCallback = gb =>
            {
                return gb.Tile == endTiles[0] && gb.EnemyMon.Species.Name == "CATERPIE" && gb.Yoloball();
            },
            FoundCallback = (state) =>
            {
                Trace.WriteLine("https://gunnermaniac.com/pokeworld?local=51#21/61/" + state.Log + " Captured: " + state.IGT.TotalSuccesses + " Failed: " + (state.IGT.TotalFailures - state.IGT.TotalOverworld) + " NoEnc: " + state.IGT.TotalOverworld + " Cost: " + state.WastedFrames);
            }
        };

        DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states, 0);
        Elapsed("search");
    }
}
