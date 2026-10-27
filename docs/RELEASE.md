# 发布流程 / Release procedure

## 中文

发布由项目维护者执行。构建和打包脚本只生成本地文件，不创建仓库或上传 Release。

1. 核对版本号、许可、品牌规则、功能说明及兼容性。VERSION、AssemblyInfo.cs 和更新记录应对应同一个版本。
2. 确认许可和文档的最终文本，并核对仓库所有者、版本地址和下载文件名。
3. 对待发布的源码提交运行 CI。不要使用尚未通过检查的提交制作版本标签。
4. 运行 scripts/package.ps1，检查生成的程序包、源码包与 SHA-256。源码包应能在独立目录中重新构建并通过自测。
5. 核对包内文件，不包含用户配置、密钥、私人截图或无关文件。保留许可、品牌和第三方声明。
6. 创建与源码提交对应的版本标签。早期版本使用 GitHub pre-release，并上传程序包、对应源码包和 SHA256SUMS.txt。
7. 使用 [RELEASE_NOTES.md](RELEASE_NOTES.md) 填写发布说明。发布后确认 README 的下载链接和所有附件可访问。

仓库设置应按实际维护需要配置私密漏洞报告、分支审阅规则和可用的密钥保护功能。账号和访问令牌不写入项目文件。

## English

The maintainer performs releases. Local scripts build and package files without creating a repository or publishing a release.

1. Align VERSION, AssemblyInfo.cs and the changelog. Review licensing, branding, features and compatibility.
2. Approve the final documentation and terms. Verify the repository owner, release URL and asset names.
3. Run CI against the exact source commit to be released.
4. Run scripts/package.ps1. Verify binary/source archives and checksums, then rebuild and test from the extracted source archive.
5. Exclude settings, credentials, private screenshots and unrelated files. Retain licensing and attribution documents.
6. Tag the verified source commit. Publish early versions as GitHub prereleases with both archives and SHA256SUMS.txt.
7. Use [RELEASE_NOTES.md](RELEASE_NOTES.md) for the release description. After publication, verify the README download links and all release assets.

Configure private vulnerability reporting, review rules and available credential protections according to maintenance needs. Keep account credentials and access tokens outside the project.
