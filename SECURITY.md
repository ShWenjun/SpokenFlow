# 安全问题 / Security reporting

本项目处于早期预览，没有经过独立安全审计，也没有长期支持版本承诺。优先检查当前预览版的问题。

This early preview has no independent security audit or long-term-support promise. Reports against the current preview are prioritized.

请通过 GitHub 的[私密漏洞报告入口](https://github.com/ShWenjun/SpokenFlow/security/advisories/new)提交安全问题。仓库已启用此功能。若入口不可用，请先向维护者请求私密联系渠道，公开内容只说明需要安全联系，不要附漏洞细节或密钥。

Use GitHub's [private vulnerability reporting form](https://github.com/ShWenjun/SpokenFlow/security/advisories/new), which is enabled for this repository. If the form is unavailable, request a private contact route without posting exploit details or credentials publicly.

请提供受影响版本、问题类别、固定复现步骤、影响和可行修复建议。敏感数据应先移除；如果密钥已经泄露，应由密钥所有者在服务商处撤销并更换，不要把新密钥发给维护者。

Include version, category, synthetic reproduction, impact and possible fixes. Remove sensitive data. Exposed keys should be revoked and replaced by their owner at the provider, never sent to the maintainer.

已知防护边界包括：同一 Windows 用户下的进程可接触解密能力；自动读取普通文本框不能识别所有秘密；焦点和替换不是跨进程原子操作；剪贴板历史可能保留译文。详见 [PRIVACY.md](PRIVACY.md) 和 [兼容性说明](docs/COMPATIBILITY.md)。

Known boundaries include same-user access, unclassified secrets in ordinary controls, non-atomic cross-process replacement and independent clipboard history. See privacy and compatibility documentation. No disclosure or fix deadline is guaranteed.
