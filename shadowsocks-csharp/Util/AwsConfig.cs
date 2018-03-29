using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shadowsocks.Util
{
    class AwsConfig
    {
        public Shadowsocks.Model.AwsConfig config;

        public AwsConfig()
        {
            var dir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            var configFile = Path.Combine(dir, "aws.json");
            var json = File.ReadAllText(configFile);
            var data = JsonConvert.DeserializeObject<Shadowsocks.Model.AwsConfig>(json);
            config = data;
        }
    }
}
