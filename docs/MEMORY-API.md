# Windows 内存清理：调用与坑

这份文档只记录**事实**（哪个调用、作用于什么、代价是什么、已知问题），不记录任何一行上游代码。
它的用途是：无论以后继续改 MemMop 还是从零重写，都不必再翻旧源码。

---

## 一、系统内存的分段

| 段 | 含义 |
|---|---|
| **活动** | 当前被引用、驻留在物理内存中的页 |
| **已修改** | 脏页，等内核写回磁盘；写回后转入待机 |
| **待机** | 不再被引用但内容仍有效，可被随时回收；**它本身已计入"可用"** |
| **空闲** | 完全未被使用 |

**"使用中" = 活动 + 已修改。** 释放待机只会让"空闲"变大，**不会让"可用"变大**——这是所有"清理内存"宣传里最常见的一句话术错误。

---

## 二、可用的调用

### 1. 清空待机列表

```
NtSetSystemInformation(SystemMemoryListInformation = 80,
                       &command, sizeof(int))
    command = 4   // MemoryPurgeStandbyList       清空全部待机页
    command = 5   // MemoryPurgeLowPriorityStandbyList  只清优先级 0 的待机页
```

- **作用**：待机页 → 空闲页
- **代价**：被清掉的文件缓存需要重新读盘；对"可用内存"几乎无影响
- **关系**：4 是 5 的超集；同时请求没有意义
- **权限**：`SeProfileSingleProcessPrivilege`

### 2. 收缩进程工作集

```
EmptyWorkingSet(handle)          // psapi
command = 2                      // MemoryEmptyWorkingSets：系统级，清所有进程
```

- **作用**：把各进程驻留的页移出工作集（转入待机或页面文件）
- **代价**：程序再次访问时缺页，**用户能直接感知卡顿**；这是最激进的一项
- **系统级命令无法遵守任何排除名单**——只在"不需要排除"时才可用它
- **权限**：`SeDebugPrivilege`（清其他账户的进程）

### 3. 写回已修改页

```
NtSetSystemInformation(80, &command, sizeof(int))
    command = 3   // MemoryFlushModifiedList
```

- **作用**：脏页写回磁盘 → 转入待机 → "使用中"下降
- **代价**：**实测约 8 秒**（机械盘更久）；期间没有可反馈的进度
- **权限**：`SeProfileSingleProcessPrivilege`

### 4. 合并重复内存页

```
NtSetSystemInformation(SystemCombinePhysicalMemoryInformation = ?, NULL, 0)
```

- **作用**：内核把内容完全相同的物理页合并（去重），不删除数据
- **代价**：合并本身消耗 CPU；收益取决于系统里重复内容的多少
- **可用性**：Windows 8 及以上

### 5. 收缩系统文件缓存

```
NtSetSystemInformation(SystemFileCacheInformation = 21, &info, sizeof(info))
SetSystemFileCacheSize(minimum, maximum, 0)
```

- **作用**：压低内核给文件系统留的缓存上限
- **代价**：之后文件读写变慢；影响持续到缓存重新增长，**不是一次性效果**

### 6. 注册表缓存整理

```
NtSetSystemInformation(SystemRegistryReconciliationInformation = 155, NULL, 0)
```

- **作用**：触发注册表 hive 的整理与落盘
- **代价**：小
- **收益**：**因环境而异，大部分情况下难以观测**

### 7. 已修改的文件缓存 —— **不建议使用**

上游的实现是：枚举每个固定卷 → 打开卷句柄 → `DeviceIoControl` 一个常量
`0x000900F8`（自称 `FSCTL_RESET_WRITE_ORDER`）。

- 同文件里还有一个 `0x00090054`（自称 `FSCTL_DISCARD_VOLUME_CACHE`）
- **两个都在公开文档里查不到**，名称是反推的
- 作用于**卷的写序提示**，不是内存对象；**没有任何证据表明它能释放物理内存**

结论：无法用一句人话解释它做什么 → 不应出现在面向用户的界面上。

---

## 三、指标口径（容易做错的地方）

| 指标 | 取自 | 陷阱 |
|---|---|---|
| 工作集 | `Process.WorkingSet64` / `GetProcessMemoryInfo.WorkingSetSize` | **含共享页**，28 个 chrome 各算一遍共用 DLL，求和会超过物理内存总量 |
| **专用工作集** | `PROCESS_MEMORY_COUNTERS_EX2.PrivateWorkingSetSize` | 任务管理器「内存」列就是它；**求和不虚高**，但不含共享页 |
| 专用提交 | `PrivateMemorySize64` / `PrivateUsage` | 含已换出到页面文件的部分，求和接近"已提交"，不等于物理占用 |

**结论**：面向用户的"谁占了多少内存"要用**专用工作集**；合计必须标注"不含共享页"，否则会与系统的"使用中"对不上。

---

## 四、本次踩过的具体坑（值得写进任何实现的检查清单）

1. **单位**：内部按 MB 算差值，界面按字节显示 → 数字缩小 1048576 倍（12.8 GB 显示成 12.8 KB）
2. **负数**：可用内存来自 5 秒刷新一次的缓存，前后差可能为负 → 必须钳到 0 或显示"无变化"
3. **进程名归一化**：为"防手输"做 `ToLower` + 去空格，把 `DSH Desktop` 存成 `dshdesktop`，从此永远匹配不上
4. **同名进程**：排除与统计都按名字，`chrome` 一次作用于 28 个进程；界面要显示数量，否则用户以为是单个
5. **容器样式的连带效果**：基础样式里 `IsMouseOver → Foreground=Accent` 会让 `ItemsControl` 之类的容器整块变色
6. **绑定被顶掉**：代码里直接给已绑定的依赖属性赋值（如 `window.Topmost = ...`），会永久移除该绑定
7. **静默失败**：创建计划任务失败仍然把勾选状态存下来 → 用户以为开了，其实没有；必须回滚并提示
8. **权限**：`EmptyWorkingSet` 默认按 `QUERY_INFORMATION | VM_READ` 取句柄，失败时回退 `LIMITED`
