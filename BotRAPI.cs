using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Web;

namespace BotR.API {

    public class BotRAPI {

        private string _apiURL = "";

        private string _args = "";

        private NameValueCollection _queryString = null;

        public string Key { get; set; }

        public string Secret { get; set; }

        public string APIFormat { get; set; }

        public BotRAPI(string key, string secret) : this("http://api.bitsontherun.com", "v1", key, secret) { }

        public BotRAPI(string url, string version, string key, string secret) {

            Key = key;
            Secret = secret;

            _apiURL = string.Format("{0}/{1}", url, version);
        }

        /// <summary>
        /// Call the API method with no params beyond the required
        /// </summary>
        /// <param name="apiCall">The path to the API method call (/videos/list)</param>
        /// <returns>The string response from the API call</returns>
        public string Call(string apiCall) {
            return Call(apiCall, null);
        }

        /// <summary>
        /// Call the API method with additional, non-required params
        /// </summary>
        /// <param name="apiCall">The path to the API method call (/videos/list)</param>
        /// <param name="args">Additional, non-required arguments</param>
        /// <returns>The string response from the API call</returns>
        public string Call(string apiCall, NameValueCollection args) {

            _queryString = new NameValueCollection();

            //add the non-required args to the required args
            if (args != null)
                _queryString.Add(args);

            buildArgs();
            WebClient client = createWebClient();

            string callUrl = _apiURL + apiCall;

            try {
                return client.DownloadString(callUrl);
            } catch  {
                return "";
            }
        }

        /// <summary>
        /// Upload a file to account
        /// </summary>
        /// <param name="uploadUrl">The url returned from /videos/create call</param>
        /// <param name="args">Optional args (video meta data)</param>
        /// <param name="filePath">Path to file to upload</param>
        /// <returns>The string response from the API call</returns>
        public string Upload(string uploadUrl, NameValueCollection args, string filePath) {

            _queryString = args; //no required args

            WebClient client = createWebClient();
            _queryString["api_format"] = APIFormat ?? "xml"; //xml if not specified - normally set in required args routine
            queryStringToArgs();

            string callUrl = _apiURL + uploadUrl + "?" + _args;
            callUrl = uploadUrl + "?" + _args;

            try {
                 byte[] response = client.UploadFile(callUrl, filePath);
                 return Encoding.UTF8.GetString(response);
            } catch {
                return "";
            }
        }

        /// <summary>
        /// Hash the provided arguments
        /// </summary>
        private string signArgs() {

            queryStringToArgs();

            HashAlgorithm ha = HashAlgorithm.Create("SHA");
            byte[] hashed = ha.ComputeHash(Encoding.UTF8.GetBytes(_args + Secret));
            return BitConverter.ToString(hashed).Replace("-", "").ToLower();
        }

        /// <summary>
        /// Convert args collection to ordered string
        /// </summary>
        private void queryStringToArgs() {

            Array.Sort(_queryString.AllKeys);
            StringBuilder sb = new StringBuilder();

            foreach (string key in _queryString.AllKeys) {
                sb.AppendFormat("{0}={1}&", key, _queryString[key]);
            }
            sb.Remove(sb.Length - 1, 1); //remove trailing &

            _args = sb.ToString();
        }

        /// <summary>
        /// Append required arguments to URL
        /// </summary>
        private void buildArgs() {

            _queryString["api_format"] = APIFormat ?? "xml"; //xml if not specified
            _queryString["api_key"] = Key;
            _queryString["api_kit"] = "dnet-1.0";
            _queryString["api_nonce"] = string.Format("{0:00000000}", new Random().Next(99999999));
            _queryString["api_timestamp"] = getUnixTime().ToString();
            _queryString["api_signature"] = signArgs();

            _args = string.Concat(_args, "&api_signature=", _queryString["api_signature"]);
        }

        /// <summary>
        /// Construct instance of WebClient for request
        /// </summary>
        /// <returns></returns>
        private WebClient createWebClient() {

            ServicePointManager.Expect100Continue = false; //upload will fail w/o
            WebClient client = new WebClient();
            client.BaseAddress = _apiURL;
            client.QueryString = _queryString;
            client.Encoding = UTF8Encoding.UTF8;
            return client;
        }

        /// <summary>
        /// Get timestamp in Unix format
        /// </summary>
        /// <returns></returns>
        private int getUnixTime() {
            return (int)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        }
    }
}
