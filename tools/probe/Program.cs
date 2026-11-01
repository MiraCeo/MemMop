using MemMop.Core;
using MemMop.ViewModel;

var vm = new MainViewModel();

Console.WriteLine($"start-minimized default   {vm.StartMinimized}");
vm.StartMinimized = false;
Console.WriteLine($"after set false           {vm.StartMinimized} (settings={Settings.StartMinimized})");
vm.StartMinimized = true;

Console.WriteLine($"auto-clean due (interval 0) {vm.TryAutoCleanAsync(DateTime.Now).Result ?? "null (not due)"}");

vm.SelectedProcess = vm.AvailableProcesses.FirstOrDefault() ?? "";
vm.AddExclusionCommand.Execute(null);
var added = vm.ExcludedProcesses.FirstOrDefault() ?? "";
Console.WriteLine($"excluded added            {added}");
vm.SelectedExclusion = added;
Console.WriteLine($"can remove                {vm.RemoveExclusionCommand.CanExecute(null)}");
vm.RemoveExclusionCommand.Execute(null);
Console.WriteLine($"after remove              {vm.ExcludedProcesses.Count}");
