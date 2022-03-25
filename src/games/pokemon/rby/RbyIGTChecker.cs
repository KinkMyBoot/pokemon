using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.IO;

public static class RbyIGTChecker<Gb> where Gb : Rby {

    public class IGTResult {
        public RbyPokemon Mon;
        public RbyMap Map;
        public RbyTile Tile;
        public bool Yoloball;

        public string ToString(bool dvs = false) {
            return $"[{IGTSec}] [{IGTFrame}]: " + (Mon != null ? (dvs ? Mon.ToString() : $"L{Mon.Level} {Mon.Species.Name}")+$" on {Tile}, Yoloball: {Yoloball}" : "");
        }
        public byte IGTSec;
        public byte IGTFrame;
        public string Info;
        public IGTResult Extended;
    }

    public static int CheckIGT(string statePath, RbyIntroSequence intro, string path, string targetPoke = "", int numFrames = 60, bool checkDV = false,
                                List<(int, byte, byte)> itemPickups = null, bool selectBall = false, int startFrame = 0, int stepFrame = 1,
                                int numThreads = 16, bool verbose = true, bool forceRedBar = false, int nameLength = -1, Func<Gb, bool> memeBall = null) {
        byte[] state = File.ReadAllBytes(statePath);

        Gb[] gbs = MultiThread.MakeThreads<Gb>(numThreads);
        Gb gb = gbs[0];

        gb.LoadState(state);
        gb.HardReset();
        if(numThreads == 1)
            gb.Record("test");
        intro.ExecuteUntilIGT(gb);
        byte[] igtState = gb.SaveState();

        List<IGTResult> manipResults = new List<IGTResult>();
        Dictionary<string, int> manipSummary = new Dictionary<string, int>();

        MultiThread.For(numFrames, gbs, (gb, iterator) => {
            IGTResult res = new IGTResult();

            int igt = startFrame + iterator * stepFrame;
            res.IGTSec = (byte)(igt / 60);
            res.IGTFrame = (byte)(igt % 60);
            if(verbose && numFrames >= 100 && (iterator + 1) * 100 / numFrames > iterator * 100 / numFrames) Console.WriteLine("%");

            gb.LoadState(igtState);
            gb.CpuWrite("wPlayTimeSeconds", res.IGTSec);
            gb.CpuWrite("wPlayTimeFrames", res.IGTFrame);

            intro.ExecuteAfterIGT(gb);
            if(nameLength > 0) {
                for(int i = 0; i < nameLength; ++i) gb.CpuWrite(gb.SYM["wPartyMonNicks"] + i, 0x80);
                gb.CpuWrite(gb.SYM["wPartyMonNicks"] + nameLength, 0x50);
            }
            if(forceRedBar) {
                gb.CpuWriteBE<ushort>("wPartyMon1HP", 1);
            }

            int ret = 0;
            foreach(string step in SpacePath(path).Split()) {
                ret = gb.Execute(step);
                if(itemPickups != null && itemPickups.Contains((gb.Tile.Map.Id, gb.Tile.X, gb.Tile.Y)))
                    gb.PickupItem();
                if(ret != gb.SYM["JoypadOverworld"]) break;
            }

            if(ret == gb.SYM["CalcStats"]) {
                res.Yoloball = memeBall != null ? memeBall(gb) : selectBall ? gb.Selectball() : gb.Yoloball();
                res.Mon = gb.EnemyMon;
            }
            res.Tile = gb.Tile != null ? gb.Tile : new RbyTile() { Map = gb.Map, X = gb.XCoord, Y = gb.YCoord };
            res.Map = gb.Map;

            lock(manipResults) {
                manipResults.Add(res);
            }
        });

        // print out manip success
        int success = 0;
        manipResults.Sort(delegate(IGTResult a, IGTResult b) {
            return (a.IGTSec*60 + a.IGTFrame).CompareTo(b.IGTSec*60 + b.IGTFrame);
        });

        foreach(var item in manipResults) {
            if(verbose) Trace.WriteLine(item.ToString(checkDV));
            if((String.IsNullOrEmpty(targetPoke) && item.Mon == null) ||
                (item.Mon != null && item.Mon.Species.Name.ToLower() == targetPoke.ToLower() && item.Yoloball)) {
                success++;
            }
            string summary;
            if(item.Mon != null) {
                summary = $", Tile: {item.Tile.ToString()}, Yoloball: {item.Yoloball}";
                summary = checkDV ? item.Mon + summary : "L" + item.Mon.Level + " " + item.Mon.Species.Name + summary;
            } else {
                summary = "No Encounter";
            }
            if(!manipSummary.ContainsKey(summary)) {
                manipSummary.Add(summary, 1);
            } else {
                manipSummary[summary]++;
            }
        }
        if(verbose) Trace.WriteLine("");

        foreach(var item in manipSummary) {
            Trace.WriteLine($"{item.Key}, {item.Value}/{numFrames}");
        }

        Trace.WriteLine($"Success: {success}/{numFrames}");
        return success;
    }

    public class CheckIGTParameters {
        public string StatePath;
        public RbyIntroSequence Intro;
        public string Path;
        public string TargetPoke = "";
        public int NumFrames = 60;
        public bool CheckDV = false;
        public List<(int, byte, byte)> ItemPickups = null;
        public bool SelectBall = false;
        public int StartFrame = 0;
        public int StepFrame = 1;
        public int NumThreads = 16;
        public bool Verbose = true;
        public bool ForceRedBar = false;
        public int NameLength = -1;
        public Func<Gb, bool> MemeBall = null;
    }

    public static int CheckIGT(CheckIGTParameters p) {
        return CheckIGT(p.StatePath, p.Intro, p.Path, p.TargetPoke, p.NumFrames, p.CheckDV, p.ItemPickups, p.SelectBall, p.StartFrame, p.StepFrame, p.NumThreads, p.Verbose, p.ForceRedBar, p.NameLength, p.MemeBall);
    }

    public static string SpacePath(string path) {
        string output = "";

        string[] validActions = new string[] { "A", "U", "D", "L", "R", "S", "S_B" };
        while(path.Length > 0) {
            if (validActions.Any(path.StartsWith)) {
                if (path.StartsWith("S_B")) {
                    output += "S_B";
                    path = path.Remove(0, 3);
                } else if(path.StartsWith("S")) {
                    output += "S_B";
                    path = path.Remove(0, 1);
                } else {
                    output += path[0];
                    path = path.Remove(0, 1);
                }

                output += " ";
            } else {
                throw new Exception(String.Format("Invalid Path Action Recieved: {0}", path));
            }
        }

        return output.Trim();
    }
}
