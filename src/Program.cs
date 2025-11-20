using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using static SearchCommon;
using static RbyIGTChecker<Red>;

class Program {

    static void Main(string[] args) {
        Trace.Listeners.Add(new TextWriterTraceListener(File.CreateText("log.txt")));
        Trace.AutoFlush = true;

        // Tests.RunAllTests();
        
        List<int> targetsecs = Enumerable.Range(54, 1).ToList();
        List<int> successList = new List<int>();
        List<int> targetIGT = Enumerable.Range(43, 4).ToList();
        
        //Surge.Search(1,1,42,numThreads:12,minClusterSize:3,igtFrameCluster:4, offset60fps:0, wantQA:false, wantTackle:false);
        Surge.CheckPathsInFile(1,"surgepaths.txt",12);
        Console.WriteLine(DateTime.Now);
    }
}
