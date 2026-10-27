# Changelog / 更新记录

## 0.1.0-alpha.1

### 中文

早期预览版，面向 Windows 10/11 x64。

- 输入框文字确认后自动翻译，支持流式显示、结果缓存和过期结果失效。
- 中文、英语、法语、德语、俄语、韩语和日语可任选两种双向互译。输入和划词共用语言设置。
- 七种界面语言，默认英语，语言列表使用各自的名称。
- 支持自定义 Chat Completions 服务地址、模型及密钥，默认连接 DeepSeek。
- 翻译浮窗可拖动、缩放、滚动，并可调整透明度和背景。最小化时暂停请求，恢复时重新读取当前文字。
- 快捷键替换已核对的输入范围，并可尝试恢复原文。
- 划词结果显示在可复制的临时浮窗中。辅助功能读取失败时，可使用选区复制补救。

微信和豆包桌面端不支持，其他软件的验证程度见[兼容性](docs/COMPATIBILITY.md)。程序未签名，不提供 OCR。模型输出和界面译文仍可能存在错误。

### English

Early preview for Windows 10/11 x64.

- Translate committed input with streaming output, result caching and stale-result rejection.
- Choose two of seven languages for bidirectional translation, shared by typing and selection modes.
- Seven UI languages, English by default, with native language names.
- Configurable Chat Completions endpoint, model and key. DeepSeek is the default.
- Movable, resizable, scrollable popup with opacity and background controls. Collapsing pauses requests; restoring captures current text.
- Shortcuts to replace verified input and attempt restoration.
- Copyable selection popup with an optional clipboard fallback when accessibility capture fails.

WeChat and Doubao desktop applications are unsupported. See [compatibility](docs/COMPATIBILITY.md) for other applications. The binary is unsigned and OCR is unavailable. Model output and UI translations may contain errors.
