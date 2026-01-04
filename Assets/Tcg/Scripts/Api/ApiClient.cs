using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine.Events;

namespace TcgEngine
{
    /// <summary>
    /// API 客户端，用于与 NodeJS Web API 通信
    /// 可发送请求并接收响应
    /// </summary>
    public class ApiClient : MonoBehaviour
    {
        public bool is_server; // 标记是否为服务器端

        // 事件回调
        public UnityAction<RegisterResponse> onRegister; // 注册完成后触发，即使失败也触发
        public UnityAction<LoginResponse> onLogin;       // 登录完成后触发，即使失败也触发
        public UnityAction<LoginResponse> onRefresh;     // 登录刷新完成后触发，即使失败也触发
        public UnityAction onLogout;                     // 注销触发

        // 用户信息
        private string user_id = "";        // 用户ID
        private string username = "";       // 用户名
        private string access_token = "";   // AccessToken，用于API认证
        private string refresh_token = "";  // RefreshToken，用于刷新AccessToken
        private string api_version = "";    // 服务器API版本

        // 登录状态
        private bool logged_in = false;     // 是否已登录
        private bool expired = false;       // Token是否过期

        private UserData udata = null;      // 用户数据缓存

        private int sending = 0;            // 当前正在发送的请求数量
        private string last_error = "";     // 最近一次请求错误信息
        private float refresh_timer = 0f;   // 刷新计时器
        private float online_timer = 0f;    // 在线保持计时器
        private long expiration_timestamp = 0; // AccessToken过期时间戳

        private const float online_duration = 60f * 5f; // 在线保持间隔：5分钟

        private static ApiClient instance; // 单例实例

        void Awake()
        {
            // API 客户端挂在 OnDestroyOnLoad 上，保证场景切换不销毁
            // 如果已有实例，不再覆盖
            if(instance == null)
                instance = this;

            LoadTokens(); // 加载本地保存的Token
        }

        private void Update()
        {
            // 每帧刷新Token或在线状态
            Refresh();
        }

        /// <summary>
        /// 从本地存储加载Token
        /// </summary>
        private void LoadTokens()
        {
            if (!is_server && string.IsNullOrEmpty(user_id))
            {
                access_token = PlayerPrefs.GetString("tcg_access_token");
                refresh_token = PlayerPrefs.GetString("tcg_refresh_token");
            }
        }

        /// <summary>
        /// 保存Token到本地
        /// </summary>
        private void SaveTokens()
        {
            if (!is_server)
            {
                PlayerPrefs.SetString("tcg_access_token", access_token);
                PlayerPrefs.SetString("tcg_refresh_token", refresh_token);
            }
        }

        /// <summary>
        /// 刷新Token或保持在线状态
        /// </summary>
        private async void Refresh()
        {
            if (!logged_in)
                return;

            // 检查Token是否过期
            if (!expired)
            {
                long current = GetTimestamp();
                expired = current > (expiration_timestamp - 10);
            }

            // 如果过期，每5秒尝试刷新
            refresh_timer += Time.deltaTime;
            if (expired && refresh_timer > 5f)
            {
                refresh_timer = 0f;
                await RefreshLogin(); // 尝试重新登录
            }

            // 在线保持
            online_timer += Time.deltaTime;
            if (!expired && online_timer > online_duration)
            {
                online_timer = 0f;
                await KeepOnline();
            }
        }

        // ==============================
        // 注册 / 登录 / 刷新
        // ==============================

        public async Task<RegisterResponse> Register(string email, string user, string password)
        {
            RegisterRequest data = new RegisterRequest();
            data.email = email;
            data.username = user;
            data.password = password;
            data.avatar = "";
            return await Register(data);
        }

        public async Task<RegisterResponse> Register(RegisterRequest data)
        {
            Logout(); // 先注销当前登录

            string url = ServerURL + "/users/register";
            string json = ApiTool.ToJson(data);

            WebResponse res = await SendPostRequest(url, json);
            RegisterResponse regist_res = ApiTool.JsonToObject<RegisterResponse>(res.data);
            regist_res.success = res.success;
            regist_res.error = res.error;
            onRegister?.Invoke(regist_res); // 触发注册回调
            return regist_res;
        }

