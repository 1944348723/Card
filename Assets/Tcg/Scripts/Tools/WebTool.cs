using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
using System;
using UnityEngine;
using System.Net;
using System.IO;
using System.Collections;
using System.Threading.Tasks;

namespace TcgEngine
{
    /// <summary>
    /// HTTP请求工具类，用于创建各种类型的Web请求
    /// </summary>
    public class WebRequest
    {
        public const string METHOD_GET = "GET";       // GET请求方法
        public const string METHOD_POST = "POST";     // POST请求方法
        public const string METHOD_PATCH = "PATCH";   // PATCH请求方法
        public const string METHOD_DELETE = "DELETE"; // DELETE请求方法
        public const int timeout = 10;                // 请求超时时间（秒）

        /// <summary>
        /// 创建一个默认GET请求，返回UnityWebRequest对象
        /// </summary>
        public static UnityWebRequest Create(string url)
        {
            UnityWebRequest request = new UnityWebRequest(url, METHOD_GET); // 使用GET方法
            request.SetRequestHeader("Content-Type", "application/json");  // 设置请求内容类型
            request.downloadHandler = new DownloadHandlerBuffer();         // 设置响应缓冲
            request.timeout = timeout;                                      // 设置超时
            return request;
        }

        /// <summary>
        /// 创建请求，可自定义方法、JSON数据和Token
        /// </summary>
        public static UnityWebRequest Create(string url, string method, string json_data, string token)
        {
            UnityWebRequest request = new UnityWebRequest(url, method);
            request.SetRequestHeader("Content-Type", "application/json");  // 设置请求头
            if(token != null)
                request.SetRequestHeader("Authorization", token);          // 添加授权Token
            request.downloadHandler = new DownloadHandlerBuffer();         // 设置响应缓冲
            request.timeout = timeout;

            // 如果不是GET请求且有数据，则设置上传内容
            if (method != METHOD_GET && !string.IsNullOrEmpty(json_data))
            {
                UploadHandler uploader = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json_data)); // 上传JSON字节流
                uploader.contentType = "application/json";                                       // 设置上传类型
                request.uploadHandler = uploader;
            }

