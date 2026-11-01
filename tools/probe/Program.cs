using MemMop.ViewModel;

var vm = new MainViewModel();
Console.WriteLine($"last clean text   {vm.LastCleanText}");
Console.WriteLine($"has timestamp     {vm.LastCleanAt.HasValue}");
