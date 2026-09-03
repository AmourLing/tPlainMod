using System;
using System.IO;
using System.Text;
using Terraria;

namespace RecipeBrowser
{
    /// <summary>
    /// 轻量错误日志: 写到存档目录 RecipeBrowser-error.log, 便于定位 UI 异常
    /// </summary>
    internal static class ErrorLog
    {
        private const int MaxEntries = 20;
        private static int _count;

        public static void Write(Exception ex, string where)
        {
            if (ex == null || _count >= MaxEntries) return;
            _count++;
            try
            {
                string file = Path.Combine(Main.SavePath, "RecipeBrowser-error.log");
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"===== [{DateTime.Now:HH:mm:ss}] {where} =====");
                sb.AppendLine(ex.ToString());
                sb.AppendLine();
                File.AppendAllText(file, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }
    }
}
