using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LillyApp_TEST.PrekoIFaktu
{
    public class Limit
    {

        public string Broj { get; set; }

        public string Ime { get; set; }

        public double LimitPaketa { get; set; }

        public Limit(string broj, string ime, double limit)
        {
            Broj = broj;
            Ime = ime;
            LimitPaketa = limit;
        }




    }
}
