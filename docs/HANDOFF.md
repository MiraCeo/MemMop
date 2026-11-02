# 交接文档

写给下一个接手的人（或下一个会话的我自己）。目的是让人在**半小时内**知道项目在哪、怎么验证、以及哪些做法会出问题。

---

## 一、现状

`D:\MemClean\MemMop` —— .NET Framework 4.7.2 + WPF，版本 0.2.0，单个 exe（约 70 KB），无外部依赖。

- 功能：七个清理区域、进程表、排除名单、托盘、热键、开机自启、单实例、事件日志、双语、紧凑视图
- 已完成：功能对齐 1.x（对照表见提交历史），主题层（刻俄柏配色为默认）
- 未完成：排除卡片的三处 UI 改动（见第五节）

---

## 二、构建与验证

```powershell
cd <仓库根>
pwsh -File build.ps1          # 产出 dist\MemMop.exe
```

**构建前必须关掉正在运行的 MemMop**，否则替换 dist 里的 exe 会失败（脚本会明确报错，而不是静默复制旧文件）。

三个验证工具，全部不需要提权：

| 工具 | 用途 | 命令 |
|---|---|---|
| `tools/probe` | 编译同一份 Core 源码，跑引擎/策略/VM，输出数值 | `dotnet run --project tools/probe -c Release` |
| `tools/shot` | 把 `MainWindow` 渲染成 PNG（144 DPI），三套色板 × 主页/设置/紧凑 | 构建后运行 `tools\shot\bin\Release\net472\Shot.exe <输出目录>` |
| `tools/measure` | 渲染单个自绘元素并扫像素，报告包围盒与偏移 | `dotnet run --project tools/measure` |

截图默认输出到 `D:\MemClean\tools\mmshots\`。

---

## 三、工作方式（按重要性排序，全部来自实际返工）

### 1. 改文件：按行号整块替换，不做"删相邻行"

**反例**：`del lines[i-1:i+1]` 删除标题行——行号算错就删进相邻结构，XAML 直接失效，报 `Name cannot begin with '<'`。

**正确**：先定位块起止（`Border` 的 `<Border` 到匹配的 `</Border>`），整块替换，然后**打印写入后的内容**。

### 2. 找闭合标签要按深度计数，不能取第一个

`t.index("</Grid>")` 会撞上嵌套结构的闭合标签。要么按缩进匹配，要么按标签深度计数。

### 3. 字符串替换后必须验证生效

`str.replace(a, b)` 匹配不上时**静默返回原字符串**。脚本里必须：

```python
print("ok" if old in t else "NOT FOUND")
```

否则会出现"脚本说改完了，文件没变，图片 MD5 也没变"。

### 4. 能度量的就度量，不要看图判断

同一处居中我三版都声称改好，实测分别是 上6/下59、上6/下1、上25/下19。**缩略图看不出这些差别。**

可以用数字描述的一律扫描像素：位置、间距、宽高、对齐。

### 5. 改完界面必须自己看，且要 3 倍放大

```powershell
# 裁切 + 放大，看清局部
$big = New-Object System.Drawing.Bitmap 720,300
$g.DrawImage($src, (New-Object System.Drawing.Rectangle 0,0,720,300), (New-Object System.Drawing.Rectangle 0,20,240,100), 'Pixel')
```

**不满意不交付。** 交付时说清楚"我认可的是哪几点"。

### 6. 交付前给图

每个改界面的回合，最后一步固定调用 `present` 附上截图。不要"我改了但没给图"。

### 7. 一次只做一件能被验证的事

"删标题 + 换样式 + 加三个按钮"一次做，坏了不知道坏在哪。分开做，每步编译 + 渲染。

### 8. 技术选型先算交付代价

.NET 8 的教训：自包含单文件 150 MB、框架依赖要用户装 55 MB 运行时——对一个几百 KB 的工具都不成立。**先算用户要付出什么，再谈开发便利。**

---

## 四、主题层

```
Theme/
  Tokens.Ceobe.xaml      默认（暖棕 + 橙）
  Tokens.Amber.xaml
  Tokens.Mono.xaml
  Tokens.Slate.xaml
  Tokens.Ember.xaml      备选
  Styles.xaml            全部控件模板
```

- 换色板 = 换 `App.xaml` 里的一个 `ResourceDictionary`
- 所有颜色用 `{DynamicResource ...}`，否则运行时换不了
- **不要用 `x:Key` 之外的隐式样式覆盖主题**：`MainWindow.xaml` 里曾有一个只设 `VerticalAlignment` 的隐式 `CheckBox` 样式，把主题的复选框模板整个顶掉，退回系统黑白样式

---

## 五、未完成清单

### 排除卡片（`MainWindow.xaml`，`排除的进程（不参与清理）` 那张 Border）

数据层已完成（`Settings.ExcludedProcesses` 是 `List`，`MoveUpCommand`/`MoveDownCommand`/`HasSelectedExclusion`/`IconButton` 样式都在）。剩下三处 XAML：

1. 删除 `Text="添加排除"` 的 TextBlock（连同上方的空行）
2. 搜索用的 `ToggleButton` 加 `Style="{StaticResource IconButton}"`
3. 「排除列表」块：去掉右侧 `−` 按钮，改成列表下方一行三个按钮——`上移` / `下移` / `删除`，`HorizontalAlignment="Right"`，`Visibility="{Binding HasSelectedExclusion, Converter={StaticResource BoolToVisibility}}"`

**做法**：整块行号替换（上次用这个方式重写这张卡片成功过）。

### 其他

- 关于页仍是纯文本堆，没有按卡片规则排
- 主页「进程占用」在折叠线以下，要滚动才看得到
- 紧凑视图的环形图中心按钮：已加，但未在真实桌面验证过

---

## 六、目录改名准备（`D:\MemClean` → `D:\MemMop`）

改名后需要检查的**绝对路径**：

| 位置 | 内容 |
|---|---|
| `tools/measure/Program.cs` | `ReadChevron()` 里的兜底路径 `D:\MemClean\MemMop\src\MemMop\Theme\Styles.xaml` |
| `D:\MemClean\tools\*.py` | 会话期间写的临时脚本，大量硬编码 `D:\MemClean\MemMop\...`（不在仓库内，可不迁移） |
| `docs/*.md` | 若引用了绝对路径 |
| 本文件 | 第一节的路径 |

`build.ps1` 用的是 `$MyInvocation.MyCommand.Path`，**没有硬编码，改名不受影响**。

改名后请重新跑一次 `build.ps1` + `tools/shot`，确认没有残留路径失效。

---

## 七、一句话

**这个仓库的可信度来自三件事：能编译、能量化、能看图。** 任何绕过其中之一的"改好了"，都曾经是错的。
