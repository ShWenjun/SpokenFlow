# 模型服务 / Model services

## 配置方式

主窗口提供 Service URL、Model 和 API Key。默认连接 DeepSeek。可以填写其他服务的 API 基础地址，例如 https://example.test/v1，程序会补上 /chat/completions。也接受完整的 Chat Completions 地址，不会重复追加路径。example.test 仅用于说明，请替换为服务商文档中的真实地址。

更换地址会暂停翻译并清空密钥输入，防止沿用另一服务的密钥。填写对应模型及密钥后，可用 Test API 发送所选语言的固定短句。测试成功后点击 Save and enable。测试和正常翻译都可能计费。

远程服务必须使用 HTTPS。本机回环地址允许 HTTP，例如 http://127.0.0.1:1234/v1，端口应以实际服务为准，本机服务允许不填密钥。不会自动下载或启动本机模型。

## 支持的格式

当前支持 OpenAI Chat Completions 风格的 POST 请求，使用 model、messages、stream 字段，并通过 Authorization: Bearer 发送非空密钥。流式输出须包含 choices[].delta.content，正常结束标记为 finish_reason: stop 和 [DONE]。截断、空结果和错误流不会用于替换。

DeepSeek 官方地址会额外发送关闭思考和输出长度参数。通用地址不发送这些专用参数，以减少服务间差异。API 类型为 Responses、Anthropic Messages、Gemini 原生接口，或需要其他认证头、部署参数的服务，不能直接接入；需要服务商提供兼容接口或另做适配。

当前真实请求验证使用 DeepSeek。其他服务只完成了模拟接口测试，不能保证每个服务或模型均支持。模型名称、额度、数据处理和可用性由服务商决定。这里的兼容指请求与返回格式，不表示与 OpenAI 有合作或背书关系。

## English

Set Service URL, Model and API Key in the main window. DeepSeek is the default. A base URL such as https://example.test/v1 is normalized by appending /chat/completions; a full endpoint is also accepted. Replace example.test with the real endpoint from your provider's documentation.

Changing the URL pauses translation and clears the key input. Enter the matching key and model, use Test API with a fixed example, then Save and enable. Requests may incur charges. Remote URLs require HTTPS. Local loopback services may use HTTP and omit the key, for example http://127.0.0.1:1234/v1 with the actual port. SpokenFlow does not download or start local models.

The client sends a Chat Completions-style POST with model, messages and stream. Nonempty keys use Bearer authentication. Streaming requires choices[].delta.content, finish_reason: stop and [DONE]. Truncated, empty or failed output cannot replace input. Only the official DeepSeek endpoint receives its thinking and output-limit options.

Responses API, Anthropic Messages, Gemini native APIs and endpoints requiring different authentication or deployment parameters need a compatible gateway or further adaptation. DeepSeek has live test evidence; other providers have only mocked transport checks. Compatibility is not guaranteed for every model and does not imply endorsement by OpenAI.

接口参考 / Protocol references: [OpenAI Chat Completions](https://developers.openai.com/api/reference/resources/chat), [DeepSeek Chat Completions](https://api-docs.deepseek.com/api/create-chat-completion/).
