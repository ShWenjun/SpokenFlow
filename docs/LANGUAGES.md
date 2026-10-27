# 语言设置 / Language settings

## 中文

主窗口提供两个语言下拉框，可从中文、英语、法语、德语、俄语、韩语和日语中任选两种，共 21 种组合。默认是中文与英语。输入翻译和划词翻译共用这一组设置，不需要分别切换。

更换语言后设置立即保存，旧请求取消，旧缓存与可替换结果失效。如果选中另一侧已有的语言，两个选项会交换，避免两侧相同。程序再次启动时保留选择；旧版配置升级时默认使用中英组合，并保留原有密钥。

程序在一次翻译请求中让模型判断方向，没有额外发送语言识别请求。例如选择法语和德语，输入法语时译成德语，输入德语时译成法语。混合文字按主要语言处理。提示词要求在两种语言都不占主导或方向不明确时保留原文，但模型不能保证每次判断正确。

短词、姓名、借词和中日共有汉字可能存在歧义。需要时补充上下文，并在替换前检查译文。只有数字、标点或表情的输入不会触发自动翻译。翻译保留原意、语气和礼貌程度，不补写未完成的句子。

主窗口另有界面语言选项，提供相同七种语言，首次启动默认 English，之后记住选择。语言名称固定显示为 中文、English、Français、Deutsch、Русский、한국어、日本語。切换界面时会暂停翻译，点击开启后继续，互译语言不变。Windows 自带对话框及系统控件提示可能沿用系统语言。界面文案随程序打包，切换时不调用 API。增加目标语言不等于相应输入法和应用已全部验证。

## English

Choose two of Chinese, English, French, German, Russian, Korean and Japanese in the main window. There are 21 possible pairs; Chinese and English are the default. Typing and selection translation share this setting.

Changes are saved immediately and invalidate previous requests, caches and replaceable results. Selecting the language already used on the other side swaps the choices. The pair persists across restarts. Older settings default to Chinese/English while retaining the existing credential.

Direction detection happens within the translation request, without a separate detection call. French input becomes German when that pair is selected, and vice versa. The prompt uses the dominant language for mixed text and asks the model to return the original when neither language dominates or direction is ambiguous. This is not guaranteed model behavior.

Short words, names, loanwords and shared Chinese/Japanese characters can be ambiguous. Add context where possible and check translations before replacement. Numbers, punctuation and emoji alone do not trigger automatic translation. The prompt preserves meaning, tone and politeness without completing unfinished sentences.

A separate UI language setting offers the same seven languages, defaults to English and persists across restarts. Language names use their native spelling. Switching UI language pauses translation; enable it again to continue with the unchanged translation pair. Windows dialogs and system-control labels may follow the OS language. UI translations are bundled and switching makes no API call. Language support does not establish input-method or application compatibility.
