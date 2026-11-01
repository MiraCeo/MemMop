using MemMop.Core;

var rows = ProcessUsageReader.Read(8);
Console.WriteLine($"rows {rows.Count}");
foreach (var r in rows)
    Console.WriteLine($"  {r.Name,-22} x{r.Instances,-3} {r.Size}");

var total = rows.Sum(r => r.PrivateWorkingSet) / (1024d * 1024 * 1024);
Console.WriteLine($"top-8 private working set {total:0.0} GB");
