using MemMop.Core;
using MemMop.Core.Reclaimers;

const double gb = 1024d * 1024 * 1024;
var engine = new CleaningEngine(new IMemoryReclaimer[]
{
    new StandbyListReclaimer(),
    new StandbyListReclaimer(true),
    new ModifiedPageListReclaimer(),
    new CombinedPageListReclaimer(),
    new SystemFileCacheReclaimer(),
    new RegistryCacheReclaimer(),
    new WorkingSetReclaimer(),
});

foreach (var (name, r) in engine.Run(new[] { "StandbyList", "StandbyListLowPriority", "ModifiedPageList", "CombinedPageList", "SystemFileCache", "RegistryCache", "WorkingSet" }))
    Console.WriteLine($"{name,-22} ok={r.Succeeded,-5} freed={r.FreedBytes / gb,6:0.00} GB  {r.Message ?? "-"}");
