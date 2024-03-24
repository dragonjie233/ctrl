using System.Collections.Specialized;
using System.Drawing.Imaging;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;

namespace ctrl
{
    public class HTTPServer
    {
        private HttpListener? listener;

        public void RunServer()
        {
            string url = Environment.GetEnvironmentVariable("CTRLHTTP") ?? "http://*:3000/";
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            listener.BeginGetContext(ListenerCallback, listener);
        }

        public static void StopServer()
        {
            Application.Exit();
        }

        private void ListenerCallback(IAsyncResult result)  
        {
            if (listener is null || !listener.IsListening) return;
            listener.BeginGetContext(ListenerCallback, listener);

            HttpListenerContext ctx = listener.EndGetContext(result);
            HttpListenerRequest req = ctx.Request;
            HttpListenerResponse res = ctx.Response;

            NameValueCollection arg = [];
            string path = req.RawUrl ?? "/";
            string _temp = "";

            if (path.Contains('?'))
            {
                string[] pathArr = path.Split('?');

                foreach (string str in pathArr[1].Split('&'))
                {
                    string[] nv = str.Split('=');
                    arg.Add(HttpUtility.UrlDecode(nv[0]),
                            HttpUtility.UrlDecode(nv[^1]));
                }

                path = pathArr[0];
            }

            foreach (string str in path[1..].Split('/'))
            {
                if (str == "")
                {
                    _temp = "Index";
                    break;
                }

                _temp += string.Concat(str[..1].ToUpper(), str.AsSpan(1));
            }

            path = _temp;

            Response _res = new(res);
            HTTPRoutes handlers = new(_res, arg);
            MethodInfo? method = typeof(HTTPRoutes).GetMethod(path);

            if (method is not null)
            {
                method.Invoke(handlers, null);
            }
            else
            {
                _res.NotFound();
            }
        }
    }

    public class ContentType
    {
        public static string Html => "text/html";
        public static string Text => "text/plain";
        public static string Xml => "text/xml";
        public static string Jpeg => "image/jpeg";
        public static string Png => "image/png";
        public static string Json => "application/json";
    }

    public class Response(HttpListenerResponse response)
    {
        public void Send(string contentType, string value, HttpStatusCode code = HttpStatusCode.OK)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(value);
            response.StatusCode = (int)code;
            response.ContentType = contentType + ";charset=UTF-8";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = buffer.Length;
            response.AddHeader("Server", "");
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        public StreamReturn BeginSend(string contentType)
        {
            response.ContentType = contentType + ";charset=UTF-8";

            return new StreamReturn(response.OutputStream);
        }

        public void Text(string text)
        {
            Send(ContentType.Text, text);
        }

        public void Img(Bitmap image)
        {
            MemoryStream ms = new();
            image.Save(ms, ImageFormat.Jpeg);
            byte[] imageData = ms.ToArray();

            response.ContentType = ContentType.Jpeg;
            response.OutputStream.Write(imageData, 0, imageData.Length);
            response.OutputStream.Close();
        }

        public void NotFound()
        {
            Send(ContentType.Text, "404 啥也没有哦", HttpStatusCode.NotFound);
        }

        internal void Text(object value)
        {
            throw new NotImplementedException();
        }
    }

    public class StreamReturn(Stream ros)
    {
        private readonly StreamWriter w = new(ros, Encoding.UTF8);

        public void Write(string value)
        {
            w.WriteAsync(value);
            w.FlushAsync();
        }

        public void EndSend()
        {
            ros.Close();
        }
    }
}
