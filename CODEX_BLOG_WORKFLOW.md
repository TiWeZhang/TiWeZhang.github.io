# 博客维护交接说明（供 Codex 使用）

此仓库是 `TiWeZhang.github.io` 的 GitHub Pages 博客，使用 Jekyll 的 Chirpy 主题。
线上地址：<https://tiwezhang.github.io>

## 约定的写作与发布流程

作者在 Typora 中撰写文章，不要求本地启动 Jekyll 预览。完成后，由 Codex 协助发布并推送 GitHub。

```text
writing（Typora 图文源稿）
  -> tools/BlogPublisher/BlogPublisher.exe（推荐：填写元数据并发布）
  -> tools/publish-post.ps1（备用命令行发布转换）
  -> _posts + assets/img/posts（Chirpy 发布源）
  -> git commit + git push origin main
```

## 推荐：使用 Blog Publisher 图形界面

发布前先安装 [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)。

双击仓库内的 `tools/BlogPublisher/BlogPublisher.exe`：

1. 左侧选择 `writing/` 中的一篇 Markdown 源稿（包含子目录）。
2. 填写标题、发布日期时间、分类和标签；分类、标签均用逗号分隔。
3. 点击“导出至 _posts”。工具会写入源稿 Front Matter，并同步生成发布文件和图片。
4. 图形界面不会提交、推送或部署；随后按本文的 Git 发布步骤完成上线。

源稿中会保存 `publish_target` 映射；发布稿中会保存 `source_path`。这两项由工具维护，不需要手动编辑。它们确保一个源稿只对应一个 `_posts` 文件。修改发布日期后，工具会自动重命名发布文件及其图片目录。

## Typora 设置

Typora 的“偏好设置 -> 图像”应保持：

- 插入图片时：复制图片到 `./${filename}.assets` 文件夹
- 勾选“对本地位置的图片应用上述规则”
- 勾选“优先使用相对路径”
- 不勾选“为相对路径添加 `./`”
- 不启用自动上传图床

## 写作目录结构

文章源稿位于 `writing/`。文件可以直接放在其中：

```text
writing/
  测试文档.md
  测试文档.assets/
    IMG_0752.PNG
```

也可采用每篇文章一个目录：

```text
writing/
  2026-08-12-my-post/
    2026-08-12-my-post.md
    2026-08-12-my-post.assets/
      image-1.png
```

Typora 图片链接一般形如：

```markdown
![图片说明](文章名.assets/image-1.png)
```

## 发布文章

图形界面不可用时，可从仓库根目录运行 Windows PowerShell：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\tools\publish-post.ps1 -Source ".\writing\测试文档.md"
```

脚本会：

1. 要求源稿已有 `title` 和 `date` Front Matter；图形界面会自动创建它们。
2. 根据 `date` 生成目标名，例如 `测试文档.md` 变为 `_posts/YYYY-MM-DD-测试文档.md`。
3. 保存源稿到发布稿的一对一映射；同一篇源稿重复发布时更新原文件，日期变化时迁移旧文件和图片目录。
4. 对未带映射的历史文章，按精确文件名自动认领；冲突或歧义会停止，不会覆盖任何文件。
5. 镜像同步同名 `.assets` 图片目录到 `assets/img/posts/YYYY-MM-DD-测试文档/`。
6. 将 Typora 的 Markdown 和 HTML 图片相对路径改为图片文件名，并添加或更新 Chirpy 的 `media_subpath`。
7. 若 Markdown 或 HTML 图片引用了同名 `.assets` 目录但该目录缺失，停止发布并报告错误。

发布后的文章应类似：

```yaml
---
title: "测试文档"
date: 2026-08-12 12:00:00 +0800
media_subpath: /assets/img/posts/2026-08-12-测试文档/
source_path: writing/测试文档.md
---
```

文章正文中的图片则类似：

```markdown
![图片说明](IMG_0752.PNG)
```

Chirpy 会依据 `media_subpath` 将其解析为正确的线上图片 URL。

## Git 发布

发布后检查变更，再提交和推送：

```powershell
git status
git add writing _posts assets/img tools CODEX_BLOG_WORKFLOW.md
git commit -m "Publish 文章标题"
git push origin main
```

推送 `main` 会触发 GitHub Actions 部署 GitHub Pages。除非作者明确要求，不要删除 `writing/` 中的源稿或 `.assets`；它们是可编辑的图文原稿备份。

## Codex 协助要点

- 先查看 `git status --short --branch`，避免覆盖用户未提交的内容。
- 优先使用图形界面。命令行备用方式要求源稿已有有效的 `title` 与 `date` Front Matter。
- 文章发布前确认图片目录和 Markdown 文件同名；若正文引用了该目录而目录不存在，脚本会拒绝发布。
- 若用户需要 Word 导入，Pandoc 已安装，可用 `--extract-media` 将图片导出到同名 `.assets` 目录，再用 Typora 检查和编辑。
- 本仓库当前不要求本地 Jekyll 预览；不要为此安装 Ruby 或修改站点配置，除非作者要求。
- `tools/publish-post.ps1` 已兼容 Windows PowerShell 5.1，脚本输出应保持 ASCII/英文，避免 UTF-8 无 BOM 的中文解析问题。
