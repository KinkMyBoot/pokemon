using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.IO;

public static class RbyIGTChecker<Gb> where Gb : Rby {
    // todo fix edge case: yellow pidgey manip picks up item after yoloball on last tile
    // flag for verbosity

    public class IGTResult {
        public RbyPokemon Mon;
        public RbyMap Map;
        public RbyTile Tile;
        public bool Yoloball;

        public string ToString(bool dvs=false) {
            return $"[{IGTSec}] [{IGTFrame}]: " + (Mon!=null ? (dvs ? Mon.ToString() : $"L{Mon.Level} {Mon.Species.Name}")+$" on {Tile}, Yoloball: {Yoloball}" : "");
        }
        public byte IGTSec;
        public byte IGTFrame;
        public string Info;
        public IGTResult Extended;
    }

    public static List<(int, byte, byte)> Empty = new List<(int, byte, byte)>();

    public static void CheckIGT(string statePath, RbyIntroSequence intro, string path, string targetPoke, int numFrames = 60, bool checkDV = false,
                                List<(int, byte, byte)> itemPickups = null, bool selectball = false, int startFrame = 0, int stepFrame = 1, int numThreads = 16, bool verbose = true) {
        byte[] state = File.ReadAllBytes(statePath);

        if(itemPickups==null)
            itemPickups=Empty;

        Gb[] gbs = MultiThread.MakeThreads<Gb>(numThreads);

        gbs[0].LoadState(state);
        gbs[0].HardReset();
        if(numThreads==1)
            gbs[0].Record("test");
        intro.ExecuteUntilIGT(gbs[0]);
        byte[] igtState = gbs[0].SaveState();

        List<IGTResult> manipResults = new List<IGTResult>();
        Dictionary<string, int> manipSummary = new Dictionary<string, int>();

        object frameLock = new object();
        object writeLock = new object();
        int igtCount = startFrame;

        MultiThread.For(numFrames, gbs, (gb, iterator) => {
            IGTResult res = new IGTResult();
            lock(frameLock) {
                res.IGTSec = (byte)(igtCount / 60);
                res.IGTFrame = (byte)(igtCount % 60);
                igtCount += stepFrame;
                if(verbose && igtCount%100==0) Console.WriteLine(igtCount);
            }
            gb.LoadState(igtState);
            // Console.WriteLine(res.IGTSec + " " + res.IGTFrame);
            // gb.CpuWrite("wPlayTimeMinutes", 5);
            gb.CpuWrite("wPlayTimeSeconds", res.IGTSec);
            gb.CpuWrite("wPlayTimeFrames", res.IGTFrame);

            intro.ExecuteAfterIGT(gb);
            int ret = 0;
            foreach(string step in SpacePath(path).Split()) {
                ret = gb.Execute(step);
                if(itemPickups.Contains((gb.Tile.Map.Id, gb.Tile.X, gb.Tile.Y)))
                    gb.PickupItem();
                if(ret != gb.SYM["JoypadOverworld"]) break;
            }

            if(ret == gb.SYM["CalcStats"]) {
                res.Yoloball = selectball ? gb.SelectBall() : gb.Yoloball();
                res.Mon = gb.EnemyMon;
            }
            res.Tile = gb.Tile;
            res.Map = gb.Map;

            lock(writeLock) {
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
