# 交接文档

写给下一个接手的人（或下一个会话的我自己）。目标：**半小时内**知道项目在哪、怎么构建、怎么验证、以及哪些说法已经不成立。

本文件核对时间：**2026-11-02**。下面每一条"实测"都是这一轮跑出来的，不是从旧文档抄的。

---

## 一、一分钟版

| 项 | 值 |
|---|---|
| 仓库 | `D:\MemMop\MemMop`（分支 `main`，工作区**干净**，HEAD `8d00749`） |
| 远程 | `https://github.com/MiraCeo/MemMop.git` |
| 同步状态 | **本地领先 79 个提交，全部没推**；`origin/main` 只到 `072e437 chore: project founding` |
| 形态 | .NET Framework 4.7.2 + WPF，单 exe，产物 **99.5 KB**，无外部依赖 |
| 版本 | `0.2.0`（`src/MemMop/MemMop.csproj`），构建时自动附加 git sha |
| 规模 | `src` 下 40 个 `.cs` / `.xaml`，共 **4309 行**（不含 bin/obj） |
| 一句话 | 功能对齐 1.x；界面还差几处；双语只做了三成；自动清理没有设置入口 |

**接手第一件事：先 `git push` 或至少 `git bundle` 备份那 79 个提交。** 现在这套代码只存在于这一块磁盘上。

---

## 二、代码结构

```
src/MemMop/
  Core/                    与界面无关的一切
    NativeMethods.cs       NtSetSystemInformation / EmptyWorkingSet 等 P/Invoke
    MemorySnapshot.cs      四段构成（活动/已修改/待机/空闲）+ 已提交
    IMemoryReclaimer.cs    清理项的统一接口（Try / 名称 / 代价）
    Reclaimers/*.cs        7 个清理项的实现
    AutoCleanPolicy.cs     自动清理的"什么时候跑、跑哪些"（纯规则，可单测）
    Settings.cs            HKCU 存储
    AutoStart.cs           登录触发的计划任务（schtasks）
    Privileges.cs          SeProfileSingleProcess / SeDebug 提权
    Log.cs                 事件日志优先，失败落文件
    Strings.cs             中英两份文案（**没有 JSON**，全部内联在这里）
  Services/
    TrayService.cs         唯一知道 WinForms 的文件（NotifyIcon + ContextMenuStrip）
    HotkeyService.cs       RegisterHotKey 全局热键
  ViewModel/MainViewModel.cs  700 行，逻辑都在这里
  Controls/                ProportionBar（四段条）、RingChart（紧凑视图环形）
  Theme/                   6 套色板 + Styles.xaml（519 行控件模板），默认 `Tokens.Ceobe.xaml`
```

分层与 `docs/SPEC.md` 第 10 节一致：UI 不直接调系统 API，引擎不知道"手动还是自动"，策略层决定这次跑哪些区域。

**运行时事实**（改之前先知道）：

| 项 | 事实 |
|---|---|
| 设置存放 | `HKCU\SOFTWARE\MemMop`（绿色免安装的前提） |
| 日志 | Windows 事件日志，源名 `MemMop`；注册失败时降级写文件，**任何路径都不抛异常** |
| 单实例 | `Mutex("MemMop.SingleInstance")` |
| 开机自启 | `schtasks /TN MemMop`，最高权限、登录触发，不弹 UAC |
| 清理热键 | 默认**关闭**，`Ctrl+Alt+M`（`Settings.HotkeyModifiers/Key`） |
| 自动清理 | `DispatcherTimer` 30 秒一跳 → `TryAutoCleanAsync`；**但界面上没有设置入口，`AutoCleanMinutes` / `LowMemoryPercent` 默认 0 ⇒ 实际永不触发** |

---

## 三、构建

```powershell
cd D:\MemMop\MemMop
pwsh -NoProfile -File build.ps1
```

实测输出（2026-11-02）：

```
  output  : D:\MemMop\MemMop\dist\MemMop.exe
  version : 0.2.0+8d007490c4b5cb1d5b2a19972dc28115880039f2
  size    : 99.5 KB
  sha256  : FD7FC512E1E18D19560BDA270444E71D02F4F2BDFB4A000C0F114DFC202B019B
  needs   : .NET Framework 4.7.2+ (installed on Windows 10 1803+ and Windows 11)
  beside  : MemMop.exe.config, MemMop.pdb (not required to run)
```

- **构建前必须关掉正在运行的 MemMop**，否则替换 `dist` 里的 exe 会失败（脚本会报错，不会静默复制旧文件）。
- `MemMop.exe.config` 只声明 `supportedRuntime`，`MemMop.pdb` 只用于调试，**两个都不是运行必需**，交付时只发 exe。
- 本机 SDK 是 **dotnet 10**，能编译 `net472`（.NET Framework 参考程序集已装），不需要 VS。
- `build.ps1` 用 `$MyInvocation.MyCommand.Path` 定位根目录，**没有硬编码路径**，整个仓库可以搬到任何位置。

---

## 四、三个验证工具

