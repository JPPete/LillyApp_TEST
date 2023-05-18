using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LillyApp_TEST.PrekoIFaktu
{

    public class Korisnik
    {
        public string Broj { get; set; }

        public double Zaduzenje { get; set; }




        public Korisnik(string broj, string zaduzenja)
        {
            Broj = broj;
            Zaduzenje = double.Parse(zaduzenja);
        }





    }
}
