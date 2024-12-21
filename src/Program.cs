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

class Program {

    static void Main(string[] args) {
        Trace.Listeners.Add(new TextWriterTraceListener(File.CreateText("log.txt")));
        Trace.AutoFlush = true;

        // Tests.RunAllTests();
        var stat = new List<byte>{21,11,12,10,11};
        //FrontupPidgey.SearchForest(stats:stat,r1damage:3,minClusterSize:1,path:"UAULULLULLUURUUUUU",maxcost:2);
        
        FrontupPidgey.PruneForest(stats:stat,r1damage:1,minClusterSize:1,maxcost:4);
        //FrontupPidgey.Check();
    }
}
