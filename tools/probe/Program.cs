using MemMop.ViewModel;

var vm = new MainViewModel();
Console.WriteLine($"areas        {vm.Areas.Count}");
Console.WriteLine($"can execute  {vm.CleanCommand.CanExecute(null)}");

foreach (var a in vm.Areas)
    Console.WriteLine($"  {(a.IsSelected ? "x" : " ")} {a.Title,-16} {a.Risk}");

vm.Areas[0].IsSelected = false;
Console.WriteLine($"after untick can-execute still {vm.CleanCommand.CanExecute(null)}");

vm.Areas[0].Apply(new MemMop.Core.ReclaimResult(true, 3L * 1024 * 1024 * 1024, null));
Console.WriteLine($"row result   {vm.Areas[0].LastResult}");
vm.Areas[0].Apply(new MemMop.Core.ReclaimResult(true, -5, null));
Console.WriteLine($"zero result  {vm.Areas[0].LastResult}");
vm.Areas[0].Apply(MemMop.Core.ReclaimResult.Failure("privilege not held"));
Console.WriteLine($"failure      {vm.Areas[0].LastResult}");
