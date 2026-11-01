using MemMop.Core;

var s = MemorySnapshot.Capture();
var physical = string.Join(",", s.Active, s.Modified, s.Standby, s.Free);
var commit = s.CommitUsed + "," + (s.CommitLimit - s.CommitUsed);
Console.WriteLine($"physical values  {physical}");
Console.WriteLine($"commit values    {commit}");
Console.WriteLine($"palette physical  #E2523B,#9E9E9E,#A5D66A,#43B047");
var parts = physical.Split(',');
var sum = parts.Sum(double.Parse);
Console.WriteLine($"segments count   {parts.Length}  sum={sum:0}  total={s.Total}");
Console.WriteLine($"sum == total     {Math.Abs(sum - s.Total) < 1024}");
