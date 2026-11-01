using MemMop.Core;

var all = new[] { "StandbyList", "StandbyListLowPriority", "ModifiedPageList", "CombinedPageList", "SystemFileCache", "RegistryCache", "WorkingSet" };

Console.WriteLine("manual, safe on   : " + string.Join(",", AutoCleanPolicy.SelectForRun(all, false, true)));
Console.WriteLine("auto, safe on     : " + string.Join(",", AutoCleanPolicy.SelectForRun(all, true, true)));
Console.WriteLine("auto, safe off    : " + string.Join(",", AutoCleanPolicy.SelectForRun(all, true, false)));

var policy = new AutoCleanPolicy();
var now = DateTime.Now;
Console.WriteLine($"interval 0 min    : {policy.IsIntervalDue(now, 0)}");
Console.WriteLine($"interval 30, fresh: {policy.IsIntervalDue(now, 30)}");
policy.MarkIntervalRun(now.AddMinutes(-31));
Console.WriteLine($"interval 30, 31m  : {policy.IsIntervalDue(now, 30)}");

var snapshot = MemorySnapshot.Capture();
var availablePercent = snapshot.Available * 100.0 / snapshot.Total;
Console.WriteLine($"available         : {availablePercent:0.0}%");
Console.WriteLine($"memory due @ 0    : {AutoCleanPolicy.IsMemoryDue(snapshot, 0)}");
Console.WriteLine($"memory due @ 10   : {AutoCleanPolicy.IsMemoryDue(snapshot, 10)}");
Console.WriteLine($"memory due @ 90   : {AutoCleanPolicy.IsMemoryDue(snapshot, 90)}");
