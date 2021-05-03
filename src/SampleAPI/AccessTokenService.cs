using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleAPI
{
    public class AccessTokenService
    {
        readonly HashSet<string> _accessTokens = new();

        public string Acquire()
        {
            var token = Guid.NewGuid().ToString();
            _accessTokens.Add(token);
            
            return token;
        }

        public bool IsAuthorized(string token)
        {
            return _accessTokens.Contains(token);
        }

        public bool Revoke(string token)
        {
            return _accessTokens.Remove(token);
        }
    }
}
