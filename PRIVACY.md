# 隐私与数据流 / Privacy and data flow

## 读取和发送 / Reading and sending

开启自动翻译后，程序轮询前台可编辑区域。普通输入框读取整段；文档和编辑器按适配器读取段落、文本框、编辑单元格或行，详见兼容矩阵。开启划词后，会安装进程内持有的低级鼠标钩子以检测选择动作，再通过可访问接口读取选区。密码控件会被排除，但软件无法识别普通输入框中的所有敏感内容。

Automatic translation polls the foreground editable scope. Selection mode observes mouse gestures through a low-level hook and reads accessible selected text. Password controls are excluded; arbitrary sensitive text in ordinary fields cannot be classified reliably.

划词模式另监听键盘按下事件，仅用于取消过期的选区任务，不记录按键内容。标准选区读取失败后，可通过复制当前选区补救；此项默认开启，可在浮窗外观中关闭。该功能不全选、不持续轮询剪贴板，仅在一次有效鼠标划词动作后的有限时间内读取复制结果。

Selection mode also observes key-down events solely to cancel stale work; it does not record key contents. An optional, default-on fallback copies the current selection when accessibility capture fails. Disable it in Appearance. It never selects all or continuously monitors the clipboard; it waits briefly for a new copy result after a mouse-selection gesture.

翻译将原文、模型名和包含所选两种语言的翻译指令发送到用户配置的 Chat Completions 地址，API Key 用于认证。默认是 `https://api.deepseek.com/chat/completions`，远程地址要求 HTTPS，本机回环地址也允许 HTTP。语言判断与翻译使用同一次请求，没有单独的语言识别请求。没有向项目维护者发送遥测或文字的应用代码。所选服务对收到的数据按其条款处理，本项目不能代其保证存储或保留期限。更换服务前请核对地址和数据处理规则。

Translation sends scoped/selected text, model and instructions containing the selected language pair to the configured Chat Completions endpoint, authenticated with your key. DeepSeek is the default. Remote endpoints require HTTPS; local loopback endpoints may use HTTP. Direction detection uses the same request as translation. The app has no telemetry/text reporting to the maintainer. Provider retention and processing are governed by its terms; this project cannot promise them on its behalf.

更换服务地址会清空界面的密钥输入并暂停请求。用户填写新服务密钥并保存后才启用新配置，原配置在保存前仍保存在本机。HTTP 重定向不自动跟随，地址中不允许嵌入账号、密钥、查询参数或片段。

Changing the service URL clears the key input and pauses requests. Enter and save the new service key to use the new configuration; the previous saved settings remain on disk until then. Redirects are not followed automatically, and URLs cannot contain credentials, query parameters or fragments.

## 本机保存 / Local storage

`%LOCALAPPDATA%\SpokenFlow\settings.json` 保存界面语言、两种翻译语言、服务地址、模型、停笔时间及经当前 Windows 用户 DPAPI 加密的密钥。`appearance.json` 保存外观和划词开关。同一 Windows 账户下的其他进程仍可能解密该密钥。程序会在运行时解密密钥用于请求。

Settings store the UI language, translation pair, service URL, model, delay and current-user DPAPI-encrypted key; appearance settings include the selection toggle. DPAPI is not protection from other processes running as the same user. The key is decrypted in memory for requests.

程序不主动将输入原文和译文写入日志。当前文字、结果和精确缓存存在内存中，退出进程后不持久保存；关闭划词不等于立即清空所有划词缓存。自测报告只含固定测试的结果。

The app does not intentionally log source/translated text. Live state and exact-result caches stay in process memory; disabling selection is not an immediate purge of all selection caches. Offline reports contain synthetic test results only.

## 剪贴板与用户控制 / Clipboard and controls

复制按钮把译文写入系统剪贴板。某些替换路径使用临时剪贴板粘贴，并在约 700ms 后、剪贴板序号未变化时尝试恢复原内容。系统剪贴板历史或同步服务可能另行保存内容，恢复不保证清除这些副本。

Copy writes the translation to the clipboard. Some replacements paste through a temporary clipboard value and attempt restoration after about 700ms if the sequence has not changed. OS clipboard history/sync may retain copies independently.

划词补救在本机内存中先保存可支持的剪贴板格式，再发送 `Ctrl+C`；不清空剪贴板或注入占位文本。只有剪贴板序号发生变化、所有者属于目标应用且选区任务未过期，文字才会用于翻译。读取结束后仅在序号和所有者仍匹配时尝试恢复，其他应用或用户的新复制内容不会主动被恢复步骤覆盖。保存失败、内容过大或无法可靠核对时跳过补救；恢复是尽力行为，不能消除剪贴板历史或同步副本。

Selection fallback materializes supported clipboard formats in memory before sending Ctrl+C; it does not clear the clipboard or insert a marker. Translation requires a changed sequence, target-app ownership and a still-current gesture. Restoration is attempted only while sequence and ownership match, to avoid overwriting newer clipboard writes. Unsupported, excessive or unverifiable clipboard data causes the fallback to be skipped. Restoration is best effort and cannot erase clipboard history or synchronized copies.

暂停会停止自动翻译并关闭划词检测；退出会结束进程。兼容性检测、离线演示、自测和外观预览不发送翻译请求。API 测试会发送固定句子并可能计费。任何实际翻译请求都可能计费，取消不能保证免费。

Pause stops automatic translation and selection monitoring. Exit ends the process. Inspection, offline demo, self-tests and appearance previews do not request translation. API testing sends a fixed sentence and may incur charges; cancellation does not guarantee free usage.

本版无 OCR、屏幕截图上传、微信数据库访问或进程内存补丁。This version includes no OCR, screenshot upload, WeChat database access or memory patch.
