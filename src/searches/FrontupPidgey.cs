using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using static SearchCommon;
using static RbyIGTChecker<Red>;
using System.Text;

class FrontupPidgey
{
    static SortedSet<int> IgnoredFrames = new SortedSet<int> { 36, 37, 38, 39, 40, 41};
    static bool CheckEncounter(int address, Red gb, string pokename, IGTResult res)
    {
        if(address != gb.WildEncounterAddress)
            return false;

        res.Mon = gb.EnemyMon;
        if(res.Mon.Species.Name != pokename)
            return false;

        res.Yoloball = gb.Yoloball(0, Joypad.B);
        return res.Yoloball;
    }

    static bool CheckNoEncounter(int address, Red gb, IGTResult res)
    {
        if(address != gb.WildEncounterAddress)
            return true;

        res.Mon = gb.EnemyMon;
        return false;
    }
    const string State = "basesaves/red/manip/preforestpidgeyfirsttile.gqs";
    static void BuildStates(List<byte> stats = null)
    {
        if(System.IO.File.Exists("basesaves/red/manip/pext/prefPidgey_0_0.gqs"))
            return;

        System.IO.Directory.CreateDirectory("basesaves/red/manip/pext");
        const int numThreads = 12;
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.Pal);
        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];
        // if (numThreads == 1) gb.Record("test");

        gb.LoadState("basesaves/red/manip/preforestpidgeyfirsttile.gqs");
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


            gb.CpuWrite("wPlayTimeMinutes", 7);
            gb.CpuWrite("wPlayTimeSeconds", sec);
            gb.CpuWrite("wPlayTimeFrames", frame);
            intro.ExecuteAfterIGT(gb);
            if (stats != null && stats.Count == 5){
                gb.CpuWriteBE<ushort>("wPartyMon1HP", stats[0]);
                gb.CpuWriteBE<ushort>("wPartyMon1MaxHP", stats[0]);
                gb.CpuWriteBE<ushort>("wPartyMon1Attack", stats[1]);
                gb.CpuWriteBE<ushort>("wPartyMon1Defense", stats[2]);
                gb.CpuWriteBE<ushort>("wPartyMon1Speed", stats[3]);
                gb.CpuWriteBE<ushort>("wPartyMon1Special", stats[4]);
            }
            string pidgeypath = "UUUUUUUUAUUUUUUULLULUUUURUUUURRR";
            int ret;
            ret = gb.Execute(SpacePath(pidgeypath));

            if(!CheckEncounter(ret, gb, "PIDGEY", new IGTResult()))
                return;

            gb.ClearText(Joypad.A);
            gb.Press(Joypad.B);
            gb.SaveState("basesaves/red/manip/pext/preFpidgey_" + sec + "_" + frame + ".gqs");
        });
    }

