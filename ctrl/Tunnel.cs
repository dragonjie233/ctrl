using Tunnel;

namespace ctrl
{
    internal class Tunnel
    {
        private static readonly string confDir = Path.Combine(Environment.CurrentDirectory, "config");
        private static readonly string confFile = Directory.GetFiles(confDir, "*.conf")[0];
        private static readonly string logFile = Path.Combine(confDir, "log.bin");

        private bool connected;
        private Ringlogger? logger;
        public static volatile bool logOutThreadRun;

        public async void Run()
        {
            if (connected) return;

            logger = new Ringlogger(logFile, "TunnelClass");

            try
            {
                await Task.Run(() => Service.Add(confFile));
                connected = true;
            }
            catch (Exception ex)
            {
                logger.Write(ex.Message);
            }
        }

        public async void Stop()
        {
            if (!connected) return;
            await Task.Run(() => Service.Remove());
            connected = false;
        }

        public void BeginLogging(StreamReturn res)
        {
            if (logOutThreadRun)
            {
                res.Write("日志线程已被启动，请使用 tunnel/log/end 关闭线程后再重新查看日志输出");
                res.EndSend();
                return;
            }

            logOutThreadRun = true;

            new Thread(() =>
            {
                logger = new Ringlogger(logFile, "TunnelCtrl");
                var cursor = Ringlogger.CursorAll;

                while (logOutThreadRun)
                {
                    var lines = logger.FollowFromCursor(ref cursor);

                    foreach (var line in lines)
                    {
                        string _line = line;
                        string newLine = "\r\n";
                        if (line.Contains("Starting WireGuard"))
                            _line = newLine + line;
                        if (line.Contains("Shutting down"))
                            newLine += newLine;
                        res.Write(_line + newLine);
                    }
                }

                try
                {
                    res.Write("日志输出线程关闭，可能是被别人关闭了");
                    res.EndSend();
                } catch { }
            }).Start();
        }

        public static bool DeleteLogFile(out string? error)
        {
            try
            {
                File.Delete(logFile);
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
