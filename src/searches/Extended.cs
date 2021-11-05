using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;

using static SearchCommon;
using static RbyIGTChecker<Red>;

class Extended
{
    static bool CheckEncounter(int ret, Red gb, string pokename, IGTResult res)
    {
        if (ret != gb.SYM["CalcStats"])
            return false;

        res.Mon = gb.EnemyMon;
        if (res.Mon.Species.Name != pokename)
            return false;

        res.Yoloball = gb.Yoloball();
        return res.Yoloball;
    }

    static void BuildStates()
    {
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.NoPal);
        Red gb = new Red();

        gb.LoadState("basesaves/red/manip/nido.gqs");
        gb.HardReset();
        intro.ExecuteUntilIGT(gb);
        byte[] igtState = gb.SaveState();

        const int maxframe = 60;
        for (int f = 0; f < maxframe; ++f)
        {
            if (IgnoredFrames.Contains(f))
                continue;

            gb.LoadState(igtState);
            byte sec = (byte)(f / 60);
            byte frame = (byte)(f % 60);
            gb.CpuWrite("wPlayTimeMinutes", 5);
            gb.CpuWrite("wPlayTimeSeconds", sec);
            gb.CpuWrite("wPlayTimeFrames", frame);
            intro.ExecuteAfterIGT(gb);

            string nidopath = "LLLULLUAULALDLDLLDADDADLALLALUUAU";
            int ret;
            ret = gb.Execute(SpacePath(nidopath));

            if (!CheckEncounter(ret, gb, "NIDORANM", new IGTResult()))
                continue;

            gb.ClearText(Joypad.B);
            gb.Press(Joypad.A);
            gb.RunUntil("_Joypad");
            gb.AdvanceFrame();
            gb.SaveState("basesaves/red/manip/ext/nido_" + sec + "_" + frame + ".gqs");

            if (f % 10 == 0)
                Console.WriteLine(f + "/" + maxframe);
        }
    }

    class IGTStateResult : IGTResult
    {
        public byte[] State;
    }
    static Dictionary<string, IGTStateResult[]> PersistentStates;
    static Red[] PersistentGbs;

    static List<IGTResult> CheckIGTPersistent(int framesToWait, string path, int numThreads = 15, bool verbose = false)
    {
        if (PersistentGbs == null)
        {
            PersistentGbs = MultiThread.MakeThreads<Red>(numThreads);
            PersistentStates = new Dictionary<string, IGTStateResult[]>();
        }
        Red[] gbs = PersistentGbs;

        if (PersistentStates.Count > 2000) // unreasonable memory usage
            PersistentStates.Clear();

        const int maxframe = 60;
        if (!PersistentStates.ContainsKey(""))
        {
            IGTStateResult[] results = new IGTStateResult[maxframe];
            MultiThread.For(maxframe, gbs, (gb, f) =>
            {
                if (IgnoredFrames.Contains(f))
                    return;

                IGTStateResult res = results[f] = new IGTStateResult();

                res.IGTSec = (byte)(f / 60);
                res.IGTFrame = (byte)(f % 60);
                gb.LoadState("basesaves/red/nido_" + res.IGTSec + "_" + res.IGTFrame + ".gqs");

                gb.AdvanceFrames(framesToWait);
                gb.Press(Joypad.A);
                gb.Press(Joypad.Start);
                gb.AdvanceFrames(40);

                res.Tile = gb.Tile;
                res.Map = gb.Map;
                res.State = gb.SaveState();
            });
            PersistentStates[""] = results;
        }

        int step = path.Length;
        while (!PersistentStates.ContainsKey(path.Substring(0, step)))
            --step;

        if (step < path.Length)
        {
            for (int i = step + 1; i <= path.Length; ++i)
                PersistentStates[path.Substring(0, i)] = new IGTStateResult[maxframe];

            MultiThread.For(maxframe, gbs, (gb, f) =>
            {
                int curstep = step;
                IGTStateResult prev = PersistentStates[path.Substring(0, curstep)][f];
                if (prev != null && prev.State != null)
                    gb.LoadState(prev.State);
                while (curstep < path.Length)
                {
                    string curpath = path.Substring(0, curstep + 1);

                    if (prev == null || prev.State == null) // we already have a result
                    {
                        PersistentStates[curpath][f] = prev;
                    }
                    else
                    {
                        string action = path[curstep].ToString().Replace("S", "S_B");
                        if (action != "_" && action != "B")
                        {
                            IGTStateResult res = PersistentStates[curpath][f] = new IGTStateResult();

                            res.IGTSec = prev.IGTSec;
                            res.IGTFrame = prev.IGTFrame;

                            int ret = gb.Execute(action);
                            if (ret == gb.OverworldLoopAddress)
                                res.State = gb.SaveState();
                            else
                                CheckEncounter(ret, gb, "PIDGEY", res);
                            res.Tile = gb.Tile;
                            res.Map = gb.Map;
                            prev = res;
                        }
                    }
                    ++curstep;
                }
            });
        }

        List<IGTResult> final = new List<IGTResult>(PersistentStates[path]);
        final.RemoveAll(x => x == null);
        return final;
    }

    static void ClearIGTPersistent()
    {
        PersistentStates = null;
        PersistentGbs = null;
    }

    static List<IGTResult> CheckIGT(int framesToWait, string path, int numThreads = 12, bool verbose = false, int maxframe = 60)
    {
        numThreads = 1;
        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        if (numThreads == 1)
            gbs[0].Record("test");
        List<IGTResult> results = new List<IGTResult>();

        MultiThread.For(maxframe, gbs, (gb, f) =>
        {
            if (IgnoredFrames.Contains(f % 60))
                return;

            IGTResult res = new IGTResult();

            res.IGTSec = (byte)(f / 60);
            res.IGTFrame = (byte)(f % 60);
            gb.LoadState("basesaves/red/manip/ext/nido_" + res.IGTSec + "_" + res.IGTFrame + ".gqs");

            gb.AdvanceFrames(framesToWait);
            gb.Press(Joypad.A);
            gb.Press(Joypad.Start);

            Dictionary<int, string> npcMovement = new Dictionary<int, string>();
            // gb.AddInterrupt(gb.SYM["TryWalking"] + 25, (gb) =>
            // {
            //     if (gb.GetMap().Id != 1)
            //         return;
            //     string movement;
            //     Registers reg = gb.Registers;
            //     switch (reg.B)
            //     {
            //         case 1: movement = "r"; break;
            //         case 2: movement = "l"; break;
            //         case 4: movement = "d"; break;
            //         case 8: movement = "u"; break;
            //         default: movement = ""; break;
            //     }
            //     if ((reg.F & 0x10) == 0)
            //         movement = movement.ToUpper();
            //     int npc = gb.CpuRead(0xffda) / 16;

            //     string log = npcMovement.GetValueOrDefault(npc);
            //     if (log == null || log.Last().ToString().ToLower() != movement)
            //         npcMovement[npc] = log + movement;
            //     // Console.WriteLine("npc "+npc+" move "+movement+" B="+reg.B+" F="+reg.F);
            // });

            int ret = gb.Execute(SpacePath(path));

            CheckEncounter(ret, gb, "PIDGEY", res);

            res.Info = npcMovement.GetValueOrDefault(1) + "," + npcMovement.GetValueOrDefault(7);
            res.Tile = gb.Tile;
            res.Map = gb.Map;

            lock (results)
                results.Add(res);

            if (verbose && f % 10 == 0)
                Console.WriteLine(f + "/" + maxframe);
        });
        if (verbose)
            Console.WriteLine();

        return results;
    }

    static Dictionary<string, int> GetIGTSummary(List<IGTResult> results, bool verbose = false)
    {
        results.Sort(delegate (IGTResult a, IGTResult b)
        {
            return (a.IGTSec * 60 + a.IGTFrame).CompareTo(b.IGTSec * 60 + b.IGTFrame);
        });

        Dictionary<string, int> summary = new Dictionary<string, int>();
        foreach (IGTResult res in results)
        {
            string line = "";
            if (res.Mon != null)
            {
                line = res.Mon.Species.Name + " " + res.Mon.Level;
                if (verbose)
                    line += " @" + res.Tile;
                if (res.Mon.Species.Name == "PIDGEY")
                {
                    if (res.Yoloball)
                        line += " captured";
                    else
                        line += " failedtocapture";
                }
            }
            if (res.Info != null)
                line = res.Info + " " + line;
            if (verbose)
                Console.WriteLine($"{res.IGTFrame,2} " + line);
            if (!summary.ContainsKey(line))
                summary.Add(line, 1);
            else
                summary[line]++;
        }
        if (verbose)
            Console.WriteLine();

        return summary;
    }

    static void DisplayIGTResults(List<IGTResult> results, int frame = -1, bool verbose = true)
    {
        if (frame >= 0)
            Console.WriteLine("PATH " + FramePath(frame) + " (frame " + frame + ")");

        Dictionary<string, int> summary = GetIGTSummary(results, verbose);

        foreach (var item in summary.OrderByDescending(x => x.Value))
        {
            Console.WriteLine(item.Value + "/" + results.Count + " " + (item.Key != "" ? item.Key : "No encounter"));
        }
    }

    static List<DFState<RbyMap,RbyTile>> Search(int framesToWait, string path, int numThreads = 14, int numFrames = 57, int success = -1)
    {
        Red[] gbs = {};
        Red gb = null;

        Profile("threads", () =>
        {
            gbs = MultiThread.MakeThreads<Red>(numThreads);
            gb = gbs[0];
            if (numThreads == 1)
                gb.Record("test");
        });

        // byte[][] states = new byte[numFrames][];
        IGTResults states = new IGTResults(numFrames);

        Profile("states", () =>
        {
            MultiThread.For(states.Length, gbs, (gb, i) =>
            {
                int f = i;
                foreach (int skip in IgnoredFrames)
                    if (f >= skip)
                        ++f;

                gb.LoadState("basesaves/red/manip/ext/nido_" + (byte)(f / 60) + "_" + (byte)(f % 60) + ".gqs");

                gb.AdvanceFrames(framesToWait);
                gb.Press(Joypad.A);
                gb.Press(Joypad.Start);

                int ret = gb.Execute(SpacePath(path));
                states[i]=new IGTState(gb, false, f);
            });
        });

        RbyMap viridian = gb.Maps[1];
        RbyMap route2 = gb.Maps[13];
        viridian.Sprites.Remove(18, 9);
        Action dirs = Action.Right | Action.Left | Action.Up | Action.Down;
        RbyTile[] endTiles = { route2[8, 48] };
        // Pathfinding.GenerateEdges<RbyMap,RbyTile>(gb, 0, viridian[17, 0], dirs | Action.A);
        Pathfinding.GenerateEdges<RbyMap,RbyTile>(gb, 0, endTiles.First(), dirs | Action.A | Action.StartB);
        // route2[8, 48].RemoveEdge(0, Action.Left | Action.Right | Action.Down);
        // viridian[18, 0].AddEdge(0, new Edge<RbyMap,RbyTile>() { Action = Action.Up, NextTile = route2[8, 71], NextEdgeset = 0, Cost = 0 });
        // viridian[17, 0].AddEdge(0, new Edge<RbyMap,RbyTile>() { Action = Action.Up, NextTile = route2[7, 71], NextEdgeset = 0, Cost = 0 });
        Pathfinding.DebugDrawEdges(gb, viridian, 0);

        RbyTile[] encounterTiles = { route2[6, 48], route2[7, 48], route2[8, 48], route2[7, 49], route2[8, 49], route2[8, 50] };
        RbyTile tile = gb.Tile;

        List<DFState<RbyMap,RbyTile>> results = new List<DFState<RbyMap,RbyTile>>();

        DFParameters<Red,RbyMap,RbyTile> parameters = new DFParameters<Red,RbyMap,RbyTile>()
        {
            MaxCost = 10,
            SuccessSS = success >= 0 ? success : Math.Max(1, states.Length - 3),// amount of yoloball success for found
            EndTiles = endTiles,
            EncounterCallback = gb =>
            {
                return gb.EnemyMon.Species.Name == "PIDGEY" && gb.Yoloball() && encounterTiles.Any(t => t.X == gb.Tile.X && t.Y == gb.Tile.Y);
            },
            FoundCallback = state =>
            {
                results.Add(state);
                Console.WriteLine(tile.PokeworldLink + "/" + state.Log + " Captured: " + state.IGT.TotalSuccesses + " Failed: " + (state.IGT.TotalFailures - state.IGT.TotalOverworld) + " NoEnc: " + state.IGT.TotalOverworld + " Cost: " + state.WastedFrames);
            }
        };

        Profile("dfs", () =>
        {
            DepthFirstSearch.StartSearch(gbs, parameters, tile, 0, states);
        });

        return new List<DFState<RbyMap,RbyTile>>(results.OrderByDescending((dfs) => dfs.IGT.TotalSuccesses).OrderBy((dfs) => APressCount(dfs.Log)).OrderBy((dfs) => TurnCount(dfs.Log)));
    }

    const string BasePath = "DRRUUURRRRRRRRRRRRRRRRRRRRRUR";
    const string BasePathToGirl = BasePath + "UUUUUUR";
    const string BasePathToSignL = BasePathToGirl + "UUUULUUUUUU";
    const string BasePathToSignR = BasePathToGirl + "UUUUUUUUUUL";
    static Dictionary<string,string> Link = new Dictionary<string,string> {
        { BasePath, "https://gunnermaniac.com/pokeworld?map=1#57/178/" },
        { BasePathToGirl, "https://gunnermaniac.com/pokeworld?map=1#58/172/" },
        { BasePathToSignL, "https://gunnermaniac.com/pokeworld?map=1#57/162/" },
        { BasePathToSignR, "https://gunnermaniac.com/pokeworld?map=1#57/162/" }
    };
    static string[] Pathsv3 = { "",
        "DRRRRUUURRRRRRRRRRRRRRRRRRRURRUUUUUUUUUULUUUUUUUUUUUUUUUUUUUUULLLUUUUURRRRUUUSUUUUULLLLLU",      // 1
        "DRRRRUUURRRRRRRRRRRRRRRRRRRURRUUUUUUUUUUUUUUUUUUUUUUUUUULUUUUULLLUUUUUUARRARRSUUUUUUULLLLLU",    // 2
        "DRRRRUUURRRRRRRRRRRRRRRRRRRURRUUUUUUUUUUUUUUUUUUUUUUUUUULUUUUULLLUUUUUUARRARRSUUUUUUULLLLLU",    // 2
        "DRRRRUUURRRRRRRRRRRRRRRRRRRURRUUUUUUUUUUUUUUUUUUUUUUUUUULUUUUULLLUUUUURRRARUSUUUUUUULLLLLU",     // 3
        "DRRRRUUURRRRRRRRRRRRRRRRRRRURRUUUUUUUUUULUUUUUUUUUUUUUUUUUUUUULLLUUUUUSURRRRUUUUUUULLLLLU",      // 4
        "DRRRRUUURRRRRRRRRRRRRRRRRRRURRUAUUAUUUUUUUUUUUUUUUUUUUUUUULUUUUULLLUUUUURRRUUAURSUUUUUUUULLLLLU",// 5
        "DRRRRUUURRRRRRRRRRRRRRRRRRRURRUAUUAUUUUUUULUUUUUUUUUUUUUUUUUUUUULLLUUUUUURRARRAUUUUUUUULLLLLU",  // 6
        "DRRRRUUURRRRRRRRRRRRRRRRRRRURRUUUUUUUUUULUUUUUUUUUUUUUUUUUUUUULLLUUUUURRRRUUUAUUUUULLLLLU",      // 7
        "DRRRRUUURRRRRRRRRRRRRRRRRRRURRUUUUUUUUUULUUUUUUUUUUUUUUUUUUUUULLLUUUUUARRRRSUUAUUUUUULLLLLU",    // 8
        "DRRRRUUURRRRRRRRRRRRRRRRRRRURRUUUUUUUUUULUUUUUUUUUUUUUUUUUUUUULLLUUUUURRRRUUUUUUUULLLLLU"        // 9
    };
    static string[] Paths = { "",
        BasePath + "UUUUUURUUUULUUUUUUAUUUUUUUUUUUUULLLUUUUUUUUUUURRR",          // 1
        BasePath + "UUUUUURUUAUULUUUAUUUUUUUUUUUUUUAUULLLUUUUUUURRRRUAUUU",      // 2
    //  BasePath + "UUUUUURUUAUULUUUAUUUUUUUUUUUUUUAUULLLUUUUUUURRRRUAUUU",      // 2b
        BasePath + "UUUUUURAUUUUUUUUUUUUUUUUUUUULUAUULLLUUUUUUUUUURRRARU",       // 3
        BasePath + "UUUUUURUUUUUUUUUULAUUUUAUUUUAUUUAUULLLUUUUUUUUAURRRRU",      // 4
        BasePath + "UUUUUURUUUULUUUUUUAUUUUUUUUAUUUAUULLLUUUUUUAUURRUUAURR",     // 5
        BasePath + "UUUUUURUUUUUUUUUULAUUUUUUUUUUUUUULLLUUAUUUAURRRRAUU",        // 6
        BasePath + "UUUUUURUUUULUUUUUUAUUUUUUUUUUUAUULLLUUUUUUUURRRRU",          // 7 dUR "2A"
     // BasePath + "UUUUUURUUUULUUUAUUUUUUUUUUUUUUUULLLUUUUUUUARRRRUAUU",        // 7 dUR "fence"
     // BasePath + "UUUUUURUULUUUUUAUUUUUUUUUUUUUUAUULLLUUUUUUURRRRUAUU",        // 7 dUR "girl turn"
     // BasePath + "UUUUUURUUUUUUUUUULAUUUUUUUUUUUUUULLLUUUUUURRRRUU",           // 7 dD "new"
     // BasePath + "RUUUUUUUUUULUUUUUUUUUUUUUUUUUUUUULLLUUUUURRRRUUUAU",         // 7 dD "3.0"
     // BasePath + "UAUUUUURUULUUUUUUUUAUUUUUUUUUUUUULLLUUUUUUURRRRUAUU",        // 7 "universal"
        BasePath + "UUUUUURUUUUUUUUUULUUUUUUUUUUUUULLLUUUUUUURRRRUUU",           // 8 "0A"
     // BasePath + "UUUUUURUUUUUUUUUULUUUUUUUUUUUUUULLLUUUUUUUURRRRUAU",         // 8 "1A"
        BasePath + "UUUUUURUUUULUUUUUUUUUUUUUUUUUAUUUURLALLLAUUUUUUUUURRR",      // 9 "extrastep" (c=40)
     // BasePath + "UUUUUURUUUULUUUUUUUUUUUUUUUUUUULLLUUUUUUUSUUUURRRAR",        // 9 "startflash" (c=55)
     // BasePath + "UUUUUURRUUUUUUUULLUUUUUUUUUUUUUUULLLUUUUUUUUUURRRRU",        // 9 "0A" 53/54
     // BasePath + "UUUUUURUUUAULUUUAUUUUUUUUUAUUUUUUUULLULUUAUUUUUUARRRAUR",    // 9 "6A"
     // BasePath + "UUUUUURUAUUAULAUUAUUAUUUUUUUUUUUUUUUULLALUAUUAUUAUURRRUUUAR",// 9 "10A"
        BasePath + "UUUUUURUUUULUUAUUUUUUUUUUUUUUUUULLLAUUUUUUUUUARRRRAUU",      // 10 "4A"
     // BasePath + "UUUUUURUUUULUUUUUUAUUUUAUUUUAUUUAUUALLLAUUUUUUUUUARRRRAUU",  // 10 "8A"
     // BasePath + "UUUUUURUUUULUUUUUUUUUUUUUUUUUAUUURLLLLUUUUUUUUUUARRR",       // 10 "extrastep" (c=36)
    };
    static SortedSet<int> IgnoredFrames = new SortedSet<int> { 33, 36, 37 };

    static int PathFrame(int path)
    {
        return path > 2 ? path + 1 : path;
    }
    static int FramePath(int frame)
    {
        return frame > 2 ? frame - 1 : frame;
    }

    static void IgnorePathFrames(int path)
    {
        if (path < 2)
        {
            IgnoredFrames.Add(14 - path);
            IgnoredFrames.Add(15 - path);
        }
        else
        {
            IgnoredFrames.Add( (13 - path) % 60 );
            IgnoredFrames.Add( (14 - path) % 60 );
        }
        IgnoredFrames.Add(34);
    }
    static void PathMovements(bool withA)
    {
        for(int frame=1; frame<=10; ++frame) {
            List<IGTResult> res;
            if(withA) res = CheckIGT(frame, Paths[frame]);
            else res = CheckIGT(frame, Paths[frame].Replace("A","").Substring(0,50));
            DisplayIGTResults(res, frame, false);
            Console.WriteLine();
        }
    }
    static void CheckPathsInFile(int frame, string basepath)
    {
        string[] pathstocheck=File.ReadAllLines("basesaves/paths.txt");
        List<Display> display = new List<Display>();
        foreach (var path in pathstocheck)
        {
            if(!path.Contains('S')) continue;
            List<IGTResult> igt = CheckIGTPersistent(frame, basepath + path);
            int success = GetIGTSummary(igt).GetValueOrDefault("PIDGEY 5 captured");
            display.Add(new Display { Path = path, S = success, T = TurnCount(path), A = APressCount(path) });
        }
        foreach (Display d in display.OrderByDescending((d) => d.S).ThenBy((d) => d.A).ThenBy((d) => d.T))
            Console.WriteLine(Link[basepath] + d.Path + " " + d.S + " t:" + d.T + " a:" + d.A);
    }

    public static void Check()
    {
        int path = 8;
        int frame = PathFrame(path);

        DisplayIGTResults(
            CheckIGT(frame, Paths[path], 12, false, 60),
            frame, true);
    }

    public static void Search()
    {
        int path = 8;
        int frame = PathFrame(path);
        string basepath = BasePathToSignR;

        Profile("search+igt", () => {
            List<DFState<RbyMap,RbyTile>> results = null;

        Profile("search", () => {
            results = Search(frame, basepath, 4, 4, 4);

        }); Profile("igt", () => {
            List<Display> display = new List<Display>();
            foreach (var res in results)
            {
                List<IGTResult> igt = CheckIGTPersistent(frame, basepath + res.Log);
                int success = GetIGTSummary(igt).GetValueOrDefault("PIDGEY 5 captured");
                display.Add(new Display { Path = res.Log, S = success, T = TurnCount(res.Log), A = APressCount(res.Log) });
            }
            foreach (Display d in display.OrderByDescending((d) => d.S).ThenBy((d) => d.A).ThenBy((d) => d.T))
                Console.WriteLine(Link[basepath] + d.Path + " " + d.S + " T:" + d.T + " A:" + d.A);
        }); });
    }

    public Extended()
    {
        Check();
    }
}
