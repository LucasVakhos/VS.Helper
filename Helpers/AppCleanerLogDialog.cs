// Helpers\AppCleanerLogDialog.cs
// Commands\AppCleanerLogDialog.cs
using System.Windows.Forms;

namespace VS.Helper.Commands;

internal sealed class AppCleanerLogDialog : Form
{
    private AppCleanerLogDialog(string log)
    {
        Text = "VS.Helper / AppCleaner Log";
        Width = 980;
        Height = 700;
        StartPosition = FormStartPosition.CenterScreen;

        TextBox textBox = new TextBox();
        textBox.Multiline = true;
        textBox.ReadOnly = true;
        textBox.ScrollBars = ScrollBars.Both;
        textBox.WordWrap = false;
        textBox.Dock = DockStyle.Fill;
        textBox.Font = new System.Drawing.Font("Consolas", 10);
        textBox.Text = log;

        Controls.Add(textBox);
    }

    public static void ShowLog(string log)
    {
        using AppCleanerLogDialog dialog = new AppCleanerLogDialog(log);
        dialog.ShowDialog();
    }
}
