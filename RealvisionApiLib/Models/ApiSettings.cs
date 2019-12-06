using System;
using System.Collections.Generic;
using System.Text;

namespace REALvisionApiLib.Models
{
    public class ApiSettings
    {
        public string ApiKey { get; set; }
        public string ApiUrl { get; set; }
        public string AuthServerUrl { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }

        public ApiSettings()
        {
        }

        public ApiSettings(string apiKey, string apiUrl, string authServerUrl, string clientId, string clientSecret)
        {
            ApiKey = apiKey;
            ApiUrl = apiUrl;
            AuthServerUrl = authServerUrl;
            ClientId = clientId;
            ClientSecret = clientSecret;
        }
    }   
}
