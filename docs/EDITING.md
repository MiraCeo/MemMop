# 修改 MemMop 代码的纪律

这份文档不是流程表演。它是同一类返工在一个会话里出现 **八次** 之后总结出来的清单——
每次返工都让使用者额外等一轮，而原因几乎都是"我假设了代码长什么样，而没有先确认"。

---

## 第零条：先算交付形态，再定技术选型

**反例（真发生过）**：为一个几百 KB 的清理工具推荐 .NET 8 + WPF，理由写着"迁移成本最低"。
结果交付时才发现：自包含单文件约 **150 MB**，框架依赖则要求每个用户先装 **55 MB 运行时**。
两条路都让这个产品不成立，只能整体回退到 .NET Framework 4.7.2。

**规则**：涉及运行时、框架、依赖包、安装方式的选择，**先把最终用户要付出的代价摆出来**
（体积、是否要装环境、权限、启动时间），再谈开发便利。顺序颠倒一次，就要重做一轮。

---

## 四条硬规则

### 1. 先 grep 真实文本，再写替换

**反例**：凭记忆写 16 空格缩进，实际是 24 空格 → 锚点不匹配，静默不生效。
一个文件连续三次锚点失败，最后只能改成按行号插入。

**做法**：写替换前先打印目标区域的原文（含缩进），或直接用行号定位。

### 2. 用边界匹配，不用裸子串

**这次的实例（全部真实发生过）**：

| 替换 | 被误伤 | 结果 |
|---|---|---|
| `_trayIcon` | `_trayIconDescription` | 声明被切成 `Description` 残片 |
| `Optimize` | `OptimizedManually` | 变成 `OptimizingdManually`，编译失败 |
| `"_lastCleanAt;"` 判断存在性 | 命中 getter `return _lastCleanAt;` | 字段没插入，编译失败 |
| `"_openMainWindow;"` 判断存在性 | 同上 | 同上 |

**做法**：用完整声明（含类型与修饰符）判断存在性；替换用正则 `\b` 或精确整行。

### 3. 分组声明要手术，不要整行删

```csharp
private string _about, _add, _alwaysOnTop, _alwaysOnTopDescription, _autoOptimization;
```

整行删除会**带走兄弟字段**。这次因此把 `_hotkeyExpanded`、`_defaultState` 连根拔了两次，
只能 `git checkout` 回滚重做。

**做法**：只删目标名字和相邻的那个逗号，其余原样保留。

### 4. 改完必须查残留引用

删除或重命名后，全仓搜索被改的标识符：

```powershell
Get-ChildItem src -Recurse -Include *.cs,*.xaml |
  Select-String -Pattern '旧标识符' |
  ForEach-Object { "$($_.Filename):$($_.LineNumber) $($_.Line.Trim())" }
```

期望 0 处，或只剩**预期之内**的（例如 `ObsoleteValueNames` 里的字符串常量）。

**另外**：`XAML` 里绑定的语言键被删后，WPF **静默失败**——不报错，只显示空白。
所以删语言键时必须同时清理引用它的 XAML。

---

## 一个必须记住的编译期陷阱

测试文件与主项目**编译在同一个 csproj 里**。所以删任何 `Settings` 成员，
`Test/*.cs` 里的引用同样会让构建失败。改完主代码要顺手搜测试目录。

---

## 验证流程（每次改代码后）

```powershell
# 1. 构建
pwsh -NoProfile -File D:\MemClean\refs\wnmc\build.ps1

# 2. 不变量（22 项：身份替换、清单、注册表键、通知、语言……）
pwsh -NoProfile -File D:\MemClean\tools\verify-fork-invariants.ps1

# 3. 语言加载（2 种语言，强制所有键非空）
pwsh -NoProfile -File D:\MemClean\tools\verify_localization_load.ps1

# 4. 需要时截图（144 DPI 离屏渲染）
pwsh -NoProfile -File D:\MemClean\tools\render-ui.ps1 -Normal
```

