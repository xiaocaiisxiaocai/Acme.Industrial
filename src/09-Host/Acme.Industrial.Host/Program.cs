using System;
using System.Windows.Forms;
using Acme.Industrial.Core.Bootstrap;

namespace Acme.Industrial.Host;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

public class MainForm : Form
{
    public MainForm()
    {
        Text = "Acme Industrial SCADA";
        Width = 1280;
        Height = 800;
    }
}
