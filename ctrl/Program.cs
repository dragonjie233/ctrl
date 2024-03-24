using System.Diagnostics;
using Tunnel;

namespace ctrl
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 3 && args[0] == "/service")
            {
                AsService(args);
                return;
            }

            if (!Directory.Exists(@".\config"))
            {
                MessageBox.Show("目录 config 不存在", "CTRL Error");
                return;
            }

            if (Directory.GetFiles(@".\config", "*.conf").Length == 0)
            {
                MessageBox.Show("缺失 WireGuard 配置文件", "CTRL Error");
                return;
            }

            AsApp();
        }

        static void AsApp()
        {
            ApplicationConfiguration.Initialize();
            new HTTPServer().RunServer();
            Tunnel tunnel = new();
            tunnel.Run();
            Application.ApplicationExit += new EventHandler((object? s, EventArgs e) =>
            {
                Tunnel.logOutThreadRun = false;
                tunnel.Stop();
            });
            Application.Run();
        }

        static void AsService(string[] args)
        {
            var checkMainRunning = new Thread(() =>
            {
                try
                {
                    var tunProc = Process.GetCurrentProcess();
                    var ctrlProc = Process.GetProcessById(int.Parse(args[2]));
                    if (ctrlProc.MainModule.FileName != tunProc.MainModule.FileName)
                        return;
                    ctrlProc.WaitForExit();
                    Service.Remove(false);
                }
                catch { }
            });
            checkMainRunning.Start();
            Service.Run(args[1]);
            checkMainRunning.Interrupt();
        }
    }
}