        public async Task<LoginResponse> Login(string user, string password)
        {
            Logout(); // 先注销当前登录

            LoginRequest data = new LoginRequest();
            data.password = password;

            if (user.Contains("@"))
                data.email = user;
            else
                data.username = user;

            string url = ServerURL + "/auth";
            string json = ApiTool.ToJson(data);

            WebResponse res = await SendPostRequest(url, json);
            LoginResponse login_res = GetLoginRes(res);
            AfterLogin(login_res);

            onLogin?.Invoke(login_res); // 触发登录回调
            return login_res;
        }

        public async Task<LoginResponse> RefreshLogin()
        {
            string url = ServerURL + "/auth/refresh";
            AutoLoginRequest data = new AutoLoginRequest();
            data.refresh_token = refresh_token;
            string json = ApiTool.ToJson(data);

            WebResponse res = await SendPostRequest(url, json);
            LoginResponse login_res = GetLoginRes(res);
            AfterLogin(login_res);

            onRefresh?.Invoke(login_res); // 触发刷新回调
            return login_res;
        }

        /// <summary>
        /// 解析登录请求返回值
        /// </summary>
        private LoginResponse GetLoginRes(WebResponse res)
        {
            LoginResponse login_res = ApiTool.JsonToObject<LoginResponse>(res.data);
            login_res.success = res.success;
            login_res.error = res.error;
            return login_res;
        }

        /// <summary>
        /// 登录成功后的处理
        /// </summary>
        private void AfterLogin(LoginResponse login_res)
        {
            last_error = login_res.error;

            if (login_res.success)
            {
                user_id = login_res.id;
                username = login_res.username;
                access_token = login_res.access_token;
                refresh_token = login_res.refresh_token;
                api_version = login_res.version;
                expiration_timestamp = GetTimestamp() + login_res.duration;
                refresh_timer = 0f;
                online_timer = 0f;
                logged_in = true;
                expired = false;
                SaveTokens(); // 保存Token到本地
            }
        }

        // ==============================
        // 用户数据
        // ==============================
        public async Task<UserData> LoadUserData()
        {
            udata = await LoadUserData(this.username);
            return udata;
        }

        public async Task<UserData> LoadUserData(string username)
        {
            if (!IsConnected())
                return null;

            string url = ServerURL + "/users/" + username;
            WebResponse res = await SendGetRequest(url);

            UserData udata = null;
            if (res.success)
            {
                udata = ApiTool.JsonToObject<UserData>(res.data);
            }

            return udata;
        }

        // ==============================
        // 在线保持 / 验证
        // ==============================
        public async Task<bool> KeepOnline()
        {
            if (!IsConnected())
                return false;

            string url = ServerURL + "/auth/keep";
            WebResponse res = await SendGetRequest(url);
            expired = !res.success;
            return res.success;
        }

        public async Task<bool> Validate()
        {
            if (!IsConnected())
                return false;

            string url = ServerURL + "/auth/validate";
            WebResponse res = await SendGetRequest(url);
            expired = !res.success;
            return res.success;
        }

        // ==============================
        // 注销
        // ==============================
        public void Logout()
        {
            user_id = "";
            username = "";
            access_token = "";
            refresh_token = "";
            api_version = "";
            last_error = "";
            logged_in = false;
            onLogout?.Invoke(); // 触发注销回调
            SaveTokens();
        }

        // ==============================
        // 创建/结束比赛
        // ==============================
        public async void CreateMatch(Game game_data)
        {
            if (game_data.settings.game_type != GameType.Multiplayer)
                return;

            AddMatchRequest req = new AddMatchRequest();
            req.players = new string[2];
            req.players[0] = game_data.players[0].username;
            req.players[1] = game_data.players[1].username;
            req.tid = game_data.game_uid;
            req.ranked = game_data.settings.IsRanked();
            req.mode = game_data.settings.GetGameModeId();

            string url = ServerURL + "/matches/add";
            string json = ApiTool.ToJson(req);
            WebResponse res = await SendPostRequest(url, json);
            Debug.Log("Match Started! " + res.success);
        }

