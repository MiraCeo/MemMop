using MemMop.Core;
using MemMop.ViewModel;

var vm = new MainViewModel();
Console.WriteLine($"hotkey enabled   {vm.HotkeyEnabled}");
Console.WriteLine($"modifiers        0x{vm.HotkeyModifiers:X}  (MOD_CONTROL)");
Console.WriteLine($"key              0x{vm.HotkeyKey:X}  (C)");
vm.HotkeyEnabled = true;
Console.WriteLine($"after enable     {vm.HotkeyEnabled} (settings={Settings.HotkeyEnabled})");
vm.HotkeyEnabled = false;
