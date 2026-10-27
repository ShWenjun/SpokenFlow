# 使用指南 / Usage

## 开启与关闭 / Enable and exit

先在练习输入框验证。仅检测兼容性不调用 API，只显示是否可读取及字数。测试 API发送当前第一种语言的固定短句；保存并开启翻译才开启编辑区域自动翻译。关闭设置窗口会留在托盘；右键托盘选择退出才结束程序。没有自动开机启动或自动更新。

Use the playground first. Compatibility inspection makes no API calls. Test API sends a fixed example. Save and enable starts editing-area translation. Closing settings leaves the app in the tray; use the tray Exit command to quit. There is no autostart registration or automatic updater.

## 服务设置 / Model service

默认连接 DeepSeek。其他兼容服务可填写服务地址、模型和对应密钥，详情见[模型服务设置](PROVIDERS.md)。更换地址会清空密钥输入并暂停翻译，点击保存后才使用新配置。

DeepSeek is the default. Configure another compatible endpoint with its URL, model and matching key. Changing the URL clears the key input and pauses translation; saving activates the new configuration.

## 语言选择 / Languages

界面语言默认英语，可独立切换为七种语言之一。切换后需重新开启翻译。互译列表使用各语言自己的名称。在主窗口选择两种语言，设置立即保存并同时用于输入和划词翻译。默认中英，可选中、英、法、德、俄、韩、日。切换后旧请求与结果失效，详情见[语言设置](LANGUAGES.md)。

The UI defaults to English and has its own seven-language selector. Re-enable translation after changing UI language. Translation lists use native names. Choose two languages in the main window. Changes are saved immediately and shared by typing and selection translation. Chinese/English is the default; all seven languages and ambiguous-input behavior are described in the language guide.

## 快捷键 / Shortcuts

| Key | 功能 / Action |
|---|---|
| `Ctrl+Alt+E` | 替换已核对的当前区域 / Replace the verified area with a completed translation |
| `Ctrl+Alt+Z` | 恢复最近一次原文，要求当前内容仍匹配 / Restore the last original if the target still matches |
| `Ctrl+Alt+Space` | 开启或暂停 / Enable or pause |
| `Ctrl+Alt+R` | 重新尝试当前翻译；精确缓存可能被复用 / Retry; an exact cached result may be reused |

快捷键不发送聊天消息。若被其他软件占用，主窗口会提示。The shortcuts do not send chat messages; registration conflicts are reported in settings.

## 浮窗 / Floating window

拖动标题移动，拖动边缘缩放；位置与大小保存。点击 ↺ 或双击标题恢复跟随输入位置。长文支持滚轮及细滚动条拖动。外观可设置背景颜色与 55%–100% 不透明度。浮窗最小 320×180。

Drag the title to move, edges to resize. Click ↺ or double-click the title to follow the input again. Long text scrolls with the wheel or scrollbar. Appearance controls background color and 55%–100% opacity. Minimum size is 320×180.

主翻译浮窗右上角的 − 会将其收成 52×52 的小图标，并暂停自动翻译和划词请求。拖动小图标可移动；单击恢复原尺寸，并重新读取最新输入内容，旧译文失效。最小化期间离开输入区域仍会隐藏，回到可读取输入区域会显示小图标。退出程序后不保留最小化状态。划词临时窗继续使用 × 关闭。

The main popup's − button collapses it into a draggable 52×52 icon and suspends automatic and selection translation requests. Drag to move; click to restore the full size and capture the latest input, invalidating old results. It still hides outside readable input areas and reappears collapsed when returning to one. Collapse state resets on restart. The temporary selection popup retains its × close button.

最小化会取消正在生成的任务；已经发送到服务商的请求仍可能计费。仅在本机读取输入状态不产生翻译 API 费用。

Minimizing cancels in-flight translation work, but already-sent requests may still be billed. Local input-state capture does not make translation API calls.

