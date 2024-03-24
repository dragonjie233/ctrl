# CTRL 基于 HTTP 的远程控制器
那天，无所事事的我随意搜索了“c# 被控端”想着就看看，第一条名为 EasyControl 的 GitHub 项目映入眼帘，在好奇心的驱使下便有了这个玩具项目。
本项目使用 C# .NET 8.0 框架，同时参考 [simpleHTTPServer](https://github.com/dragonjie233/simpleHTTPServer) 和 [EasyControl](https://github.com/Mangofang/EasyControl) 项目进行开发，内置 [WireGuard](https://git.zx2c4.com/wireguard-windows/about/embeddable-dll-service/README.md) 实现外网访问内网服务达到对目标进行远程控制的目的。
**程序存在一定的安全性问题和 BUG 不建议在正式环境使用，仅供学习和娱乐。**

## 文档
### 运行项目

1. 下载 releass 里面已编译好的程序；
2. 在程序的同级目录下新建 config 文件夹；
3. 把 WireGuard 客户端配置文件放进 config 文件夹；
	- 配置文件名随意，多个配置文件则按系统文件排序读取第一个。
4. 双击运行程序。

程序启动后不会有任何界面窗口，因为项目使用了无窗体方式运行。

若要停止程序请在任务管理器中停止程序的运行或使用下面的路由来停止程序。

### 环境变量
`CTRLHTTP` （可选）设置服务的监听地址，默认为 `http://*:3000/`

### 路由说明
`/` 主页
`/stop/server` 停止控制器程序
`/base/exec` 在被控端执行 cmd 或 ps 命令
`/base/run` 在被控端运行软件
`/tunnel/log` 开启日志输出线程，查看 WireGuard 运行日志
`/tunnel/log/end` 关闭日志输出线程
`/tunnel/log/clear` 清除所有 WireGuard 运行日志
`/screenshot` 截取被控端全屏

## 功能预览

![DemoPreview_OwoserImg](https://github.com/dragonjie233/ctrl/blob/master/preview.gif)