public static List<DFState<RbyMap, RbyTile>> PruneForest(string path=null, int numThreads = 12, int numFrames = 54, int success = -1, int maxcost = 10, List<byte> stats = null, int r1damage=0, int minClusterSize=3)
    {
        BuildStates(stats);
        StartWatch();

        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];
        if(numThreads == 1)
            gb.Record("test");
        Elapsed("threads");

        IGTResults states = new IGTResults(numFrames);
        MultiThread.For(states.Length, gbs, (gb, i) =>
        {
            int f = i;
            for(int s = 0; s < 60; ++s)
                foreach(int skip in IgnoredFrames)
                    if(f >= skip + 60 * s)
                        ++f;

            gb.LoadState("basesaves/red/manip/pext/preFpidgey_" + (f / 60) + "_" + (f % 60) + ".gqs");
            if(path != null){
                int ret = gb.Execute(SpacePath(path));}

            states[i] = new IGTState(gb, false, f);
        });
        Elapsed("states");

        RbyMap viridian = gb.Maps[1];
        RbyMap route2 = gb.Maps[13];
        RbyMap gate = gb.Maps[50];
        RbyMap forest = gb.Maps[51];
        forest.Sprites.Remove(25, 11);
        Action actions = Action.Right | Action.Left | Action.Up | Action.Down | Action.A | Action.StartB;
        //viridian.Sprites.Remove(18, 9);
        //viridian.Sprites.Remove(17, 5);
        RbyTile startTile = gb.Tile;
        RbyTile[] endTiles = { gate[5, 1] };

        

        RbyTile[] blockedTiles = {
            forest[26, 12],
            forest[2, 19]};
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, endTiles[0], actions,blockedTiles);
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, gate[5,1], actions,blockedTiles);
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, route2[3,44], actions,blockedTiles);
        
        forest[1, 19].RemoveEdge(0, Action.A);
        forest[1, 20].RemoveEdge(0, Action.A);
        forest[25, 12].RemoveEdge(0, Action.A);
        forest[25, 13].RemoveEdge(0, Action.A);
        route2[3, 44].AddEdge(0, new Edge<RbyMap, RbyTile>() { Action = Action.Up, NextTile = gate[4, 7], NextEdgeset = 0, Cost = 0 });
        gate[5, 1].AddEdge(0, new Edge<RbyMap, RbyTile>() { Action = Action.Up, NextTile = forest[17, 47], NextEdgeset = 0, Cost = 0 });
        
        //Pathfinding.DebugDrawEdges(gb,forest, 0);
        //Pathfinding.DebugDrawEdges(gb, gate, 0);
        //Pathfinding.DebugDrawEdges(gb, forest, 0);
        var results = new List<DFState<RbyMap, RbyTile>>();
        var parameters = new DFParameters<Red, RbyMap, RbyTile>()
        {
            MaxCost = maxcost,
            SuccessSS = success >= 0 ? success : Math.Max(1, states.Length - 3),// amount of yoloball success for found
            EndTiles = endTiles,
            TileCallbacks = new (Tile<RbyMap, RbyTile>, Action<Red>)[] { (forest[25, 12], gb => gb.PickupItem()), (forest[1, 19], gb => gb.PickupItem())},
            FoundCallback = state =>
            {
                if(state.Log.Length==19){
                    Trace.WriteLine(state.Log);
                }
                
                //Elapsed("checking");
                /*var fightResults = FullCheck(path+state.Log,stats:stats ,r1damage:r1damage, minClusterSize:minClusterSize, numThreads:12);
                //Trace.WriteLine(startTile.PokeworldLink + "/" + state.Log);
                int count =0;
                StringBuilder trace = new StringBuilder();
                trace.AppendLine(startTile.PokeworldLink + "/" + state.Log);
                for(int i = 0;i<60;i++){
                    if(IgnoredFrames.Contains(i)){continue;}
                    int successcount = 0;
                    int totalDmg=0;
                    foreach(var res in fightResults.Where(res => res.IGTFrame == i)){
                        successcount++;
                        totalDmg+=res.dmgTaken.Sum();
                    }     
                    float avgDamage = (float)totalDmg / ((r1damage+1)*minClusterSize);
                    if(successcount>0){
                        count++;                        
                        trace.AppendLine("Frame: " + i +" Success: " + successcount + "/1 IGT seconds. Average Damage: " + avgDamage);
                    }          
                    }
                if (count >= 1){                    
                    Trace.WriteLine(trace.ToString());
                }
                
                //Trace.WriteLine(startTile.PokeworldLink + "/" + state.Log);*/
            }
        };

        DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states, 0);
        Elapsed("search");

        return results;
    }


    public static List<DFState<RbyMap, RbyTile>> SearchForest(string path=null, int numThreads = 12, int numFrames = 54, int success = -1, int maxcost = 10, List<byte> stats = null, int r1damage=0, int minClusterSize=3)
    {
        BuildStates(stats);
        StartWatch();

        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];
        if(numThreads == 1)
            gb.Record("test");
        Elapsed("threads");

        IGTResults states = new IGTResults(numFrames);
        MultiThread.For(states.Length, gbs, (gb, i) =>
        {
            int f = i;
            for(int s = 0; s < 60; ++s)
                foreach(int skip in IgnoredFrames)
                    if(f >= skip + 60 * s)
                        ++f;

            gb.LoadState("basesaves/red/manip/pext/preFpidgey_" + (f / 60) + "_" + (f % 60) + ".gqs");
            if(path != null){
                int ret = gb.Execute(SpacePath(path));}

            states[i] = new IGTState(gb, false, f);
        });
        Elapsed("states");

        RbyMap viridian = gb.Maps[1];
        RbyMap route2 = gb.Maps[13];
        RbyMap gate = gb.Maps[50];
        RbyMap forest = gb.Maps[51];
        forest.Sprites.Remove(25, 11);
        Action actions = Action.Right | Action.Left | Action.Up | Action.Down | Action.A | Action.StartB;
        //viridian.Sprites.Remove(18, 9);
        //viridian.Sprites.Remove(17, 5);
        RbyTile startTile = gb.Tile;
        RbyTile[] endTiles = { forest[1, 19] };

        

        RbyTile[] blockedTiles = {
            forest[26, 12],
            forest[2, 19]};
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, endTiles[0], actions,blockedTiles);
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, gate[5,1], actions,blockedTiles);
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, route2[3,44], actions,blockedTiles);
        
        forest[1, 19].RemoveEdge(0, Action.A);
        forest[1, 20].RemoveEdge(0, Action.A);
        forest[25, 12].RemoveEdge(0, Action.A);
        forest[25, 13].RemoveEdge(0, Action.A);
        route2[3, 44].AddEdge(0, new Edge<RbyMap, RbyTile>() { Action = Action.Up, NextTile = gate[4, 7], NextEdgeset = 0, Cost = 0 });
        gate[5, 1].AddEdge(0, new Edge<RbyMap, RbyTile>() { Action = Action.Up, NextTile = forest[17, 47], NextEdgeset = 0, Cost = 0 });
        
        //Pathfinding.DebugDrawEdges(gb,forest, 0);
        //Pathfinding.DebugDrawEdges(gb, gate, 0);
        //Pathfinding.DebugDrawEdges(gb, forest, 0);
        var results = new List<DFState<RbyMap, RbyTile>>();
        var parameters = new DFParameters<Red, RbyMap, RbyTile>()
        {
            MaxCost = maxcost,
            SuccessSS = success >= 0 ? success : Math.Max(1, states.Length - 3),// amount of yoloball success for found
            EndTiles = endTiles,
            TileCallbacks = new (Tile<RbyMap, RbyTile>, Action<Red>)[] { (forest[25, 12], gb => gb.PickupItem()), (forest[1, 19], gb => gb.PickupItem())},
            FoundCallback = state =>
            {
                Elapsed("checking");
                var fightResults = FullCheck(path+state.Log,stats:stats ,r1damage:r1damage, minClusterSize:minClusterSize, numThreads:12);
                //Trace.WriteLine(startTile.PokeworldLink + "/" + state.Log);
                int count =0;
                StringBuilder trace = new StringBuilder();
                trace.AppendLine(startTile.PokeworldLink + "/" + state.Log);
                for(int i = 0;i<60;i++){
                    if(IgnoredFrames.Contains(i)){continue;}
                    int successcount = 0;
                    int totalDmg=0;
                    foreach(var res in fightResults.Where(res => res.IGTFrame == i)){
                        successcount++;
                        totalDmg+=res.dmgTaken.Sum();
                    }     
                    float avgDamage = (float)totalDmg / ((r1damage+1)*minClusterSize);
                    if(successcount>0){
                        count++;                        
                        trace.AppendLine("Frame: " + i +" Success: " + successcount + "/1 IGT seconds. Average Damage: " + avgDamage);
                    }          
                    }
                if (count >= 1){                    
                    Trace.WriteLine(trace.ToString());
                }
                
                //Trace.WriteLine(startTile.PokeworldLink + "/" + state.Log);
            }
        };

        DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states, 0);
        Elapsed("search");

        return results;
    }

    public static void Check()
    {
        string path;
        RbyStrat pal;
        
        //path = "UUUUUUUUUUUAUUUULLLUUUUURUUUURRR"; pal = RbyStrat.Pal;
        //CheckIGT(State, new RbyIntroSequence(pal), path, "PIDGEY", 3600, verbose:Verbosity.Summary);
        //path = "UUUUUUUUUUUAUUUULLULUUUURUUUURRR"; pal = RbyStrat.Pal;
        //CheckIGT(State, new RbyIntroSequence(pal), path, "PIDGEY", 3600, verbose:Verbosity.Summary);
        //path = "UUUUUUUUAUUUUUUULLULUUUURUUUURRR"; pal = RbyStrat.Pal; // 57/60 
        //string forest = "UUUULLLLLUUUUUUURUUUUURRRRRRRRUUUUUUAUUUUUUUUUUUUUUUUUUUUUUUUUUUULLALLALLLLDDDDDDDLLLLUUUUUUUUUUUUULLLLLLDDDDDDDDDDDDDDDDDDDALLLLLLUUU";
        string forest = "UUUULLLLLUUUUUUURUUUUURRRRRRRRUUUUUUAUUUUUUUUUUUUUUUUUUUUUUUUUUUULLALLALLLLDDDDDDDLLLLUUUUUUUUUUUUULLLLLLDDDDDDDDDDDDDDDDDADDLLLLLLUUU";
        //BuildStates();
        //RedCb[] gbs = MultiThread.MakeThreads<RedCb>(1);
        //gbs[0].Record("test");
        var stat = new List<byte>{21,11,12,10,11};
        var fightResults = FullCheck(forest,stats:stat ,r1damage:0, minClusterSize:3, numThreads:1);
        //Trace.WriteLine(startTile.PokeworldLink + "/" + state.Log);
        int count =0;
        StringBuilder trace = new StringBuilder();
        trace.AppendLine("https://gunnermaniac.com/pokeworld?local=13#8/48/" + forest);
        for(int i = 0;i<60;i++){
            if(IgnoredFrames.Contains(i)){continue;}
            int successcount = 0;
            int totalDmg=0;
            foreach(var res in fightResults.Where(res => res.IGTFrame == i)){
                successcount++;
                totalDmg+=res.dmgTaken.Sum();
            }     
            float avgDamage = (float)totalDmg / ((0+1)*3);
            if(successcount>0){
                count++;                        
                trace.AppendLine("Frame: " + i +" Success: " + successcount + "/1 IGT seconds. Average Damage: " + avgDamage);
            }          
        }
        if (count >= 1){
            Trace.WriteLine(trace.ToString());
        } 
    }

    public static void Search()
    {
        for(RbyStrat pal = RbyStrat.NoPal; pal <= RbyStrat.PalRel; pal++)
            for(RbyStrat gf = RbyStrat.GfSkip; gf <= RbyStrat.GfSkip; ++gf)
                for(RbyStrat hop = RbyStrat.Hop0; hop <= RbyStrat.Hop0; ++hop)
                    for(int backouts = 0; backouts <= 0; ++backouts)
                        Search(new RbyIntroSequence(pal, gf, hop, backouts), 12, 16, 14);
    }

    static List<IGTResult> FullCheck(string path, int numFrames = 60, 
                                 bool verbose = true, int numThreads = 12, int minClusterSize = 3,
                                 List<byte> stats=null, int r1damage = 9)
        {
            RedCb[] gbs = MultiThread.MakeThreads<RedCb>(numThreads);
            if(numThreads == 1){
                gbs[0].Record("test");}
            List<IGTResult> results = new List<IGTResult>();

            //Console.WriteLine( "poison: "+(int)gbs[0].SYM["PoisonEffect.inflictPoison"]);
            //Console.WriteLine( "miss: "+(int)gbs[0].SYM["MoveHitTest.moveMissed"]);
            //Console.WriteLine( "crit: "+ (int)(gbs[0].SYM["CriticalHitTest.SkipHighCritical"]+ 0xB));
            //Console.WriteLine( "menuinput: "+(int)gbs[0].SYM["HandleMenuInput"]);
            //Console.WriteLine( "enemyfaint: "+(int)gbs[0].SYM["HandleEnemyMonFainted"]);
            //Console.WriteLine( "playerfaint: "+(int)gbs[0].SYM["HandlePlayerMonFainted"]);

            MultiThread.For(numFrames, gbs, (gb, f) =>
            {
                if(verbose && numFrames >= 100 && (f + 1) * 100 / numFrames > f * 100 / numFrames) Console.WriteLine("%");
                
                

                if(IgnoredFrames.Contains(f % 60)){return;}
                //if(!(f==7 || f==9 || f==12 || f==14 || f==15 || f==16 || f==17 || f==18 || f==20 || f==21 || f==22 || f==35 || f==47 || f==48 || f==49)){return;}

                IGTResult res = new IGTResult();
                res.Success=false;
                res.IGTSec = (byte) (f / 60);
                res.IGTFrame = (byte) (f % 60);
                res.dmgTaken = new List<int>();
                string state = "basesaves/red/manip/pext/preFpidgey_" + res.IGTSec + "_" + res.IGTFrame + ".gqs";
                for(byte dmgTaken=0;dmgTaken<=r1damage;dmgTaken++){
                    

                    if(System.IO.File.Exists(state)){gb.LoadState(state);}
                    else{return;}

                    //var npcTracker = new NpcTracker<RedCb>(gb.CallbackHandler);
                    var newhp = stats[0]-dmgTaken;
                    gb.CpuWriteBE<ushort>("wPartyMon1HP", (byte)newhp);
                    
                    int address = gb.Execute(SpacePath(path), (gb.Maps[51][25, 12], gb.PickupItem));
                    //res.Info = npcTracker.GetMovement((50, 2), (51, 1), (51, 8));
                    res.Tile = gb.Tile;
                    res.Map = gb.Map;
                    
                    if(!CheckNoEncounter(address,gb,res)){
                        //Console.WriteLine("encounter");
                        return; //encounter anywhere
                    }
                    gb.PickupItem();
                    var ret = gb.Hold(Joypad.Up,"ManualTextScroll", "WaitForTextScrollButtonPress");
                    if(ret == gb.SYM["ManualTextScroll"]){
                        //Console.WriteLine("pot encounter");
                        return; //encounter on potion tile
                    }
                    gb.RunUntil("Joypad");
                    byte[] initialState = gb.SaveState();

                    int clusterDamage = 0;
                    for (byte i = 0; i < minClusterSize; i++)
                    {
                        gb.LoadState(initialState);
                        gb.AdvanceFrames(i);
                        gb.Press(Joypad.B);
                        gb.Hold(Joypad.A,"WaitForTextScrollButtonPress");
                        gb.Press(Joypad.B);
                        gb.ClearText(Joypad.B);
                        //turn 1
                        //gb.RunUntil(gb.SYM["HandleMenuInput"]);
                        //gb.MenuPress(Joypad.A);
                        //gb.MenuPress(Joypad.Up);
                        //gb.MenuPress(Joypad.A);

                        gb.BattleMenu(0, 0);
                        gb.ChooseMenuItem(1);

                        gb.RunUntil(gb.SYM["SelectEnemyMove.done"] + 0x3);

                        if (gb.CpuRead("wEnemySelectedMove") == 40) //PS
                        {
                            var ret0 = gb.Hold(Joypad.A, 
                            gb.SYM["PoisonEffect.inflictPoison"], 
                            gb.SYM["MoveHitTest.moveMissed"], 
                            gb.SYM["WaitForTextScrollButtonPress"], 
                            gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, 
                            gb.SYM["HandlePlayerMonFainted"]
                            );
                            
                            if(ret0 != (int)gb.SYM["WaitForTextScrollButtonPress"]){
                                //Console.WriteLine("turn1 PS "+ ret0);
                                return;} //PS miss crit poison, or dead, or tw miss                     
                        }
                        else
                        {
                            gb.Hold(Joypad.A,gb.SYM["WaitForTextScrollButtonPress"]);
                            gb.Press(Joypad.B);
                            var ret2 = gb.Hold(Joypad.A,gb.SYM["MoveHitTest.moveMissed"],gb.SYM["WaitForTextScrollButtonPress"]);
                            if (ret2 == (int)gb.SYM["MoveHitTest.moveMissed"]){
                                //Console.WriteLine("turn1 SS "+ ret2);
                                return;} //TW miss
                            
                        }
                        gb.Press(Joypad.B);

                        //turn 2
                        gb.MenuPress(Joypad.A);
                        gb.MenuPress(Joypad.Select);
                        gb.MenuPress(Joypad.A);
                        gb.RunUntil(gb.SYM["SelectEnemyMove.done"] + 0x3);

                        if (gb.CpuRead("wEnemySelectedMove") == 40)
                        {
                            var ret0 = gb.Hold(Joypad.A, gb.SYM["PoisonEffect.inflictPoison"] , gb.SYM["MoveHitTest.moveMissed"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, gb.SYM["WaitForTextScrollButtonPress"], gb.SYM["HandlePlayerMonFainted"]);
                            if(ret0 != (int)gb.SYM["WaitForTextScrollButtonPress"]){
                                //Console.WriteLine("turn2 PS "+ ret0);
                                return;} //PS miss crit poison, or dead, or TW miss
                        }
                        else
                        {
                            gb.Hold(Joypad.A,gb.SYM["WaitForTextScrollButtonPress"]);
                            gb.Press(Joypad.B);
                            var ret2 = gb.Hold(Joypad.A,gb.SYM["MoveHitTest.moveMissed"],gb.SYM["WaitForTextScrollButtonPress"]);
                            if (ret2 == (int)gb.SYM["MoveHitTest.moveMissed"]){
                                //Console.WriteLine("turn2 SS "+ ret2);
                                return;} // TW miss
                        }
                        gb.Press(Joypad.B);

                        //turn 3
                        gb.MenuPress(Joypad.A);
                        gb.MenuPress(Joypad.Up);
                        gb.MenuPress(Joypad.A);
                        gb.RunUntil(gb.SYM["SelectEnemyMove.done"] + 0x3);
                        
                        if (gb.CpuRead("wEnemySelectedMove") == 40)
                        {
                            var ret0 = gb.Hold(Joypad.B, gb.SYM["PoisonEffect.inflictPoison"] , gb.SYM["MoveHitTest.moveMissed"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, gb.SYM["HandleMenuInput"],gb.SYM["HandleEnemyMonFainted"], gb.SYM["HandlePlayerMonFainted"]);
                            if(ret0==(int)gb.SYM["HandleEnemyMonFainted"]){
                                clusterDamage += newhp-gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]);
                                continue; //killed him
                                }
                            if(ret0 != (int)gb.SYM["HandleMenuInput"]){
                                //Console.WriteLine("turn3 PS "+ ret0);
                                return;} //PS miss crit poison, or dead, or TW miss
                        }
                        else
                        {
                            gb.Hold(Joypad.A,gb.SYM["WaitForTextScrollButtonPress"]);
                            gb.Press(Joypad.B);
                            var ret2 = gb.Hold(Joypad.B,gb.SYM["MoveHitTest.moveMissed"],gb.SYM["HandleMenuInput"],gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB,gb.SYM["HandleEnemyMonFainted"]);
                            if(ret2==(int)gb.SYM["HandleEnemyMonFainted"]){
                                clusterDamage += newhp-gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]);
                                continue; //killed him
                                }
                            if (ret2 != (int)gb.SYM["HandleMenuInput"]){
                                //Console.WriteLine("turn3 SS "+ ret2);
                                return;} // tackle crit or miss
                        }

                        //turn 4
                        gb.MenuPress(Joypad.A);
                        gb.MenuPress(Joypad.Select);
                        gb.MenuPress(Joypad.A);
                        gb.RunUntil(gb.SYM["SelectEnemyMove.done"] + 0x3);

                        if (gb.CpuRead("wEnemySelectedMove") == 40)
                        {
                            var ret0 = gb.Hold(Joypad.B, gb.SYM["PoisonEffect.inflictPoison"] , gb.SYM["MoveHitTest.moveMissed"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, gb.SYM["HandleMenuInput"],gb.SYM["HandleEnemyMonFainted"], gb.SYM["HandlePlayerMonFainted"]);
                            if(ret0==(int)gb.SYM["HandleEnemyMonFainted"]){
                                clusterDamage += newhp-gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]);
                                continue; //killed him
                                }
                            if(ret0 != (int)gb.SYM["HandleMenuInput"]){
                                //Console.WriteLine("turn4 PS "+ ret0);
                                return;} //PS miss crit poison, or dead, or TW miss
                        }
                        else
                        {
                            gb.Hold(Joypad.A,gb.SYM["WaitForTextScrollButtonPress"]);
                            gb.Press(Joypad.B);
                            var ret2 = gb.Hold(Joypad.B,gb.SYM["MoveHitTest.moveMissed"],gb.SYM["HandleMenuInput"],gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB,gb.SYM["HandleEnemyMonFainted"]);
                            if(ret2==(int)gb.SYM["HandleEnemyMonFainted"]){
                                clusterDamage += newhp-gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]);
                                continue; //killed him
                                }
                            if (ret2 != (int)gb.SYM["HandleMenuInput"]){
                                //Console.WriteLine("turn4 SS "+ ret2);
                                return;} // tackle crit or miss
                        }

                        //turn 5
                        gb.MenuPress(Joypad.A);
                        gb.MenuPress(Joypad.Select);
                        gb.MenuPress(Joypad.A);
                        gb.RunUntil(gb.SYM["SelectEnemyMove.done"] + 0x3);

                        if (gb.CpuRead("wEnemySelectedMove") == 40)
                        {
                            var ret0 = gb.Hold(Joypad.B, gb.SYM["PoisonEffect.inflictPoison"] , gb.SYM["MoveHitTest.moveMissed"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, gb.SYM["HandleMenuInput"],gb.SYM["HandleEnemyMonFainted"], gb.SYM["HandlePlayerMonFainted"]);
                            if(ret0==(int)gb.SYM["HandleEnemyMonFainted"]){
                                clusterDamage += newhp-gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]);
                                continue; //killed him
                                }
                            if(ret0 != (int)gb.SYM["HandleMenuInput"]){
                                
                                //Console.WriteLine("turn5 PS "+ ret0);
                                return;} //PS miss crit poison, or dead, or TW miss
                        }
                        else
                        {
                            gb.Hold(Joypad.A,gb.SYM["WaitForTextScrollButtonPress"]);
                            gb.Press(Joypad.B);
                            var ret2 = gb.Hold(Joypad.B,gb.SYM["MoveHitTest.moveMissed"],gb.SYM["HandleMenuInput"],gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB,gb.SYM["HandleEnemyMonFainted"]);
                            if(ret2==(int)gb.SYM["HandleEnemyMonFainted"]){
                                clusterDamage += newhp-gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]);
                                continue; //killed him
                                }
                            if (ret2 != (int)gb.SYM["HandleMenuInput"]){
                                
                                //Console.WriteLine("turn5 SS "+ ret2);
                                return;} // tackle crit or miss
                        }

                        //turn 6
                        gb.MenuPress(Joypad.A);
                        gb.MenuPress(Joypad.Select);
                        gb.MenuPress(Joypad.A);
                        gb.RunUntil(gb.SYM["SelectEnemyMove.done"] + 0x3);

                        if (gb.CpuRead("wEnemySelectedMove") == 40)
                        {
                            var ret0 = gb.Hold(Joypad.B, gb.SYM["PoisonEffect.inflictPoison"] , gb.SYM["MoveHitTest.moveMissed"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, gb.SYM["HandleMenuInput"],gb.SYM["HandleEnemyMonFainted"], gb.SYM["HandlePlayerMonFainted"]);
                            if(ret0==(int)gb.SYM["HandleEnemyMonFainted"]){
                                clusterDamage += newhp-gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]);
                                
                                //Console.WriteLine("killed him PS");
                                continue; //killed him
                                }
                            if(ret0 != (int)gb.SYM["HandleMenuInput"]){
                                
                                //Console.WriteLine("turn 6 PS "+ ret0);
                                return;} //PS miss crit poison, or dead, or TW miss
                        }
                        else
                        {
                            gb.Hold(Joypad.A,gb.SYM["WaitForTextScrollButtonPress"]);
                            gb.Press(Joypad.B);
                            var ret2 = gb.Hold(Joypad.B,gb.SYM["MoveHitTest.moveMissed"],gb.SYM["HandleMenuInput"],gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB,gb.SYM["HandleEnemyMonFainted"]);
                            if(ret2==(int)gb.SYM["HandleEnemyMonFainted"]){
                                clusterDamage += newhp-gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]);
                                
                                //Console.WriteLine("killed him SS");
                                continue; //killed him
                                }
                            if (ret2 != (int)gb.SYM["HandleMenuInput"]){
                                
                                //Console.WriteLine("turn6 SS " + ret2);
                                
                                return;} // tackle crit or miss
                        }
                        
                        //Console.WriteLine("miss range");
                        return; //didnt kill it in 6 turns, lowrolls
                    }
                    res.Success = true;
                    res.dmgTaken.Add(clusterDamage);
                    
                }
                lock(results)
                    results.Add(res);
                
                
            });
            return results;
            }
    public static void Search(RbyIntroSequence intro, int numThreads = 12, int numFrames = 16, int success = 15)
    {
        StartWatch();

        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];
        if(numThreads == 1) gb.Record("test");

        gb.LoadState(State);
        IGTResults states = Red.IGTCheckParallel(gbs, intro, numFrames);

        RbyMap cave = gb.Maps[61];
        Action actions = Action.Right | Action.Down | Action.Up | Action.Left | Action.A | Action.StartB;
        RbyTile startTile = gb.Tile;
        // RbyTile[] endTiles = { cave[36, 31], cave[37, 30], cave[37, 32] };

        List<RbyTile> blockedTiles = new List<RbyTile>(){  };
        


        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, gb.Maps[13][8, 48], actions, blockedTiles.ToArray());
        //gb.Maps[60][26, 3].AddEdge(0, new Edge<RbyMap, RbyTile>() { Action = Action.Down, NextTile = gb.Maps[60][26, 3], NextEdgeset = 0, Cost = 2 });
        //Pathfinding.DebugDrawEdges<RbyMap, RbyTile>(gb, gb.Maps[13], 0);
        // map 13 is route 2 map 1 is viridian
        var parameters = new DFParameters<Red, RbyMap, RbyTile>()
        {
            MaxCost = 4,
            SuccessSS = success,
            EndTiles = new RbyTile[]{ gb.Maps[13][8, 48]},
            EncounterCallback = gb => gb.EnemyMon.Species.Name == "PIDGEY" && (gb.Tile == gb.Maps[13][8, 48] || gb.Tile == gb.Maps[13][7, 48] || gb.Tile == gb.Maps[13][6, 48] || gb.Tile == gb.Maps[13][8, 49] || gb.Tile == gb.Maps[13][7, 49] || gb.Tile == gb.Maps[13][8, 50]) 
            && gb.Yoloball() && gb.EnemyMon.DVs.HP <= 9 && gb.EnemyMon.Level == 3,
            //LogStart = startTile.PokeworldLink + "/",
            FoundCallback = state =>
            {
                success = CheckIGT(State, intro, state.Log, "PIDGEY", 60, false, false, Verbosity.Nothing);
                if(success>57){
                    Trace.WriteLine(state.Log + " " + CheckIGT(State, intro, state.Log, "PIDGEY", 60, false, false, Verbosity.Summary) + "/60 " + state.WastedFrames + " " + intro);
                }
            }
        };
        Trace.WriteLine(startTile.PokeworldLink + "/");
        /*DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states);
        Elapsed("search");*/
    }
}
