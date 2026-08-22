using System.Globalization;

namespace BlogPublisher.WinForms;

internal sealed class MainForm : Form
{
    private const string DateFormat = "yyyy-MM-dd HH:mm:ss +0800";
    private readonly string _repositoryRoot;
    private readonly ListView _articleList = new();
    private readonly TextBox _titleTextBox = new();
    private readonly TextBox _sourceTextBox = CreateReadOnlyTextBox();
    private readonly DateTimePicker _datePicker = new();
    private readonly TextBox _categoriesTextBox = new();
    private readonly TextBox _tagsTextBox = new();
    private readonly TableLayoutPanel _contentLayout = new();
    private readonly FlowLayoutPanel _tagSuggestionsPanel = new();
    private readonly Button _saveButton = new();
    private readonly Button _publishButton = new();
    private readonly TextBox _outputTextBox = new();
    private List<ArticleInfo> _articles = [];
    private List<PublishedPostInfo> _publishedPosts = [];
    private List<string> _availableTags = [];
    private ArticleInfo? _selectedArticle;

    public MainForm(string repositoryRoot)
    {
        _repositoryRoot = repositoryRoot;
        Text = "Blog Publisher";
        MinimumSize = new Size(900, 780);
        ClientSize = new Size(1100, 800);
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout();
        LoadArticles();
    }

