using DamienG.Security.Cryptography;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CG.Web.MegaApiClient
{
    public class LogonSessionToken : IEquatable<LogonSessionToken>
    {
        [JsonProperty]
		public string SessionId {get; private set;}

        [JsonProperty]
		public byte[] MasterKey {get; private set;}
		
        private LogonSessionToken()
        {

        }

		public LogonSessionToken(string sessionId, byte[] masterKey)
		{
			SessionId = sessionId;
			MasterKey = masterKey;
		}

        public bool Equals(LogonSessionToken other)
        {
            if (other == null)
                return false;

            if (this.SessionId == null || other.SessionId == null || String.Compare(this.SessionId, other.SessionId) != 0)
            {
                return false;
            }

            if (this.MasterKey == null || other.MasterKey == null || !Enumerable.SequenceEqual(MasterKey, other.MasterKey))
            {
                return false;
            }
            return true;

        }
    }
}
