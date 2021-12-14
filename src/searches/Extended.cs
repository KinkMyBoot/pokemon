using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;

using static SearchCommon;
using static RbyIGTChecker<Red>;

class Extended
{
    static bool CheckEncounter(int address, Red gb, string pokename, IGTResult res)
    {
        if (address != gb.SYM["CalcStats"])
            return false;

        res.Mon = gb.EnemyMon;
        if (res.Mon.Species.Name != pokename)
            return false;

        res.Yoloball = gb.Yoloball(0, Joypad.B);
        return res.Yoloball;
    }
    static bool CheckNoEncounter(int address, Red gb, IGTResult res)
    {
        if (address != gb.SYM["CalcStats"])
            return true;

        res.Mon = gb.EnemyMon;
        return false;
    }

    static void BuildStates()
    {
        const int numThreads = 16;
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.NoPal);
        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];
        // if (numThreads == 1) gb.Record("test");

        gb.LoadState("basesaves/red/manip/nido.gqs");
        gb.HardReset();
        intro.ExecuteUntilIGT(gb);
        byte[] igtState = gb.SaveState();

        const int numFrames = 3598;
        MultiThread.For(numFrames, gbs, (gb, f) =>
        {
            if (IgnoredFrames.Contains(f % 60))
                return;

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
                return;

            gb.ClearText(Joypad.B);
            gb.Press(Joypad.A);
            gb.RunUntil("_Joypad");
            gb.AdvanceFrame();
            gb.SaveState("basesaves/red/manip/ext/nido_" + sec + "_" + frame + ".gqs");

            if(f % (numFrames / 100) == 0) Console.WriteLine("%");
        });
    }

    static void LogRNG()
    {
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.NoPal);
        RedCb gb = new RedCb();
        gb.Record("test");

        gb.LoadState("basesaves/red/manip/nido.gqs");
        gb.HardReset();
        intro.ExecuteUntilIGT(gb);
        gb.CpuWrite("wPlayTimeMinutes", 4);
        gb.CpuWrite("wPlayTimeSeconds", 58);
        gb.CpuWrite("wPlayTimeFrames", 0);
        intro.ExecuteAfterIGT(gb);
        gb.SetCallback(gb.SYM["VBlank"], (gb) =>
        {
            Trace.WriteLine($"{gb.CpuRead("wPlayTimeMinutes"):d2}:{gb.CpuRead("wPlayTimeSeconds"):d2}.{gb.CpuRead("wPlayTimeFrames"):d2} {gb.CpuRead("hRandomAdd"):x2}{gb.CpuRead("hRandomSub"):x2}");
        });
        gb.Execute(SpacePath("LLLULLUAULALDLDLLDADDADLALLALUUAU"));
        gb.Yoloball(0, Joypad.B);
        gb.Hold(Joypad.B, "ManualTextScroll"); // all right
        // gb.AdvanceFrame(); // all right late
            // Trace.Listeners.Add(new TextWriterTraceListener(File.CreateText("allrightlate.txt"), "allrightlate.txt"));
        gb.ClearText(Joypad.B, 2);
        gb.Hold(Joypad.B, "ManualTextScroll"); // will be
        // gb.AdvanceFrame(); // will be late
        gb.ClearText(Joypad.B, 4);
        gb.Hold(Joypad.B, "ManualTextScroll"); // give a
        gb.ClearText(Joypad.B, 1);
        // gb.AdvanceFrame(); // yesno late
        gb.ClearText(Joypad.B, 1); // yesno
        gb.Press(Joypad.A);
        gb.RunUntil("_Joypad");
            // Trace.Listeners.Remove("allrightlate.txt");
        byte[] state = gb.SaveState();
        // return;

        // for(int f = -2; f <= 11; ++f)
        for(int f = -2; f <= 10; ++f)
        {
            // int p = FramePath(f);
            // string name = "path" + p + (f==3?"b":"") + ".txt";
            int p = f>=2 ? f : (f+1);
            string name = "path" + p + (f==2?"c":"b") + ".txt";
            Trace.Listeners.Add(new TextWriterTraceListener(File.CreateText(name), name));
            gb.LoadState(state);
            if(f < 0)
                gb.AdvanceFrames(f + 2);
            else
            {
                gb.AdvanceFrames(f + 1);
                gb.Press(Joypad.A);
            }
            gb.Press(Joypad.Start);
            string path;
            if(p < 1)
                path = BasePathToGirl;
            else
                path = Paths[p];
            gb.Execute(SpacePath(path));
            if(p >= 1)
                gb.Yoloball(0, Joypad.B);
            if(p >= 1 && p <= 6)
            {
                gb.ClearText(Joypad.A);
                gb.Press(Joypad.B);
                gb.Execute(SpacePath(Forest[p]), (gb.Maps[51][25,12], gb.PickupItem));
            }
            Trace.Listeners.Remove(name);
        }
    }

    class IGTStateResult : IGTResult
    {
        public byte[] State;
    }
    static Dictionary<string, IGTStateResult[]> PersistentStates;
    static Red[] PersistentGbs;

    static List<IGTResult> CheckIGTPersistent(int framesToWait, string path, int numFrames = 60, int numThreads = 16)
    {
        if (PersistentGbs == null)
        {
            PersistentGbs = MultiThread.MakeThreads<Red>(numThreads);
            PersistentStates = new Dictionary<string, IGTStateResult[]>();
        }
        Red[] gbs = PersistentGbs;

        if (PersistentStates.Count * numFrames > 2000 * 60) // unreasonable memory usage
            PersistentStates = PersistentStates.Where(x => x.Key.Length < 48).ToDictionary(x => x.Key, x => x.Value);

        if (!PersistentStates.ContainsKey(""))
        {
            IGTStateResult[] results = new IGTStateResult[numFrames];
            MultiThread.For(numFrames, gbs, (gb, f) =>
            {
                if (IgnoredFrames.Contains(f % 60))
                    return;

                IGTStateResult res = results[f] = new IGTStateResult();

                res.IGTSec = (byte)(f / 60);
                res.IGTFrame = (byte)(f % 60);
                try {
                    gb.LoadState("basesaves/red/manip/ext/nido_" + res.IGTSec + "_" + res.IGTFrame + ".gqs");
                } catch(System.IO.FileNotFoundException) {
                    return;
                }

                gb.AdvanceFrames(framesToWait);
                gb.Press(Joypad.A, Joypad.Start);
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
                PersistentStates[path.Substring(0, i)] = new IGTStateResult[numFrames];

            MultiThread.For(numFrames, gbs, (gb, f) =>
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

                            int address = gb.Execute(action);
                            if (address == gb.OverworldLoopAddress)
                                res.State = gb.SaveState();
                            else
                                CheckEncounter(address, gb, "PIDGEY", res);
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

    class RedCb : Red
    {
        int Address;
        Action<RedCb> Callback = null;
        public RedCb(string savFile = null, bool speedup = true) : base(savFile, speedup) { }
        public void SetCallback(int address, Action<RedCb> callback)
        {
            Address = address;
            Callback = callback;
        }
        public unsafe override int Hold(Joypad joypad, params int[] addrs)
        {
            if(Callback != null)
                addrs = addrs.Append(Address).ToArray();
            int ret;
            while((ret = base.Hold(joypad, addrs)) == Address)
            {
                Callback(this);
                RunFor(1);
            }
            return ret;
        }
    }

    static List<IGTResult> CheckIGT(int framesToWait, string path, int numFrames = 60, int numThreads = 16, int startFrame = 0, bool verbose = true)
    {
        return CheckIGT(framesToWait, path, null, numFrames, numThreads, startFrame, verbose);
    }
    static List<IGTResult> CheckIGT(int framesToWait, string path, string forest, int numFrames = 60, int numThreads = 16, int startFrame = 0, bool verbose = true)
    {
        RedCb[] gbs = MultiThread.MakeThreads<RedCb>(numThreads);
        if (numThreads == 1)
            gbs[0].Record("test");
        List<IGTResult> results = new List<IGTResult>();

        MultiThread.For(numFrames, gbs, (gb, f) =>
        {
            f += startFrame;
            if (IgnoredFrames.Contains(f % 60))
                return;

            IGTResult res = new IGTResult();

            res.IGTSec = (byte)(f / 60);
            res.IGTFrame = (byte)(f % 60);
            try {
                gb.LoadState("basesaves/red/manip/ext/nido_" + res.IGTSec + "_" + res.IGTFrame + ".gqs");
            } catch(System.IO.FileNotFoundException) {
                return;
            }

            gb.AdvanceFrames(framesToWait);
            gb.Press(Joypad.A, Joypad.Start);

            var npcMovement = new Dictionary<(int, int), string>();
            gb.SetCallback(gb.SYM["TryWalking"] + 25, (gb) =>
            {
                string movement;
                Registers reg = gb.Registers;
                switch (reg.B)
                {
                    case 1: movement = "r"; break;
                    case 2: movement = "l"; break;
                    case 4: movement = "d"; break;
                    case 8: movement = "u"; break;
                    default: movement = ""; break;
                }
                if ((reg.F & 0x10) == 0)
                    movement = movement.ToUpper();
                (int, int) npc = (gb.Map.Id, gb.CpuRead(0xffda) / 16);

                string log = npcMovement.GetValueOrDefault(npc);
                if (log == null || log.Last().ToString().ToLower() != movement)
                    npcMovement[npc] = log + movement;
            });

            int address = gb.Execute(SpacePath(path));
            // address = DecideMovement(gb, res, npcMovement);

            CheckEncounter(address, gb, "PIDGEY", res);

            res.Info = npcMovement.GetValueOrDefault((1, 1)) + "," + npcMovement.GetValueOrDefault((1, 7));
            res.Tile = gb.Tile;
            res.Map = gb.Map;

            if(res.Yoloball && forest != null)
            {
                IGTResult ext = res.Extended = new IGTResult();

                gb.ClearText(Joypad.A);
                gb.Press(Joypad.B);
                address = gb.Execute(SpacePath(forest), (gb.Maps[51][25,12], gb.PickupItem));

                CheckNoEncounter(address, gb, ext);

                ext.Info = npcMovement.GetValueOrDefault((50, 2)) + "," + npcMovement.GetValueOrDefault((51, 1)) + "," + npcMovement.GetValueOrDefault((51, 8));
                ext.Tile = gb.Tile;
                ext.Map = gb.Map;
            }

            lock (results)
                results.Add(res);

            if(verbose && (f * 100) % numFrames == 0) Console.WriteLine("%");
        });
        gbs[0].Dispose();
        if (verbose)
            Console.WriteLine();

        return results;
    }

    static int DecideMovement(Red gb, IGTResult res, Dictionary<(int, int), string> npcMovement)
    {
        res.Info=" path";
        int address;
        string npc1,npc2;
        npc1=npcMovement.GetValueOrDefault((1, 1));
        if(npc1=="dD")
            gb.Execute(SpacePath("RUUUUUU"));
        else
            gb.Execute(SpacePath("UUUUUUR"));
        npc1=npcMovement.GetValueOrDefault((1, 1));
        if(npc1=="uL")
            gb.Execute(SpacePath("UUAUU"));
        else if(npc1=="rL" || npc1=="rD")
            gb.Execute(SpacePath("AUUUU"));
        else
            gb.Execute(SpacePath("UUUU"));
        npc1=npcMovement.GetValueOrDefault((1, 1));
        npc2=npcMovement.GetValueOrDefault((1, 7));
        if(npc1=="r" && npc2=="R")
            address = gb.Execute(SpacePath(Paths[1].Substring(40)));
        else if(npc1=="uL" && npc2=="R")
            address = gb.Execute(SpacePath(Paths[2].Substring(41)));
        else if((npc1=="rL" || npc1=="rD") && npc2=="L")
            address = gb.Execute(SpacePath(Paths[3].Substring(41)));
        else if(npc1=="uU" && npc2=="L")
            address = gb.Execute(SpacePath(Paths[4].Substring(40)));
        else if(npc1=="d" && npc2=="R")
            address = gb.Execute(SpacePath(Paths[5].Substring(40)));
        else if(npc1=="d" && npc2=="L")
            address = gb.Execute(SpacePath(Paths[6].Substring(40)));
        else if(npc1=="dUR" && npc2=="R")
            address = gb.Execute(SpacePath(Paths[7].Substring(40)));
        else if(npc1=="dD" && npc2=="R")
            address = gb.Execute(SpacePath("LUUUUUUUUUUUUUUUUUUUUULLLUUUUURRRRUUUAU"));
        else if(npc1=="r" && npc2=="L")
            address = gb.Execute(SpacePath(Paths[8].Substring(40)));
        else if(npc1=="dr" && npc2=="R")
            address = gb.Execute(SpacePath(Paths[9].Substring(40)));
        else if(npc1=="u" && npc2=="R")
            address = gb.Execute(SpacePath(Paths[10].Substring(40)));
        else
        {
            res.Info="";
            if(npc2.Contains("R"))
                address = gb.Execute(SpacePath("LUUUUUUU"));
            else
                address = gb.Execute(SpacePath("UUUUUUU"));
        }
        return address;
    }

    enum PrintFlags
    {
        None = 0,
        List = 1,
        Level = 2,
        Tile = 4,
        Info = 8,
        NoEnc = 16,
    }
    static string ResultInfo(IGTResult res, PrintFlags flags = PrintFlags.Info)
    {
        string line = "";
        if ((flags & PrintFlags.Info) != 0 && res.Info != null)
            line += res.Info + " ";
        if (res.Mon == null)
        {
            if ((flags & PrintFlags.Tile) != 0)
                line += "@" + res.Tile;
            else if ((flags & PrintFlags.NoEnc) != 0)
                line += "No encounter";
        }
        else
        {
            line += res.Mon.Species.Name;
            if ((flags & PrintFlags.Level) != 0)
                line += " " + res.Mon.Level;
            if ((flags & PrintFlags.Tile) != 0)
                line += " @" + res.Tile;
            if (res.Mon.Species.Name == "PIDGEY")
            {
                if (res.Yoloball)
                    line += " captured";
                else
                    line += " failedtocapture";
            }
        }
        if (res.Extended != null)
        {
            line += ", " + ResultInfo(res.Extended, flags | PrintFlags.NoEnc);
        }
        return line;
    }
    static Dictionary<string, int> GetIGTSummary(List<IGTResult> results, PrintFlags flags = PrintFlags.Info)
    {
        results.Sort(delegate (IGTResult a, IGTResult b)
        {
            return (a.IGTSec * 60 + a.IGTFrame).CompareTo(b.IGTSec * 60 + b.IGTFrame);
        });

        Dictionary<string, int> summary = new Dictionary<string, int>();
        foreach (IGTResult res in results)
        {
            string line = ResultInfo(res, flags);
            if ((flags & PrintFlags.List) != 0)
                Trace.WriteLine((results.Count > 60 ? $"{res.IGTSec,2} " : "") + $"{res.IGTFrame,2} " + line);
            if (!summary.ContainsKey(line))
                summary.Add(line, 1);
            else
                summary[line]++;
        }
        if ((flags & PrintFlags.List) != 0)
            Trace.WriteLine("");

        return summary;
    }

    static void DisplayIGTResults(List<IGTResult> results, int frame = -1, PrintFlags flags = PrintFlags.List | PrintFlags.Level | PrintFlags.Tile | PrintFlags.Info)
    {
        if (frame >= 0)
            Trace.WriteLine("PATH " + FramePath(frame) + " (frame " + frame + ")");

        Dictionary<string, int> summary = GetIGTSummary(results, flags);

        foreach (var item in summary.OrderByDescending(x => x.Value))
        {
            Trace.WriteLine(item.Value + "/" + results.Count + " " + (item.Key != "" ? item.Key : "No encounter"));
        }
    }

    static List<DFState<RbyMap,RbyTile>> Search(int framesToWait, string path, int numThreads = 14, int numFrames = 57, int success = -1, int maxcost = 10)
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

        IGTResults states = new IGTResults(numFrames);

        Profile("states", () =>
        {
            MultiThread.For(states.Length, gbs, (gb, i) =>
            {
                int f = i;
                for(int s=0; s<60; ++s)
                    foreach (int skip in IgnoredFrames)
                        if (f >= skip + 60*s)
                            ++f;

                gb.LoadState("basesaves/red/manip/ext/nido_" + (f / 60) + "_" + (f % 60) + ".gqs");

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
        viridian.Sprites.Remove(17, 5);
        RbyTile[] endTiles = { route2[8, 48] };
        Pathfinding.GenerateEdges<RbyMap,RbyTile>(gb, 0, endTiles.First(), Action.Right | Action.Left | Action.Up | Action.Down | Action.A | Action.StartB);
        // Pathfinding.DebugDrawEdges(gb, viridian, 0);

        RbyTile[] encounterTiles = { route2[6, 48], route2[7, 48], route2[8, 48], route2[7, 49], route2[8, 49], route2[8, 50] };
        RbyTile tile = gb.Tile;

        List<DFState<RbyMap,RbyTile>> results = new List<DFState<RbyMap,RbyTile>>();

        DFParameters<Red,RbyMap,RbyTile> parameters = new DFParameters<Red,RbyMap,RbyTile>()
        {
            MaxCost = maxcost,
            SuccessSS = success >= 0 ? success : Math.Max(1, states.Length - 3),// amount of yoloball success for found
            EndTiles = endTiles,
            EncounterCallback = gb =>
            {
                return gb.EnemyMon.Species.Name == "PIDGEY" && gb.Yoloball() && encounterTiles.Any(t => t.X == gb.Tile.X && t.Y == gb.Tile.Y);
            },
            FoundCallback = state =>
            {
                results.Add(state);
                Trace.WriteLine(tile.PokeworldLink + "/" + state.Log + " Captured: " + state.IGT.TotalSuccesses + " Failed: " + (state.IGT.TotalFailures - state.IGT.TotalOverworld) + " NoEnc: " + state.IGT.TotalOverworld + " Cost: " + state.WastedFrames);
            }
        };

        Profile("dfs", () =>
        {
            DepthFirstSearch.StartSearch(gbs, parameters, tile, 0, states, 0);
        });

        return new List<DFState<RbyMap,RbyTile>>(results.OrderByDescending((dfs) => dfs.IGT.TotalSuccesses).OrderBy((dfs) => APressCount(dfs.Log)).OrderBy((dfs) => TurnCount(dfs.Log)));
    }

    static List<DFState<RbyMap,RbyTile>> SearchForest(int framesToWait, string pidgeypath, string forestpath, int numThreads = 14, int numFrames = 57, int success = -1, int maxcost = 10)
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

        IGTResults states = new IGTResults(numFrames);

        Profile("states", () =>
        {
            MultiThread.For(states.Length, gbs, (gb, i) =>
            {
                int f = i;
                for(int s=0; s<60; ++s)
                    foreach (int skip in IgnoredFrames)
                        if (f >= skip + 60*s)
                            ++f;

                gb.LoadState("basesaves/red/manip/ext/nido_" + (f / 60) + "_" + (f % 60) + ".gqs");

                gb.AdvanceFrames(framesToWait);
                gb.Press(Joypad.A);
                gb.Press(Joypad.Start);

                int ret = gb.Execute(SpacePath(pidgeypath));

                CheckEncounter(ret, gb, "PIDGEY", new IGTResult());
                gb.ClearText(Joypad.A);
                gb.Press(Joypad.B);

                if(forestpath != null)
                    ret = gb.Execute(SpacePath(forestpath), (gb.Maps[51][25,12], gb.PickupItem));

                states[i]=new IGTState(gb, false, f);
            });
        });

        RbyMap route2 = gb.Maps[13];
        RbyMap gate = gb.Maps[50];
        RbyMap forest = gb.Maps[51];
        // forest.Sprites.Remove(25, 11); //?
        RbyTile[] endTiles = { forest[1, 19] };
        RbyTile[] blockedTiles = { forest[16, 10], forest[18, 10], forest[16, 15], forest[18, 15], forest[11, 15], forest[13, 15], forest[11, 6], forest[13, 6], forest[6, 6], forest[8, 6], forest[6, 15], forest[8, 15], forest[2, 19] };
        Pathfinding.GenerateEdges<RbyMap,RbyTile>(gb, 0, endTiles.First(), Action.Right | Action.Left | Action.Up | Action.Down | Action.A | Action.StartB, blockedTiles);
        Pathfinding.DebugDrawEdges(gb, route2, 0);
        // Pathfinding.DebugDrawEdges(gb, gate, 0);
        // Pathfinding.DebugDrawEdges(gb, forest, 0);

        RbyTile tile = gb.Tile;

        List<DFState<RbyMap,RbyTile>> results = new List<DFState<RbyMap,RbyTile>>();

        DFParameters<Red,RbyMap,RbyTile> parameters = new DFParameters<Red,RbyMap,RbyTile>()
        {
            MaxCost = maxcost,
            SuccessSS = success >= 0 ? success : Math.Max(1, states.Length - 3),
            // EndTiles = endTiles,
            EncounterCallback = gb =>
            {
                return gb.EnemyMon.Species.Name == "CATERPIE" && endTiles.Any(t => t.X == gb.Tile.X && t.Y == gb.Tile.Y);
            },
            FoundCallback = state =>
            {
                results.Add(state);
                Trace.WriteLine(tile.PokeworldLink + "/" + state.Log + " Success: " + state.IGT.TotalSuccesses + " Failed: " + (state.IGT.TotalFailures - state.IGT.TotalOverworld) + " NoEnc: " + state.IGT.TotalOverworld + " Cost: " + state.WastedFrames);
            }
        };

        Profile("dfs", () =>
        {
            DepthFirstSearch.StartSearch(gbs, parameters, tile, 0, states, 0);
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
     // BasePath + "UUUUUURUUUUUUUUUULAUUUUUUUUUUUUUULLLUUAUUUAURRRRAUU",        // 6
        BasePath + "UUUUUURUUUUUUUUUULAUUUUUUUUUUUUULLLAUUUUUUUARRRRAUU",        // 6 "normal turn"
     // BasePath + "UUUUUURUUUULUUUUUUAUUUUUUUUUUUAUULLLUUUUUUUURRRRU",          // 7 dUR "2A"
     // BasePath + "UUUUUURUUUULUUUAUUUUUUUUUUUUUUUULLLUUUUUUUARRRRUAUU",        // 7 dUR "fence"
     // BasePath + "UUUUUURUULUUUUUAUUUUUUUUUUUUUUAUULLLUUUUUUURRRRUAUU",        // 7 dUR "girl turn"
     // BasePath + "UUUUUURUUUUUUUUUULAUUUUUUUUUUUUUULLLUUUUUURRRRUU",           // 7 dD "new"
     // BasePath + "RUUUUUUUUUULUUUUUUUUUUUUUUUUUUUUULLLUUUUURRRRUUUAU",         // 7 dD "3.0"
     // BasePath + "UAUUUUURUULUUUUUUUUAUUUUUUUUUUUUULLLUUUUUUURRRRUAUU",        // 7 "universal"
        BasePath + "UUUUUURUUUULAUUAUUUUAUUUUUUUUAUUUAUULLLUUUUUUURRRRUU",       // 7 dUR "perfect"
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
    static string[] Forest = { "",
        "RUULLLLLUUU" + "RUUUUUUU" + "UUUURRRRRURRRUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUALLLLLLLLDDDDDDDLLLLUUUUUUUUUUUUULLLLLLDDDDDDDDDDDDDDDDDDLDLLLLUUU",       // 1
        "UUUULLLLLU" + "UUUUURUU" + "UUAUURUARRRRRRRUUUUUUUUUUUAUUAUUUUUUUUUUUUUUUUUUUULLLLLLLLDDDDDDDLLLLUUUUUUUUUUUUULLLLLLDDDDDDDDDDDDDDDDDDLDLLLLUUU",     // 2
        "UUUAULLLLLU" + "RUUUUUUU" + "UUURURRURRRRRUAUUUUUUUUUUUUUUUUUUAUUUUUUUUUUUUUULLLLLLLLDDDDDDDLLLLUUUUUUUUUUUUULLLLLLDDDDDDDDDDDDDDDDDDDLLLLLUAUU",     // 3
        "UUUUULLLLLU" + "UUUUUURU" + "UUUURURRRRRRRAUUUAUUUAUUUUUUUUUUUUUUUAUUUUUUUUUUUULLLLLLLLDDDDDDDLLLLUUUUUUUUUUUUULLLLLLDDDDDADDADDADDDDDDDDDDLLLLLAUUU",// 4
        "UUUULLLLLU" + "UUUURUUU" + "UUUURRRRRRRURAUUUUUAUUUUUUAUUUUUUUUUUUUUUUUUAUUUUULLLLLLLLDDDDDDDLLLLUUUUUUUUUUUUULLLLLLDDDDDDDDDDDDDDDDDDDLLLLLAUUU",    // 5
        "UUUULALLLLUUU" + "RUUUUUUU" + "UUURURRRRRRRUUUUUUUAUUUUAUUUUUUUUUUUUUAUUUAUUUUUUULLLLLLLLDDDDDDDLLLLUUUUUUUUUUUUULLLLLLDDDDDDDDDDDDDDDDDDLDLLLLUUU",  // 6
        "","","",""
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

    static void IgnoreNpcIgts(int path, string p7 = null)
    {
        IgnoredFrames = new SortedSet<int> { 33, 36, 37 };
        if (path < 2)
        {
            IgnoredFrames.Add(14 - path);
            IgnoredFrames.Add(15 - path);
        }
        else
        {
            IgnoredFrames.Add( (13 - path) % 60 );
            IgnoredFrames.Add( (14 - path) % 60 );
            if(path == 7)
            {
                if(p7 == "dUR")
                {
                    IgnoredFrames.Add(12);
                    IgnoredFrames.Add(52);
                    IgnoredFrames.Add(53);
                    IgnoredFrames.Add(54);
                    IgnoredFrames.Add(55);
                }
                else if(p7 == "dD")
                {
                    for(int i=0; i<=11; ++i)
                        IgnoredFrames.Add(i);
                    for(int i=13; i<=51; ++i)
                        IgnoredFrames.Add(i);
                    for(int i=56; i<=59; ++i)
                        IgnoredFrames.Add(i);
                }
            }
        }
        IgnoredFrames.Add(34);
    }
    static void PathUnknownMovements()
    {
        for(int frame=1; frame<=11; ++frame) {
            List<IGTResult> res;
            res = CheckIGT(frame, BasePath, 60);
            DisplayIGTResults(res, frame, PrintFlags.Info);
            Trace.WriteLine("");
        }
    }
    static void PathMovement3600(int frame)
    {
        var res=CheckIGT(frame, BasePathToSignR, 3600);
        var ds=new Dictionary<string,int[]>();
        var df=new Dictionary<string,int[]>();
        foreach(var r in res)
        {
            if(!ds.ContainsKey(r.Info)) {
                ds.Add(r.Info, new int[60]);
                df.Add(r.Info, new int[60]);
            }
            ds[r.Info][r.IGTSec]++;
            df[r.Info][r.IGTFrame]++;
        }
        foreach(var x in ds)
        {
            Trace.WriteLine(x.Key);
            for(int i=0; i<60; ++i)
                Trace.WriteLine("s" + i + ": " + x.Value[i]);
        }
        foreach(var x in df)
        {
            Trace.WriteLine(x.Key);
            for(int i=0; i<60; ++i)
                Trace.WriteLine("f" + i + ": " + x.Value[i]);
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
            int success = GetIGTSummary(igt).GetValueOrDefault("PIDGEY captured");
            display.Add(new Display { Path = path, S = success, T = TurnCount(path), A = APressCount(path) });
        }
        foreach (Display d in display.OrderByDescending((d) => d.S).ThenBy((d) => d.A).ThenBy((d) => d.T))
            Trace.WriteLine(Link[basepath] + d.Path + " " + d.S + " t:" + d.T + " a:" + d.A);
    }

    public static void Check(int path)
    {
        int frame = PathFrame(path);
        string p = Paths[path];
        string f = Forest[path];
        IgnoreNpcIgts(path);

        DisplayIGTResults(
            CheckIGT(frame, p, f, 60),
            frame
            );
    }

    public static void Search(int path)
    {
        int frame = PathFrame(path);
        string basepath = BasePathToGirl;
        IgnoreNpcIgts(path);

        Profile("search + igt", () => {
            List<DFState<RbyMap,RbyTile>> results = null;

        Profile("search", () => {
            results = Search(frame, basepath, 4, 4, 4, 8);

        }); Profile("igt", () => {
            List<Display> display = new List<Display>();
            foreach (var res in results)
            {
                List<IGTResult> igt = CheckIGTPersistent(frame, basepath + res.Log, 60*2);
                int success = GetIGTSummary(igt).GetValueOrDefault("PIDGEY captured");
                display.Add(new Display { Path = res.Log, S = success, T = TurnCount(res.Log), A = APressCount(res.Log) });
            }
            foreach (Display d in display.OrderByDescending((d) => d.S).ThenBy((d) => d.A).ThenBy((d) => d.T))
                Trace.WriteLine(Link[basepath] + d.Path + " " + d.S + " T:" + d.T + " A:" + d.A);
        }); });
    }

    public Extended()
    {
        int path = 6;
        Check(path);
        // Search(path);
    }
}