| 工具 | 命令 | 现在的实际状态 |
|---|---|---|
| `tools/probe` | `dotnet run --project tools/probe -c Release` | **只验热键**。`Program.cs` 只剩 10 行，输出 4 行热键数值。`README` 里"验证内存数值、清理结果、策略规则"的描述已经不成立 |
| `tools/shot` | `dotnet build tools/shot/Shot.csproj -c Release` 然后 `tools\shot\bin\Release\net472\Shot.exe <目录>` | **可用**。离屏把主页/设置/紧凑视图渲染成 PNG（540×640 @144 DPI）。只保留了 `Ceobe` 一套色板和 `L3` 一个变体 |
| `tools/measure` | `dotnet run --project tools/measure` | 渲染单个自绘元素并扫像素，报包围盒与偏移。用于"居中到底居中没"这类必须量数字的地方 |

`tools/probe` 会被改写之前，**不要指望它验证清理逻辑**。要验证清理结果，用 `tools/shot` 看界面 + 手动跑一次清理看通知。

截图不依赖提权、不依赖桌面会话，可以在这台机器的无头环境里跑。

---

## 五、git 状态（最需要注意的一条）

```
HEAD        8d00749  docs: a handoff note ...           2026-11-02 23:45
origin/main 072e437  chore: project founding            2026-11-01 16:16
差距        79 个提交，从未推送
```

`origin/main` 上只有一个"奠基"提交，也就是说**远端仓库里没有任何实现代码**。推送是本轮交接里优先级最高的一件事。

---

## 六、未完成清单（按"能立刻动手"排序）

### 1. 排除卡片还剩三处 XAML（数据层已就绪，只差界面）

`MainWindow.xaml` 里「排除的进程（不参与清理）」那张 Border：

| # | 要做什么 | 现状 |
|---|---|---|
| 1 | 删掉 `Text="添加排除"` 的小标题（连同上方的空行） | 还在，`MainWindow.xaml:356-359` |
| 2 | 搜索用的 `ToggleButton` 加 `Style="{StaticResource IconButton}"` | 没有 `Style`，放大镜裸着，和右边的 `+` 按钮不齐 |
| 3 | 「排除列表」去掉右侧 `−`，改成列表下方一行 `上移` / `下移` / `删除`，右对齐，`Visibility` 绑 `HasSelectedExclusion` | 还是单个 `−` 按钮，`MainWindow.xaml:419-446` |

数据层都在：`MoveUpCommand`、`MoveDownCommand`、`HasSelectedExclusion`、`SelectedExclusion` 在 `MainViewModel`；`IconButton` 样式在 `Theme/Styles.xaml`。**只改 XAML，不要动 ViewModel。**

### 2. 双语只做了三成

`MainWindow.xaml` 里 **30 行写着硬编码中文**，只有 **6 行**绑定了 `Strings.Current`。所以切到 English 之后，标签页标题会变、设置页那一片不会变。

`Strings.cs` 是内联的两份文案（没有 JSON、没有 Localizer），加键的成本是"改一个属性 + 两个分支"。要真正双语，得把 XAML 里 30 处中文换成绑定。

### 3. 自动清理没有设置入口

逻辑全在（`AutoCleanPolicy` 的 `IsIntervalDue` / `IsMemoryDue` / `SelectForRun`，`App.xaml.cs:91` 的 30 秒定时器），但设置页没有间隔滑块、没有低内存阈值滑块，`Settings.AutoCleanMinutes` 和 `LowMemoryPercent` 默认 0，等于关。**要么加两个滑块，要么把这条能力从 README 里删掉。**

### 4. 设置页内容超出窗口

截图可见：`排除列表` 的 ListBox 在窗口底部被裁掉，`重置` 按钮在折叠线以下。`ScrollViewer` 在，但默认视口不够。

### 5. 主页密度

「清理区域」卡片在折叠线以上只露出 6 行（第 7 项 `收缩程序驻留内存` 要滚）；「进程占用」整段在折叠线以下。

### 6. 关于页

仍是三个 TextBlock 堆着，没有按卡片规则排。文案与 `Strings.AboutText` 重复。

### 7. 交付形态

图标仍是系统默认、没有安装包、没有签名、`app.manifest` 已要求 `requireAdministrator`。

### 8. 仓库卫生

`src/MemMop/` 下有 3 个 **被 git 跟踪**的 WPF 临时工程：`MemMop_do5rt4j5_wpftmp.csproj`、`MemMop_n0rr53ki_wpftmp.csproj`、`MemMop_qkedarj5_wpftmp.csproj`。它们是 `dotnet build` 断掉时留下的垃圾，应当 `git rm` 并在 `.gitignore` 里加 `*_wpftmp.csproj`。

---

## 七、文档地图：哪份可信

