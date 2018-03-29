using System;

namespace Shadowsocks.Model
{
    /*
     * Data come from WinINET
     */

    [Serializable]
    public class AwsConfig
    {
        public string accessKey;
        public string accessSecret;
        public string region;

        public AwsConfig()
        {
        }
    }
}