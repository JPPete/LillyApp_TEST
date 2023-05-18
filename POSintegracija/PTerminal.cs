using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LillyApp_TEST.POSintegracija
{
    public class PTerminal
    {

        public string Ip { get; set; }

        public string MAC { get; set; }

        public string StatDhcp { get; set; }


        public PTerminal(string ip, string mac, string statDhcp)
        {
            Ip = ip;

            MAC = mac;

            StatDhcp = statDhcp;
        }




    }
}
