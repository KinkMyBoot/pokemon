using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;

using static SearchCommon;
using static RbyIGTChecker<Ao>;

class AoCans
{
    public static void Check()
    {
        // Check("basesaves/ao/cans1.gqs");
        // Check("basesaves/ao/cans2.gqs");
        // Check("basesaves/ao/cans3.gqs");
        // Check("basesaves/ao/cans4.gqs");
        // Check("basesaves/ao/cans5.gqs");
        // Check("basesaves/ao/cans6.gqs");

        // Check("basesaves/ao/cans1.gqs", new RbyIntroSequence(RbyStrat.PalHold));
        // Check("basesaves/ao/cans3.gqs", new RbyIntroSequence(RbyStrat.PalHold));
        // Check("basesaves/ao/cans5.gqs", new RbyIntroSequence(RbyStrat.PalHold));

        // Check("basesaves/ao/cans2.gqs", new RbyIntroSequence(RbyStrat.PalAB), "DLLLURUUUUUA");
        Check("basesaves/ao/cans2.gqs", new RbyIntroSequence(RbyStrat.PalAB), "DLLLAURAUUUUUA"); //58
        // Check("basesaves/ao/cans2.gqs", new RbyIntroSequence(RbyStrat.PalAB), "DALLLURAUUAUUUA"); //58

        // Check("basesaves/ao/cans4.gqs", new RbyIntroSequence(RbyStrat.PalHold), "LLAURUUUUUA"); //57

        // Check("basesaves/ao/cans6.gqs", new RbyIntroSequence(RbyStrat.PalHold), "URUUUUUA"); //57
    }

    public static void Check(string state)
    {
        Trace.WriteLine(state);
        for(RbyStrat pal = RbyStrat.NoPal; pal <= RbyStrat.PalRel; ++pal)
            Check(state, new RbyIntroSequence(pal), "");
    }

    public static void Check(string state, RbyIntroSequence intro, string path)
    {
        Trace.WriteLine(state + " " + intro + " " + path);
        int numFrames = 60;
        int numThreads = 16;
        // int numFrames = 1;
        // int numThreads = 1;

        Ao[] gbs = MultiThread.MakeThreads<Ao>(numThreads);
        Ao gb = gbs[0];
        if(numThreads == 1) gb.Record("test");

        gb.LoadState(state);
        gb.HardReset();
        intro.ExecuteUntilIGT(gb);
        byte[] igtState = gb.SaveState();

        var full = new List<string>();
        var results = new Dictionary<(byte first, byte second), int>();

        MultiThread.For(numFrames, gbs, (gb, f) =>
        {
            gb.LoadState(igtState);
            gb.CpuWrite("wPlayTimeSeconds", (byte) (f / 60));
            gb.CpuWrite("wPlayTimeFrames", (byte) (f % 60));

            intro.ExecuteAfterIGT(gb);
            gb.Execute(SpacePath(path));

            (byte first, byte second) cans = (gb.CpuRead("wFirstLockTrashCanIndex"), gb.CpuRead("wSecondLockTrashCanIndex"));
            lock(results)
            {
                full.Add($"{f / 60,2} {f % 60,2}: {cans.first},{cans.second}");
                if(!results.ContainsKey(cans))
                    results.Add(cans, 1);
                else
                    results[cans]++;
            }
        });
        full.Sort();
        foreach(string line in full)
            Trace.WriteLine(line);
        Trace.WriteLine("");
        foreach(var cans in results)
            Trace.WriteLine(cans.Key.first + "," + cans.Key.second + ": " + cans.Value);
        // gb.ClearText();gb.Execute(SpacePath("LUUUL"));gb.Press(Joypad.A);gb.ClearText();gb.AdvanceFrames(10);gb.Dispose();
    }
}
