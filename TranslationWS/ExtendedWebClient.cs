using System;
using System.Net;

namespace TranslationWS
{
    public class ExtendedWebClient : WebClient
    {
        private int _Timeout = DefinitionsWB.DefaultTimeoutSpace;
        public int Timeout
        {
            get
            {
                return _Timeout;
            }
            set
            {
                if (DefinitionsWB.MinTimeoutSpace <= value && value <= DefinitionsWB.MaxTimeoutSpace)
                    _Timeout = value;
                else
                    _Timeout = DefinitionsWB.DefaultTimeoutSpace;
            }
        }
        public ExtendedWebClient(Uri address, int timeout)
        {
            this._Timeout = timeout;//In Milli seconds
            var objWebClient = GetWebRequest(address);
        }
        public ExtendedWebClient(string stringUriAddress, int timeout)
        {
            this._Timeout = timeout;//In Milli seconds
            if (!Uri.IsWellFormedUriString(stringUriAddress, UriKind.Absolute))
            {
                throw new Exception("Your uri address is not valid.");
            }
            var objWebClient = GetWebRequest(new Uri(stringUriAddress, UriKind.Absolute));
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest wr = base.GetWebRequest(address);
            wr.Timeout = this._Timeout;
            ((HttpWebRequest)wr).ReadWriteTimeout = this._Timeout; // dont remove
            return wr;
        }


    }


}
