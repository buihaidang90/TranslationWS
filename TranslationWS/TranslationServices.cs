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
        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="Token">Access token</param>
        public TranslationServices(object Token) // contructor
        {
            this._Token = Token;
            SetWebServiceUrl(); // step 1
            ReadBaseUrlFromFile(); // step 2
        }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="Token">Access token</param>
        /// <param name="TimeoutSpace">Timeout space when must to wait a long time (millisecond)</param>
        public TranslationServices(object Token, int TimeoutSpace) // contructor
        {
            if (DefinitionsWB.MinTimeoutSpace <= TimeoutSpace && TimeoutSpace <= DefinitionsWB.MaxTimeoutSpace)
                this._Timeout = TimeoutSpace;
            this._Token = Token;
            SetWebServiceUrl(); // step 1
            ReadBaseUrlFromFile(); // step 2
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
        private object _Token = "";
        private void SetWebServiceUrl()
        {
            _TestUrl = _BaseUrl + @"/api/Test";
            _TranslateV2Url = _BaseUrl + @"/api/TranslateV2";
            _SqlTranslateV2Url = _BaseUrl + @"/api/SqlTranslateV2";
        }
        private void ReadBaseUrlFromFile()
        {
            string _pathFileConfig = @"Config_Dictionary.ini";
            string _TextBaseUrl = "BaseUrl=";
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
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
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

        private struct RequestWB
        {
            /* request structure like from web service */
            public string[] data { get; set; }
            public string source { get; set; }
            public string target { get; set; }
            public string key { get; set; } // authentication required if have
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
            JA,JP,
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
        /// <param name="Status">State code be returned from web service</param>
        /// <param name="Message">Message described operation detail</param>
        /// <param name="TimeoutSpace">Time-out space for request</param>
        /// <returns></returns>
        public TranslationResult TranslateText(string RequestString, LanguageCodes Source, LanguageCodes Target, out int Status, out string Message, int? TimeoutSpace)
        {
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
            //if (TimeoutSpace != null)
            //{
            //    if (TimeoutSpace < DefinitionsWB.MinTimeoutSpace || DefinitionsWB.MaxTimeoutSpace < TimeoutSpace)
            //    {
            //        Status = StatusWB.TimeoutSpaceInvalid;
            //        Message = "TimeoutSpaceInvalid";
            //        return _result;
            //    }
            //}
            //else TimeoutSpace = _Timeout;
            try
            {
                int _timeOutSpace = GetValidTimeoutSpace(TimeoutSpace);
                using (ExtendedWebClient _client = new ExtendedWebClient(_TranslateV2Url, _timeOutSpace))
                {
                    /// Header part
                    //client.Headers[HttpRequestHeader.ContentType] = "text/xml";
                    //client.Headers.Add(HttpRequestHeader.ContentType, "text/xml");
                    _client.Headers[HttpRequestHeader.ContentType] = "application/json"; // push parameters follow json type
                    _client.Encoding = Encoding.UTF8; // encode string has sign

                    /// Body part
                    RequestWB _req = new RequestWB();
                    _req.data = new string[] { RequestString };
                    _req.source = ParseLanguageCode(Source);
                    _req.target = ParseLanguageCode(Target);
                    _req.key = _Token.ToString();

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
                }
                else if (ex.Message.Contains("address is not valid")) //Your uri address is not valid.
                {
                    Status = StatusWB.UrlInvalid; // HttpStatusCode.Ambiguous
                    Message = "UrlInvalid";
                }
                else
                {
                    Status = StatusWB.InternalServerError;
                    Message = HttpStatusCode.InternalServerError.ToString();
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
        /// <param name="Status">State code be returned from web service</param>
        /// <param name="Message">Message described operation detail</param>
        /// <returns></returns>
        public TranslationResult TranslateText(string RequestString, LanguageCodes Source, LanguageCodes Target, out int Status, out string Message)
        {
            return TranslateText(RequestString, Source, Target, out Status, out Message, null);
        }

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
        public List<TranslationResult> TranslateText(string[] RequestArray, LanguageCodes Source, LanguageCodes Target, out int Status, out string Message, int? TimeoutSpace)
        {
            List<TranslationResult> _results = new List<TranslationResult>();
            Status = StatusWB.Gone;
            Message = HttpStatusCode.Gone.ToString();
            if (Target == LanguageCodes.Detect)
            {
                Status = StatusWB.NotAcceptable;
                Message = HttpStatusCode.NotAcceptable.ToString();
                return _results;
            }
            //if (TimeoutSpace != null)
            //{
            //    if (TimeoutSpace < DefinitionsWB.MinTimeoutSpace || DefinitionsWB.MaxTimeoutSpace < TimeoutSpace)
            //    {
            //        Status = StatusWB.TimeoutSpaceInvalid;
            //        Message = "TimeoutSpaceInvalid";
            //        return _results;
            //    }
            //}
            //else TimeoutSpace = _Timeout;
            try
            {
                int _timeOutSpace = GetValidTimeoutSpace(TimeoutSpace);
                using (ExtendedWebClient _client = new ExtendedWebClient(_TranslateV2Url, _timeOutSpace))
                {
                    /// Header part
                    //client.Headers[HttpRequestHeader.ContentType] = "text/xml";
                    //client.Headers.Add(HttpRequestHeader.ContentType, "text/xml");
                    _client.Headers[HttpRequestHeader.ContentType] = "application/json"; // push parameters follow json type
                    _client.Encoding = Encoding.UTF8; // encode string has sign

                    /// Body part
                    RequestWB _req = new RequestWB();
                    _req.data = RequestArray;
                    _req.source = ParseLanguageCode(Source);
                    _req.target = ParseLanguageCode(Target);
                    _req.key = _Token.ToString();

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
                }
                else
                {
                    Status = StatusWB.InternalServerError;
                    Message = HttpStatusCode.InternalServerError.ToString();
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
        /// <param name="Status">State code be returned from web service</param>
        /// <param name="Message">Message described operation detail</param>
        /// <returns></returns>
        public List<TranslationResult> TranslateText(string[] RequestArray, LanguageCodes Source, LanguageCodes Target, out int Status, out string Message)
        {
            return TranslateText(RequestArray, Source, Target, out Status, out Message, null);
        }

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
    }



}