            return request;
        }

        /// <summary>
        /// 创建原始字节流请求，可自定义方法、内容类型、字节数据和Token
        /// </summary>
        public static UnityWebRequest CreateRaw(string url, string method, string contentType, byte[] data, string token)
        {
            UnityWebRequest request = new UnityWebRequest(url, method);
            request.SetRequestHeader("Content-Type", contentType);  // 自定义内容类型
            if (token != null)
                request.SetRequestHeader("Authorization", token);   // 添加Token
            request.downloadHandler = new DownloadHandlerBuffer(); // 响应缓冲
            request.timeout = timeout;

            // 非GET请求时，如果指定了Content-Type，则上传字节数据
            if (method != METHOD_GET && !string.IsNullOrEmpty(contentType))
            {
                UploadHandler uploader = new UploadHandlerRaw(data);
                uploader.contentType = contentType;
                request.uploadHandler = uploader;
            }

            return request;
        }

        /// <summary>
        /// 创建HEAD请求（只获取HTTP头信息）
        /// </summary>
        public static UnityWebRequest CreateHeader(string url)
        {
            UnityWebRequest request = UnityWebRequest.Head(url);
            return request;
        }

        /// <summary>
        /// 创建下载图片的请求
        /// </summary>
        public static UnityWebRequest CreateTexture(string url)
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            request.SetRequestHeader("Content-Type", "image/png"); // 设置Content-Type为PNG图片
            return request;
        }

        /// <summary>
        /// 创建表单上传图片请求
        /// </summary>
        public static UnityWebRequest CreateImageUploadForm(string url, string path, byte[] data, string token)
        {
            List<IMultipartFormSection> requestData = new List<IMultipartFormSection>();
            requestData.Add(new MultipartFormDataSection("path", path, "text"));                    // 上传路径参数
            requestData.Add(new MultipartFormFileSection("data", data, "file.png", "image/png"));   // 上传文件

            UnityWebRequest request = UnityWebRequest.Post(url, requestData);
            if (token != null)
                request.SetRequestHeader("Authorization", token); // 添加Token
            request.timeout = 200;                                  // 上传大文件时设置较长超时

            return request;
        }

        /// <summary>
        /// 获取UnityWebRequest的响应，返回WebResponse对象
        /// </summary>
        public static WebResponse GetResponse(UnityWebRequest request)
        {
            WebResponse res = new WebResponse();
            res.success = request.responseCode >= 200 && request.responseCode < 300; // 响应码200-299为成功
            res.status = request.responseCode;
            res.error = request.error;
            res.data = "";
            if (request.downloadHandler != null)
                res.data = request.downloadHandler.text; // 获取响应数据
            return res;
        }

        /// <summary>
        /// 获取HEAD请求响应信息
        /// </summary>
        public static HeadResponse GetHeadResponse(UnityWebRequest request)
        {
            HeadResponse res = new HeadResponse();
            res.success = request.responseCode >= 200 && request.responseCode < 300;
            res.status = request.responseCode;

            string type = request.GetResponseHeader("Content-Type");                  // 获取内容类型
            DateTime.TryParse(request.GetResponseHeader("Last-Modified"), out DateTime date); // 获取最后修改时间
            int.TryParse(request.GetResponseHeader("Content-Length"), out int size);        // 获取内容长度

            res.content_type = type;
            res.last_edit = date;
            res.size = size;

            return res;
        }
    }

    /// <summary>
    /// Web工具类，提供JSON解析、对象转换及发送请求功能
    /// </summary>
    public class WebTool
    {
        /// <summary>
        /// 将JSON字符串转换为对象
        /// </summary>
        public static T JsonToObject<T>(string json)
        {
            T value = (T)Activator.CreateInstance(typeof(T));
            try
            {
                value = JsonUtility.FromJson<T>(json); // 使用Unity自带JSON解析
            }
            catch (Exception) { }
            return value;
        }

        /// <summary>
        /// 将JSON数组字符串转换为对象数组
        /// </summary>
        public static T[] JsonToArray<T>(string json)
        {
            ListJson<T> list = new ListJson<T>();
            list.list = new T[0];
            try
            {
                string wrap_json = "{ \"list\": " + json + "}";   // Unity JsonUtility不支持直接解析数组，需要包装
                list = JsonUtility.FromJson<ListJson<T>>(wrap_json);
                return list.list;
            }
            catch (Exception) { }
            return new T[0];
        }

        /// <summary>
        /// 将对象序列化为JSON字符串
        /// </summary>
        public static string ToJson(object data)
        {
            return JsonUtility.ToJson(data);
        }

        /// <summary>
        /// 解析字符串为整数，如果解析失败返回默认值
        /// </summary>
        public static int Parse(string int_str, int default_val = 0)
        {
            bool success = int.TryParse(int_str, out int val);
            return success ? val : default_val;
        }

        /// <summary>
        /// 发送GET请求，并返回WebResponse
        /// </summary>
        public static async Task<WebResponse> SendRequest(string url)
        {
            UnityWebRequest req = WebRequest.Create(url);
            return await SendRequest(req);
        }

        /// <summary>
        /// 发送UnityWebRequest请求，并返回WebResponse
        /// </summary>
        public static async Task<WebResponse> SendRequest(UnityWebRequest request)
        {
            try
            {
                var asyncOp = request.SendWebRequest(); // 异步发送请求
                while (!asyncOp.isDone)
                    await TimeTool.Delay(200);         // 每200ms轮询一次完成状态
            }
            catch (Exception) { }

            if (request.result != UnityWebRequest.Result.Success)
                Debug.Log(request.error);              // 请求失败输出错误

            WebResponse res = WebRequest.GetResponse(request); // 获取响应
            request.Dispose();                                 // 释放请求资源

            return res;
        }
    }
    
    public class WebContext
    {
        public HttpListenerContext http;
        public string method;
        public string token;
        public string path;
        public string data;

        // 发送泛型响应
        public void SendResponse<T>(T value)
        {
            string val = WebTool.ToJson(value);
            SendResponse(val);
        }

        // 发送 ulong 类型响应
        public void SendResponse(ulong value)
        {
            SendResponse(value.ToString());
        }

        // 发送 int 类型响应
        public void SendResponse(int value)
        {
            SendResponse(value.ToString());
        }

        // 发送 bool 类型响应
        public void SendResponse(bool value)
        {
            SendResponse(value.ToString());
        }

        // 发送字符串响应
        public void SendResponse(string value)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
            SendResponse(bytes, 200);
        }

        // 发送错误响应
        public void SendError(string value)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
            SendResponse(bytes, 400);
        }

        // 发送空响应
        public void SendResponse()
        {
            try
            {
                WriteHeader();
                http.Response.StatusCode = 200;
                http.Response.Close();
            }
            catch (Exception e) { Debug.Log(e); }
        }
        
        // 发送字节数组响应，带状态码
        public void SendResponse(byte[] bytes, int code)
        {
            try
            {
                WriteHeader();
                http.Response.StatusCode = code;
                http.Response.OutputStream.Write(bytes, 0, bytes.Length);
                http.Response.Close();
            }
            catch (Exception e) { Debug.Log(e); }
        }

        // 写入 HTTP 响应头
        private void WriteHeader()
        {
            http.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            http.Response.Headers.Add("Access-Control-Allow-Methods", "GET,HEAD,OPTIONS,POST,PUT");
            http.Response.Headers.Add("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept, Authorization");
        }

        // 获取请求数据并解析为指定类型
        public T GetData<T>()
        {
            return WebTool.JsonToObject<T>(data);
        }

        // 获取请求数据的 ulong 类型值
        public ulong GetInt64()
        {
            bool valid = ulong.TryParse(data, out ulong val);
            return valid ? val : 0;
        }

        // 获取请求数据的 int 类型值
        public int GetInt()
        {
            bool valid = int.TryParse(data, out int val);
            return valid ? val : 0;
        }

        // 获取请求数据的 bool 类型值
        public bool GetBool()
        {
            bool valid = bool.TryParse(data, out bool val);
            return valid ? val : false;
        }

        // 获取客户端 ID（从 token 中解析 ulong）
        public ulong GetClientID()
        {
            bool valid = ulong.TryParse(token, out ulong val);
            return valid ? val : 0;
        }

        // 获取客户端 IP
        public string GetIP()
        {
            return http.Request.RemoteEndPoint.Address.ToString();
        }

        // 获取请求 token
        public string GetKey()
        {
            return token;
        }

        // 检查 token 是否有效
        public bool IsKeyValid(string key)
        {
            return token == key;
        }

        // 获取 URL 查询参数
        public string GetQuery(string key)
        {
            try
            {
                return http.Request.QueryString.Get(key);
            }
            catch (Exception e) { Debug.Log(e); }

            return "";
        }

        // 关闭响应流
        public void Close()
        {
            try
            {
                http.Response.Close();
            }
            catch (Exception e) { Debug.Log(e); }
        }

        // 根据 HttpListenerContext 创建 WebContext 实例
        public static WebContext Create(HttpListenerContext http)
        {
            WebContext req = new WebContext();
            req.http = http;
            req.path = "";
            req.data = "";

            try
            {
                req.method = http.Request.HttpMethod;
                req.path = http.Request.RawUrl.Remove(0, 1);
                req.token = http.Request.Headers.Get("Authorization");

                if (http.Request.InputStream != null)
                {
                    StreamReader reader = new StreamReader(http.Request.InputStream, http.Request.ContentEncoding);
                    req.data = reader.ReadToEnd();
                }
            }
            catch (Exception e) { Debug.Log(e);  }

            return req;
        }
    }

    public struct WebResponse
    {
        public bool success;
        public long status;
        public string data;
        public string error;

        // 将响应数据解析为 ulong
        public ulong GetInt64()
        {
            bool valid = ulong.TryParse(data, out ulong val);
            return valid ? val : 0;
        }

        // 将响应数据解析为 int
        public int GetInt()
        {
            bool valid = int.TryParse(data, out int val);
            return valid ? val : 0;
        }

        // 将响应数据解析为 bool
        public bool GetBool()
        {
            bool valid = bool.TryParse(data, out bool val);
            return valid ? val : false;
        }

        // 将响应数据解析为指定类型
        public T GetData<T>()
        {
            return WebTool.JsonToObject<T>(data);
        }
		
        // 获取响应的错误信息
		public string GetError()
        {
            ErrorResponse err = WebTool.JsonToObject<ErrorResponse>(data);
            if(err != null)
                return err.error;
            return error;
        }
    }

    // HTTP 头部响应信息
    public class HeadResponse
    {
        // 请求是否成功
        public bool success;
        // HTTP 状态码
        public long status;
        // 资源最后修改时间
        public DateTime last_edit;
        // 资源大小
        public int size;
        // 资源类型（Content-Type）
        public string content_type;
    }

    // 错误响应对象
    [Serializable]
    public class ErrorResponse
    {
        // 错误信息
        public string error;
    }

    // 用于 JSON 数组解析的包装类
    [Serializable]
    public class ListJson<T>
    {
        // 数据列表
        public T[] list;
        // 错误信息
        public string error;
    }

}