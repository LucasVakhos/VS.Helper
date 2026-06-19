// Helpers\VSHelperLogDialog.cs
// Commands\VSHelperLogDialog.cs
using System.Windows.Forms;

namespace VS.Helper.Commands;

internal sealed class VSHelperLogDialog : Form
{
    private VSHelperLogDialog(string log)
    {
        Text = "VS.Helper / VSHelper Log";
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
        using VSHelperLogDialog dialog = new VSHelperLogDialog(log);
        dialog.ShowDialog();
    }
}
