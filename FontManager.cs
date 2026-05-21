using System;
using System.Drawing;
using System.Drawing.Text;

namespace HomeworkViewer
{
    public static class FontManager
    {
        public static Font GetFont(string fontName, float size, FontStyle style = FontStyle.Regular)
        {
            try
            {
                using (var testFont = new Font(fontName, size))
                {
                    return new Font(fontName, size, style);
                }
            }
            catch
            {
                return new Font("微软雅黑", size, style);
            }
        }

        public static string[] GetInstalledFonts()
        {
            using (var fonts = new InstalledFontCollection())
            {
                var families = fonts.Families;
                string[] fontNames = new string[families.Length];
                for (int i = 0; i < families.Length; i++)
                    fontNames[i] = families[i].Name;
                Array.Sort(fontNames);
                return fontNames;
            }
        }
    }
}