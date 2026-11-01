using MemMop.Core;
using MemMop.Core.Reclaimers;

const double gb = 1024d * 1024 * 1024;
var s = MemorySnapshot.Capture();

Console.WriteLine($"before     available {s.Available / gb:0.0} GB · standby {s.Standby / gb:0.0} GB");

var engine = new CleaningEngine(new IMemoryReclaimer[] { new StandbyListReclaimer() });

foreach (var (name, result) in engine.Run(new[] { "StandbyList" }))
{
    Console.WriteLine($"reclaim    {name}");
    Console.WriteLine($"  succeeded {result.Succeeded}");
    Console.WriteLine($"  freed     {result.FreedBytes / gb:0.00} GB");
    Console.WriteLine($"  message   {result.Message ?? "-"}");
}

var after = MemorySnapshot.Capture();
Console.WriteLine($"after      available {after.Available / gb:0.0} GB · standby {after.Standby / gb:0.0} GB");
