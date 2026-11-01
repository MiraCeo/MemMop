using MemMop.Core;

var s = MemorySnapshot.Capture();
var t = Strings.Current;

Console.WriteLine(t.Physical(s.Total - s.Available, s.Total));
Console.WriteLine(t.Segments(s.Active, s.Modified, s.Standby, s.Free));
Console.WriteLine(t.Commit(s.CommitUsed, s.CommitLimit));

t.SetLanguage(true);
Console.WriteLine(t.Physical(s.Total - s.Available, s.Total));
Console.WriteLine(t.Segments(s.Active, s.Modified, s.Standby, s.Free));
t.SetLanguage(false);