同一输入区更新时保留浮窗，旧结果暂不可替换；切换应用或离开可读取区域时隐藏。仅有短暂焦点容错，不保证所有软件都不闪动。

The popup stays during updates in the same field; old text becomes nonreplaceable. Switching apps or leaving an accessible field hides it. Brief focus tolerance is not a universal no-flicker guarantee.

## 划词 / Selection translation

开启 划词：开 后，鼠标拖选或双击文字。按主窗口选择的两种语言自动双向翻译，与输入翻译共用设置。临时小窗可复制，点击其他位置或 × 关闭。先通过标准接口最多读取三次；失败时，默认尝试 `Ctrl+C` 复制当前选区，并尽量恢复剪贴板。可在 浮窗外观 中关闭这项补救。不会发送 `Ctrl+A`；不能识别图片，键盘选区不自动触发。新点击或键盘输入会取消旧选区任务。

Enable selection mode, then drag-select or double-click text. Selections translate in either direction using the same language pair as typing translation. Copy from the transient popup. After up to three accessibility reads, selection mode can fall back to Ctrl+C with best-effort clipboard restoration; disable this fallback in Appearance. It never sends Ctrl+A. Images and keyboard-only selection are unsupported. New clicks or physical key presses cancel older selection work.

读取重试不调用 API。仅接收本次复制产生的新文本；剪贴板繁忙、格式无法保存、备份超过 16 MiB、权限不同或应用不响应复制时仍可能失败。只在复制结果仍由目标应用持有且剪贴板序号未改变时尝试恢复，不能保证所有软件均可恢复。取词后才发起翻译，已发出的请求取消后仍可能计费。

Capture retries make no API calls. The fallback accepts only newly copied text. Busy or unsupported clipboard formats, backups over 16 MiB, privilege differences or apps that ignore copy may prevent capture. Restoration is attempted only while the target still owns the unchanged clipboard result; it is not guaranteed. Translation starts after capture, and cancelling a sent request may still incur charges.

## 速度、费用和长文本 / Latency, cost and length

默认停笔等待 350ms，可调到 150–3000ms。生成耗时显示从请求发出后计时，另加停笔和读取时间。延长等待可减少输入停顿造成的请求，代价是响应开始更晚。

The default pause is 350ms (adjustable 150–3000ms). Displayed generation timing excludes capture and pause latency. Longer pauses can reduce requests but delay the start.

每次请求发送当前范围完整文字，不只是新增加的部分。取消已发请求不保证免计费。精确缓存不请求 API，但没有承诺固定费用或响应速度。范围上限 6000 个 .NET 字符单位；超限会暂停，不把截断内容冒充完整范围。

Each request sends the full scoped text. Cancelling a sent request does not guarantee zero billing. Exact cache hits avoid a new request. The limit is 6000 .NET character units; over-limit scopes are rejected rather than silently truncated.

默认服务的费用见 [DeepSeek 计费文档](https://api-docs.deepseek.com/quick_start/pricing/)。使用其他服务时请查其计费规则。Model availability and prices are governed by the configured provider, not this repository.

## 常见情况 / Troubleshooting

- 没有浮窗：先检测兼容性，核对矩阵；密码框、只读编辑区、自绘控件或权限不同的程序可能不可读取。No popup: inspect compatibility and check the matrix; inaccessible, read-only or elevated controls may fail.
- 快捷键无法替换：等待完整译文，保持相同输入区和原文；不支持的目标可手动复制。Replacement refused: wait for completion and keep the target/text unchanged; copy manually if unsupported.
- 401/402/429：分别检查密钥、余额、请求频率；不要公开错误截图中的密钥。Check key, balance or rate limits without sharing credentials.
- 配置位置：`%LOCALAPPDATA%\SpokenFlow`。配置不随 ZIP 分发，API Key 绑定当前 Windows 用户加密。Settings are local and excluded from distribution.
