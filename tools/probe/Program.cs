using MemMop.ViewModel;

var vm = new MainViewModel();
vm.ProcessUsageExpanded = true;

Console.WriteLine($"rows          {vm.ProcessUsageItems.Count}");
foreach (var r in vm.ProcessUsageItems.Take(3))
    Console.WriteLine($"  {r.Name,-18} x{r.Instances,-3} {r.Size}");
Console.WriteLine($"total line    {vm.ProcessUsageTotal}");

Console.WriteLine($"available     {vm.AvailableProcesses.Count}");
vm.ProcessSearchText = "chr";
Console.WriteLine($"filter 'chr'  {string.Join(",", vm.AvailableProcesses.Take(5))}");

Console.WriteLine($"excluded now  {vm.ExcludedProcesses.Count}");
vm.SelectedProcess = vm.AvailableProcesses.FirstOrDefault() ?? "";
vm.AddExclusionCommand.Execute(null);
Console.WriteLine($"after add     {vm.ExcludedProcesses.Count} -> {string.Join(",", vm.ExcludedProcesses)}");
