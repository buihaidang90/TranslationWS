using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace TranslationWS
{
    public class TranslationServices
    {
        #region For Debug
        private enum LogMode { Non, All, Fail, Success }
        private int _DevMode = (int)LogMode.Non;
        private string _LogFilePath = "_LogFile.txt";
        #endregion

        #region AuthenticationConfiguration
        private object _Token = "";
        private string _User = "";
        public void SetToken(object Token)
        {
            if (Token != null) this._Token = Token;
        }
        public void SetUser(string User)
        {
            if (User != null) this._User = User;
        }
        #endregion

        #region Constructor
        private void constructClass(object Token, string User, int TimeoutSpace)
        {
            if (DefinitionsWB.MinTimeoutSpace <= TimeoutSpace && TimeoutSpace <= DefinitionsWB.MaxTimeoutSpace)
                this._Timeout = TimeoutSpace;
            if (Token != null) this._Token = Token;
            if (User != null) this._User = User;
            string _currentPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            //
            if (_currentPath.EndsWith("\\")) _LogFilePath = _currentPath + _LogFilePath;
            else _LogFilePath = string.Concat(_currentPath, "\\", _LogFilePath);
            //
            SetWebServiceUrl(); // step 1
            ReadConfigFromFile(); // step 2
        }
        /// <summary>
        /// Constructor
        /// </summary>
        public TranslationServices() { constructClass(null, null, DefinitionsWB.DefaultTimeoutSpace); }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="User">User is using this web service</param>
        public TranslationServices(string User) { constructClass(null, User, DefinitionsWB.DefaultTimeoutSpace); }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="User">User is using this web service</param>
        /// <param name="TimeoutSpace">Timeout space when must to wait a long time (millisecond)</param>
        public TranslationServices(string User, int TimeoutSpace) { constructClass(null, User, DefinitionsWB.DefaultTimeoutSpace); }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="Token"></param>
        /// <param name="User">User is using this web service</param>
        public TranslationServices(object Token, string User) { constructClass(Token, User, DefinitionsWB.DefaultTimeoutSpace); }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="Token"></param>
        /// <param name="User">User is using this web service</param>
        /// <param name="TimeoutSpace">Timeout space when must to wait a long time (millisecond)</param>
        public TranslationServices(object Token, string User, int TimeoutSpace) { constructClass(Token, User, DefinitionsWB.DefaultTimeoutSpace); }
        #endregion

        #region Read configuration
        private void ReadConfigFromFile()
        {
            string _pathFileConfig = @"Config_Dictionary.ini";
            string _TextBaseUrl = "BaseUrl=";
            string _TextDevMode = "DevMode=";
            string _TextLogFile = "LogFile=";
            try
            {
                if (_pathFileConfig == "" || !File.Exists(_pathFileConfig)) return;
                foreach (string line in File.ReadAllLines(_pathFileConfig))
                {
                    if (!line.StartsWith(_TextBaseUrl)) continue;
                    int iBegin = line.IndexOf(_TextBaseUrl) + _TextBaseUrl.Length;
                    string _strUrl = line.Substring(iBegin, line.Length - iBegin);
                    //Console.WriteLine(_strUrl);
                    if (_strUrl.Length == 0) return;
                    if (!Uri.IsWellFormedUriString(_strUrl, UriKind.Absolute)) return;
                    if (_strUrl.EndsWith(@"/")) _strUrl = _strUrl.Substring(0, _strUrl.Length - 1);
                    this._BaseUrl = _strUrl;
                    SetWebServiceUrl();
                    break;
                }
                foreach (string line in File.ReadAllLines(_pathFileConfig))
                {
                    if (!line.StartsWith(_TextDevMode)) continue;
                    int iBegin = line.IndexOf(_TextDevMode) + _TextDevMode.Length;
                    string _str = line.Substring(iBegin, line.Length - iBegin);
                    //Console.WriteLine(_str);
                    if (_str.Length == 0) return;
                    int _num = 0; int.TryParse(_str, out _num);
                    this._DevMode = _num;
                    break;
                }
                foreach (string line in File.ReadAllLines(_pathFileConfig))
                {
                    if (!line.StartsWith(_TextLogFile)) continue;
                    int iBegin = line.IndexOf(_TextLogFile) + _TextLogFile.Length;
                    string _str = line.Substring(iBegin, line.Length - iBegin);
                    //Console.WriteLine(_str);
                    if (_str.Length == 0) return;
                    _LogFilePath = _str;
                    break;
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
        #endregion

        #region TimeoutConfiguration
        private int _Timeout = DefinitionsWB.DefaultTimeoutSpace;
        private int GetValidTimeoutSpace(int? TimeoutSpace)
        {
            if (TimeoutSpace != null)
            {
                if (TimeoutSpace < DefinitionsWB.MinTimeoutSpace || DefinitionsWB.MaxTimeoutSpace < TimeoutSpace)
                {
                    throw new Exception("Time-out space value is not valid.");
                }
            }
            else TimeoutSpace = _Timeout;
            //
            int _timeOutSpace = (TimeoutSpace.Value == _Timeout ? _Timeout : TimeoutSpace.Value);
            return _timeOutSpace;
        }

        /// <summary>
        /// Timeout space when must to wait a long time (millisecond)
        /// </summary>
        public int Timeout
        {
            get { return _Timeout; }
            set
            {
                if (DefinitionsWB.MinTimeoutSpace <= value && value <= DefinitionsWB.MaxTimeoutSpace)
                    _Timeout = value;
                else
                    _Timeout = DefinitionsWB.DefaultTimeoutSpace;
            }
        }
        /// <summary>
        /// Configure timeout space
        /// </summary>
        /// <param name="TimeoutSpace">Timeout space when must to wait a long time (millisecond)</param>
        public void SetTimeoutSpace(int TimeoutSpace)
        {
            if (DefinitionsWB.MinTimeoutSpace <= TimeoutSpace && TimeoutSpace <= DefinitionsWB.MaxTimeoutSpace)
                this._Timeout = TimeoutSpace;
        }
        #endregion

        #region UrlConfiguration
        private string _BaseUrl = @"http://101.53.26.74:10803"; // localhost:8083
        private string _TestUrl = @"";
        private string _TranslateV2Url = @"";
        private string _SqlTranslateV2Url = @"";
        private string _CustomerUrl = @"";
        private void SetWebServiceUrl()
        {
            _TestUrl = _BaseUrl + @"/api/Test";
            _TranslateV2Url = _BaseUrl + @"/api/TranslateV2";
            _SqlTranslateV2Url = _BaseUrl + @"/api/SqlTranslateV2";
            _CustomerUrl = _BaseUrl + @"/api/Customer";
        }
        /// <summary>
        /// Configure base URL
        /// </summary>
        /// <param name="UrlString">Operation URL string</param>
        public void SetBaseUrl(string UrlString)
        {
            this._BaseUrl = UrlString;
            SetWebServiceUrl();
        }
        #endregion

        #region Structs
        private struct RequestWB
        {
            /* request structure like from web service */
            public string[] data { get; set; }
            public string source { get; set; }
            public string target { get; set; }
            public string key { get; set; } // authentication required if have
            public string user { get; set; }
        }
        private struct ResponseWB
        {
            public DataResponseWB[] data { get; set; }
            public int status { get; set; }
            public string message { get; set; }
        }
        private struct DataResponseWB
        {
            public string text { get; set; }
            public string source { get; set; }
            public int charge { get; set; }
        }
        public enum LanguageCodes
        {
            CNs, // zh-CN: Chinese simplified
            CNt, // zh-TW: Chinese traditional
            Detect,
            EN,
            VI,
            JA, JP,
            TH, // Thai
            MY, // Myanmar (Burmese)
            ID, // Indonesian
            MS, // Malay
            KO, // Korean
            FR, // French
            DE, // Germany
            IT, // Italian
            ES, // Spanish
            RU, // Russian
        }
        #endregion

        #region Methods
        private string ParseLanguageCode(LanguageCodes LangCode)
        {
            string _result = "";
            switch (LangCode)
            {
                case LanguageCodes.CNs:
                    _result = "zh-CN";
                    break;
                case LanguageCodes.CNt:
                    _result = "zh-TW";
                    break;
                case LanguageCodes.JP:
                    _result = "ja";
                    break;
                case LanguageCodes.Detect:
                    break;
                default:
                    _result = Enum.GetName(typeof(LanguageCodes), LangCode).ToLower();
                    break;
            }
            return _result;
        }

        /// <summary>
        /// The result of translation operation
        /// </summary>
        public struct TranslationResult
        {
            public string TranslatedText { get; set; }
            public string Source { get; set; }
            public int? Charge { get; set; }
        }

        /// <summary>
        /// Translate single or multiple items of text request
        /// </summary>
        /// <param name="RequestString">Request string need to translate</param>
        /// <param name="Source">Source ISO language code</param>
        /// <param name="Target">Target ISO language code</param>
        /// <param name="Token"></param>
        /// <param name="Status">State code be returned from web service</param>
        /// <param name="Message">Message described operation detail</param>
        /// <param name="TimeoutSpace">Time-out space for request</param>
        /// <returns></returns>
        public TranslationResult TranslateText(string RequestString, LanguageCodes Source, LanguageCodes Target, out int Status, out string Message, int? TimeoutSpace, object Token)
        {
            object tokenAuthen = (Token == null ? this._Token : Token);
            TranslationResult _result = new TranslationResult();
            _result.TranslatedText = "";
            _result.Source = "";
            _result.Charge = null;
            Status = StatusWB.Gone;
            Message = HttpStatusCode.Gone.ToString();
            if (Target == LanguageCodes.Detect)
            {
                Status = StatusWB.NotAcceptable;
                Message = HttpStatusCode.NotAcceptable.ToString();
                return _result;
            }
            try
            {
                int _timeOutSpace = GetValidTimeoutSpace(TimeoutSpace);
                using (ExtendedWebClient _client = new ExtendedWebClient(_TranslateV2Url, _timeOutSpace))
                {
                    /// Header part
                    //client.Headers[HttpRequestHeader.ContentType] = "text/xml";
                    //client.Headers.Add(HttpRequestHeader.ContentType, "text/xml");
                    _client.Headers[HttpRequestHeader.ContentType] = "application/json"; // push parameters follow json type
                    _client.Headers.Add(DefinitionsWB.UserAgentKeyword, DefinitionsWB.TranslateDllKeywork);
                    _client.Encoding = Encoding.UTF8; // encode string has sign

                    /// Body part
                    RequestWB _req = new RequestWB();
                    _req.data = new string[] { RequestString };
                    _req.source = ParseLanguageCode(Source);
                    _req.target = ParseLanguageCode(Target);
                    _req.key = tokenAuthen.ToString();
                    _req.user = _User;

                    /// Calling web service
                    //Console.WriteLine(TranslateV2Url);
                    string _data = JsonConvert.SerializeObject(_req);
                    string _jsonString = _client.UploadString(_TranslateV2Url, MethodsWB.Post, _data); // POST method
                    //Console.WriteLine(_jsonString);

                    /// Receive output
                    ResponseWB _res = JsonConvert.DeserializeObject<ResponseWB>(_jsonString);
                    _result.TranslatedText = _res.data[0].text;
                    _result.Source = _res.data[0].source;
                    _result.Charge = _res.data[0].charge;
                    Status = _res.status;
                    Message = _res.message;


                    // Write log
                    if (_DevMode == (int)LogMode.All || _DevMode == (int)LogMode.Success) WriteLog(FormatLine(Status.ToString(), Message, (int)LogMode.Success));
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
                //
                _result.TranslatedText = "";
                _result.Source = "";
                _result.Charge = null;
                if (ex.Message.Contains("timed out")) //The operation has timed out
                {
                    Status = StatusWB.RequestTimeout;
                    Message = HttpStatusCode.RequestTimeout.ToString();
                    // Write log
                    if (_DevMode == (int)LogMode.All || _DevMode == (int)LogMode.Fail) WriteLog(FormatLine(Status.ToString(), Message, (int)LogMode.Fail));
                }
                else if (ex.Message.Contains("address is not valid")) //Your uri address is not valid.
                {
                    Status = StatusWB.UrlInvalid; // HttpStatusCode.Ambiguous
                    Message = "UrlInvalid";
                    // Write log
                    if (_DevMode == (int)LogMode.All || _DevMode == (int)LogMode.Fail) WriteLog(FormatLine(Status.ToString(), Message, (int)LogMode.Fail));
                }
                else
                {
                    Status = StatusWB.ExceptionError;
                    Message = ex.Message;
                    // Write log
                    if (_DevMode == (int)LogMode.All || _DevMode == (int)LogMode.Fail) WriteLog(FormatLine(Status.ToString(), Message, (int)LogMode.Fail));
                }
            }
            return _result;
        }

        /// <summary>
        /// Translate single or multiple items of text request
        /// </summary>
        /// <param name="RequestString">Request string need to translate</param>
        /// <param name="Source">Source ISO language code</param>
        /// <param name="Target">Target ISO language code</param>
        /// <param name="Token"></param>
        /// <param name="Status">State code be returned from web service</param>
        /// <param name="Message">Message described operation detail</param>
        /// <returns></returns>
        public TranslationResult TranslateText(string RequestString, LanguageCodes Source, LanguageCodes Target, out int Status, out string Message, object Token) { return TranslateText(RequestString, Source, Target, out Status, out Message, null, Token); }

        /// <summary>
        /// Translate single or multiple items of text request
        /// </summary>
        /// <param name="RequestString">Request string need to translate</param>
        /// <param name="Source">Source ISO language code</param>
        /// <param name="Target">Target ISO language code</param>
        /// <param name="Status">State code be returned from web service</param>
        /// <param name="Message">Message described operation detail</param>
        /// <param name="TimeoutSpace">Time-out space for request</param>
        /// <returns></returns>
        public TranslationResult TranslateText(string RequestString, LanguageCodes Source, LanguageCodes Target, out int Status, out string Message, int? TimeoutSpace) { return TranslateText(RequestString, Source, Target, out Status, out Message, TimeoutSpace, null); }

        /// <summary>
        /// Translate single or multiple items of text request
        /// </summary>
        /// <param name="RequestString">Request string need to translate</param>
        /// <param name="Source">Source ISO language code</param>
        /// <param name="Target">Target ISO language code</param>
        /// <param name="Status">State code be returned from web service</param>
        /// <param name="Message">Message described operation detail</param>
        /// <returns></returns>
        public TranslationResult TranslateText(string RequestString, LanguageCodes Source, LanguageCodes Target, out int Status, out string Message) { return TranslateText(RequestString, Source, Target, out Status, out Message, null, null); }

        /// <summary>
        /// Translate single or multiple items of text request
        /// </summary>
        /// <param name="RequestArray">Request strings array need to translate</param>
        /// <param name="Source">Source ISO language code</param>
        /// <param name="Target">Target ISO language code</param>
        /// <param name="Token"></param>
        /// <param name="Status">State code be returned from web service</param>
        /// <param name="Message">Message described operation detail</param>
        /// <param name="TimeoutSpace">Time-out space for request</param>
        /// <returns></returns>
        public List<TranslationResult> TranslateText(string[] RequestArray, LanguageCodes Source, LanguageCodes Target, out int Status, out string Message, int? TimeoutSpace, object Token)
        {
            object tokenAuthen = (Token == null ? this._Token : Token);
            List<TranslationResult> _results = new List<TranslationResult>();
            Status = StatusWB.Gone;
            Message = HttpStatusCode.Gone.ToString();
            if (Target == LanguageCodes.Detect)
            {
                Status = StatusWB.NotAcceptable;
                Message = HttpStatusCode.NotAcceptable.ToString();
                return _results;
            }
            try
            {
                int _timeOutSpace = GetValidTimeoutSpace(TimeoutSpace);
                using (ExtendedWebClient _client = new ExtendedWebClient(_TranslateV2Url, _timeOutSpace))
                {
                    /// Header part
                    //client.Headers[HttpRequestHeader.ContentType] = "text/xml";
                    //client.Headers.Add(HttpRequestHeader.ContentType, "text/xml");
                    _client.Headers[HttpRequestHeader.ContentType] = "application/json"; // push parameters follow json type
                    _client.Headers.Add(DefinitionsWB.UserAgentKeyword, DefinitionsWB.TranslateDllKeywork);
                    _client.Encoding = Encoding.UTF8; // encode string has sign

                    /// Body part
                    RequestWB _req = new RequestWB();
                    _req.data = RequestArray;
                    _req.source = ParseLanguageCode(Source);
                    _req.target = ParseLanguageCode(Target);
                    _req.key = tokenAuthen.ToString();
                    _req.user = _User;

                    /// Calling web service
                    //Console.WriteLine(TranslateV2Url);
                    string _data = JsonConvert.SerializeObject(_req);
                    string _jsonString = _client.UploadString(_TranslateV2Url, MethodsWB.Post, _data); // POST method
                    //Console.WriteLine(_jsonString);

                    /// Receive output
                    ResponseWB _res = JsonConvert.DeserializeObject<ResponseWB>(_jsonString);
                    foreach (DataResponseWB item in _res.data)
                    {
                        TranslationResult _tr = new TranslationResult();
                        _tr.TranslatedText = item.text;
                        _tr.Source = item.source;
                        _tr.Charge = item.charge;
                        _results.Add(_tr);
                    }
                    Status = _res.status;
                    Message = _res.message;

                    // Write log
                    if (_DevMode == (int)LogMode.All || _DevMode == (int)LogMode.Success) WriteLog(FormatLine(Status.ToString(), Message, (int)LogMode.Success));
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
                //
                if (ex.Message.Contains("timed out")) //The operation has timed out
                {
                    Status = StatusWB.RequestTimeout;
                    Message = HttpStatusCode.RequestTimeout.ToString();
                    // Write log
                    if (_DevMode == (int)LogMode.All || _DevMode == (int)LogMode.Fail) WriteLog(FormatLine(Status.ToString(), Message, (int)LogMode.Fail));
                }
                else
                {
                    Status = StatusWB.ExceptionError;
                    Message = ex.Message;
                    // Write log
                    if (_DevMode == (int)LogMode.All || _DevMode == (int)LogMode.Fail) WriteLog(FormatLine(Status.ToString(), Message, (int)LogMode.Fail));
                }
            }
            return _results;
        }

        /// <summary>
        /// Translate single or multiple items of text request
        /// </summary>
        /// <param name="RequestArray">Request strings array need to translate</param>
        /// <param name="Source">Source ISO language code</param>
        /// <param name="Target">Target ISO language code</param>
        /// <param name="Token"></param>
        /// <param name="Status">State code be returned from web service</param>
        /// <param name="Message">Message described operation detail</param>
        /// <returns></returns>
        public List<TranslationResult> TranslateText(string[] RequestArray, LanguageCodes Source, LanguageCodes Target, out int Status, out string Message, object Token) { return TranslateText(RequestArray, Source, Target, out Status, out Message, null, Token); }

        /// <summary>
        /// Translate single or multiple items of text request
        /// </summary>
        /// <param name="RequestArray">Request strings array need to translate</param>
        /// <param name="Source">Source ISO language code</param>
        /// <param name="Target">Target ISO language code</param>
        /// <param name="Status">State code be returned from web service</param>
        /// <param name="Message">Message described operation detail</param>
        /// <param name="TimeoutSpace">Time-out space for request</param>
        /// <returns></returns>
        public List<TranslationResult> TranslateText(string[] RequestArray, LanguageCodes Source, LanguageCodes Target, out int Status, out string Message, int? TimeoutSpace) { return TranslateText(RequestArray, Source, Target, out Status, out Message, null, null); }

        /// <summary>
        /// Translate single or multiple items of text request
        /// </summary>
        /// <param name="RequestArray">Request strings array need to translate</param>
        /// <param name="Source">Source ISO language code</param>
        /// <param name="Target">Target ISO language code</param>
        /// <param name="Status">State code be returned from web service</param>
        /// <param name="Message">Message described operation detail</param>
        /// <returns></returns>
        public List<TranslationResult> TranslateText(string[] RequestArray, LanguageCodes Source, LanguageCodes Target, out int Status, out string Message) { return TranslateText(RequestArray, Source, Target, out Status, out Message, null, null); }

        /// <summary>
        /// Verify connection state of web service
        /// </summary>
        /// <param name="TimeoutSpace">Time-out space for request (millisecond)</param>
        /// <returns></returns>
        public bool CheckConnectionState(int? TimeoutSpace)
        {
            bool _result = false;
            try
            {
                int _timeOutSpace = GetValidTimeoutSpace(TimeoutSpace);
                using (ExtendedWebClient client = new ExtendedWebClient(_TestUrl, _timeOutSpace))
                {
                    client.DownloadString(_TestUrl);
                    _result = true;
                }
            }
            catch { }
            return _result;
        }
        #endregion


        #region Customer Request
        private struct CustomerRequest
        {
            public CustomerStruct[] data { get; set; }
            public string key { get; set; }
            public string user { get; set; }
        }
        private struct CustomerResponse
        {
            public int status { get; set; }
            public string message { get; set; }
        }
        #endregion

        /// <summary>
        /// Information of customer need to push to server
        /// </summary>
        public struct CustomerStruct
        {
            public string Code { get; set; }
            public string Name { get; set; }
            public string Address { get; set; }
            public string TaxCode { get; set; }
            public string Phone { get; set; }
            public string Remark { get; set; }
        }
        /// <summary>
        /// /// Post information of single/multiple customers to server
        /// </summary>
        /// <param name="ListCusts">Customers list</param>
        /// <returns></returns>
        public bool PostCustomerInfo(List<CustomerStruct> ListCusts, out string Message) { return PostCustomerInfo(ListCusts.ToArray(), out Message); }
        /// <summary>
        /// Post information of single/multiple customers to server
        /// </summary>
        /// <param name="ArrayCusts">Customers array</param>
        /// <returns></returns>
        public bool PostCustomerInfo(CustomerStruct[] ArrayCusts, out string Message)
        {
            CustomerResponse res = new CustomerResponse();
            try
            {
                using (ExtendedWebClient _client = new ExtendedWebClient(_CustomerUrl, null))
                {
                    //client.Headers.Add(HttpRequestHeader.ContentType, "text/xml");
                    _client.Headers[HttpRequestHeader.ContentType] = "application/json"; // push parameters follow json type
                    _client.Headers.Add(DefinitionsWB.UserAgentKeyword, DefinitionsWB.TranslateDllKeywork);
                    _client.Encoding = Encoding.UTF8; // encode string has sign

                    /// Body part
                    CustomerRequest req = new CustomerRequest();
                    req.data = ArrayCusts;
                    req.key = _Token.ToString();
                    req.user = _User;

                    /// Calling web service
                    //Console.WriteLine(TranslateV2Url);
                    string _data = JsonConvert.SerializeObject(req);
                    string _jsonString = _client.UploadString(_CustomerUrl, MethodsWB.Post, _data); // POST method
                    //Console.WriteLine(_jsonString);

                    /// Receive output
                    CustomerResponse _res = JsonConvert.DeserializeObject<CustomerResponse>(_jsonString);
                    res.status = _res.status;
                    res.message = _res.message;
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.Message);
                if (ex.Message.Contains("timed out")) //The operation has timed out
                {
                    res.status = StatusWB.RequestTimeout;
                    res.message = HttpStatusCode.RequestTimeout.ToString();
                }
                else if (ex.Message.Contains("address is not valid")) //Your uri address is not valid.
                {
                    res.status = StatusWB.UrlInvalid; // HttpStatusCode.Ambiguous
                    res.message = "UrlInvalid";
                }
                else
                {
                    res.status = StatusWB.ExceptionError;
                    res.message = ex.Message;
                }
            }
            Message = res.message;
            return res.status == StatusWB.OK;
        }

        #region Private Method - Log
        private string[] FormatLine(string StatusString, string MessageString, int Mode)
        {
            List<string> lst = new List<string>();
            if (Mode <= (int)LogMode.Non || Mode > Enum.GetNames(typeof(LogMode)).Length - 1) return lst.ToArray();
            if (StatusString.Trim().Length == 0 && MessageString.Trim().Length == 0) return lst.ToArray();
            if (Mode == (int)LogMode.Success)
                lst.Add(string.Concat("[Success]", " - [", DateTime.Now.ToString(), "]"));
            else if (Mode == (int)LogMode.Fail)
                lst.Add(string.Concat("[Fail]", " - [", DateTime.Now.ToString(), "]"));
            lst.Add(string.Concat("ResCode: ", StatusString, "; ResMsg: ", MessageString));
            return lst.ToArray();
        }
        private void WriteLog(string[] YourData)
        {
            if (_DevMode == (int)LogMode.Non) return;
            if (_DevMode <= (int)LogMode.Non || _DevMode > Enum.GetNames(typeof(LogMode)).Length - 1) return;
            WriteToFile(_LogFilePath, YourData, true);
        }
        private void WriteToFile(string FilePath, string[] YourData, bool CreateFileIfNotExists)
        {
            if (YourData == null) return;
            if (YourData.Length == 0) return;
            List<string> lst = new List<string>();
            foreach (string data in YourData)
            {
                if (data == null) continue;
                if (data.Trim() == string.Empty) continue;
                lst.Add(data);
            }
            if (lst.Count == 0) return;
            if (CreateFileIfNotExists)
            {
                string prefix = "file:\\";
                if (FilePath.StartsWith(prefix)) FilePath = FilePath.Substring(prefix.Length, FilePath.Length - prefix.Length);
                if (!File.Exists(FilePath))
                    try {
                        // Create a new file
                        using (FileStream fs = File.Create(FilePath))
                        {
                            // Add some text to file    
                            Byte[] title = new UTF8Encoding(true).GetBytes("[Auto-generated log file]");
                            fs.Write(title, 0, title.Length);
                            //byte[] author = new UTF8Encoding(true).GetBytes("Mahesh Chand");
                            //fs.Write(author, 0, author.Length);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[WriteToFile] Can not create file!");
                        Console.WriteLine(ex.Message);
                        return;
                    }
            }
            //if (!FilePath.EndsWith(".txt")) return;
            if (File.Exists(FilePath))
            {
                List<string> results = new List<string>();
                string[] lines = File.ReadAllLines(FilePath);
                foreach (string line in lines)
                {
                    results.Add(line);
                }
                foreach (string item in lst)
                {
                    results.Add(item);
                }
                try { File.WriteAllLines(FilePath, results.ToArray()); }
                catch (Exception ex)
                {
                    Console.WriteLine("[WriteToFile] Can not write to file!");
                    Console.WriteLine(ex.Message);
                    return;
                }
                return;
            }
            Console.WriteLine("[WriteToFile] File path is not exist!");
        }
        #endregion
    }


    //=======================================================
    public static class MethodsWB
    {
        public static readonly string Get = "GET";
        public static readonly string Post = "POST";
        public static readonly string Put = "PUT";
        public static readonly string Delete = "DELETE";
    }
    public static class DefinitionsWB
    {
        public static readonly int DefaultTimeoutSpace = 10000; /// millisecond = 10 second
        public static readonly int MinTimeoutSpace = 1; /// millisecond
        public static readonly int MaxTimeoutSpace = 300000; /// millisecond = 10 minute

        public static readonly string UserAgentKeyword = "User-Agent";
        public static readonly string TranslateDllKeywork = "TranslateDLL";
    }
    public static class StatusWB
    {
        public static readonly int UrlInvalid = 800; // (int)HttpStatusCode.Ambiguous
        //public static readonly int TimeoutSpaceInvalid = 801;
        public static readonly int OK = (int)HttpStatusCode.OK;
        public static readonly int Gone = (int)HttpStatusCode.Gone;
        public static readonly int NotAcceptable = (int)HttpStatusCode.NotAcceptable;
        public static readonly int RequestTimeout = (int)HttpStatusCode.RequestTimeout;
        public static readonly int InternalServerError = (int)HttpStatusCode.InternalServerError;
        public static readonly int ExceptionError = 666;
    }



}
