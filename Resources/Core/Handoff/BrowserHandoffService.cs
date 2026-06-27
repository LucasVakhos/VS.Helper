using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace VS.Helper.Core.Handoff;

internal static class BrowserHandoffService
{
    public static void OpenBrowser()
    {
        try
        {
            // Не открываем новую вкладку ChatGPT по URL и не форсим Edge.
            // Сначала поднимаем уже открытый обычный браузер, затем запускаем Chrome/Firefox/Brave/Opera.
            StartPreferredBrowser();
        }
        catch
        {
            // Browser handoff is best-effort only.
        }
    }

    public static void OpenChatGpt()
    {
        // Совместимость со старым вызовом: теперь это просто браузер без новой вкладки по URL.
        OpenBrowser();
    }

    private static void StartPreferredBrowser()
    {
        string[] preferredProcessNames = { "chrome", "firefox", "brave", "opera" };

        foreach (string name in preferredProcessNames)
        {
            Process? proc = Process.GetProcessesByName(name)
                .FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);

            if (proc != null)
            {
                SetForegroundWindow(proc.MainWindowHandle);
                return;
            }
        }

        // Edge намеренно НЕ используем: пользователь просил не открывать Edge.
        string[] candidates = { "chrome.exe", "firefox.exe", "brave.exe", "opera.exe" };

        foreach (string candidate in candidates)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = candidate,
                    UseShellExecute = true
                });
                return;
            }
            catch
            {
                // пробуем следующий браузер
            }
        }

        // Последний безопасный fallback: открыть проводник, чтобы хотя бы не падать и не лезть в Edge.
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            UseShellExecute = true
        });
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
