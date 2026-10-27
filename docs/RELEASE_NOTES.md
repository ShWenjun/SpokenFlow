# SpokenFlow 0.1.0-alpha.1

## 中文

SpokenFlow 是 Windows 桌面翻译工具。保留现有输入法，在输入框附近查看自然、简洁的译文，也可选中文字进行翻译。

### 功能

- 七种语言任选两种，输入与划词共用设置，自动双向互译。
- 七种界面语言，首次启动默认英语。
- 流式译文浮窗，可移动、缩放、滚动和调整外观。最小化时暂停翻译请求。
- 快捷键替换已核对的文字，并尝试恢复原文。
- 自定义 Chat Completions 服务地址、模型和密钥，默认连接 DeepSeek。

### 运行与文件

需要 Windows 10/11 x64 和 .NET Framework 4.8。解压 Windows 程序包后运行 SpokenFlow.exe，配置自己的模型服务。无需安装新的输入法。

| 文件 | 内容 |
|---|---|
| SpokenFlow-0.1.0-alpha.1-windows-x64.zip | 便携程序和文档 |
| SpokenFlow-0.1.0-alpha.1-source.zip | 对应源码、构建脚本和文档 |
| SHA256SUMS.txt | 两个压缩包的 SHA-256 校验值 |

### 使用限制

这是早期预览版。微信和豆包桌面端不支持，部分浏览器、Office 和编辑器尚未逐项验证。程序未签名，不提供 OCR。DeepSeek 有真实请求验证，其他兼容服务仅完成模拟接口测试。界面译文仍待母语使用者校对。

翻译会将文字发送到配置的模型服务，可能产生费用。取消请求不保证免计费。软件仅允许许可规定的非商业用途，个人标识单独保留权利。使用前请阅读包内 LICENSE、PRIVACY.md 和兼容性说明。

## English

SpokenFlow is a Windows desktop translation tool. Keep your input method and see natural, concise translations near the input field, or select text to translate it.

### Features

- Bidirectional translation between two of seven languages, shared by typing and selection modes.
- Seven UI languages, English by default.
- Streaming popup with movement, resizing, scrolling and appearance controls. Minimization pauses translation requests.
- Shortcuts for verified replacement and attempted restoration.
- Configurable Chat Completions endpoint, model and key, with DeepSeek as the default.

### Requirements and files

Requires Windows 10/11 x64 and .NET Framework 4.8. Extract the Windows archive, run SpokenFlow.exe and configure your model service. No replacement input method is required.

The Windows ZIP contains the portable application and documentation. The source ZIP contains matching code, build scripts and documentation. SHA256SUMS.txt provides checksums for both archives.

### Limitations

This is an early preview. WeChat and Doubao desktop applications are unsupported; some browsers, Office applications and editors remain unverified. The executable is unsigned and OCR is unavailable. DeepSeek has live request evidence; other compatible services have mocked tests only. UI translations await native-speaker review.

Translation sends text to the configured service and may incur charges. Cancellation does not guarantee zero billing. Use is limited to the noncommercial purposes in LICENSE; branding rights are reserved separately. Read the included privacy and compatibility documents before use.
