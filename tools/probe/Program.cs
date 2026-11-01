using MemMop.Core;

var s = MemorySnapshot.Capture();
const double gb = 1024d * 1024 * 1024;

Console.WriteLine($"total      {s.Total / gb,8:0.0} GB");
Console.WriteLine($"available  {s.Available / gb,8:0.0} GB");
Console.WriteLine($"active     {s.Active / gb,8:0.0} GB");
Console.WriteLine($"modified   {s.Modified / gb,8:0.0} GB");
Console.WriteLine($"standby    {s.Standby / gb,8:0.0} GB");
Console.WriteLine($"free       {s.Free / gb,8:0.0} GB");
Console.WriteLine($"commit     {s.CommitUsed / gb,8:0.0} / {s.CommitLimit / gb:0.0} GB");
Console.WriteLine($"segments   {s.HasSegments}");
var sum = s.Active + s.Modified + s.Standby + s.Free;
Console.WriteLine($"sum-check  {sum / gb,8:0.0} GB  (must equal total)");
