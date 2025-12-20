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

class Surge
{
    const string State = "basesaves/red/manip/surge.gqs";
    const int Second_Lock_Opened_Flag = 352;
    static SortedSet<int> IgnoredFrames = new SortedSet<int> {34, 35, 36, 37};
    public static void Check()
    {
        string path;
        RbyStrat pal;
        // path = "DDDADDADDDDDDDRRARRDADDRDRRDADDDRDDRRRRRRRRRARRRDDDDARRRRRRRRRRRR"; pal = RbyStrat.PalHold;
        // path = "DDDDDDDADDADDDRRARRDDDDRRRRDDDADDDDDARRRRRRARRRRRRDDARRRRRRRRRRRR"; pal = RbyStrat.PalAB;
        // path = "DDDDDDDADDADDDRARRDDDDRRRRDDDDADDDDRARRARRRRRRRRRDDRARRRRRRRRRRRR"; pal = RbyStrat.PalAB;

        // path = "DDDDDDDDDADDDARRRADDDADRRRRDDDDADDDDRRRRRRARRRRRRDRRRARRRS_BRRRRDDDRRUUU"; pal = RbyStrat.Pal; // current 56/60 c168
        // path = "DUUURRDDDDDDDDDDDDRRRRDDDDRRRRDDDDDDRRARRRRARRRARRADDADRARRARRRRRRRR"; pal = RbyStrat.NoPalAB;
        // path = "DUUURRDDDDDDDDDDDDRRRRDDDDRRRARDADDADDDDDRARRARRRRRRRARDRARRRRRRRRRR"; pal = RbyStrat.NoPalAB;
        path = "LLLLLLDLALLDLDDDRRRR"; pal = RbyStrat.NoPalAB; // 57/60 c154
        //CheckIGT(State, new RbyIntroSequence(pal), path, "PARAS", 3600);
    }

    public static void Search()
    {
        for(RbyStrat pal = RbyStrat.NoPal; pal <= RbyStrat.NoPal; pal++)
            for(RbyStrat gf = RbyStrat.GfSkip; gf <= RbyStrat.GfSkip; ++gf)
                for(RbyStrat hop = RbyStrat.Hop0; hop <= RbyStrat.Hop0; ++hop)
                    for(int backouts = 0; backouts <= 0; ++backouts)
                        SearchRightCan(new RbyIntroSequence(pal, gf, hop, backouts), 12, 60, 57);
    }

