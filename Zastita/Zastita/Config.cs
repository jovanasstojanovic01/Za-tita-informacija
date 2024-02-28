using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace Zastita
{
    class Config
    {
        private readonly string key;
        private readonly string serverAddress;
        private readonly int port;
        private readonly int r;
        private readonly int w;
        private readonly uint p;
        private readonly uint q;
        private readonly string rc6key;
        private readonly string iv;
        public Config()
        {
            key = ConfigurationManager.AppSettings["key"];
            serverAddress = ConfigurationManager.AppSettings["serverAddress"];
            port = int.Parse(ConfigurationManager.AppSettings["port"]);

            r = int.Parse(ConfigurationManager.AppSettings["r"]);
            w = int.Parse(ConfigurationManager.AppSettings["w"]);
            rc6key = ConfigurationManager.AppSettings["rc6key"];
            iv = ConfigurationManager.AppSettings["iv"];

            string hexString = ConfigurationManager.AppSettings["p"];
            p = Convert.ToUInt32(hexString, 16);

            string hexStringq = ConfigurationManager.AppSettings["q"];
            q = Convert.ToUInt32(hexStringq, 16);
        }

        public string Key
        {
            get { return key; }
        }

        public string RC6Key
        {
            get { return rc6key; }
        }
        public string IV
        {
            get { return iv; }
        }
        public string ServerAddress
        {
            get { return serverAddress; }
        }

        public int Port
        {
            get { return port; }
        }

        public int R
        {
            get { return r; }
        }


        public int W
        {
            get { return w; }
        }

        public uint P
        {
            get { return p; }
        }

        public uint Q
        {
            get { return q; }
        }
    }
}
