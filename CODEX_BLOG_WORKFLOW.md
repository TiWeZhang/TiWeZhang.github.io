# 博客维护交接说明（供 Codex 使用）

此仓库是 `TiWeZhang.github.io` 的 GitHub Pages 博客，使用 Jekyll 的 Chirpy 主题。
线上地址：<https://tiwezhang.github.io>

## 约定的写作与发布流程

作者在 Typora 中撰写文章，不要求本地启动 Jekyll 预览。完成后，由 Codex 协助发布并推送 GitHub。

```text
writing（Typora 图文源稿）
  -> tools/publish-post.ps1（发布转换）
  -> _posts + assets/img/posts（Chirpy 发布源）
  -> git commit + git push origin main
```

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

从仓库根目录运行 Windows PowerShell：

```powershell
.\tools\publish-post.ps1 -Source ".\writing\测试文档.md"
```

脚本会：

1. 若原文件名没有日期前缀，为发布文章添加当天日期，例如 `测试文档.md` 变为 `_posts/YYYY-MM-DD-测试文档.md`。
2. 复制同名 `.assets` 图片目录到 `assets/img/posts/YYYY-MM-DD-测试文档/`。
3. 将 Typora 的图片相对路径改为图片文件名。
4. 添加或更新 Chirpy 的 `media_subpath`，使线上页面正确引用图片。
5. 若文章没有 YAML Front Matter，自动创建包含标题、日期和 `media_subpath` 的基本 Front Matter。

发布后的文章应类似：

```yaml
---
title: "测试文档"
date: 2026-08-12 12:00:00 +0800
media_subpath: /assets/img/posts/2026-08-12-测试文档/
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
- 文章发布前确认图片目录和 Markdown 文件同名；否则脚本不会同步图片。
- 若用户需要 Word 导入，Pandoc 已安装，可用 `--extract-media` 将图片导出到同名 `.assets` 目录，再用 Typora 检查和编辑。
- 本仓库当前不要求本地 Jekyll 预览；不要为此安装 Ruby 或修改站点配置，除非作者要求。
- `tools/publish-post.ps1` 已兼容 Windows PowerShell 5.1，脚本输出应保持 ASCII/英文，避免 UTF-8 无 BOM 的中文解析问题。