    private void BuildLayout()
    {
        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12)
        };
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        Controls.Add(rootLayout);

        // A table layout deliberately replaces SplitContainer here. SplitContainer
        // validates its splitter position before a form has a usable width on some
        // Windows/DPI combinations, which caused the startup exception.
        _contentLayout.Dock = DockStyle.Fill;
        _contentLayout.ColumnCount = 1;
        _contentLayout.RowCount = 2;
        _contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        _contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        rootLayout.Controls.Add(_contentLayout, 0, 0);

        _articleList.Dock = DockStyle.Fill;
        _articleList.View = View.Details;
        _articleList.FullRowSelect = true;
        _articleList.HideSelection = false;
        _articleList.MultiSelect = false;
        _articleList.Columns.Add("源稿", 190);
        _articleList.Columns.Add("标题", 125);
        _articleList.Columns.Add("日期", 125);
        _articleList.Columns.Add("状态", 70);
        _articleList.SelectedIndexChanged += (_, _) => SelectArticle();
        _articleList.SizeChanged += (_, _) => ResizeArticleColumns();
        _contentLayout.Controls.Add(_articleList, 0, 0);

        var editor = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8,
            Padding = new Padding(12)
        };
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var index = 0; index < 5; index++)
        {
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        }
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _contentLayout.Controls.Add(editor, 0, 1);

        AddField(editor, 0, "源稿", _sourceTextBox);
        AddField(editor, 1, "标题", _titleTextBox);
        _datePicker.Format = DateTimePickerFormat.Custom;
        _datePicker.CustomFormat = "yyyy-MM-dd HH:mm:ss";
        _datePicker.ShowUpDown = true;
        AddField(editor, 2, "发布日期", _datePicker);
        AddField(editor, 3, "分类", _categoriesTextBox);
        AddField(editor, 4, "标签", _tagsTextBox);

        _tagSuggestionsPanel.Dock = DockStyle.Fill;
        _tagSuggestionsPanel.AutoScroll = true;
        _tagSuggestionsPanel.WrapContents = true;
        _tagSuggestionsPanel.FlowDirection = FlowDirection.LeftToRight;
        _tagSuggestionsPanel.BorderStyle = BorderStyle.FixedSingle;
        _tagSuggestionsPanel.Padding = new Padding(6, 5, 0, 0);
        editor.Controls.Add(new Label { Text = "已有标签", TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, 5);
        editor.Controls.Add(_tagSuggestionsPanel, 1, 5);
        _tagsTextBox.TextChanged += (_, _) => UpdateTagSuggestionSelection();

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        _saveButton.Text = "保存元数据";
        _saveButton.AutoSize = true;
        _saveButton.Click += (_, _) => SaveMetadata();
        _publishButton.Text = "导出至 _posts";
        _publishButton.AutoSize = true;
        _publishButton.Click += async (_, _) => await PublishSelectedAsync();
        buttons.Controls.Add(_saveButton);
        buttons.Controls.Add(_publishButton);
        editor.Controls.Add(buttons, 1, 6);

        var note = new Label
        {
            Dock = DockStyle.Fill,
            Text = "分类和标签使用逗号分隔。导出仅更新 _posts 与图片，不会提交或推送 Git。",
            ForeColor = SystemColors.GrayText,
            AutoSize = true
        };
        editor.Controls.Add(note, 1, 7);

        _outputTextBox.Dock = DockStyle.Fill;
        _outputTextBox.ReadOnly = true;
        _outputTextBox.Multiline = true;
        _outputTextBox.ScrollBars = ScrollBars.Vertical;
        _outputTextBox.Font = new Font(FontFamily.GenericMonospace, 9);
        rootLayout.Controls.Add(_outputTextBox, 0, 1);
    }

    private static TextBox CreateReadOnlyTextBox() => new()
    {
        ReadOnly = true,
        BorderStyle = BorderStyle.FixedSingle,
        Dock = DockStyle.Fill
    };

    private void AddField(TableLayoutPanel editor, int row, string label, Control control)
    {
        editor.Controls.Add(new Label { Text = label, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Fill }, 0, row);
        control.Dock = DockStyle.Fill;
        editor.Controls.Add(control, 1, row);
    }

    private void LoadArticles(string? selectRelativePath = null)
    {
        _publishedPosts = Directory.EnumerateFiles(Path.Combine(_repositoryRoot, "_posts"), "*.md", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => PublishedPostInfo.Load(_repositoryRoot, path))
            .ToList();
        _articles = Directory.EnumerateFiles(Path.Combine(_repositoryRoot, "writing"), "*.md", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => ArticleInfo.Load(_repositoryRoot, path))
            .ToList();
        PublicationMatcher.Match(_articles, _publishedPosts);
        RefreshAvailableTags();

        _articleList.BeginUpdate();
        _articleList.Items.Clear();
        foreach (var article in _articles)
        {
            var item = new ListViewItem(article.RelativePath) { Tag = article };
            item.SubItems.Add(article.Title);
            item.SubItems.Add(article.Date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            item.SubItems.Add(GetPublicationStatusText(article.PublicationStatus));
            _articleList.Items.Add(item);
            if (article.RelativePath.Equals(selectRelativePath, StringComparison.OrdinalIgnoreCase))
            {
                item.Selected = true;
            }
        }
        _articleList.EndUpdate();
        ResizeArticleColumns();

        if (_articleList.SelectedItems.Count == 0 && _articleList.Items.Count > 0)
        {
            _articleList.Items[0].Selected = true;
        }
    }

    private void ResizeArticleColumns()
    {
        if (_articleList.Columns.Count == 0)
        {
            return;
        }

        for (var columnIndex = 0; columnIndex < _articleList.Columns.Count; columnIndex++)
        {
            var width = TextRenderer.MeasureText(_articleList.Columns[columnIndex].Text, _articleList.Font).Width + 24;
            foreach (ListViewItem item in _articleList.Items)
            {
                var text = columnIndex == 0 ? item.Text : item.SubItems[columnIndex].Text;
                width = Math.Max(width, TextRenderer.MeasureText(text, _articleList.Font).Width + 24);
            }

            _articleList.Columns[columnIndex].Width = width;
        }
    }

    private void RefreshAvailableTags()
    {
        _availableTags = _publishedPosts
            .SelectMany(post => post.Tags)
            .Select(tag => tag.Trim())
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        _tagSuggestionsPanel.SuspendLayout();
        _tagSuggestionsPanel.Controls.Clear();
        foreach (var tag in _availableTags)
        {
            var button = new Button
            {
                Text = tag,
                Tag = tag,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 8, 6),
                Padding = new Padding(6, 1, 6, 1),
                UseVisualStyleBackColor = false
            };
            button.Click += ToggleTagSuggestion;
            _tagSuggestionsPanel.Controls.Add(button);
        }
        _tagSuggestionsPanel.ResumeLayout();
        UpdateTagSuggestionSelection();
    }

    private static string GetPublicationStatusText(PublicationStatus status) => status switch
    {
        PublicationStatus.Published => "已导出",
        PublicationStatus.Uncertain => "匹配不确定",
        _ => "未导出"
    };

    private void ToggleTagSuggestion(object? sender, EventArgs eventArgs)
    {
        if (sender is not Button { Tag: string tag })
        {
            return;
        }

        var tags = ParseList(_tagsTextBox.Text).ToList();
        var existingIndex = tags.FindIndex(value => value.Equals(tag, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
        {
            tags.RemoveAt(existingIndex);
        }
        else
        {
            tags.Add(tag);
        }

        _tagsTextBox.Text = string.Join(", ", tags);
    }

    private void UpdateTagSuggestionSelection()
    {
        var selectedTags = ParseList(_tagsTextBox.Text).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var button in _tagSuggestionsPanel.Controls.OfType<Button>())
        {
            var isSelected = button.Tag is string tag && selectedTags.Contains(tag);
            button.BackColor = isSelected ? SystemColors.Highlight : SystemColors.Control;
            button.ForeColor = isSelected ? SystemColors.HighlightText : SystemColors.ControlText;
            button.FlatAppearance.BorderColor = isSelected ? SystemColors.Highlight : SystemColors.ControlDark;
        }
    }

    private void SelectArticle()
    {
        _selectedArticle = _articleList.SelectedItems.Count == 1
            ? _articleList.SelectedItems[0].Tag as ArticleInfo
            : null;

        var enabled = _selectedArticle is not null;
        _titleTextBox.Enabled = enabled;
        _datePicker.Enabled = enabled;
        _categoriesTextBox.Enabled = enabled;
        _tagsTextBox.Enabled = enabled;
        _saveButton.Enabled = enabled;
        _publishButton.Enabled = enabled;

        if (!enabled)
        {
            return;
        }

        _titleTextBox.Text = _selectedArticle!.Title;
        _datePicker.Value = _selectedArticle.Date.LocalDateTime;
        _categoriesTextBox.Text = string.Join(", ", _selectedArticle.Categories);
        _tagsTextBox.Text = string.Join(", ", _selectedArticle.Tags);
        _sourceTextBox.Text = _selectedArticle.RelativePath;
    }

    private bool SaveMetadata()
    {
        if (_selectedArticle is null)
        {
            return false;
        }

        var title = _titleTextBox.Text.Trim();
        if (title.Length == 0)
        {
            ShowError("Title cannot be empty.");
            return false;
        }

        try
        {
            var document = FrontMatterDocument.Load(_selectedArticle.SourcePath);
            document.SetScalar("title", title);
            document.SetScalar("date", new DateTimeOffset(_datePicker.Value).ToString(DateFormat, CultureInfo.InvariantCulture));
            document.SetSequence("categories", ParseList(_categoriesTextBox.Text));
            document.SetSequence("tags", ParseList(_tagsTextBox.Text));
            document.Save(_selectedArticle.SourcePath);
            _outputTextBox.Text = $"Saved metadata: {_selectedArticle.RelativePath}";
            return true;
        }
        catch (Exception exception)
        {
            ShowError(exception.Message);
            return false;
        }
    }

    private async Task PublishSelectedAsync()
    {
        if (_selectedArticle is null || !SaveMetadata())
        {
            return;
        }

        TogglePublishing(false);
        _outputTextBox.Text = "Publishing...";
        try
        {
            var result = await PublisherRunner.PublishAsync(_repositoryRoot, _selectedArticle.SourcePath);
            _outputTextBox.Text = result.Output;
            if (!result.Success)
            {
                ShowError(result.Output.Length == 0 ? "Publishing failed." : result.Output);
                return;
            }

            LoadArticles(_selectedArticle.RelativePath);
        }
        catch (Exception exception)
        {
            _outputTextBox.Text = exception.ToString();
            ShowError(exception.Message);
        }
        finally
        {
            TogglePublishing(true);
        }
    }

    private void TogglePublishing(bool enabled)
    {
        _articleList.Enabled = enabled;
        _saveButton.Enabled = enabled;
        _publishButton.Enabled = enabled;
    }

    private static IReadOnlyList<string> ParseList(string value) => value
        .Split([',', '，'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private void ShowError(string message) =>
        MessageBox.Show(message, "Blog Publisher", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