    public static void BuildStates()
    {
        if(System.IO.File.Exists("basesaves/red/manip/surge/surge0_0.gqs"))
            return;
        System.IO.Directory.CreateDirectory("basesaves/red/manip/surge");
        const int numThreads = 12;
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.NoPal);
        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];
        if(numThreads == 1) gb.Record("test");

        gb.LoadState(State);
        gb.HardReset();
        intro.ExecuteUntilIGT(gb);
        
        byte[] igtState = gb.SaveState();

        const int numFrames = 3600;
        MultiThread.For(numFrames, gbs, (gb, f) =>
        {
            if((f + 1) * 100 / numFrames > f * 100 / numFrames) Console.WriteLine("%");

            gb.LoadState(igtState);
            byte sec = (byte) (f / 60);
            byte frame = (byte) (f % 60);


            gb.CpuWrite("wPlayTimeMinutes", 41);
            gb.CpuWrite("wPlayTimeSeconds", sec);
            gb.CpuWrite("wPlayTimeFrames", frame);
            intro.ExecuteAfterIGT(gb);

            //if (f == 4)
            //{
            //    gb.CpuWriteBE<ushort>("wPartyMon1HP", (byte)8);
            //    gb.SaveState("basesaves/red/manip/surgealthp.gqs");
            //    gb.CpuWriteBE<ushort>("wPartyMon1HP", (byte)11);
            //}
            

            //string can1path = "DLLLUUURRLAUUUA";
            //int ret;
            //ret = gb.Execute(SpacePath(can1path));
            //gb.Hold(Joypad.A, "WaitForTextScrollButtonPress");
            //gb.Press(Joypad.B);
            //gb.Hold(Joypad.A, "WaitForTextScrollButtonPress");
            //gb.Press(Joypad.B);
            //gb.Hold(Joypad.A, "WaitForTextScrollButtonPress");
            //gb.Press(Joypad.B);
            //gb.Hold(Joypad.A, "WaitForTextScrollButtonPress");
            //gb.Press(Joypad.B);
            //gb.Execute(SpacePath("RRDUA"));
            //gb.Hold(Joypad.A, "WaitForTextScrollButtonPress");
            //gb.Press(Joypad.B);
            //gb.Hold(Joypad.A, "WaitForTextScrollButtonPress");
            //gb.Press(Joypad.B);
            gb.SaveState("basesaves/red/manip/surge/surge" + sec + "_" + frame +".gqs");
        });
    }
    public static void Search(int maxcost, int numThreads = 12, int numFrames = 56, int success = 56, string path = null, int minClusterSize=3, int igtFrameCluster = 5, int offset60fps=0, bool wantQA = false, bool wantSonicboom = false, string betweenCans= "RRDUA")
    {
        BuildStates();
        StartWatch();
        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];

        IGTResults states = new IGTResults(numFrames);
        MultiThread.For(states.Length, gbs, (gb, i) =>
        {
            int f = i;
            for(int s = 0; s < 60; ++s)
                foreach(int skip in IgnoredFrames)
                    if(f >= skip + 60 * s)
                        ++f;

            gb.LoadState("basesaves/red/manip/surge/surge" + (f / 60) + "_" + (f % 60) + ".gqs");
            if(path != null){
                int ret = gb.Execute(SpacePath(path));}

            states[i] = new IGTState(gb, false, f);
        });

        if(numThreads == 1) gb.Record("test");
        Elapsed("States");
        RbyMap vermilion = gb.Maps[5];
        RbyMap gym = gb.Maps[92];
        Action actions = Action.Right | Action.Left | Action.Up | Action.Down | Action.A | Action.StartB;
        RbyTile startTile = gb.Tile;
        RbyTile firstCanTile = gym[5, 12];
        RbyTile secondCanTile = gym[7,13];
        RbyTile surgeTile = gym[5,2];

        List<RbyTile> blockedTiles = new List<RbyTile>(){
            gym[3,12],
            gym[4,13],
            gym[7,14],
            gym[8,14],
            gym[9,14],
            gym[4,2],
            gym[6,2]
        };

        
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 2, surgeTile, actions, blockedTiles.ToArray());
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 1, secondCanTile, actions, blockedTiles.ToArray());
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, firstCanTile, actions, blockedTiles.ToArray());
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, vermilion[12,20], actions,blockedTiles.ToArray());
        vermilion[12,20].AddEdge(0, new Edge<RbyMap, RbyTile>() { Action = Action.Up, NextTile = gym[4, 17], NextEdgeset = 0, Cost = 0 });


        firstCanTile.RemoveEdge(0, Action.A);
        gym[5, 13].RemoveEdge(0, Action.A); //tile before the can
        
        //firstCanTile.RemoveEdge(1, Action.A); //
        secondCanTile.RemoveEdge(1, Action.A);
        //gym[7, 12].RemoveEdge(1, Action.A);

        //gym[7, 12].RemoveEdge(2, Action.A);
        gym[5, 3].RemoveEdge(2, Action.A);

        firstCanTile.JoinEdgeset(0, Action.Right, 1, gym[6, 12]);
        secondCanTile.JoinEdgeset(1, Action.Left, 2, gym[6, 12]);
        Pathfinding.DebugDrawEdges<RbyMap, RbyTile>(gb, gym, 2);

        RbyTile[] endTiles = { surgeTile };
        ManualResetEventSlim pauseSearch = new ManualResetEventSlim(true);
        List<string> paths = new List<string>();
        var parameters = new DFParameters<Red, RbyMap, RbyTile>()
        {
            MaxCost = maxcost,
            SuccessSS = success >= 0 ? success : Math.Max(1, states.Length - 3),
            EndTiles = endTiles,
            EndEdgeSet = 2,
            //LogStart = startTile.PokeworldLink + "/",
            PauseSearch = pauseSearch,
            FuncCallbacks = new (Tile<RbyMap, RbyTile>, Func<Red,bool>)[] {(firstCanTile, gb => DoFirstCan(gb)),
                                                                      (secondCanTile, gb => DoSecondCan(gb))},
            FoundCallback = state =>
            {
                //if(state.WastedFrames >= 44)
                //{
                //    Trace.WriteLine(path+state.Log);
                //}
                Trace.WriteLine(path+state.Log);
                //pauseSearch.Reset();
                //paths.Add(path+state.Log);
                //CheckIGT(path+state.Log, minhp, maxhp,numThreads:numThreads, minClusterSize:minClusterSize, igtFrameCluster:igtFrameCluster, offset60fps:offset60fps, wantQA:wantQA, wantTackle:wantTackle);
                //pauseSearch.Set();
            }
        };

        ChainDepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states);
        Elapsed("search");
        
        //foreach(var p in paths)
        //{
        //    CheckIGT(p, minhp, maxhp,numThreads:numThreads, minClusterSize:minClusterSize, igtFrameCluster:igtFrameCluster, offset60fps:offset60fps, wantQA:wantQA, wantTackle:wantTackle);
        //}
        //Elapsed("check");
    }
    public static bool DoFirstCan(Red gb)
    {
        //Console.WriteLine($"First can index: {gb.CpuRead("wFirstLockTrashCanIndex")} Second can index: {gb.CpuRead("wSecondLockTrashCanIndex")}");
        if (CheckEventFlag(gb,Second_Lock_Opened_Flag))
            return true;
        gb.Press(Joypad.A);
        gb.Hold(Joypad.A, "WaitForTextScrollButtonPress");
        gb.Press(Joypad.B);
        gb.Hold(Joypad.A, "WaitForTextScrollButtonPress");
        gb.Press(Joypad.B);
        gb.Hold(Joypad.A, "WaitForTextScrollButtonPress");
        gb.Press(Joypad.B);
        gb.Hold(Joypad.A, "WaitForTextScrollButtonPress");
        gb.Press(Joypad.B);
        return gb.CpuRead("wSecondLockTrashCanIndex") == 11;
    }
    public static bool DoSecondCan(Red gb)
    {
        if (CheckEventFlag(gb,Second_Lock_Opened_Flag))
            return true;
        gb.Execute(SpacePath("UA"));
        gb.Hold(Joypad.A, "WaitForTextScrollButtonPress");
        gb.Press(Joypad.B);
        gb.Hold(Joypad.A, "WaitForTextScrollButtonPress");
        gb.Press(Joypad.B);
        gb.Maps[92][7,12].RemoveEdge(1, Action.A); // remove can edge
        return true;
    }
    private static bool CheckEventFlag(Red gb, int flag) {
        int offs = flag / 8;
        int bit = flag % 8;
        var ret = (gb.CpuRead(gb.SYM["wEventFlags"] + offs) & (1 << bit)) > 0;
        return (gb.CpuRead(gb.SYM["wEventFlags"] + offs) & (1 << bit)) > 0;
    }
    public static void CheckPathsInFile(int hp, string filename, int numThreads=12)
    {
        BuildStates();
        RedCb[] gbs = MultiThread.MakeThreads<RedCb>(numThreads);
        using (var reader = new StreamReader(filename))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line)) {
                    continue;
                }
                Console.WriteLine("Checking path: " + line);
                CheckIGT(line, hp, hp, gbs, numThreads, minClusterSize:3, igtFrameCluster:4, offset60fps:0,wantQA:(12 <= hp && hp <= 20)||(32 <= hp), wantSonicboom:hp>20);
                //GC.Collect();
            }
        }
    }
    public static void CheckExe()
    {
        RedCb gb = new RedCb();
        gb.Record("test");
        gb.LoadState("basesaves/red/manip/surge/surge0_0.gqs");
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.NoPal);
        gb.LoadState(State);
        gb.HardReset();
        intro.ExecuteUntilIGT(gb);
        gb.CpuWrite("wPlayTimeMinutes", 51);
        gb.CpuWrite("wPlayTimeSeconds", 44);
        gb.CpuWrite("wPlayTimeFrames", 3);
        intro.ExecuteAfterIGT(gb);
        string can1path = "DLLLUUURRLUAUUA";
        int ret;
        ret = gb.Execute(SpacePath(can1path));
        gb.Hold(Joypad.A, "WaitForTextScrollButtonPress");
        gb.Press(Joypad.B);
        gb.Hold(Joypad.A, "WaitForTextScrollButtonPress");
        gb.Press(Joypad.B);
        gb.Hold(Joypad.A, "WaitForTextScrollButtonPress");
        gb.Press(Joypad.B);
        gb.Hold(Joypad.A, "WaitForTextScrollButtonPress");
        gb.Press(Joypad.B);
        gb.Execute(SpacePath("RRDUA"));
        gb.Hold(Joypad.A, "WaitForTextScrollButtonPress");
        gb.Press(Joypad.B);
        gb.Hold(Joypad.A, "WaitForTextScrollButtonPress");
        gb.Press(Joypad.B);
        gb.Execute(SpacePath("RD"));
        Console.WriteLine("wFirstLockTrashCanIndex: " + gb.CpuRead("wFirstLockTrashCanIndex"));
    }
    public static void CheckIGT(string path, int minhp, int maxhp, RedCb[] gbs = null, int numThreads = 12, int numFrames = 56, bool verbose = true, List<int> targetFrames = null, List<int> targetSecs = null, int minClusterSize = 3, int igtFrameCluster = 5, int offset60fps = 0, bool wantQA = false, bool wantSonicboom = false)
    {
        StringBuilder trace = new StringBuilder();
        List<IGTResult> results = null;
        trace.AppendLine("https://gunnermaniac.com/pokeworld?local=92#7/17/" + path);
        if(gbs==null)
            gbs = MultiThread.MakeThreads<RedCb>(numThreads);
        if (!wantSonicboom)
        {
            results = CheckFight(path, minhp, maxhp, gbs, numThreads, numFrames, verbose, targetFrames, targetSecs, minClusterSize, offset60fps: offset60fps, wantQA: wantQA);
        }
        else
        {
            results = CheckTackleQAFight(path, minhp, maxhp, gbs, numThreads, numFrames, verbose, targetFrames, targetSecs, minClusterSize, offset60fps: offset60fps, wantQA: wantQA);
        }
        List<int> goodFrames = new List<int>();
        for (int i = 0; i < 60; i++)
        {
            if (IgnoredFrames.Contains(i)) { continue; }
            foreach (var res in results.Where(res => res.IGTFrame == i))
            {
                goodFrames.Add(i);
            }
        }
        if (goodFrames.Count > 0)
        {
            int count = 1;
            int start = goodFrames[0];
            List<(int, int)> intPairs = new List<(int, int)>();
            for (int i = 1; i < goodFrames.Count; i++)
            {
                if (goodFrames[i] == goodFrames[i - 1] + 1)
                {
                    count++;
                }
                else
                {
                    if (count >= igtFrameCluster)
                        intPairs.Add((count, start+count));
                    start = goodFrames[i];
                    count = 1;
                }
            }
            // Check the last cluster
            if (count >= igtFrameCluster)
                intPairs.Add((count, start+count));
            List<int> targetCluster = new List<int>();
            foreach ((int, int) rawcluster in intPairs)
            {
                for (int i = rawcluster.Item2 - rawcluster.Item1; i < rawcluster.Item2; i++)
                {
                    targetCluster.Add(i);
                }
            }
            if (targetCluster.Count >= igtFrameCluster)
            {
                List<RbyIGTChecker<Red>.IGTResult> igtSecResults = null;
                if (!wantSonicboom)
                {
                    igtSecResults = CheckFight(path, minhp, maxhp, gbs, numThreads, numFrames: 3600, verbose, targetFrames: targetCluster, targetSecs, minClusterSize, offset60fps, wantQA: wantQA);
                }
                else
                {
                    igtSecResults = CheckTackleQAFight(path, minhp, maxhp, gbs, numThreads, numFrames: 3600, verbose, targetFrames: targetCluster, targetSecs, minClusterSize, offset60fps, wantQA: wantQA);
                }
                foreach (int i in targetCluster)
                    {
                        if (IgnoredFrames.Contains(i)) { continue; }
                        List<int> seconds = Enumerable.Range(0, 60).ToList();
                        int successcount = 0;
                        int crits = 0;
                        int qaDeaths = 0;
                        int dmgTaken = 0;
                        int threeturns = 0;
                        foreach (var res2 in igtSecResults.Where(res2 => res2.IGTFrame == i))
                        {
                            successcount++;
                            seconds.Remove(res2.IGTSec);
                            crits += res2.Crits;
                            qaDeaths += res2.qaDeaths;
                            dmgTaken += res2.dmgTaken[0];
                            threeturns += res2.threeTurn;
                        }
                        if (successcount >= 0)
                        {
                            string badSecs = string.Join(", ", seconds);
                            trace.AppendLine("Frame: " + i + " Success: " + successcount + $"/60 IGT seconds ({badSecs}). Raichu crits: " + crits + $"/{successcount * 3 * (maxhp - minhp + 1)} QA crits: " + qaDeaths + " Avg Dmg taken: " + (float)dmgTaken / (float)(successcount * minClusterSize * (maxhp - minhp + 1)));

                        }
                    }

                Trace.WriteLine(trace.ToString());

                //Trace.WriteLine(startTile.PokeworldLink + "/" + state.Log);
            }
        }
    }
    
    public static List<IGTResult> CheckFight(string path, int minhp, int maxhp, RedCb[] gbs = null, int numThreads = 12, int numFrames = 60, bool verbose = true, List<int> targetFrames = null, List<int> targetSecs = null, int minClusterSize = 3, int offset60fps = 0, bool wantQA = false){
        
        List<IGTResult> results = new List<IGTResult>();
        if(gbs == null)
            gbs = MultiThread.MakeThreads<RedCb>(numThreads);
        if(numThreads == 1){
            gbs[0].Record("test");
        }

        MultiThread.For(numFrames, gbs, (gb, f) =>
        {
            RbyTile firstCanTile = gb.Maps[92][5, 12];
            RbyTile secondCanTile = gb.Maps[92][7, 13];
            if(verbose && numFrames >= 100 && (f + 1) * 100 / numFrames > f * 100 / numFrames) Console.WriteLine("%");
            if(IgnoredFrames.Contains(f % 60)){return;}

            if(targetFrames!=null && !targetFrames.Contains(f % 60)){return;}
            if(targetSecs!=null && !targetSecs.Contains(f / 60)){return;}
            IGTResult res = new IGTResult();
            res.Crits=0;
            res.qaDeaths=0;
            res.dmgTaken = new List<int>{0};
            res.Success=false;
            res.threeTurn= 0;
            res.IGTSec = (byte) (f / 60);
            res.IGTFrame = (byte) (f % 60);
            
            string state = "basesaves/red/manip/surge/surge" + res.IGTSec + "_" + res.IGTFrame + ".gqs";
            
            for(byte currentHP=(byte)minhp;currentHP<=maxhp;currentHP++)
            {   
                if(System.IO.File.Exists(state)){gb.LoadState(state);}
                else{return;} 
                gb.CpuWriteBE<ushort>("wPartyMon1HP", (byte)currentHP);
                if (res.IGTSec == 0 && res.IGTFrame == 32 && currentHP == 14)
                {
                    gb.SaveState("basesaves/red/manip/surge" + res.IGTSec + "_" + res.IGTFrame + "_" + currentHP + ".gqs");
                    //gb.Record("test");
                }
                var tryPath = gb.TryExecute(SpacePath(path), (firstCanTile, ()=>gb.DoFirstCan()),(secondCanTile, ()=>gb.DoSecondCan()));  
                if(!tryPath){return;}   
                gb.Press(Joypad.A);
                for(int i =0; i<9;i++){
                    gb.Hold(Joypad.A,"WaitForTextScrollButtonPress");
                    gb.Press(Joypad.B);
                }
                gb.Hold(Joypad.A,"WaitForTextScrollButtonPress");
                gb.RunUntil("Joypad");

                byte[] initialState = gb.SaveState();

                for (byte i = 0; i < minClusterSize; i++)
                {
                    gb.LoadState(initialState);
                    gb.AdvanceFrames(i+offset60fps);
                    gb.Press(Joypad.B);
                    gb.Hold(Joypad.A,"WaitForTextScrollButtonPress");
                    gb.Press(Joypad.B);
                    gb.Hold(Joypad.B,"HandleMenuInput");

                    gb.MenuPress(Joypad.A);
                    gb.MenuPress(Joypad.Select);
                    gb.MenuPress(Joypad.A);

                    var retV = gb.Hold(Joypad.A,gb.SYM["HandleEnemyMonFainted"],gb.SYM["MoveHitTest.moveMissed"],gb.SYM["CriticalHitTest.SkipHighCritical"]+0xB,gb.SYM["CheckIfEnemyNeedsToChargeUp"],gb.SYM["WaitForTextScrollButtonPress"]);
                    if(retV!=(int)gb.SYM["HandleEnemyMonFainted"]){
                        return;
                    }

                    if(gb.CpuRead("wPlayerNumAttacksLeft") == 2
                     //&& i + offset60fps != 0
                     ){
                        return;
                    }
                    else if (gb.CpuRead("wPlayerNumAttacksLeft")==2){ 
                        res.threeTurn++;
                    }

                    gb.Hold(Joypad.A,gb.SYM["WaitForTextScrollButtonPress"]);
                    gb.Press(Joypad.B);
                    gb.Hold(Joypad.A,gb.SYM["WaitForTextScrollButtonPress"]);
                    gb.Press(Joypad.B);

                    gb.Hold(Joypad.A,gb.SYM["SelectEnemyMove.done"] + 0x3);
                    if (gb.CpuRead("wEnemySelectedMove") == 98)
                    { //QA
                        var retXspeed = gb.Hold(Joypad.A, gb.SYM["CalculateDamage"], gb.SYM["WaitForTextScrollButtonPress"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB);
                        if (retXspeed == (int)gb.SYM["CalculateDamage"] || retXspeed == (int)gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB
                        //&& i + offset60fps != 0
                        )
                        {
                            if (!wantQA)
                            {
                                return; // bad path if you do NOT want QA
                            }
                            if (retXspeed == (int)gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB)
                            {
                                res.qaDeaths++; // using this to count crits
                                gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"]);
                                res.dmgTaken[0] += currentHP - gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]);
                                continue; // got crit, still ok
                            }
                        }
                        else if (wantQA)
                        {
                            return; // wanted QA but got xspeed
                        }
                        else
                        {
                            //Console.WriteLine("xspeed");
                        }
                    }
                    if(!wantQA && gb.CpuRead("wEnemySelectedMove") != 98 || wantQA)
                    {
                        if(wantQA && gb.CpuRead("wEnemySelectedMove") != 98){
                            return; //wanted qa but got growl
                        }
                        var retP = gb.Hold(Joypad.A, gb.SYM["HandleEnemyMonFainted"], gb.SYM["MoveHitTest.moveMissed"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, gb.SYM["CheckIfEnemyNeedsToChargeUp"], gb.SYM["WaitForTextScrollButtonPress"]);
                        res.dmgTaken[0] += currentHP - gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]);
                        if (retP != gb.SYM["HandleEnemyMonFainted"])
                        {
                            //Console.WriteLine("bad retP: " + retP);
                            continue;
                        }
                        gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"]);
                        gb.Press(Joypad.B);
                        gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"]);
                        gb.Press(Joypad.B);

                        var retR = gb.Hold(Joypad.A, gb.SYM["HandleEnemyMonFainted"], gb.SYM["MoveHitTest.moveMissed"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, gb.SYM["CheckIfEnemyNeedsToChargeUp"], gb.SYM["WaitForTextScrollButtonPress"]);
                        //Console.WriteLine("retR: " + retR);
                        
                        gb.RunUntil(gb.SYM["WaitForTextScrollButtonPress"]);
                        
                        if (retR == gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB)
                        {
                            gb.RunUntil(gb.SYM["WaitForTextScrollButtonPress"]);
                            res.Crits++;
                        }
                    }

                }
            }
            res.Success = true;
            lock(results)
                results.Add(res); 
                          
        });
        
        return results;
    
    }
    public static List<IGTResult> CheckSonicboomFight(string path, int minhp, int maxhp, int numThreads = 12, int numFrames = 60, bool verbose = true, List<int> targetFrames = null, List<int> targetSecs = null, int minClusterSize = 3, int offset60fps = 0, bool wantQA = false){
        //DEPRECATED
        List<IGTResult> results = new List<IGTResult>();
        RedCb[] gbs = MultiThread.MakeThreads<RedCb>(numThreads);
        if(numThreads == 1){
            gbs[0].Record("test");
        }

        MultiThread.For(numFrames, gbs, (gb, f) =>
        {
            if(verbose && numFrames >= 100 && (f + 1) * 100 / numFrames > f * 100 / numFrames) Console.WriteLine("%");
            if(IgnoredFrames.Contains(f % 60)){return;}

            if(targetFrames!=null && !targetFrames.Contains(f % 60)){return;}
            if(targetSecs!=null && !targetSecs.Contains(f / 60)){return;}
            IGTResult res = new IGTResult();
            res.Crits=0;
            res.qaDeaths=0;
            res.dmgTaken = new List<int>{0};
            res.Success=false;
            res.threeTurn= 0;
            res.IGTSec = (byte) (f / 60);
            res.IGTFrame = (byte) (f % 60);
            
            string state = "basesaves/red/manip/surge/surge" + res.IGTSec + "_" + res.IGTFrame + ".gqs";
            
            for(byte currentHP=(byte)minhp;currentHP<=maxhp;currentHP++)
            {   
                if(System.IO.File.Exists(state)){gb.LoadState(state);}
                else{return;} 
                gb.CpuWriteBE<ushort>("wPartyMon1HP", (byte)currentHP);
                if (res.IGTSec == 0 && res.IGTFrame == 32 && currentHP == 14)
                {
                    gb.SaveState("basesaves/red/manip/surge" + res.IGTSec + "_" + res.IGTFrame + "_" + currentHP + ".gqs");
                    //gb.Record("test");
                }
                int address = gb.Execute(SpacePath(path));            
                gb.Press(Joypad.A);
                for(int i =0; i<9;i++){
                    gb.Hold(Joypad.A,"WaitForTextScrollButtonPress");
                    gb.Press(Joypad.B);
                }
                gb.Hold(Joypad.A,"WaitForTextScrollButtonPress");
                gb.RunUntil("Joypad");

                byte[] initialState = gb.SaveState();

                for (byte i = 0; i < minClusterSize; i++)
                {
                    gb.LoadState(initialState);
                    gb.AdvanceFrames(i+offset60fps);
                    gb.Press(Joypad.B);
                    gb.Hold(Joypad.A,"WaitForTextScrollButtonPress");
                    gb.Press(Joypad.B);
                    gb.Hold(Joypad.B,"HandleMenuInput");

                    gb.MenuPress(Joypad.A);
                    gb.MenuPress(Joypad.Up);
                    gb.MenuPress(Joypad.A);

                    var retV = gb.Hold(Joypad.A,gb.SYM["HandleEnemyMonFainted"],gb.SYM["MoveHitTest.moveMissed"],gb.SYM["CriticalHitTest.SkipHighCritical"]+0xB,gb.SYM["CheckIfEnemyNeedsToChargeUp"],gb.SYM["WaitForTextScrollButtonPress"]);
                    if(retV!=(int)gb.SYM["HandleEnemyMonFainted"]){
                        return;
                    }

                    if(gb.CpuRead("wPlayerNumAttacksLeft") == 2
                     //&& i + offset60fps != 0
                     ){
                        return;
                    }
                    else if (gb.CpuRead("wPlayerNumAttacksLeft")==2){ 
                        res.threeTurn++;
                    }

                    gb.Hold(Joypad.A,gb.SYM["WaitForTextScrollButtonPress"]);
                    gb.Press(Joypad.B);
                    gb.Hold(Joypad.A,gb.SYM["WaitForTextScrollButtonPress"]);
                    gb.Press(Joypad.B);

                    gb.Hold(Joypad.A,gb.SYM["SelectEnemyMove.done"] + 0x3);
                    if (gb.CpuRead("wEnemySelectedMove") == 98)
                    { //QA
                        var retXspeed = gb.Hold(Joypad.A, gb.SYM["CalculateDamage"], gb.SYM["WaitForTextScrollButtonPress"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB);
                        if (retXspeed == (int)gb.SYM["CalculateDamage"] || retXspeed == (int)gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB
                        //&& i + offset60fps != 0
                        )
                        {
                            if (!wantQA)
                            {
                                return; // bad path if you do NOT want QA
                            }
                            if (retXspeed == (int)gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB)
                            {
                                res.qaDeaths++; // using this to count crits
                                gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"]);
                                res.dmgTaken[0] += currentHP - gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]);
                                continue; // got crit, still ok
                            }
                        }
                        else if (wantQA)
                        {
                            return; // wanted QA but got xspeed
                        }
                        else
                        {
                            //Console.WriteLine("xspeed");
                        }
                    }
                    if(!wantQA && gb.CpuRead("wEnemySelectedMove") != 98 || wantQA)
                    {
                        if(wantQA && gb.CpuRead("wEnemySelectedMove") != 98){
                            return; //wanted qa but got growl
                        }
                        var retP = gb.Hold(Joypad.A, gb.SYM["HandleEnemyMonFainted"], gb.SYM["MoveHitTest.moveMissed"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, gb.SYM["CheckIfEnemyNeedsToChargeUp"], gb.SYM["WaitForTextScrollButtonPress"]);
                        res.dmgTaken[0] += currentHP - gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]);
                        if (retP != gb.SYM["HandleEnemyMonFainted"])
                        {
                            //Console.WriteLine("bad retP: " + retP);
                            continue;
                        }
                        gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"]);
                        gb.Press(Joypad.B);
                        gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"]);
                        gb.Press(Joypad.B);

                        var retR = gb.Hold(Joypad.A, gb.SYM["HandleEnemyMonFainted"], gb.SYM["MoveHitTest.moveMissed"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, gb.SYM["CheckIfEnemyNeedsToChargeUp"], gb.SYM["WaitForTextScrollButtonPress"]);
                        //Console.WriteLine("retR: " + retR);
                        
                        gb.RunUntil(gb.SYM["WaitForTextScrollButtonPress"]);
                        
                        if (retR == gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB)
                        {
                            gb.RunUntil(gb.SYM["WaitForTextScrollButtonPress"]);
                            res.Crits++;
                        }
                    }

                }
            }
            res.Success = true;
            lock(results)
                results.Add(res); 
                          
        });


        return results;
    
    }
    public static List<IGTResult> CheckTackleQAFight(string path, int minhp, int maxhp, RedCb[] gbs = null, int numThreads = 12, int numFrames = 60, bool verbose = true, List<int> targetFrames = null, List<int> targetSecs = null, int minClusterSize = 3, int offset60fps = 0, bool wantQA = false){
        // THIS DOUBLES AS A SONICBOOM MANIP. too lazy to make it smart
        List<IGTResult> results = new List<IGTResult>();
        if(gbs == null)
            gbs = MultiThread.MakeThreads<RedCb>(numThreads);
        if(numThreads == 1){
            gbs[0].Record("test");
        }

        MultiThread.For(numFrames, gbs, (gb, f) =>
        {
            RbyTile firstCanTile = gb.Maps[92][5, 12];
            RbyTile secondCanTile = gb.Maps[92][7, 13];
            if(verbose && numFrames >= 100 && (f + 1) * 100 / numFrames > f * 100 / numFrames) Console.WriteLine("%");
            if(IgnoredFrames.Contains(f % 60)){return;}

            if(targetFrames!=null && !targetFrames.Contains(f % 60)){return;}
            if(targetSecs!=null && !targetSecs.Contains(f / 60)){return;}
            //Console.WriteLine($"Checking IGT Sec {f / 60} Frame {f % 60}");
            IGTResult res = new IGTResult();
            res.Crits=0;
            res.qaDeaths=0;
            res.dmgTaken = new List<int>{0};
            res.Success=false;
            res.threeTurn= 0;
            res.IGTSec = (byte) (f / 60);
            res.IGTFrame = (byte) (f % 60);
            
            string state = "basesaves/red/manip/surge/surge" + res.IGTSec + "_" + res.IGTFrame + ".gqs";
            
            for(byte currentHP=(byte)minhp;currentHP<=maxhp;currentHP++)
            {   
                if(System.IO.File.Exists(state)){gb.LoadState(state);}
                else{return;} 
                gb.CpuWriteBE<ushort>("wPartyMon1HP", (byte)currentHP);
                if (res.IGTSec == 0 && res.IGTFrame == 32 && currentHP == 14)
                {
                    gb.SaveState("basesaves/red/manip/surge" + res.IGTSec + "_" + res.IGTFrame + "_" + currentHP + ".gqs");
                    //gb.Record("test");
                }
                var tryPath = gb.TryExecute(SpacePath(path), (firstCanTile, ()=>gb.DoFirstCan()),(secondCanTile, ()=>gb.DoSecondCan()));  
                if(!tryPath){return;}       
                gb.Press(Joypad.A);
                for(int i =0; i<9;i++){
                    gb.Hold(Joypad.A,"WaitForTextScrollButtonPress");
                    gb.Press(Joypad.B);
                }
                gb.Hold(Joypad.A,"WaitForTextScrollButtonPress");
                gb.RunUntil("Joypad");

                byte[] initialState = gb.SaveState();

                for (byte i = 0; i < minClusterSize; i++)
                {
                    gb.LoadState(initialState);
                    gb.AdvanceFrames(i+offset60fps);
                    gb.Press(Joypad.B);
                    gb.Hold(Joypad.A,"WaitForTextScrollButtonPress");
                    gb.Press(Joypad.B);
                    gb.Hold(Joypad.B,"HandleMenuInput");

                    gb.MenuPress(Joypad.A);
                    gb.MenuPress(Joypad.Up);
                    gb.MenuPress(Joypad.A);

                    var retV = gb.Hold(Joypad.A,gb.SYM["HandleEnemyMonFainted"],gb.SYM["MoveHitTest.moveMissed"],gb.SYM["CriticalHitTest.SkipHighCritical"]+0xB,gb.SYM["CheckIfEnemyNeedsToChargeUp"],gb.SYM["WaitForTextScrollButtonPress"],gb.SYM["StatModifierDownEffect.recalculateStat"]);
                    if (retV == (int)gb.SYM["StatModifierDownEffect.recalculateStat"])
                    {
                        //Console.WriteLine("speedfall");
                        gb.RunUntil(gb.SYM["WaitForTextScrollButtonPress"]);
                        gb.Press(Joypad.B);
                    }
                    else if (retV != (int)gb.SYM["CheckIfEnemyNeedsToChargeUp"])
                    {
                        //Console.WriteLine(((int)retV).ToString());
                        gb.RunUntil(gb.SYM["WaitForTextScrollButtonPress"]);
                        return;
                    }
                    if (gb.CpuRead("wEnemySelectedMove") != 49) // sonicboom manip. (0x31 = sonicboom | 0x67 = screech | 0x21 = tackle)
                    {
                        //Console.WriteLine("screech or tackle");
                        //gb.RunUntil(gb.SYM["WaitForTextScrollButtonPress"]);
                        return;
                    }
                    var tackleHit = gb.Hold(Joypad.B,gb.SYM["HandleMenuInput"],gb.SYM["CriticalHitTest.SkipHighCritical"]+0xB,gb.SYM["MoveHitTest.moveMissed"],gb.SYM["WaitForTextScrollButtonPress"]);
                    if (tackleHit != (int)gb.SYM["HandleMenuInput"])
                    {
                        //Console.WriteLine("sonicboom error");
                        //gb.RunUntil(gb.SYM["WaitForTextScrollButtonPress"]);
                        return;
                    }
                    //Console.WriteLine("sonicboom hit");
                    gb.MenuPress(Joypad.A);
                    gb.MenuPress(Joypad.Down);
                    gb.MenuPress(Joypad.A);
                    var retVt2 = gb.Hold(Joypad.A, gb.SYM["HandleEnemyMonFainted"], gb.SYM["MoveHitTest.moveMissed"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB);
                    if(retVt2!=(int)gb.SYM["HandleEnemyMonFainted"]){
                        return;
                    }
                    else if (gb.CpuRead("wPlayerNumAttacksLeft")==2){
                        //Console.WriteLine("3turn thrash");
                        return;
                    }
                    gb.Hold(Joypad.A,gb.SYM["WaitForTextScrollButtonPress"]);
                    gb.Press(Joypad.B);
                    gb.Hold(Joypad.A,gb.SYM["WaitForTextScrollButtonPress"]);
                    gb.Press(Joypad.B);
                    gb.Hold(Joypad.A,gb.SYM["SelectEnemyMove.done"] + 0x3);
                    if (gb.CpuRead("wEnemySelectedMove") == 98)
                    { //QA
                        var retXspeed = gb.Hold(Joypad.A, gb.SYM["CalculateDamage"], gb.SYM["WaitForTextScrollButtonPress"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB);
                        if (retXspeed == (int)gb.SYM["CalculateDamage"] || retXspeed == (int)gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB
                        //&& i + offset60fps != 0
                        )
                        {
                            if (!wantQA)
                            {
                                return; // bad path if you do NOT want QA
                            }
                            if (retXspeed == (int)gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB)
                            {
                                res.qaDeaths++; // using this to count crits
                                gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"]);
                                res.dmgTaken[0] += currentHP - gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]);
                                continue; // got crit, still ok
                            }
                        }
                        else if (wantQA)
                        {
                            return; // wanted QA but got xspeed
                        }
                        else
                        {
                            //Console.WriteLine("xspeed");
                        }
                    }
                    if(!wantQA && gb.CpuRead("wEnemySelectedMove") != 98 || wantQA)
                    {
                        if(wantQA && gb.CpuRead("wEnemySelectedMove") != 98){
                            return; //wanted qa but got growl
                        }
                        var retP = gb.Hold(Joypad.A, gb.SYM["HandleEnemyMonFainted"], gb.SYM["MoveHitTest.moveMissed"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, gb.SYM["CheckIfEnemyNeedsToChargeUp"], gb.SYM["WaitForTextScrollButtonPress"]);
                        
                        if (retP != gb.SYM["HandleEnemyMonFainted"])
                        {
                            //Console.WriteLine("bad retP: " + retP);
                            res.dmgTaken[0] += currentHP - gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]);
                            continue;
                        }
                        gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"]);
                        gb.Press(Joypad.B);
                        gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"]);
                        gb.Press(Joypad.B);

                        var retR = gb.Hold(Joypad.A, gb.SYM["HandleEnemyMonFainted"], gb.SYM["MoveHitTest.moveMissed"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, gb.SYM["CheckIfEnemyNeedsToChargeUp"], gb.SYM["WaitForTextScrollButtonPress"]);
                        //Console.WriteLine(f + " retR: " + retR);
                        
                        gb.RunUntil(gb.SYM["WaitForTextScrollButtonPress"]);
                        
                        if (retR == gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB)
                        {
                            gb.RunUntil(gb.SYM["WaitForTextScrollButtonPress"]);
                            res.Crits++;
                        }
                    }
                    res.dmgTaken[0] += currentHP - gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]);
                }
            }
            res.Success = true;
            lock(results)
                results.Add(res); 
                          
        });


        return results;
    
    }
    
    public static void SearchRightCan(RbyIntroSequence intro, int numThreads = 12, int numFrames = 60, int success = 30, string path = null)
    {
        StartWatch();

        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];
        if (numThreads == 1) gb.Record("test");

        gb.LoadState(State);
        IGTResults states = Red.IGTCheckParallel(gbs, intro, numFrames);

        if (path != null)
        {

        }

        RbyMap vermilion = gb.Maps[5];
        RbyMap gym = gb.Maps[92];
        Action actions = Action.Right | Action.Left | Action.Up | Action.Down | Action.A | Action.StartB;
        RbyTile startTile = gb.Tile;
        RbyTile endTile = gym[5, 12];

        List<RbyTile> blockedTiles = new List<RbyTile>(){
            gym[3,12],
            gym[4,13],
            gym[7,14],
            gym[8,14],
            gym[9,14],
            gym[4,2]
        };

        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, endTile, actions, blockedTiles.ToArray());
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, vermilion[12, 20], actions, blockedTiles.ToArray());
        vermilion[12, 20].AddEdge(0, new Edge<RbyMap, RbyTile>() { Action = Action.Up, NextTile = gym[4, 17], NextEdgeset = 0, Cost = 2 });
        gym[5, 13].RemoveEdge(0, Action.A);
        //Pathfinding.DebugDrawEdges<RbyMap, RbyTile>(gb, gym, 0);

        var parameters = new DFParameters<Red, RbyMap, RbyTile>()
        {
            MaxCost = 40,
            SuccessSS = success,
            EndTiles = new RbyTile[] { endTile },
            //LogStart = startTile.PokeworldLink + "/",
            //TileCallbacks = new (Tile<RbyMap, RbyTile>, Action<Red>)[] { },
            FoundCallback = state =>
            {
                List<int> successList = new List<int>();
                string random = CheckCan(intro, state.Log, successList);

                if (successList.Count <= 10)
                {
                    Trace.WriteLine($"{state.Log} random: {random}");
                    foreach (int bad in successList)
                    {
                        Trace.WriteLine(bad);
                    }
                }

            }
        };

        DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states);
        Elapsed("search");
    }

    public static string CheckCan(RbyIntroSequence intro, string path, List<int> success, int numThreads = 12)
    {
        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];



        gb.LoadState(State);
        gb.HardReset();
        if(numThreads == 1)
            gb.Record("test");

        intro.ExecuteUntilIGT(gb);

        byte[] igtState = gb.SaveState();
        string result="";
        for(int i =0;i<60;i++)
        {
            success.Add(i);
            gb.LoadState(igtState);

            byte frame = (byte) (i % 60);
            byte sec = (byte) (i / 60);
            gb.CpuWrite("wPlayTimeFrames", frame);
            gb.CpuWrite("wPlayTimeSeconds", 46);
            gb.CpuWrite("wPlayTimeMinutes", 53);
            intro.ExecuteAfterIGT(gb);

            gb.Execute(SpacePath(path));
            if(gb.CpuRead("wFirstLockTrashCanIndex")!=8){
                Console.WriteLine("bad first can "+gb.CpuRead("wFirstLockTrashCanIndex"));
                break;
            }
            gb.Press(Joypad.A);
            if (gb.CpuRead("wSecondLockTrashCanIndex") == 11)
            {
                success.Remove(i);
                result = $"{gb.DividerState.ToString()} {gb.CpuRead("hRandomAdd").ToString()} {gb.CpuRead("hRandomSub").ToString()}";
            }
        }

        return result;
    }
}

