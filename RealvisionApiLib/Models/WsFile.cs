using System;
using System.Collections.Generic;
using System.Text;

namespace REALvisionApiLib
{
    public class WsFile
    {
        public String FileName { get; set; }
        public WsConfigs WsConfigs { get; set; }

        public WsFile()
        {
        }

        public WsFile(string fileName, WsConfigs wsConfigs)
        {
            FileName = fileName;
            WsConfigs = wsConfigs;
        }
    }
}
