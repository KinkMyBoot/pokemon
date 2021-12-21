using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

class SearchCommon
{
    public delegate void FunctionToProfile();
    public static float Profile(string title, FunctionToProfile fn)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        fn();
        watch.Stop();
        float t = watch.ElapsedMilliseconds / 1000.0f;
        Console.WriteLine(title + ": " + t + "s");
        return t;
    }
    public static float Profile(FunctionToProfile fn)
    {
        return Profile("elapsed", fn);
    }
    static System.Diagnostics.Stopwatch Watch;
    static long LastMs = 0;
    public static void StartWatch()
    {
        Watch = System.Diagnostics.Stopwatch.StartNew();
    }
    public static float Elapsed(string title = "elapsed", bool total = false)
    {
        float t = (Watch.ElapsedMilliseconds - (total ? 0 : LastMs)) / 1000.0f;
        LastMs = Watch.ElapsedMilliseconds;
        Console.WriteLine(title + ": " + t + "s");
        return t;
    }
    public static float ElapsedTotal(string title = "elapsed")
    {
        return Elapsed(title, true);
    }

    public struct Display {
        public string Path;
        public int SS, C, T, A, S;
        public Display(string path, int success = 0, int cost = 0)
        {
            Path = path;
            SS = success;
            C = cost;
            T = TurnCount(path);
            A = APressCount(path);
            S = StartCount(path);
        }
        public override string ToString()
        {
            string str = Path;
            if(SS > 0)
                str += " " + SS;
            if(C > 0)
                str += " C:" + C;
            if(S > 0)
                str += " S:" + S;
            str += " T:" + T;
            str += " A:" + A;
            return str;
        }
        static public void PrintAll(List<Display> list, string prefix = "")
        {
            foreach (Display d in list.OrderByDescending((d) => d.SS).ThenBy((d) => d.C).ThenBy((d) => d.S).ThenBy((d) => d.A).ThenBy((d) => d.T))
                System.Diagnostics.Trace.WriteLine(prefix + d);
        }
    };
    public static int TurnCount(string path)
    {
        path = Regex.Replace(path, "[AS_B]", "");
        string a = String.Empty;
        int turns = 0;
        for (int i = 1; i < path.Length; ++i)
            if (path[i] != path[i - 1])
                ++turns;
        return turns;
    }
    public static int APressCount(string path)
    {
        return path.Count(c => c == 'A');
    }
    public static int StartCount(string path)
    {
        return path.Count(c => c == 'S');
    }
}
