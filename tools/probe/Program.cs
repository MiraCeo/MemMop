using MemMop.Core;
using MemMop.ViewModel;

var vm = new MainViewModel();
var zh = VM_First(vm);
Strings.Current.SetLanguage(true);
var en = VM_First(vm);
Strings.Current.SetLanguage(false);
var back = VM_First(vm);

Console.WriteLine($"zh        {zh}");
Console.WriteLine($"en        {en}");
Console.WriteLine($"back      {back}");
Console.WriteLine($"tab zh    {Strings.Current.Home} / clean {Strings.Current.Clean}");

static string VM_First(MainViewModel vm) => vm.Areas.Count == 0 ? "-" : $"{vm.Areas[0].Title} | {vm.Areas[0].Risk}";
