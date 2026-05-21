using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace HomeworkViewer
{
    public static class ExportHelper
    {
        private static readonly string TemplateHtml = @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>作业展板导出</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { 
            font-family: 'Microsoft YaHei', '微软雅黑', sans-serif; 
            background: #1e1e1e; 
            padding: 30px; 
            color: #e0e0e0;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background: rgba(32, 32, 32, 0.9);
            border-radius: 16px;
            padding: 20px;
            box-shadow: 0 8px 20px rgba(0,0,0,0.3);
        }
        .date {
            text-align: center;
            font-size: 24px;
            margin-bottom: 30px;
            padding-bottom: 15px;
            border-bottom: 2px solid #ffcc00;
            color: #ffcc00;
        }
        .subject-grid {
            display: grid;
            grid-template-columns: repeat(3, 1fr);
            gap: 20px;
        }
        .subject-card {
            background: rgba(45, 45, 55, 0.85);
            border-radius: 12px;
            padding: 15px;
            border: 1px solid rgba(255,255,255,0.2);
            box-shadow: 0 4px 8px rgba(0,0,0,0.2);
        }
        .subject-name {
            font-size: 20px;
            font-weight: bold;
            color: #ffcc00;
            margin-bottom: 8px;
            padding-bottom: 5px;
            border-bottom: 1px solid #ffcc00;
        }
        .due-time {
            font-size: 14px;
            color: #aaa;
            margin-bottom: 12px;
        }
        .content {
            font-size: 15px;
            line-height: 1.5;
            white-space: pre-wrap;
            word-wrap: break-word;
            color: #f0f0f0;
        }
        .content:empty::before {
            content: '暂无作业';
            color: #888;
            font-style: italic;
        }
        @media (max-width: 800px) {
            .subject-grid { grid-template-columns: repeat(2, 1fr); }
        }
        @media (max-width: 500px) {
            .subject-grid { grid-template-columns: 1fr; }
        }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""date"">{date}</div>
        <div class=""subject-grid"">
            {subjects}
        </div>
    </div>
</body>
</html>";

        // 导出 TXT（保持原有简单逻辑）
        public static void ExportToTxt(HomeworkData data, DateTime date, string filePath, bool includeUnsubmitted)
        {
            var sb = new StringBuilder();
            string separator = new string('—', 60);
            string dateLine = $"         {date:yyyy年MM月dd日}         ";
            string titleLine = "          作业          ";

            sb.AppendLine(separator);
            sb.AppendLine(dateLine);
            sb.AppendLine(titleLine);
            sb.AppendLine(separator);

            foreach (var subject in HomeworkData.SubjectNames)
            {
                if (!data.Subjects.ContainsKey(subject)) continue;
                string content = data.Subjects[subject] ?? "";
                sb.AppendLine();
                sb.AppendLine($"【{subject}】");
                string[] lines = content.Split('\n');
                foreach (var line in lines)
                {
                    string trimmed = line.TrimEnd();
                    if (!string.IsNullOrEmpty(trimmed))
                        sb.AppendLine(trimmed);
                }
                sb.AppendLine();
                sb.AppendLine(separator);
            }
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        // 导出 PDF（暂未实现，保留原有提示）
        public static void ExportToPdf(string txtPath, string pdfPath)
        {
            MessageBox.Show("PDF 导出功能需要安装 iTextSharp，暂未启用。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            File.Copy(txtPath, pdfPath, true);
        }

        // 导出 JPG —— 直接绘制窗体内容到 Bitmap，保证完整且背景与屏幕显示一致
        public static void ExportToJpg(Form form, string jpgPath)
        {
            // 临时隐藏编辑模式下可能弹出的下拉框等控件
            bool originalTopMost = form.TopMost;
            form.TopMost = true;  // 确保窗体置前，避免遮挡（不影响绘制）
            
            // 获取窗体实际客户区大小
            Rectangle bounds = form.ClientRectangle;
            
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                form.DrawToBitmap(bitmap, bounds);
                // 保存为高质量 JPEG
                bitmap.Save(jpgPath, ImageFormat.Jpeg);
            }
            
            form.TopMost = originalTopMost;
        }

        // 导出 HTML —— 修复样式，支持纯文本和提交时间，并正确转义特殊字符
        public static void ExportToHtml(HomeworkData data, DateTime date, string htmlPath, bool includeUnsubmitted)
        {
            var subjectsHtml = new StringBuilder();
            foreach (var subject in HomeworkData.SubjectNames)
            {
                if (!data.Subjects.ContainsKey(subject)) continue;
                
                string content = data.Subjects[subject] ?? "";
                // 纯文本转义 HTML 特殊字符（防止 XSS 和格式错乱）
                string escapedContent = EscapeHtml(content);
                // 保留换行
                escapedContent = escapedContent.Replace("\n", "<br/>");
                
                string dueTime = data.DueTimes.ContainsKey(subject) ? data.DueTimes[subject] : "无";
                string dueTimeHtml = $"<div class=\"due-time\">📅 提交时间：{EscapeHtml(dueTime)}</div>";
                
                // 如果没有作业内容，显示占位提示
                if (string.IsNullOrWhiteSpace(escapedContent))
                {
                    escapedContent = "<em style=\"color:#888;\">暂无作业内容</em>";
                }
                
                subjectsHtml.Append($@"
                <div class='subject-card'>
                    <div class='subject-name'>{EscapeHtml(subject)}</div>
                    {dueTimeHtml}
                    <div class='content'>{escapedContent}</div>
                </div>");
            }
            
            string dateStr = date.ToString("yyyy年MM月dd日");
            if (dateStr.Contains("年") && date.Hour == 0) // 仅日期显示
                dateStr = date.ToString("yyyy年MM月dd日");
            else
                dateStr = date.ToString("yyyy年MM月dd日 dddd");
                
            string html = TemplateHtml.Replace("{date}", dateStr)
                                       .Replace("{subjects}", subjectsHtml.ToString());
            File.WriteAllText(htmlPath, html, Encoding.UTF8);
        }

        // HTML 转义辅助方法
        private static string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace("&", "&amp;")
                       .Replace("<", "&lt;")
                       .Replace(">", "&gt;")
                       .Replace("\"", "&quot;")
                       .Replace("'", "&#39;");
        }
    }
}