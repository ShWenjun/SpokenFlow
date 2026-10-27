# 兼容性 / Compatibility

记录日期 / Recorded: 2026-10-26. “已验证”仅指所列环境和动作，不表示所有版本均兼容。Verified means the listed environment and actions only.

| 软件 / Application | 预期范围 / Intended scope | 当前证据 / Evidence |
|---|---|---|
| 标准 Windows 多行输入框 / Standard Windows multiline textbox | 当前输入框整段 / Whole field | 已在独立练习窗口验证读取、浮窗、离线固定译文替换、恢复、过期结果拒绝 / Verified in a separate playground process |
| Microsoft Word 2019, 16.0.19127.20860 | 当前段落 / Current paragraph | 本机两段 RTF 测试：读取、替换及恢复第一段，第二段不变 / Verified with a two-paragraph synthetic RTF |
| Microsoft Excel | 编辑中的单元格 / Cell being edited | 已有受限 UIA 路径，未实测；不读取整表，拒绝以 `=` 开头的内容 / Implemented, not verified; not a spreadsheet-wide reader |
| Microsoft PowerPoint | 当前文本框 / Active text box | 原生 Office 接口已实现，未实测 / Implemented, not verified |
| WPS 文字、表格、演示 / WPS Writer, Spreadsheet, Presentation | 段落、编辑单元格、文本框 / Paragraph, editing cell, text box | 仅有通用辅助接口适配分支；没有逐软件验收 / Generic adapter paths only, unverified |
| Edge 156 / Chrome 155 / Firefox | 可编辑网页输入区 / Editable web control | UIA 路径已实现；端到端验证未完成；不承诺所有富文本 / Unverified end-to-end |
| Chrome 新标签页中央搜索框 / New-tab center search | 可编辑组合框的文字 / Editable combobox value | 使用报告确认浮窗和译文可显示；替换未验证 / Popup and translation verified by user report; replacement unverified |
| Windows Notepad | 当前可访问行 / Current accessible line | 实现未验收 / Implemented, not verified |
| VS Code 1.143.1 | 当前可访问行 / Current accessible line | 实现未验收；可能需开启编辑器辅助功能 / May need editor accessibility support; unverified |
| Notepad++ | 当前逻辑行 / Current logical line | Scintilla 路径已实现，未实测 / Implemented, not verified |
| PyCharm | 当前可访问行 / Current accessible line | Java Access Bridge 路径已实现，未实测；需可用桥接组件及读屏支持 / Requires available bridge and screen-reader support; unverified |
| WeChat / Weixin 4.1.15.56 | 原聊天输入框整段 / Whole native chat input | 未支持：本机聚焦输入框后只暴露外层窗口，未读取到输入框 / Unsupported in the tested installation |

当前仅支持 Windows。豆包桌面端的输入翻译和划词翻译不支持。

Windows is the only supported platform. Typing and selection translation in Doubao desktop are unsupported.

## 输入法 / Input methods

目标是文字确认上屏后读取内容，而不是读取未确认的拼音候选。微软拼音有基础流程测试记录；搜狗输入法、微信输入法尚未逐项验收。现有组合输入检测依赖 IMM，不能保证覆盖所有 TSF 输入法状态。其他输入法的兼容性需要分别验证。

The app targets committed text, not unfinished composition candidates. Microsoft Pinyin has basic workflow test evidence; Sogou and WeChat Input have not been individually verified. IMM composition detection does not establish comprehensive TSF coverage.

日语、韩语及其他语言输入法尚未逐项验收。七种翻译语言的固定例句测试见[测试记录](TESTING.md)，不代表输入法适配已完成。

Japanese, Korean and other input methods have not been individually verified. Translation examples do not establish IME compatibility.

## 验证方式与边界 / Evidence and limitations

- Word 使用固定中文例句和第二段英文哨兵文本，无 API 调用。端到端模型输出质量并非此测试内容。Word replacement used a fixed offline translation and preserved a second-paragraph sentinel.
- Word 替换保留段落/表格结束标记，但可能统一段内字符格式。富格式、批注、修订和复杂表格尚未验收。Word replacement may unify inline formatting; complex document features are unverified.
- UIA 的“行”由目标软件提供，可能是显示行而不是逻辑行。编辑器换页、分栏、不同 DPI 和大文档需要更多测试。UIA lines may be visual rather than logical lines.
- 焦点和文本核对是尽力防护，不是跨进程原子事务。连续输入与窗口切换的竞态仍需测试。Focus/text checks are not an atomic transaction with another process.
- 本版不提供 OCR 或微信专用读取方式。OCR and a WeChat-specific capture method are unavailable.
- 本版本不会安装或修改其他软件的辅助功能设置。请根据各软件官方说明自行配置。The app does not change other applications' accessibility settings.

报告问题时请附软件版本、Windows 版本、输入法、固定复现句和预期范围；不要附真实聊天记录或 API Key。

For reports, provide versions, IME, a synthetic example and expected scope, not real messages or API keys.
