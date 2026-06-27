﻿using System;
using System.Diagnostics;

namespace VS.Helper.Core.Handoff;

internal static class BrowserHandoffService
{
    public static void OpenBrowser()
    {
        try
        {
            // Не открываем новую вкладку ChatGPT по URL.
            // Просто поднимаем обычный браузер, чтобы пользователь сам вставил ZIP.
            StartFirstAvailableBrowser();
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

    private static void StartFirstAvailableBrowser()
    {
        string[] candidates = { "msedge.exe", "chrome.exe", "firefox.exe" };

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

        // Последний безопасный fallback: открыть проводник, чтобы хотя бы не падать.
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            UseShellExecute = true
        });
    }
}
