using System;

namespace Connector.WPF;

public class Program
{
    [STAThread]
    public static void Main()
    {
        var application = new App();
        application.Run();
    }
} 