**语言校验的作用**：`Localizer` 在**任何**字符串属性为空时抛异常。
所以"模型里有属性、JSON 里没有键"会让所有语言加载失败。删键时必须三边同步：
`Localization.cs` 的属性 + 字段 + 每个 JSON 的键。

---

## 两个会互相干扰的默认值（已修正，但值得记住）

### 截图之后，`bin\Release` 里是无清单版本

截图工具用 `-NoManifest` 构建（否则沙箱启动不了它）。所以**截完图立刻跑不变量检查，
必然有两项清单检查失败**（"manifest requires administrator" / "manifest is DPI aware"），
看起来像刚改的东西坏了。

现在 `render-ui.ps1` 在收尾时会重新做一次带清单的构建。

### 不传 `-Exe`，验证的是**原始上游二进制**

`verify-fork-invariants.ps1` 的默认值原本指向 `dist\MemMop\MemMop.exe`——那是改名后的原版
WMC（3.0.8），不是我们的构建。漏传参数就会得到一堆莫名其妙的失败（英文界面、30 个语言、
托盘组还在……），而实际代码没问题。

默认值已改为 `refs\wnmc\src\bin\Release\MemMop.exe`，但仍建议显式传参。

## 已知无法自动验证的部分

| 部分 | 原因 | 替代办法 |
|---|---|---|
| **托盘右键菜单外观** | WinForms `ContextMenuStrip` 需要真实消息循环；离屏 `DrawToBitmap` 会挂死（试过两次，已放弃） | 由使用者目视确认 |
| **气泡通知** | 由 Shell 渲染，不是我们的窗口 | 使用者手动触发清理观察 |
| **托盘图标在高 DPI 下的清晰度** | 同上 | 使用者目视 |

---

## 工具与约定

- **Shell**：优先 `pwsh`（PowerShell 7）。5.1 会把中文输出写成乱码；四个脚本已加 UTF-8 守卫，但新脚本仍建议走 pwsh。
- **截图工具参数**：`-TextSize n`、`-Detailed`、`-Expanded`、`-Compact`、`-Normal`、`-Pin`、`-Focus <元素名>`、`-SkipBuild`、`-Out`
- **截图会读真实配置**：用户注册表里的状态会影响渲染结果，所以回归时用 `-Compact` / `-Normal` 等显式指定。
- **构建产物**：`refs/wnmc/src/bin/Release/MemMop.exe`；交付包按 `MemMop_0NN.exe` 命名，版本号 = 构建号的三位数字（`049` → `0.4.9`）。
- **打包需要允许**：未经同意不要复制到 `dist/`。

---

## 版本与交付

- 版本号规则：构建号 `0NN` 直接映射为版本 `0.N.N`（`046` → `0.4.6`）
- 每次交付：构建 → 验证 → 复制为 `dist/MemMop/MemMop_0NN.exe` → 记录 sha256
- 大改动前先升版本，方便使用者确认跑的是哪一版

---

## 第零条之二：改完要说"改成了"，就去量

同一处导航居中，我连续三版都声称改好，实际是：

| 版本 | 我的说法 | 实测（像素） |
|---|---|---|
| 1 | "上下各约 12px" | 上 6px / 下 59px |
| 2 | "已居中" | 上 6px / 下 1px |
| 3 | "已居中" | 上 25px / 下 19px |

前两版的验收方式是"看截图"，而 6px 与 59px 的差距在缩略图里看不出来。**凡是可以用数字描述的（间距、对齐、尺寸），就必须量数字。**

## 第零条之三：替换带嵌套的块，要按深度配对

`t.index("</Style>")` 会遇到嵌套在 `ContentPresenter.Resources` 里的那个 `</Style>`，于是截断位置错误，整个 XAML 结构损坏。

正确做法：找到起始标签后，**按同名标签的嵌套深度计数**，找到真正配对的那个；或者用行号范围替换。替换后必须打印改动的实际内容，而不是"已完成"。