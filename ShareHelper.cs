using System.Threading.Tasks;
using System.Windows.Forms;

namespace HomeworkViewer
{
    public static class ShareHelper
    {
        public static async Task ShowShareUIAsync(string filePath)
        {
            MessageBox.Show($"文件已保存至：{filePath}\n分享功能暂不可用，请手动分享。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await Task.CompletedTask;
        }
    }
}