        public async void EndMatch(Game game_data, int winner_id)
        {
            if (game_data.settings.game_type != GameType.Multiplayer)
                return;

            Player player = game_data.GetPlayer(winner_id);
            CompleteMatchRequest req = new CompleteMatchRequest();
            req.tid = game_data.game_uid;
            req.winner = player != null ? player.username : "";

            string url = ServerURL + "/matches/complete";
            string json = ApiTool.ToJson(req);
            WebResponse res = await SendPostRequest(url, json);
            Debug.Log("Match Completed! " + res.success);
        }

        // ==============================
        // 网络请求
        // ==============================
        public async Task<string> SendGetVersion()
        {
            string url = ServerURL + "/version";
            WebResponse res = await SendGetRequest(url);

            if (res.success)
            {
                VersionResponse version_data = ApiTool.JsonToObject<VersionResponse>(res.data);
                api_version = version_data.version;
                return api_version;
            }

            return null;
        }

        public async Task<WebResponse> SendGetRequest(string url)
        {
            return await SendRequest(url, WebRequest.METHOD_GET);
        }

        public async Task<WebResponse> SendPostRequest(string url, string json_data)
        {
            return await SendRequest(url, WebRequest.METHOD_POST, json_data);
        }

        public async Task<WebResponse> SendRequest(string url, string method, string json_data = "")
        {
            UnityWebRequest request = WebRequest.Create(url, method, json_data, access_token);
            return await SendRequest(request);
        }

        private async Task<WebResponse> SendRequest(UnityWebRequest request)
        {
            int wait = 0;
            int wait_max = request.timeout * 1000;
            request.timeout += 1; //增加偏移，保证超时先中断
            sending++;

            var async_oper = request.SendWebRequest();
            while (!async_oper.isDone)
            {
                await TimeTool.Delay(200);
                wait += 200;
                if (wait >= wait_max)
                    request.Abort(); //超时中断
            }

            WebResponse response = WebRequest.GetResponse(request);
            response.error = GetError(response);
            last_error = response.error;
            request.Dispose();
            sending--;

            return response;
        }

        private string GetError(WebResponse res)
        {
            if (res.success)
                return "";

            ErrorResponse err = ApiTool.JsonToObject<ErrorResponse>(res.data);
            if (err != null)
                return err.error;
            else
                return res.error;
        }

        // ==============================
        // 状态检查
        // ==============================
        public bool IsConnected() { return logged_in && !expired; }
        public bool IsLoggedIn() { return logged_in; }
        public bool IsExpired() { return expired; }
        public bool IsBusy() { return sending > 0; }

        public long GetTimestamp() { return System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(); }
        public string GetLastRequest() { return last_error; }
        public string GetLastError() { return last_error; }
        public bool IsVersionValid() { return ClientVersion == ServerVersion; }

        // ==============================
        // 属性访问
        // ==============================
        public UserData UserData { get { return udata; } }

        public string UserID { get { return user_id; } set { user_id = value; } }
        public string Username { get { return username; } set { username = value; } }
        public string AccessToken { get { return access_token; } set { access_token = value; } }
        public string RefreshToken { get { return refresh_token; } set { refresh_token = value; } }

        public string ServerVersion { get { return api_version; } }
        public string ClientVersion { get { return Application.version; } }

        public static string ServerURL
        {
            get
            {
                NetworkData data = NetworkData.Get();
                string protocol = data.api_https ? "https://" : "http://";
                return protocol + data.api_url;
            }
        }

        public static ApiClient Get()
        {
            if (instance == null)
                instance = FindObjectOfType<ApiClient>();
            return instance;
        }
    }
}
