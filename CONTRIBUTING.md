# 参与贡献 / Contributing

欢迎中文或英文 Issue 与 Pull Request。请先读 [LICENSE](LICENSE)：本项目禁止商用，分发衍生版本需提供对应源码，并非 GPL 或 OSI 开源项目。

Issues and pull requests are welcome in Chinese or English. Read the custom noncommercial [LICENSE](LICENSE) first, including corresponding-source requirements.

## 从小任务开始 / Start small

最需要的贡献是可复现的兼容性报告、无真实用户数据的测试、文档校正，以及修复明确的焦点、替换范围或流式结果问题。大型架构变更、新依赖和新的应用适配，先开 Issue 说明目的和验收范围。

Useful first contributions include reproducible compatibility reports, synthetic tests, documentation corrections and focused fixes. Discuss large redesigns, new dependencies and application adapters before implementation.

1. 在许可允许的非商业范围内 fork 或克隆项目，创建自己的工作分支。Fork/clone within permitted purposes and create a branch.
2. 使用固定例句验证；不要把真实聊天、密钥或账号配置加入测试。Use synthetic fixtures only.
3. 执行 `build.ps1`、`scripts/test.ps1` 和 `scripts/check-source.ps1`。Run build, offline tests and source hygiene checks.
4. PR 说明问题、最终行为、如何验证及未覆盖的情况。State the problem, resulting behavior, validation and gaps.
5. 修改用户可见行为时同步更新中文和英文文档；未实测的软件标为待验证。Update both languages and distinguish unverified adapters.

## 界面翻译 / UI translations

界面文案位于 source/Locales.json，包含 en、zh、fr、de、ru、ko、ja。修改译文时保留资源键、快捷键、数值和首尾空格。先运行离线自测，再切换到对应语言检查按钮、提示和较长文字。报告翻译问题时请区分界面文案与模型生成的译文。

UI strings are in source/Locales.json for en, zh, fr, de, ru, ko and ja. Preserve resource keys, shortcuts, numbers and surrounding spaces. Run offline tests and inspect the affected language in the UI. Distinguish bundled UI wording from model-generated translations when reporting problems.

## 提交权利 / Rights in contributions

向本项目提交贡献供合并，表示你有权提供该内容，并同意将自己的贡献按本项目当时明确公布的 LICENSE 提供；你保留自己的版权。这不转让所有权，不默认允许维护者把他人的贡献改为商业授权，也不授权使用个人标识。来自雇主或其他项目的代码，必须先确认你有权这样提交。

By intentionally submitting work for inclusion, you confirm that you may provide it under the project's then-published LICENSE. You retain your copyright. This is not an ownership assignment or automatic commercial relicensing permission, and grants no personal-mark rights. Verify employer and third-party permissions before contributing.

若使用 AI 辅助，请说明重要生成部分并自行检查行为、来源和测试；不要提交你无法解释或没有权利提交的内容。If AI assistance is used, disclose substantial generated portions and verify behavior, provenance and tests yourself.

## 兼容性报告 / Compatibility reports

附上 Windows / 软件 / 输入法版本、可复现步骤、固定例句、期望范围、实际结果。只用脱敏截图，不附真实对话。读到文字不等于替换、恢复和焦点切换也通过。

Provide versions, steps, a fixed example, expected scope and observed result. Reading alone does not establish safe replacement, restoration or focus behavior.

## 维护方式 / Maintenance

维护者会按复现质量、影响和可验证性审阅，不承诺响应时限。参与者须遵守 [行为准则](CODE_OF_CONDUCT.md)。真实密钥或安全漏洞请按 [SECURITY.md](SECURITY.md) 处理。

Review is based on reproducibility, impact and verifiability, without a promised response time. Follow the conduct and security policies.
