using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

using System.Text;
using static SearchCommon;
using static RbyIGTChecker<Red>;
using System.Runtime.InteropServices;

class Starter
{
    static SortedSet<int> IgnoredFrames = new SortedSet<int> { };
    const string State = "basesaves/red/manip/starter.gqs";
    const int igtSec = 47;
    public static void Search()
    {

        for (int renameDelay = 0; renameDelay <= 10; ++renameDelay)
            for (RbyStrat pal = RbyStrat.NoPal; pal <= RbyStrat.PalRel; pal++)
                for (RbyStrat gf = RbyStrat.GfSkip; gf <= RbyStrat.GfSkip; ++gf) // this means speedrunners always skip having a girlfriend
                    for (RbyStrat hop = RbyStrat.Hop0; hop <= RbyStrat.Hop0; ++hop)
                        for (int backouts = 0; backouts <= 0; ++backouts)
                        {
                            Trace.WriteLine($"Pal: {pal} path: {renameDelay}");
                            findSquirtles(new RbyIntroSequence(pal, gf, hop, backouts), numThreads: 12, numFrames: 60, path: renameDelay, igtFrameCluster: 3, minClusterSize: 2, maxturns: 8);
                        }
    }
    public static void BuildStates()
    {
        if (System.IO.File.Exists("basesaves/red/manip/starter/starter0_0.gqs"))
            return;
        System.IO.Directory.CreateDirectory("basesaves/red/manip/starter");
        const int numThreads = 12;
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.NoPal);
        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];
        if (numThreads == 1)
            gb.Record("test");
        gb.LoadState(State);
        gb.HardReset();
        intro.ExecuteUntilIGT(gb);
        byte[] igtState = gb.SaveState();
        return;
    }
    public static void BuildRoute1States(RbyIntroSequence intro, int renameDelay, string path, List<int> targetFrames, int numThreads = 12, int minClusterSize = 2)
    {
        if (System.IO.File.Exists("basesaves/red/manip/starter/route1/starter_" + targetFrames[0] + "_0_0.gqs"))
            return;
        System.IO.Directory.CreateDirectory("basesaves/red/manip/starter/route1");
        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];
        if (numThreads == 1)
            gb.Record("test");
        MultiThread.For(targetFrames.Count, gbs, (gb, f) =>
        {
            gb.LoadState(State);
            gb.HardReset();
            intro.ExecuteUntilIGT(gb);
            gb.CpuWrite("wPlayTimeMinutes", 0);
            gb.CpuWrite("wPlayTimeSeconds", (byte)igtSec);
            gb.CpuWrite("wPlayTimeFrames", (byte)targetFrames[f]);
            intro.ExecuteAfterIGT(gb);
            gb.Execute(SpacePath("S_BA"));
            gb.ClearText(Joypad.B);
            gb.Press(Joypad.A);
            gb.ClearText(Joypad.B);
            gb.Press(Joypad.A);
            gb.RunUntil("_Joypad");
            gb.AdvanceFrame();
            gb.AdvanceFrames(renameDelay);
            gb.Press(Joypad.A);
            gb.Press(Joypad.Start);
            gb.Hold(Joypad.B, gb.SYM["WaitForTextScrollButtonPress"]);
            gb.RunUntil("Joypad");
            byte[] bulbaState = gb.SaveState();
            for (byte i = 0; i < minClusterSize; i++)
            {
                gb.LoadState(bulbaState);
                gb.AdvanceFrames(i);
                gb.Press(Joypad.B);
                gb.ClearText(Joypad.B);
                gb.Execute(SpacePath(path));
                for (int j = 0; j < 3; j++)
                {
                    gb.Hold(Joypad.A, "WaitForTextScrollButtonPress");
                    gb.Press(Joypad.B);
                }
            }
        });
        
    }
    public static void findSquirtles(RbyIntroSequence intro, int numThreads = 12, int numFrames = 60, int path = 0, int igtFrameCluster = 3, int minClusterSize = 2, int maxturns = 6)
    {
        StringBuilder trace = new StringBuilder();
        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];
        if (numThreads == 1)
            gb.Record("test");
        //Elapsed("threads");
        IGTResult[] states = new IGTResult[numFrames];
        for (int i = 0; i < numFrames; i++)
            states[i] = new IGTResult();
        MultiThread.For(numFrames, gbs, (gb, f) =>
        {
            if (numFrames >= 100 && (f + 1) * 100 / numFrames > f * 100 / numFrames) Console.WriteLine("%");
            if (IgnoredFrames.Contains(f % 60)) { return; }
            gb.LoadState(State);
            gb.HardReset();
            intro.ExecuteUntilIGT(gb);
            gb.CpuWrite("wPlayTimeMinutes", 0);
            gb.CpuWrite("wPlayTimeSeconds", (byte)igtSec);
            gb.CpuWrite("wPlayTimeFrames", (byte)f);
            intro.ExecuteAfterIGT(gb);
            gb.Execute(SpacePath("S_BA"));
            gb.ClearText(Joypad.B);
            gb.Press(Joypad.A);
            gb.ClearText(Joypad.B);
            gb.Press(Joypad.A);
            gb.RunUntil("_Joypad");
            gb.AdvanceFrame();
            gb.AdvanceFrames(path);
            gb.Press(Joypad.A);
            gb.Press(Joypad.Start);
            gb.RunUntil(gb.SYM["_AddPartyMon.next4"] + 0x8);
            var dvs = gb.CpuReadBE<ushort>("wPartyMon1DVs");
            int d1 = (dvs >> 12) & 0xF; // highest hex digit
            int d2 = (dvs >> 8) & 0xF;
            int d3 = (dvs >> 4) & 0xF;
            int d4 = dvs & 0xF; // lowest hex digit

            int result =
                ((d1 % 2) << 3) | // 8's place if odd
                ((d2 % 2) << 2) | // 4's place if odd
                ((d3 % 2) << 1) | // 2's place if odd
                (d4 % 2);         // 1's place if odd
            states[f].Info = result.ToString("X") + dvs.ToString("X4");
            //Console.WriteLine($"Frame {f} DVs: {dvs:X4} Info: {states[f].Info}");
        });
        List<List<int>> clusters = FindValidClusters(states, igtFrameCluster);
        foreach (var cluster in clusters)
        {
            Console.WriteLine($"Found valid cluster: {string.Join(",", cluster)} Stat: {states[cluster[0]].Info}");
            CheckFight(cluster, path, intro, states[cluster[0]].Info, numThreads: numThreads, minClusterSize: minClusterSize, igtFrameCluster: igtFrameCluster, maxturns: maxturns);
        }

    }
    private static List<List<int>> FindValidClusters(IGTResult[] states, int igtFrameCluster)
    {
        List<List<int>> clusters = new List<List<int>>();
        List<int> currentCluster = new List<int>();
        string currentStat = null;

        for (int i = 0; i < states.Length; i++)
        {
            string info = states[i].Info;
            if (info.Length < 2) continue;
            char d4 = info[info.Length - 1];
            char d1 = info[1];
            bool isGood = ((d4 >= '6' && d4 <= '9') || (d4 >= 'A' && d4 <= 'F')) &&
                          ((d1 >= '2' && d1 <= '9') || (d1 >= 'A' && d1 <= 'F'));

            if (!isGood) continue;

            if (currentCluster.Count == 0)
            {
                currentCluster.Add(i);
                currentStat = info;
            }
            else if (info == currentStat && i == currentCluster[currentCluster.Count - 1] + 1)
            {
                currentCluster.Add(i);
            }
            else
            {
                if (currentCluster.Count >= igtFrameCluster)
                    clusters.Add(new List<int>(currentCluster));
                currentCluster.Clear();
                currentCluster.Add(i);
                currentStat = info;
            }
        }
        if (currentCluster.Count >= igtFrameCluster)
            clusters.Add(currentCluster);

        if (clusters.Count > 1)
        {
            var first = clusters[0];
            var last = clusters[clusters.Count - 1];
            string firstStat = states[first[0]].Info;
            string lastStat = states[last[0]].Info;

            if (firstStat == lastStat && last[last.Count - 1] == 59 && first[0] == 0)
            {
                // Merge last and first clusters
                var merged = new List<int>(last);
                merged.AddRange(first);
                clusters[0] = merged;
                clusters.RemoveAt(clusters.Count - 1);
            }
        }
        return clusters;
    }
    public static void CheckFight(List<int> cluster, int path, RbyIntroSequence intro, string stats, int numThreads = 12, int minClusterSize = 2, int maxturns = 6, int igtFrameCluster = 3, string spacePath = null, bool saveStates = false)
    {
        List<string> spacePaths = new List<string>();
        if (spacePath != null)
        {
            spacePaths = new List<string> { spacePath };
        }
        else
        {
            spacePaths = new List<string> { "LLDD", "LALDAD", "LALDD", "LLADD", "LLDAD", "LDLD", "LADLAD", "LADLD", "LDALD", "LDLAD", "DLLD", "DALLAD", "DALLD", "DLALD", "DLLAD" };
        }


        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];
        if (intro == null)
            intro = new RbyIntroSequence(RbyStrat.NoPal);
        if (numThreads == 1)
            gb.Record("test");
        foreach (var sp in spacePaths)
        {
            Console.WriteLine($"{sp}");
            IGTResult[] states = new IGTResult[cluster.Count];
            for (int i = 0; i < cluster.Count; i++)
            {
                states[i] = new IGTResult();
                states[i].IGTFrame = (byte)cluster[i];
                states[i].Success = false;
                states[i].Turns = 0;
                states[i].dmgTaken = new List<int>();
            }
            MultiThread.For(cluster.Count(), gbs, (gb, f) =>
                {
                    gb.LoadState(State);
                    gb.HardReset();
                    intro.ExecuteUntilIGT(gb);
                    gb.CpuWrite("wPlayTimeMinutes", 0);
                    gb.CpuWrite("wPlayTimeSeconds", (byte)igtSec);
                    gb.CpuWrite("wPlayTimeFrames", (byte)cluster[f]);
                    intro.ExecuteAfterIGT(gb);
                    gb.Execute(SpacePath("S_BA"));
                    gb.ClearText(Joypad.B);
                    gb.Press(Joypad.A);
                    gb.ClearText(Joypad.B);
                    gb.Press(Joypad.A);
                    gb.RunUntil("_Joypad");
                    gb.AdvanceFrame();
                    gb.AdvanceFrames(path);
                    gb.Press(Joypad.A);
                    gb.Press(Joypad.Start);
                    gb.Hold(Joypad.B, gb.SYM["WaitForTextScrollButtonPress"]);
                    gb.RunUntil("Joypad");

                    byte[] bulbaState = gb.SaveState();


                    for (byte i = 0; i < minClusterSize; i++)
                    {
                        gb.LoadState(bulbaState);
                        gb.AdvanceFrames(i);
                        gb.Press(Joypad.B);
                        gb.ClearText(Joypad.B);
                        gb.Execute(SpacePath(sp));
                        for (int j = 0; j < 3; j++)
                        {
                            gb.Hold(Joypad.A, "WaitForTextScrollButtonPress");
                            gb.Press(Joypad.B);
                        }
                        gb.Hold(Joypad.A, "WaitForTextScrollButtonPress");
                        gb.RunUntil("Joypad");
                        byte[] fightState = gb.SaveState();
                        for (int k = 0; k < minClusterSize; k++)
                        {
                            gb.LoadState(fightState);
                            gb.AdvanceFrames(k);
                            gb.Press(Joypad.B);
                            gb.Hold(Joypad.A, "WaitForTextScrollButtonPress");
                            gb.Press(Joypad.B);
                            gb.Hold(Joypad.B, "HandleMenuInput");
                            //turn 1
                            gb.MenuPress(Joypad.A);
                            gb.MenuPress(Joypad.Down);
                            gb.MenuPress(Joypad.A);
                            var retSpeedTieT1 = gb.Hold(Joypad.A, gb.SYM["MainInBattleLoop.enemyMovesFirst"], gb.SYM["MainInBattleLoop.playerMovesFirst"]);
                            if (retSpeedTieT1 == gb.SYM["MainInBattleLoop.enemyMovesFirst"]) // enemy moves first
                            {
                                if (gb.CpuRead("wEnemySelectedMove") == 33) // use tackle
                                {
                                    var retTack = gb.Hold(Joypad.A, gb.SYM["UpdateHPBar"], gb.SYM["MoveHitTest.moveMissed"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB);
                                    var retTW = 0;
                                    if (retTack == gb.SYM["UpdateHPBar"])
                                    {
                                        retTW = gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"], gb.SYM["MoveHitTest.moveMissed"]);
                                    }
                                    else if (retTack == gb.SYM["MoveHitTest.moveMissed"])
                                    {
                                        gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"]);
                                        gb.Press(Joypad.B);
                                        retTW = gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"], gb.SYM["MoveHitTest.moveMissed"]);
                                    }
                                    else if (retTack == gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB)
                                    {
                                        Console.WriteLine($"{cluster[f]} {(i * 2) + k} Tackle crit t1");
                                        return; // tackle crit
                                    }
                                    if (retTW == gb.SYM["MoveHitTest.moveMissed"])
                                    {
                                        Console.WriteLine($"{cluster[f]} {(i * 2) + k} tw miss t1");
                                        return; // TW miss 
                                    }
                                    gb.Press(Joypad.B);
                                }
                                else // use growl
                                {
                                    //if (gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"], gb.SYM["MoveHitTest.moveMissed"]) == gb.SYM["WaitForTextScrollButtonPress"])
                                    //{
                                    //    Console.WriteLine($"{(i * 2) + k} growl t1 before tackle");
                                    //    return;// Growl hit before first tackle
                                    //}
                                    gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"]);
                                    gb.Press(Joypad.B);
                                    if (gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"], gb.SYM["MoveHitTest.moveMissed"]) == gb.SYM["MoveHitTest.moveMissed"])
                                    {
                                        Console.WriteLine($"{cluster[f]} {(i * 2) + k} tw miss t1");
                                        return;// TW miss
                                    }
                                    gb.Press(Joypad.B);
                                }
                            }
                            else // player moves first
                            {
                                if (gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"], gb.SYM["MoveHitTest.moveMissed"]) == gb.SYM["MoveHitTest.moveMissed"])
                                {
                                    Console.WriteLine($"{(i * 2) + k} tw miss t1");
                                    return; // TW miss
                                }
                                gb.Press(Joypad.B);
                                if (gb.CpuRead("wEnemySelectedMove") == 33) // use tackle
                                {
                                    if (gb.Hold(Joypad.A, gb.SYM["UpdateHPBar"], gb.SYM["MoveHitTest.moveMissed"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB) == gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB)
                                    {
                                        Console.WriteLine($"{cluster[f]} {(i * 2) + k} tackle crit t1");
                                        return; // tackle crit
                                    }
                                    if (gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"], gb.SYM["HandleMenuInput"]) == gb.SYM["WaitForTextScrollButtonPress"])
                                    {
                                        gb.Press(Joypad.B);
                                        gb.Hold(Joypad.A, "HandleMenuInput");
                                    }
                                }
                                else // use growl
                                {
                                    //if (gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"], gb.SYM["MoveHitTest.moveMissed"]) == gb.SYM["WaitForTextScrollButtonPress"])
                                    //{
                                    //    Console.WriteLine($"{cluster[f]} {(i * 2) + k} growl t1 before tackle");
                                    //    return; // Growl hit before first tackle
                                    //}

                                    gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"]);
                                    gb.Press(Joypad.B);
                                }
                            }
                            // turn 2
                            gb.MenuPress(Joypad.A);
                            gb.MenuPress(Joypad.Down);
                            gb.MenuPress(Joypad.A);
                            bool goodEnding = false;
                            for (int t = 2; t <= maxturns; t++)
                            { // max x turn fight allowed
                                var retSpeedTie = gb.Hold(Joypad.A, gb.SYM["MainInBattleLoop.enemyMovesFirst"], gb.SYM["MainInBattleLoop.playerMovesFirst"]);
                                if (retSpeedTie == gb.SYM["MainInBattleLoop.enemyMovesFirst"]) // enemy moves first
                                {
                                    if (gb.CpuRead("wEnemySelectedMove") == 33) // use tackle
                                    {
                                        var retEnemyTack = gb.Hold(Joypad.A, gb.SYM["UpdateHPBar"], gb.SYM["MoveHitTest.moveMissed"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB);
                                        if (retEnemyTack == gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB)
                                        {
                                            Console.WriteLine($"{cluster[f]} {(i * 2) + k} enemy tackle crit t{t}");
                                            return; // enemy tackle crit
                                        }
                                        else if (retEnemyTack == gb.SYM["MoveHitTest.moveMissed"])
                                        {
                                            gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"]);
                                            gb.Press(Joypad.B);
                                        }
                                        var retPlayerTack = gb.Hold(Joypad.B, gb.SYM["MoveHitTest.moveMissed"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, gb.SYM["HandleMenuInput"], gb.SYM["HandleEnemyMonFainted"], gb.SYM["HandlePlayerMonFainted"]);
                                        if (retPlayerTack == gb.SYM["MoveHitTest.moveMissed"] || retPlayerTack == (gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB) || retPlayerTack == (gb.SYM["HandlePlayerMonFainted"]))
                                        {
                                            Console.WriteLine($"{cluster[f]} {(i * 2) + k} player tackle miss/crit/isdead t{t}");
                                            return; // player tackle miss or crit
                                        }

                                        else if (retPlayerTack == gb.SYM["HandleEnemyMonFainted"])
                                        {
                                            states[f].Turns += t;
                                            states[f].dmgTaken.Add(t);
                                            t = maxturns + 1; // endfight
                                            goodEnding = true; // signal success
                                            if (saveStates)
                                                gb.SaveState("basesaves/red/manip/starter/starter_" + cluster[f] + "_" + i + "_" + k + ".gqs");
                                            continue;
                                        }
                                    }
                                    else // use growl
                                    {
                                        //if (gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"], gb.SYM["MoveHitTest.moveMissed"]) == gb.SYM["WaitForTextScrollButtonPress"] && t == 2)
                                        //{
                                        //    Console.WriteLine($"{cluster[f]} {(i * 2) + k} growl t2 before tackle");
                                        //    return; // Growl hit before first tackle
                                        //}

                                        gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"]);
                                        gb.Press(Joypad.B);
                                        var retPlayerTack = gb.Hold(Joypad.B, gb.SYM["MoveHitTest.moveMissed"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, gb.SYM["HandleMenuInput"], gb.SYM["HandleEnemyMonFainted"]);
                                        if (retPlayerTack == gb.SYM["MoveHitTest.moveMissed"] || retPlayerTack == (gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB))
                                        {
                                            Console.WriteLine($"{cluster[f]} {(i * 2) + k} player tackle miss/crit t{t}");
                                            return; // player tackle miss or crit
                                        }

                                        else if (retPlayerTack == gb.SYM["HandleEnemyMonFainted"])
                                        {
                                            states[f].Turns += t;
                                            states[f].dmgTaken.Add(t);
                                            t = maxturns + 1; // endfight
                                            goodEnding = true; // signal success
                                            if (saveStates)
                                                gb.SaveState("basesaves/red/manip/starter/starter_" + cluster[f] + "_" + i + "_" + k + ".gqs");
                                            continue;
                                        }
                                    }
                                    gb.MenuPress(Joypad.A);
                                    gb.MenuPress(Joypad.Select);
                                    gb.MenuPress(Joypad.A);
                                }
                                else // player moves first
                                {
                                    var retPlayerTack = gb.Hold(Joypad.A, gb.SYM["MoveHitTest.moveMissed"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, gb.SYM["CheckIfEnemyNeedsToChargeUp"], gb.SYM["HandleEnemyMonFainted"]);
                                    if (retPlayerTack == gb.SYM["MoveHitTest.moveMissed"] || retPlayerTack == (gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB))
                                    {
                                        Console.WriteLine($"{cluster[f]} {(i * 2) + k} player tackle miss/crit t{t}");
                                        return; // player tackle miss or crit
                                    }

                                    else if (retPlayerTack == gb.SYM["HandleEnemyMonFainted"])
                                    {
                                        states[f].Turns += t;
                                        states[f].dmgTaken.Add(t);
                                        t = maxturns + 1; // endfight
                                        goodEnding = true; // signal success
                                        if (saveStates)
                                            gb.SaveState("basesaves/red/manip/starter/starter_" + cluster[f] + "_" + i + "_" + k + ".gqs");
                                        continue;
                                    }
                                    if (gb.CpuRead("wEnemySelectedMove") == 33) // use tackle
                                    {
                                        var retEnemyTack = gb.Hold(Joypad.B, gb.SYM["HandleMenuInput"], gb.SYM["MoveHitTest.moveMissed"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, gb.SYM["HandlePlayerMonFainted"]);
                                        if (retEnemyTack == gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB || retEnemyTack == gb.SYM["HandlePlayerMonFainted"])
                                        {
                                            Console.WriteLine($"{cluster[f]} {(i * 2) + k} enemy tackle crit/killed t{t}");
                                            return; // enemy tackle crit
                                        }
                                        else if (retEnemyTack == gb.SYM["MoveHitTest.moveMissed"])
                                        {
                                            gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"]);
                                            gb.Press(Joypad.B);
                                        }
                                    }
                                    else // use growl
                                    {
                                        gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"]);
                                        gb.Press(Joypad.B);
                                    }
                                    gb.MenuPress(Joypad.A);
                                    gb.MenuPress(Joypad.Select);
                                    gb.MenuPress(Joypad.A);
                                }
                            }
                            if (!goodEnding)
                            {
                                Console.WriteLine($"{cluster[f]} {(i * 2) + k} fight too long");
                                return; // fight not ended in 6 turns
                            }
                        }
                    }
                    states[f].Success = true;
                    Console.WriteLine($"Success frame {cluster[f]} path {sp}");
                });
            var successClusters = FindSuccessClusters(states, igtFrameCluster);
            foreach (var successCluster in successClusters)
            {
                StringBuilder trace = new StringBuilder();
                int startFrame = successCluster[0].IGTFrame;
                int endFrame = successCluster[successCluster.Count - 1].IGTFrame;
                foreach (var s in successCluster)
                {
                    string turns = "";
                    foreach (var t in s.dmgTaken)
                        turns += t.ToString();
                    float avgTurns = s.Turns / (float)(minClusterSize * 2);
                    trace.AppendLine($"Frame: {s.IGTFrame} Stats: {stats} Path: {sp} Turns: {turns}");
                }

                Trace.WriteLine(trace.ToString());
            }
        }

    }
    public static List<List<IGTResult>> FindSuccessClusters(IGTResult[] states, int igtFrameCluster)
    {
        List<List<IGTResult>> clusters = new List<List<IGTResult>>();
        List<IGTResult> current = new List<IGTResult>();
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i].Success)
            {
                if (current.Count == 0 || i == Array.IndexOf(states, current.Last()) + 1)
                {
                    current.Add(states[i]);
                }
                else
                {
                    if (current.Count >= igtFrameCluster)
                        clusters.Add(new List<IGTResult>(current));
                    current.Clear();
                    current.Add(states[i]);
                }
            }
            else
            {
                if (current.Count >= igtFrameCluster)
                    clusters.Add(new List<IGTResult>(current));
                current.Clear();
            }
        }
        if (current.Count >= igtFrameCluster)
            clusters.Add(current);

        return clusters;
    }

    public static void SearchRoute1(RbyIntroSequence intro, int renameDelay, List<int> targetFrames, string path, int numThreads = 12, int numFrames = 1,
        int success = -1, int maxcost = 10, int minClusterSize = 3)
    {
        BuildRoute1States(intro, renameDelay, path, targetFrames);
    }
}