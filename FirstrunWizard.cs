using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace HomeworkViewer
{
    public partial class FirstRunWizard : Form
    {
        private AppConfig _config;          // 原始配置（最终保存用）
        private AppConfig _tempConfig;      // 临时配置（步骤间保存）
        private int _currentStep = 0;
        private Panel _stepPanel;
        private Button _btnPrev, _btnNext, _btnFinish;
        private Label _stepTitle;

        // 第一步：科目
        private ComboBox _cmbSubjectPreset;
        private TextBox _txtCustomSubjects;
        private Label _lblCustomHint;

        // 第二步：晚修
        private CheckBox _chkHasEvening;
        private NumericUpDown _numEveningCount;
        private FlowLayoutPanel _eveningTimePanel;
        private List<DateTimePicker> _startPickers = new List<DateTimePicker>();
        private List<DateTimePicker> _endPickers = new List<DateTimePicker>();

        // 第三步：字体设置（字号、自定义字体、字体颜色）
        private ComboBox _cmbFontSize;
        private CheckBox _chkCustomFont;
        private ComboBox _cmbCustomFont;
        private RadioButton _rbFontBlack, _rbFontWhite;

        // 第四步：卡片外观（透明度滑条 + 预览卡片）
        private TrackBar _trackCardOpacity;
        private NumericUpDown _numCardOpacity;
        private Panel _previewCard;

        // 第五步：整体外观（背景类型、图片路径、背景效果、顶部栏颜色）
        private RadioButton _rbTransparentBg, _rbImageBg;
        private TextBox _txtImagePath;
        private Button _btnBrowseImage;
        private ComboBox _cmbBackgroundEffect;
        private Button _btnBarColor;
        private Panel _pnlBarColorPreview;

        // 预设科目
        private Dictionary<string, List<string>> _subjectPresets = new Dictionary<string, List<string>>
        {
            { "大理", new List<string> { "语文", "数学", "英语", "物理", "化学", "生物" } },
            { "中理", new List<string> { "语文", "数学", "英语", "物理", "化学", "地理" } },
            { "小理", new List<string> { "语文", "数学", "英语", "物理", "化学", "政治" } },
            { "大文", new List<string> { "语文", "数学", "英语", "政治", "历史", "地理" } },
            { "大文美术", new List<string> { "语文", "数学", "英语", "政治", "历史", "地理", "美术" } },
            { "大理美术", new List<string> { "语文", "数学", "英语", "物理", "化学", "生物", "美术" } },
            { "音乐", new List<string> { "语文", "数学", "英语", "政治", "历史", "地理", "音乐" } },
            { "竞赛", new List<string> { "语文", "数学", "英语", "物理", "化学", "生物", "竞赛" } },
            { "全科", new List<string> { "语文", "数学", "英语", "物理", "化学", "生物", "政治", "历史", "地理" } }
        };

        public FirstRunWizard(AppConfig config)
        {
            _config = config;
            // 克隆配置作为临时存储（深拷贝）
            _tempConfig = JsonSerializerClone(config);
            InitializeComponent();
            LoadStep(0);
        }

        // 深拷贝辅助方法
        private AppConfig JsonSerializerClone(AppConfig source)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(source);
            return System.Text.Json.JsonSerializer.Deserialize<AppConfig>(json);
        }

        private void InitializeComponent()
        {
            this.Text = "作业展板 - 首次使用向导";
            this.Size = new Size(720, 780);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;

            _stepTitle = new Label
            {
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("微软雅黑", 14, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            _stepPanel = new Panel
            {
                Location = new Point(20, 60),
                Size = new Size(this.ClientSize.Width - 40, this.ClientSize.Height - 150),
                BackColor = Color.FromArgb(45, 45, 48),
                AutoScroll = true
            };
            _btnPrev = new Button
            {
                Text = "上一步",
                Location = new Point(this.ClientSize.Width - 180, this.ClientSize.Height - 70),
                Size = new Size(75, 30),
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            _btnNext = new Button
            {
                Text = "下一步",
                Location = new Point(this.ClientSize.Width - 95, this.ClientSize.Height - 70),
                Size = new Size(75, 30),
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnFinish = new Button
            {
                Text = "完成",
                Location = new Point(this.ClientSize.Width - 95, this.ClientSize.Height - 70),
                Size = new Size(75, 30),
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Visible = false
            };

            _btnPrev.Click += (s, e) => 
            {
                if (_currentStep > 0)
                {
                    SaveCurrentStepToTemp();      // 保存当前步骤到临时配置
                    _currentStep--;
                    LoadStep(_currentStep);
                }
            };
            _btnNext.Click += (s, e) =>
            {
                if (_currentStep < 6)
                {
                    SaveCurrentStepToTemp();      // 保存当前步骤到临时配置
                    _currentStep++;
                    LoadStep(_currentStep);
                }
            };
            _btnFinish.Click += (s, e) => 
            { 
                SaveCurrentStepToTemp();           // 保存最后一步
                // 将临时配置复制到最终配置并保存
                CopyTempToConfigAndSave();
                this.DialogResult = DialogResult.OK; 
                Close(); 
            };

            this.Controls.Add(_stepTitle);
            this.Controls.Add(_stepPanel);
            this.Controls.Add(_btnPrev);
            this.Controls.Add(_btnNext);
            this.Controls.Add(_btnFinish);
        }

        private void LoadStep(int step)
        {
            _stepPanel.Controls.Clear();
            switch (step)
            {
                case 0:
                    _stepTitle.Text = "欢迎";
                    CreateWelcomePage();
                    _btnPrev.Enabled = false;
                    _btnNext.Visible = true;
                    _btnFinish.Visible = false;
                    break;
                case 1:
                    _stepTitle.Text = "第一步：设置科目";
                    CreateStep1();
                    _btnPrev.Enabled = true;
                    _btnNext.Visible = true;
                    _btnFinish.Visible = false;
                    break;
                case 2:
                    _stepTitle.Text = "第二步：设置晚修时间";
                    CreateStep2();
                    _btnPrev.Enabled = true;
                    _btnNext.Visible = true;
                    _btnFinish.Visible = false;
                    break;
                case 3:
                    _stepTitle.Text = "第三步：字体设置";
                    CreateStep3();
                    _btnPrev.Enabled = true;
                    _btnNext.Visible = true;
                    _btnFinish.Visible = false;
                    break;
                case 4:
                    _stepTitle.Text = "第四步：卡片外观";
                    CreateStep4();
                    _btnPrev.Enabled = true;
                    _btnNext.Visible = true;
                    _btnFinish.Visible = false;
                    break;
                case 5:
                    _stepTitle.Text = "第五步：整体外观";
                    CreateStep5();
                    _btnPrev.Enabled = true;
                    _btnNext.Visible = true;
                    _btnFinish.Visible = false;
                    break;
                case 6:
                    _stepTitle.Text = "完成";
                    CreateThanksPage();
                    _btnPrev.Visible = false;
                    _btnNext.Visible = false;
                    _btnFinish.Visible = true;
                    break;
            }
        }

        // 保存当前步骤的控件值到 _tempConfig
        private void SaveCurrentStepToTemp()
        {
            switch (_currentStep)
            {
                case 1: // 科目
                    string customText = _txtCustomSubjects.Text.Trim();
                    if (!string.IsNullOrEmpty(customText))
                        _tempConfig.CustomSubjects = customText.Split(',').Select(s => s.Trim()).ToList();
                    else if (_cmbSubjectPreset.SelectedItem != null)
                        _tempConfig.CustomSubjects = _subjectPresets[_cmbSubjectPreset.SelectedItem.ToString()];
                    else
                        _tempConfig.CustomSubjects = new List<string> { "语文", "数学", "英语", "物理", "化学", "生物" };
                    break;
                case 2: // 晚修
                    _tempConfig.ShowDueTime = _chkHasEvening.Checked;
                    if (_chkHasEvening.Checked)
                    {
                        _tempConfig.EveningClassCount = (int)_numEveningCount.Value;
                        _tempConfig.EveningClassTimes.Clear();
                        for (int i = 0; i < _startPickers.Count; i++)
                            _tempConfig.EveningClassTimes.Add(new EveningClassTime
                            {
                                Start = _startPickers[i].Value.ToString("HH:mm"),
                                End = _endPickers[i].Value.ToString("HH:mm")
                            });
                    }
                    else
                    {
                        _tempConfig.EveningClassCount = 0;
                        _tempConfig.EveningClassTimes.Clear();
                    }
                    break;
                case 3: // 字体设置
                    _tempConfig.FontSizeLevel = _cmbFontSize.SelectedIndex;
                    _tempConfig.UseCustomFont = _chkCustomFont.Checked;
                    if (_chkCustomFont.Checked && _cmbCustomFont.SelectedItem != null)
                        _tempConfig.CustomFontName = _cmbCustomFont.SelectedItem.ToString();
                    _tempConfig.FontColorWhite = _rbFontWhite.Checked;
                    break;
                case 4: // 卡片外观
                    if (_trackCardOpacity != null)
                        _tempConfig.CardOpacity = _trackCardOpacity.Value;
                    break;
                case 5: // 整体外观
                    _tempConfig.UseBackgroundImage = _rbImageBg.Checked;
                    _tempConfig.BackgroundImagePath = _rbImageBg.Checked ? _txtImagePath.Text.Trim() : "";
                    _tempConfig.BackgroundEffect = _cmbBackgroundEffect.SelectedItem?.ToString() ?? "Mica";
                    _tempConfig.BarColor = $"{_pnlBarColorPreview.BackColor.R},{_pnlBarColorPreview.BackColor.G},{_pnlBarColorPreview.BackColor.B}";
                    break;
            }
        }

        private void CopyTempToConfigAndSave()
        {
            // 将临时配置的所有属性复制到原始配置
            _config.CustomSubjects = _tempConfig.CustomSubjects;
            _config.ShowDueTime = _tempConfig.ShowDueTime;
            _config.EveningClassCount = _tempConfig.EveningClassCount;
            _config.EveningClassTimes = _tempConfig.EveningClassTimes;
            _config.FontSizeLevel = _tempConfig.FontSizeLevel;
            _config.UseCustomFont = _tempConfig.UseCustomFont;
            _config.CustomFontName = _tempConfig.CustomFontName;
            _config.FontColorWhite = _tempConfig.FontColorWhite;
            _config.CardOpacity = _tempConfig.CardOpacity;
            _config.UseBackgroundImage = _tempConfig.UseBackgroundImage;
            _config.BackgroundImagePath = _tempConfig.BackgroundImagePath;
            _config.BackgroundEffect = _tempConfig.BackgroundEffect;
            _config.BarColor = _tempConfig.BarColor;
            _config.Save();
        }

        // 以下各步骤创建方法，加载时从 _tempConfig 读取值
        private void CreateWelcomePage()
        {
            Label lblWelcome = new Label
            {
                Text = "欢迎您使用作业展板",
                Font = new Font("微软雅黑", 20, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 30),
                BackColor = Color.Transparent
            };
            Label lblHint = new Label
            {
                Text = "点击下一步开始首次设置",
                Font = new Font("微软雅黑", 12),
                ForeColor = Color.LightGray,
                AutoSize = true,
                Location = new Point(20, 80),
                BackColor = Color.Transparent
            };
            _stepPanel.Controls.Add(lblWelcome);
            _stepPanel.Controls.Add(lblHint);
        }

        private void CreateStep1()
        {
            int y = 10;
            Label lblPreset = new Label { Text = "选择科目模板：", Location = new Point(10, y), AutoSize = true, ForeColor = Color.White, BackColor = Color.Transparent };
            _stepPanel.Controls.Add(lblPreset);
            y += 25;
            _cmbSubjectPreset = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(20, y),
                Width = 200,
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White
            };
            _cmbSubjectPreset.Items.AddRange(_subjectPresets.Keys.ToArray());
            // 根据 _tempConfig 已有的科目列表尝试匹配预设
            if (_tempConfig.CustomSubjects != null && _tempConfig.CustomSubjects.Count > 0)
            {
                string currentStr = string.Join(",", _tempConfig.CustomSubjects);
                var match = _subjectPresets.FirstOrDefault(kvp => string.Join(",", kvp.Value) == currentStr);
                if (match.Key != null)
                    _cmbSubjectPreset.SelectedItem = match.Key;
                else
                    _cmbSubjectPreset.SelectedIndex = 0;
            }
            else
                _cmbSubjectPreset.SelectedIndex = 0;
            _stepPanel.Controls.Add(_cmbSubjectPreset);
            y += 30;
            Label lblOr = new Label { Text = "或自定义科目（逗号分隔）：", Location = new Point(10, y), AutoSize = true, ForeColor = Color.White, BackColor = Color.Transparent };
            _stepPanel.Controls.Add(lblOr);
            y += 25;
            _txtCustomSubjects = new TextBox
            {
                Location = new Point(20, y),
                Width = 400,
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White,
                Text = (_tempConfig.CustomSubjects != null) ? string.Join(",", _tempConfig.CustomSubjects) : "语文,数学,英语,物理,化学,生物"
            };
            _lblCustomHint = new Label
            {
                Text = "用英文逗号分隔科目名称",
                Location = new Point(20, y + 25),
                AutoSize = true,
                ForeColor = Color.LightGray,
                Font = new Font("微软雅黑", 8),
                BackColor = Color.Transparent
            };
            _stepPanel.Controls.Add(_txtCustomSubjects);
            _stepPanel.Controls.Add(_lblCustomHint);

            _cmbSubjectPreset.SelectedIndexChanged += (s, e) =>
            {
                if (_cmbSubjectPreset.SelectedItem != null && _txtCustomSubjects != null)
                {
                    string key = _cmbSubjectPreset.SelectedItem.ToString();
                    _txtCustomSubjects.Text = string.Join(",", _subjectPresets[key]);
                }
            };
        }

        private void CreateStep2()
        {
            int y = 10;
            _chkHasEvening = new CheckBox { Text = "启用晚修提醒", Location = new Point(10, y), AutoSize = true, ForeColor = Color.White, Checked = _tempConfig.ShowDueTime, BackColor = Color.Transparent };
            _stepPanel.Controls.Add(_chkHasEvening);
            y += 30;
            Label lblCount = new Label { Text = "晚修节数：", Location = new Point(20, y), AutoSize = true, ForeColor = Color.White, BackColor = Color.Transparent };
            _numEveningCount = new NumericUpDown { Minimum = 1, Maximum = 6, Value = _tempConfig.EveningClassCount, Width = 60, Location = new Point(120, y - 3), BackColor = Color.FromArgb(64, 64, 64), ForeColor = Color.White };
            _stepPanel.Controls.Add(lblCount);
            _stepPanel.Controls.Add(_numEveningCount);
            y += 35;
            _eveningTimePanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                Location = new Point(20, y),
                Width = 450,
                Height = 200,
                AutoSize = true,
                BackColor = Color.FromArgb(45, 45, 48)
            };
            _stepPanel.Controls.Add(_eveningTimePanel);
            _numEveningCount.ValueChanged += (s, e) => UpdateEveningEditors();
            _chkHasEvening.CheckedChanged += (s, e) =>
            {
                bool enabled = _chkHasEvening.Checked;
                _numEveningCount.Enabled = enabled;
                _eveningTimePanel.Visible = enabled;
                if (!enabled) _eveningTimePanel.Controls.Clear();
                else UpdateEveningEditors();
            };
            UpdateEveningEditors();
        }

        private void UpdateEveningEditors()
        {
            _eveningTimePanel.Controls.Clear();
            _startPickers.Clear();
            _endPickers.Clear();
            int count = (int)_numEveningCount.Value;
            // 使用临时配置中已有的时间段，如果没有则用默认值
            var existingTimes = _tempConfig.EveningClassTimes;
            string[] defaultStarts = { "18:50", "20:00", "21:10" };
            string[] defaultEnds = { "19:50", "21:00", "22:20" };
            for (int i = 0; i < count; i++)
            {
                Panel row = new Panel { Height = 35, Width = 400, BackColor = Color.FromArgb(45, 45, 48) };
                Label lbl = new Label { Text = $"第{i + 1}节：", Location = new Point(0, 8), AutoSize = true, ForeColor = Color.White, BackColor = Color.Transparent };
                string startVal = (i < existingTimes.Count) ? existingTimes[i].Start : defaultStarts[Math.Min(i, defaultStarts.Length - 1)];
                string endVal = (i < existingTimes.Count) ? existingTimes[i].End : defaultEnds[Math.Min(i, defaultEnds.Length - 1)];
                DateTimePicker startPicker = new DateTimePicker
                {
                    Format = DateTimePickerFormat.Custom,
                    CustomFormat = "HH:mm",
                    ShowUpDown = true,
                    Location = new Point(70, 5),
                    Width = 80,
                    Value = DateTime.TryParse(startVal, out DateTime s) ? s : DateTime.Parse("19:00"),
                    BackColor = Color.FromArgb(64, 64, 64),
                    ForeColor = Color.White
                };
                Label lblTo = new Label { Text = "—", Location = new Point(160, 8), AutoSize = true, ForeColor = Color.White, BackColor = Color.Transparent };
                DateTimePicker endPicker = new DateTimePicker
                {
                    Format = DateTimePickerFormat.Custom,
                    CustomFormat = "HH:mm",
                    ShowUpDown = true,
                    Location = new Point(180, 5),
                    Width = 80,
                    Value = DateTime.TryParse(endVal, out DateTime e) ? e : DateTime.Parse("19:50"),
                    BackColor = Color.FromArgb(64, 64, 64),
                    ForeColor = Color.White
                };
                _startPickers.Add(startPicker);
                _endPickers.Add(endPicker);
                row.Controls.Add(lbl);
                row.Controls.Add(startPicker);
                row.Controls.Add(lblTo);
                row.Controls.Add(endPicker);
                _eveningTimePanel.Controls.Add(row);
            }
        }

        private void CreateStep3()
        {
            int y = 10;
            Label lblFontSize = new Label { Text = "字号大小：", Location = new Point(10, y), AutoSize = true, ForeColor = Color.White, BackColor = Color.Transparent };
            _cmbFontSize = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(120, y - 3),
                Width = 100,
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White
            };
            _cmbFontSize.Items.AddRange(new object[] { "小", "中", "大" });
            _cmbFontSize.SelectedIndex = _tempConfig.FontSizeLevel;
            _stepPanel.Controls.Add(lblFontSize);
            _stepPanel.Controls.Add(_cmbFontSize);
            y += 40;

            Label lblCustomFont = new Label { Text = "自定义字体：", Location = new Point(10, y), AutoSize = true, ForeColor = Color.White, BackColor = Color.Transparent };
            _chkCustomFont = new CheckBox { Text = "启用", Location = new Point(120, y - 3), AutoSize = true, ForeColor = Color.White, Checked = _tempConfig.UseCustomFont, BackColor = Color.Transparent };
            _cmbCustomFont = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(180, y - 3),
                Width = 250,
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White,
                Enabled = _tempConfig.UseCustomFont
            };
            string[] fonts = FontManager.GetInstalledFonts();
            _cmbCustomFont.Items.AddRange(fonts);
            _cmbCustomFont.SelectedItem = _tempConfig.CustomFontName;
            _chkCustomFont.CheckedChanged += (s, e) => _cmbCustomFont.Enabled = _chkCustomFont.Checked;
            _stepPanel.Controls.Add(lblCustomFont);
            _stepPanel.Controls.Add(_chkCustomFont);
            _stepPanel.Controls.Add(_cmbCustomFont);
            y += 40;

            Label lblFontColor = new Label { Text = "字体颜色：", Location = new Point(10, y), AutoSize = true, ForeColor = Color.White, BackColor = Color.Transparent };
            FlowLayoutPanel fontColorPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Location = new Point(120, y - 3), AutoSize = true, BackColor = Color.Transparent };
            _rbFontBlack = new RadioButton { Text = "黑色", AutoSize = true, ForeColor = Color.White, Checked = !_tempConfig.FontColorWhite, BackColor = Color.Transparent };
            _rbFontWhite = new RadioButton { Text = "白色", AutoSize = true, ForeColor = Color.White, Checked = _tempConfig.FontColorWhite, BackColor = Color.Transparent };
            fontColorPanel.Controls.Add(_rbFontBlack);
            fontColorPanel.Controls.Add(_rbFontWhite);
            _stepPanel.Controls.Add(lblFontColor);
            _stepPanel.Controls.Add(fontColorPanel);
        }

        private void CreateStep4()
        {
            int y = 10;
            Label lblOpacity = new Label { Text = "卡片透明度：", Location = new Point(10, y), AutoSize = true, ForeColor = Color.White, BackColor = Color.Transparent };
            _trackCardOpacity = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = _tempConfig.CardOpacity,
                TickFrequency = 10,
                Width = 200,
                Location = new Point(120, y - 5)
            };
            _numCardOpacity = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 100,
                Value = _tempConfig.CardOpacity,
                Width = 50,
                Location = new Point(330, y - 3),
                BackColor = Color.FromArgb(64, 64, 64),
                ForeColor = Color.White
            };
            _trackCardOpacity.ValueChanged += (s, e) => { _numCardOpacity.Value = _trackCardOpacity.Value; UpdateCardPreview(); };
            _numCardOpacity.ValueChanged += (s, e) => { _trackCardOpacity.Value = (int)_numCardOpacity.Value; UpdateCardPreview(); };
            _stepPanel.Controls.Add(lblOpacity);
            _stepPanel.Controls.Add(_trackCardOpacity);
            _stepPanel.Controls.Add(_numCardOpacity);
            y += 50;

            Label lblPreview = new Label { Text = "预览效果：", Location = new Point(10, y), AutoSize = true, ForeColor = Color.White, BackColor = Color.Transparent };
            y += 25;
            _previewCard = new Panel
            {
                Location = new Point(120, y),
                Size = new Size(300, 180),
                BackColor = Color.FromArgb(45, 45, 48),
                BorderStyle = BorderStyle.FixedSingle
            };
            _previewCard.Paint += PreviewCard_Paint;
            _stepPanel.Controls.Add(lblPreview);
            _stepPanel.Controls.Add(_previewCard);
        }

        private void UpdateCardPreview()
        {
            _previewCard?.Invalidate();
        }

        private void PreviewCard_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = _previewCard.ClientRectangle;
            rect.Inflate(-2, -2);
            int opacity = _trackCardOpacity?.Value ?? 15;
            int alpha = (int)(255 * opacity / 100.0);
            int topAlpha = (int)(220 * alpha / 255.0);
            int bottomAlpha = (int)(160 * alpha / 255.0);

            using (var path = new GraphicsPath())
            {
                path.AddRectangle(rect);
                using (var brush = new LinearGradientBrush(rect, Color.FromArgb(topAlpha, 255, 255, 255), Color.FromArgb(bottomAlpha, 240, 240, 255), LinearGradientMode.Vertical))
                {
                    g.FillPath(brush, path);
                }
                using (var pen = new Pen(Color.FromArgb(100, 128, 128, 128), 1))
                {
                    g.DrawPath(pen, path);
                }
            }

            string fontName = "微软雅黑";
            if (_chkCustomFont.Checked && _cmbCustomFont.SelectedItem != null)
                fontName = _cmbCustomFont.SelectedItem.ToString();
            int fontSizeIndex = _cmbFontSize.SelectedIndex;
            float titleSize = 12, contentSize = 10;
            if (fontSizeIndex == 0) { titleSize = 10; contentSize = 8; }
            else if (fontSizeIndex == 1) { titleSize = 12; contentSize = 10; }
            else if (fontSizeIndex == 2) { titleSize = 14; contentSize = 12; }
            bool isWhite = _rbFontWhite.Checked;
            Color textColor = isWhite ? Color.White : Color.Black;

            using (Font titleFont = new Font(fontName, titleSize, FontStyle.Bold))
            using (Font contentFont = new Font(fontName, contentSize, FontStyle.Regular))
            using (Brush textBrush = new SolidBrush(textColor))
            {
                g.DrawString("语文", titleFont, textBrush, rect.X + 10, rect.Y + 10);
                g.DrawLine(Pens.Gray, rect.X + 10, rect.Y + 35, rect.Right - 10, rect.Y + 35);
                g.DrawString("预习课文，完成练习册第10页", contentFont, textBrush, rect.X + 10, rect.Y + 45);
            }
        }

        private void CreateStep5()
        {
            int y = 10;
            Label lblBgType = new Label { Text = "背景类型：", Location = new Point(10, y), AutoSize = true, ForeColor = Color.White, BackColor = Color.Transparent };
            _rbTransparentBg = new RadioButton { Text = "透明背景", Location = new Point(120, y - 3), AutoSize = true, ForeColor = Color.White, Checked = !_tempConfig.UseBackgroundImage, BackColor = Color.Transparent };
            _rbImageBg = new RadioButton { Text = "图片背景", Location = new Point(210, y - 3), AutoSize = true, ForeColor = Color.White, Checked = _tempConfig.UseBackgroundImage, BackColor = Color.Transparent };
            _stepPanel.Controls.Add(lblBgType);
            _stepPanel.Controls.Add(_rbTransparentBg);
            _stepPanel.Controls.Add(_rbImageBg);
            y += 40;

            Label lblImagePath = new Label { Text = "图片路径：", Location = new Point(10, y), AutoSize = true, ForeColor = Color.White, BackColor = Color.Transparent, Visible = _tempConfig.UseBackgroundImage };
            _txtImagePath = new TextBox { Location = new Point(120, y - 3), Width = 300, BackColor = Color.FromArgb(64, 64, 64), ForeColor = Color.White, Text = _tempConfig.BackgroundImagePath, Visible = _tempConfig.UseBackgroundImage };
            _btnBrowseImage = new Button { Text = "浏览...", Location = new Point(430, y - 5), Width = 60, Height = 23, BackColor = Color.FromArgb(64, 64, 64), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Visible = _tempConfig.UseBackgroundImage };
            _btnBrowseImage.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp";
                    ofd.Title = "选择背景图片";
                    if (ofd.ShowDialog() == DialogResult.OK)
                        _txtImagePath.Text = ofd.FileName;
                }
            };
            _stepPanel.Controls.Add(lblImagePath);
            _stepPanel.Controls.Add(_txtImagePath);
            _stepPanel.Controls.Add(_btnBrowseImage);
            y += 40;

            // Label lblEffect = new Label { Text = "背景效果：", Location = new Point(10, y), AutoSize = true, ForeColor = Color.White, BackColor = Color.Transparent };
            // _cmbBackgroundEffect = new ComboBox
            // {
            //     DropDownStyle = ComboBoxStyle.DropDownList,
            //     Location = new Point(120, y - 3),
            //     Width = 150,
            //     BackColor = Color.FromArgb(64, 64, 64),
            //     ForeColor = Color.White
            // };
            _cmbBackgroundEffect.Items.AddRange(new object[] { "Mica", "Acrylic", "Aero" });
            _cmbBackgroundEffect.SelectedItem = _tempConfig.BackgroundEffect;
            _stepPanel.Controls.Add(lblEffect);
            _stepPanel.Controls.Add(_cmbBackgroundEffect);
            y += 40;

            Label lblBarColor = new Label { Text = "顶部栏颜色：", Location = new Point(10, y), AutoSize = true, ForeColor = Color.White, BackColor = Color.Transparent };
            _btnBarColor = new Button { Text = "选择颜色", Location = new Point(120, y - 3), Width = 80, Height = 25, BackColor = Color.FromArgb(64, 64, 64), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            _pnlBarColorPreview = new Panel { Location = new Point(210, y - 3), Size = new Size(40, 25), BackColor = ParseColor(_tempConfig.BarColor, Color.FromArgb(128, 255, 255)), BorderStyle = BorderStyle.FixedSingle };
            _btnBarColor.Click += (s, e) =>
            {
                using (ColorDialog cd = new ColorDialog())
                {
                    cd.Color = _pnlBarColorPreview.BackColor;
                    if (cd.ShowDialog() == DialogResult.OK)
                        _pnlBarColorPreview.BackColor = cd.Color;
                }
            };
            _stepPanel.Controls.Add(lblBarColor);
            _stepPanel.Controls.Add(_btnBarColor);
            _stepPanel.Controls.Add(_pnlBarColorPreview);

            Action updateImageControls = () =>
            {
                bool useImage = _rbImageBg.Checked;
                lblImagePath.Visible = useImage;
                _txtImagePath.Visible = useImage;
                _btnBrowseImage.Visible = useImage;
                _cmbBackgroundEffect.Enabled = !useImage;
            };
            _rbTransparentBg.CheckedChanged += (s, e) => updateImageControls();
            _rbImageBg.CheckedChanged += (s, e) => updateImageControls();
            updateImageControls();
        }

        private Color ParseColor(string rgbString, Color defaultColor)
        {
            try
            {
                var parts = rgbString.Split(',');
                if (parts.Length == 3) return Color.FromArgb(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
            }
            catch { }
            return defaultColor;
        }

        private void CreateThanksPage()
        {
            Label lblThanks = new Label
            {
                Text = "感谢您，您已完成初次使用的设置",
                Font = new Font("微软雅黑", 16, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 50),
                BackColor = Color.Transparent
            };
            Label lblHint = new Label
            {
                Text = "这些选项以后可以在设置中更改",
                Font = new Font("微软雅黑", 10),
                ForeColor = Color.LightGray,
                AutoSize = true,
                Location = new Point(20, 100),
                BackColor = Color.Transparent
            };
            _stepPanel.Controls.Add(lblThanks);
            _stepPanel.Controls.Add(lblHint);
        }
    }
}