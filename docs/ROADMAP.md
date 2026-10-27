# 维护方向 / Roadmap

## 中文

当前重点是 Windows 版的读取稳定性、兼容性验证和翻译体验。以下方向欢迎贡献，不代表交付日期。

- 为浏览器、Office、VS Code、Notepad++、PyCharm 和不同输入法补充可复现的读取、替换及恢复测试。
- 改善辅助功能接口无响应时的处理，减少焦点切换和连续输入造成的错误。
- 校对七种界面语言，检查较长文字、显示缩放和辅助功能名称。
- 增加长文、混合语言、短词和专有名词的翻译案例，验证更多兼容模型服务。
- 改进适配器的测试方式、构建检查和发布验证，评估程序签名。

提出新适配前，请先说明具体软件、读取范围和验证方法，见[贡献指南](../CONTRIBUTING.md)。

## English

The current focus is Windows capture reliability, application testing and translation usability. Contributions are welcome in these areas; no delivery dates are implied.

- Add reproducible capture, replacement and restoration tests for browsers, Office, editors and input methods.
- Improve handling of unresponsive accessibility providers, focus changes and continuous typing.
- Review all seven UI languages, longer labels, display scaling and accessibility names.
- Expand translation cases for long or mixed text, ambiguous words and proper names; validate more compatible providers.
- Improve adapter testing, build checks and release validation, and assess code signing.

New adapter proposals should identify the application, capture scope and validation method. See [contributing](../CONTRIBUTING.md).
