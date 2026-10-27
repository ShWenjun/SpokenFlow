# 测试记录 / Test evidence

记录日期为 2026-10-26。以下是本地验证结果，GitHub Actions 尚未运行。

## 离线测试

34 项测试通过。范围包括流式响应完成判定、过期结果失效、文本范围、浏览器组合框识别、划词重试与取消、剪贴板并发保护、浮窗最小化状态，以及语言设置。

语言测试检查七种文字的触发条件、全部 21 种组合的有效性与缓存区分、双向缓存标识一致、重复语言拒绝、旧配置迁移和设置保存。固定数据测试不发送 API 请求。

发布候选从实际源码 ZIP 解压至独立目录后重新构建，同样通过 34 项测试。

## 实际 API 检查

用表达「我们晚点再聊」的固定短句调用 DeepSeek，共 14 次请求。每组分别验证两个方向，没有读取真实聊天内容。逐条检查了输出语言和基本含义。

| 语言组合 / Pair | 检查 / Check |
|---|---|
| 中文与英语 / Chinese and English | 两个方向通过 / Both directions passed |
| 英语与法语 / English and French | 两个方向通过 / Both directions passed |
| 法语与德语 / French and German | 两个方向通过 / Both directions passed |
| 德语与俄语 / German and Russian | 两个方向通过 / Both directions passed |
| 俄语与韩语 / Russian and Korean | 两个方向通过 / Both directions passed |
| 韩语与日语 / Korean and Japanese | 两个方向通过 / Both directions passed |
| 日语与中文 / Japanese and Chinese | 两个方向通过 / Both directions passed |

这只验证了简单短句，不是专业语言质量评测，也没有对全部 21 种组合进行实际 API 检查。长文、混合语言、专有名词和歧义短词仍需更多测试。

## 仍需实测

已验证英语、德语和中文主界面切换及选择保存。七种文案完整性、切换时保留文本、配置迁移、自定义地址验证、模拟接口的认证和流式数据均有离线测试。其他服务商尚未真实联网验证。语言下拉框与真实输入框的完整交互、不同页面上的划词稳定性、鼠标拖动最小化图标、不同输入法的组合状态，以及未验收应用中的替换与恢复，仍需实际环境验证。既有软件验证记录见[兼容性矩阵](COMPATIBILITY.md)。不应将编译和固定数据测试视为这些功能均已验收。

## English

On 2026-10-26, all 34 local offline checks passed. They cover streaming completion, stale-result rejection, capture scopes, browser combobox recognition, selection retries and cancellation, clipboard concurrency, minimization state and language settings. All 21 pairs are checked for validity and cache separation, alongside migration and persistence. These checks make no API calls. A fresh build from the actual source archive also passed all 34 checks.

Fourteen actual API requests used fixed examples meaning “Let's talk later,” covering the seven pairs above in both directions. Output language and basic meaning were reviewed. No private conversation was read. This is a small smoke test, not a professional quality benchmark or live coverage of all 21 pairs.

English, German and Chinese UI switching and persistence were checked locally. Offline checks cover complete catalogs, preservation of user text, configuration migration, endpoint validation and mocked authenticated streaming. Other providers have not been tested live. Full UI interaction with language controls, selection reliability across pages, dragging the minimized icon, input-method composition and unverified application adapters still need manual testing. Hosted CI has not run. See the compatibility matrix for existing evidence.
