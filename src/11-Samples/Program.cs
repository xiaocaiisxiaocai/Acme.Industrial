using System;
using System.Windows.Forms;
using Acme.Industrial.Samples.Forms;

namespace Acme.Industrial.Samples;

/// <summary>
/// 主程序入口点。
/// </summary>
static class Program
{
    /// <summary>
    /// 应用程序的主入口点。
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
