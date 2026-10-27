# 依赖与来源 / Dependencies and provenance

SpokenFlow 使用 Windows 系统接口和外部模型服务。程序包不包含模型权重、Office 或 Java 运行环境。

SpokenFlow uses Windows system interfaces and external model services. Packages do not include model weights, Office or a Java runtime.

| 组件 / Component | 使用方式 / Use | 分发情况 / Distribution |
|---|---|---|
| .NET Framework 4.8, WinForms, UI Automation, DPAPI, Windows APIs | 系统运行库和接口 / System runtime and APIs | 不附带微软运行库 / Not bundled |
| Microsoft Office native object model | 已安装 Office 提供的 COM 接口 / COM supplied by installed Office | 不附带 Office / Not bundled |
| Scintilla interface | 向已安装 Notepad++ 的控件发送标准消息 / Messages to an installed control | 不附带 Scintilla 源码或二进制 / Not bundled |
| Java Access Bridge | 加载用户系统或 PyCharm JBR 中已有的桥接组件 / Uses an existing bridge | 不附带 JBR、JDK 或桥接 DLL / Not bundled |
| Chat Completions 服务 / Services | 可配置模型接口，默认 DeepSeek / Configurable model endpoint, DeepSeek by default | 不附带模型或服务额度 / No model or service credits bundled |
| GitHub Actions checkout / upload-artifact | 仓库 CI 构建工作流 / CI workflow | 运行时由 Actions 获取，不随程序包分发 / Not bundled in app |
| 个人标识 / Personal mark | SpokenFlow 软件图标 / SpokenFlow application icon | 单独品牌条款 / Separate branding terms |

API 事实和实现参考 / Primary technical references:

- [Microsoft AccessibleObjectFromWindow](https://learn.microsoft.com/en-us/windows/win32/api/oleacc/nf-oleacc-accessibleobjectfromwindow)
- [Microsoft UI Automation text patterns](https://learn.microsoft.com/en-us/dotnet/framework/ui-automation/ui-automation-textpattern-overview)
- [Scintilla documentation](https://scintilla.org/ScintillaDoc.html)
- [Oracle Java Access Bridge API](https://docs.oracle.com/en/java/javase/17/access/java-access-bridge-api.html)
- [JetBrains accessibility](https://www.jetbrains.com/help/pycharm/accessibility.html)
- [DeepSeek API documentation](https://api-docs.deepseek.com/)

这些链接说明所用接口，不表示把第三方文档的许可证套用于本代码，也不构成相关厂商背书。未来引入代码、字体、模型或运行库时，必须补充其来源、版本与许可，不能只在此列一个链接。

References describe APIs, not a transfer of their documentation licenses onto this code or endorsement. Any future bundled third-party code, fonts, models or runtime must have versioned provenance and license notices.