| 文件 | 可信度 | 说明 |
|---|---|---|
| `docs/SPEC.md` | **高** | 产品决定（定位、界面结构、默认值、行为规则、指标口径）。2.x 大体按它实现；只有 §2.2 的"自动清理两个滑块"还没做 |
| `docs/MEMORY-API.md` | **高** | 底层调用与坑，面向事实，与实现版本无关 |
| `docs/PROVENANCE.md` | **高** | 来源记录 |
| `docs/EDITING.md` | **中** | "四条硬规则"与"第零条"仍然有效；但**末尾"验证流程"整段是 1.x 遗留**（见下） |
| `README.md` | **中** | 定位与功能描述准确；但"约 54 KB"过时（现 99.5 KB），"自动清理：定时与低内存阈值"实际没有入口 |

`docs/EDITING.md` 里失效的部分（1.x 时代、路径 `D:\MemClean` 已不存在）：

```powershell
# 这些脚本属于 1.x 的 WinMemoryCleaner 分支，与 2.x 仓库无关
pwsh -File D:\MemClean\refs\wnmc\build.ps1
pwsh -File D:\MemClean\tools\verify-fork-invariants.ps1
pwsh -File D:\MemClean\tools\verify_localization_load.ps1
pwsh -File D:\MemClean\tools\render-ui.ps1 -Normal
```

2.x 的验证只有三条：**`build.ps1` 构建 → `tools/shot` 出图 → `tools/probe` 跑一遍不崩**（probe 现在很薄，见第四节）。"语言键三边同步""清单检查"那一套在 2.x 不存在，因为 2.x 没有 JSON 语言文件和 manifest 校验脚本。

---

## 八、工作区布局（`D:\MemMop`）

| 路径 | 是什么 | 交接后还要吗 |
|---|---|---|
| `MemMop/` | **当前仓库（2.x）** | 要。唯一真相 |
| `MemClean/` | KoshyJohn Memory Cleaner 3.00 的逆向资料（原版 exe、反编译源码、`FINDINGS.md`） | 要。结论的来源 |
| `refs/` | **参考资料**（见 `refs/README.md`）：`wnmc/` 1.x 分支源码、`WMC/` 上游原版 | 要 |
| `dist/MemMop_Archived.7z` | 1.x 的 72 个 exe 归档（29 MB → 1.2 MB） | 要 |
| `docs/`（根目录，4 个 md） | 仓库 `docs/` 的旧副本，`EDITING.md` 比仓库里的旧 30 行 | 不要 |
| `tools/`（根目录） | 会话期一次性脚本：212 个 `.py`、截图、1.x 时代的 `.ps1` | 不要 |
| `build/` | 1.x WPF 阶段的构建残留（`qa/` + msbuild 日志） | 不要 |
| `debug.log` | 会话日志 | 不要 |

精确体积与删除分级见工作区根目录的 `CLEANUP.md`。

> 2026-11-03 已清理：`MemMop/src/MemMop/bin`（160.7 MB，其中 160.4 MB 是 .NET 8 死产物）、`obj`、
> 三个工具的 `bin`/`obj`、`MemClean/toolchain/autoit3`（32.5 MB）、`refs/wnmc/src/packages`（234.7 MB）
> 等共 480 MB 已删。删完 `build.ps1` 复验通过，`bin` 从 160.7 MB 变成 0.13 MB。

---

## 九、环境坑（这一轮实际踩到的）

1. **这个 shell 是 Windows PowerShell 5.1，不是 pwsh 7。** `Get-Content` 默认按 GBK 解码，读 UTF-8 无 BOM 的中文文件会变成 `蹇嗗共浜屽噣` 这种乱码。规则：**读中文一律** `Get-Content -Encoding utf8` 或 `[System.IO.File]::ReadAllText($p, [Text.Encoding]::UTF8)`；跑脚本用 `pwsh -NoProfile -File`（7.6.9 已装）。
2. **仓库里所有源文件都是 UTF-8 无 BOM**。任何"读-改-写"的脚本如果用了默认编码，会把中文写坏。
3. 本机只有 dotnet 10 SDK，没有 Visual Studio。`net472` 能编，WPF XAML 编译也正常。
4. `dotnet build` 中途被打断会在 `src/MemMop/` 留下 `*_wpftmp.csproj`，见第六节第 8 条。
5. **构建前关掉 MemMop**。`dist\MemMop.exe` 被进程占用时 `Copy-Item` 会失败并抛出。

---

## 十、一句话

**这个仓库的可信度来自三件事：能编译、能量化、能看图。** 任何绕过其中之一的"改好了"，都曾经是错的。

---

## 附：本轮交接做过的验证（可复现）

```powershell
# 1. 构建
pwsh -NoProfile -File build.ps1
#    → 0.2.0+8d00749，99.5 KB，sha256 FD7FC512E1E18D…

# 2. 探针（只验热键）
dotnet run --project tools/probe -c Release
#    → hotkey enabled False / modifiers 0x3 / key 0x4D / after enable True

# 3. 截图（主页 + 设置 + 紧凑）
dotnet build tools/shot/Shot.csproj -c Release
tools\shot\bin\Release\net472\Shot.exe D:\MemMop\tools\mmshots\handoff
#    → ceobe-L3.png / ceobe-settings.png / ceobe-compact.png
```
