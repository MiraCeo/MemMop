using MemMop.ViewModel;

var vm = new MainViewModel();
Console.WriteLine($"defaults   committed={vm.ShowCommitted} detailed={vm.DetailedMemory} font={vm.FontSize}");
vm.FontSizeIndex = 2;
Console.WriteLine($"font idx 2 -> {vm.FontSize}");
vm.ShowCommitted = false;
Console.WriteLine($"committed off -> {vm.ShowCommitted} (settings={MemMop.Core.Settings.ShowCommitted})");
vm.ResetSettingsCommand.Execute(null);
Console.WriteLine($"after reset committed={vm.ShowCommitted} detailed={vm.DetailedMemory} font={vm.FontSize}");
