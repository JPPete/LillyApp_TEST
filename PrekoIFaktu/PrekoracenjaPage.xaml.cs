using LillyApp_TEST.PrekoIFaktu;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using ClosedXML.Excel;
using Microsoft.Office.Interop.Excel;
using System;
using Application = Microsoft.Office.Interop.Excel.Application;
using Range = Microsoft.Office.Interop.Excel.Range;
using System.Diagnostics;
using static LillyApp_TEST.MessageBoxCustom;
using System.Threading.Tasks;
using System.Linq;

namespace LillyApp_TEST
{
    /// <summary>
    /// Interaction logic for Prekoracenja.xaml
    /// </summary>
    public partial class Prekoracenja : System.Windows.Controls.Page
    {
        //Gde se nalazi excel table koja sadrzi korisnike, brojeve i koliki im je limit
        private readonly string brojeviIPaketi = @"\\192.168.1.190\telekomunikacije\T E L E K O M\TELEKOM 2019\MOBILNE NUMERACIJE - 2019 - Lilly i ZUA.xlsx";
        //private readonly string brojeviIPaketi = @"C:\Users\petar.jovancic\Desktop\MOBILNE NUMERACIJE - 2019 - Lilly i ZUA.xlsx";

        //putanja koju korisnik korisnik odredi da se tu nalaze racuni
        private string putanjaDoRacuna = "";


        //public List<Korisnik> zaduzenja = new List<Korisnik>();

        //public List<Limit> paketi = new List<Limit>();


        public Prekoracenja()
        {
            InitializeComponent();
        }







        #region "BUTTONS"

        private void UzmiRacunebtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser("Pritisnuto dugme UZMI RACUNE");

            var dialog = new CommonOpenFileDialog();

            dialog.IsFolderPicker = true;

            CommonFileDialogResult result = dialog.ShowDialog();

            if(result == CommonFileDialogResult.Ok)
            {
                putanjaDoRacuna = dialog.FileName;

                string[] csvPaths = Directory.GetFiles(putanjaDoRacuna, "*.csv");

                foreach(string path in csvPaths)
                {
                    lbRacuni.Items.Add(path.Substring(path.LastIndexOf('\\') + 1));


                }





            }
            else
            {
                new MessageBoxCustom("Neuspesno izabran folder sa Telekom racunima").ShowDialog();
            }



        }






        private async void Obracunajbtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser("Pritisnuto dugme - OBRACUNAK PREKROACENJA");

            
            if (putanjaDoRacuna == "")
            {
                Shared.LogUser("Nije izabrana putanja do racuna");
                new MessageBoxCustom("Morate da izaberete putanju do racuna.\nPritisnite dugme 'UZMI RACUNE'").ShowDialog();
            }
            else
            {

                System.Data.DataTable dtRezultat = new();

                dtRezultat.Columns.Add("KORISNIK");
                dtRezultat.Columns.Add("BROJ");
                dtRezultat.Columns.Add("POTROSNJA");
                dtRezultat.Columns.Add("LIMIT");
                dtRezultat.Columns.Add("PREKORACENJE");


                bool? result = new MessageBoxCustom("Proces provere moze da potraje, molim Vas za strpljenje.\nProgram ce zatvoriti sve otvorene excel tabele.\n Da li ste sigurni da zelite da uradite proveru?", MessageType.Confirmation, MessageButtons.YesNo, "Provera", 350, 650, 20).ShowDialog();


                Shared.LogUser($"Proces provere moze da potraje, molim Vas za strpljenje.\nProgram ce zatvoriti sve otvorene excel tabele.\n Da li ste sigurni da zelite da uradite proveru?");
                Shared.LogUser($"odgovor korisnika:\t{result}");


                if (result.Value)
                {

                    laObavestenje.Visibility = Visibility.Visible;





                    Shared.LogUser("Pravim spisak zaduzenja");

                    await Task.Run(() =>
                    {
                        //Spisak objekata Korisnika (sa podacima broj telefona i zaduzenje za taj mesec
                        List<Korisnik> zaduzenja = SpisakZaduzenja(putanjaDoRacuna);
                    
                    



                        Shared.LogUser("Pravim listu paketi od tabele gde su limiti");


                        //Pravim spisak paketa od tabele gde su Limiti
                        List<Limit> paketi = PaketiKorisnika2(brojeviIPaketi);


                        //counter da proverimo ako niko nije prekoracio
                        int punoKosta = 0;


                        Shared.LogUser("Zapocinjem proveru");


                        //prolazimo kroz spisak zaduzenja
                        foreach (Korisnik k in zaduzenja)
                        {
                            //Ako zaduzenje nije preko 2000 nemoj ni da gledas koji mu je limit
                            //jer je najmanji limit 2000
                            if (k.Zaduzenje <= 2000)
                                continue;


                            foreach (Limit l in paketi)
                            {
                                if (k.Broj == l.Broj)
                                {
                                    if (l.LimitPaketa < k.Zaduzenje)
                                    {
                                        dtRezultat.Rows.Add(l.Ime, l.Broj, k.Zaduzenje, l.LimitPaketa, Math.Round(k.Zaduzenje - l.LimitPaketa, 2));
                                        Shared.LogUser($"Korisnik: {l.Ime}  sa brojem {l.Broj} je prekoracio limit za {Math.Round(k.Zaduzenje - l.LimitPaketa, 2)}  (Limit: {l.LimitPaketa} | Potrosnja: {k.Zaduzenje})");
                                        punoKosta++;
                                    }
                                }
                            }



                        }//foreach zaduzenja


                        if (punoKosta == 0)
                        {
                            Shared.LogUser("Niko nije prekoracio limit");
                            new MessageBoxCustom("Niko nije prekoracio limit").ShowDialog();
                        }

                    });

                    UgasiExcel();



                    XLWorkbook wb = new XLWorkbook();
                    wb.Worksheets.Add(dtRezultat, "Prekoracenja");

                    wb.SaveAs(putanjaDoRacuna + @"\Prekoracenja.xlsx");


                    lbRacuni.Items.Clear();

                    laObavestenje.Visibility = Visibility.Hidden;

                    new MessageBoxCustom("Proces je zavrsen").ShowDialog();
                }
                else
                {
                    Shared.LogUser("Prekinut proces promene podataka");
                    new MessageBoxCustom("Prekinut proces promene podataka");
                }




            }//else provere da li je izabrana putanja gde su racuni



            



        }


        #endregion






        //Metoda koja: Pravljenje spiska korisnika sa brojem i zaduzenjem
        private static List<Korisnik> SpisakZaduzenja(string folderSaCsv)
        {
            List<Korisnik> zaduzenja = new List<Korisnik>();


            string[] csvPaths = Directory.GetFiles(folderSaCsv, "*.csv");

            foreach(string csvPath in csvPaths)
            {

                //prolazak kroz csv zaduzenja Apoteka
                using (StreamReader reader = new StreamReader(csvPath))
                {
                    string currentLine;
                    //coutner koristimo da preskocimo prva 2 reda jer ne sadrze podatke koji nam trebaju
                    int counter = 0;
                    //ako nije prazan red
                    while ((currentLine = reader.ReadLine()) != null)
                    {
                        //preskakanje prva 2 reda
                        if (counter == 0 || counter == 1)
                        {
                            counter++;
                            continue;
                        }

                        //uzimamo red i splitujemo ga po ; jer je tako razdvojen csv dokument
                        string[] vrednosti = currentLine.Split(';');

                        //ako nije upisan broj telefona preskacemo iteraciju
                        if (vrednosti[1] == "")
                        {
                            counter++;
                            continue;
                        }
                        //dodajemo novi objekat korisnika sa brojem telefona i zaduzenjem u listu
                        zaduzenja.Add(new Korisnik(vrednosti[1], vrednosti[vrednosti.Length - 1]));


                        counter++;
                    }
                }




            }




            return zaduzenja;
        }




        //Prolaz kroz tabelu gde se nalaze limiti i pravljenje liste tih korisnika
        //potrebno je ubaciti parametar broja redova
        private static List<Limit> PaketiKorisnika(string tabelaSaPaketima)
        {
            List<Limit> paketi = new List<Limit>();


            //otvaranje excel tabele
            Application excel = new Application();
            Workbook workbook = excel.Workbooks.Open(tabelaSaPaketima);
            //ulazak u worksheet 1 (to je taj koji ima te podatke)
            Worksheet worksheet = workbook.Worksheets[1];


            int last = worksheet.Cells.SpecialCells(XlCellType.xlCellTypeLastCell, Type.Missing).Row;
            Range rowA = worksheet.get_Range("A1:A" + last);

            int brRedova = 0;
            foreach (Range r in rowA.Cells)
            {
                if (r.Value2 != null)
                    brRedova++;
            }



            //prolazak kroz tabelu gde su limiti
            for (int i = 1; i < brRedova; i++)
            {
                if (worksheet.Rows[i].Cells[6].Value2 == null)
                    continue;


                //ako nema limit preskoci red
                if (worksheet.Rows[i].Cells[6].Value2.GetType() == typeof(string))
                    if (worksheet.Rows[i].Cells[6].Value2 == "NEMA")
                        continue;


                if (worksheet.Rows[i].Cells[6].Value2.GetType() == typeof(double))
                {
                    paketi.Add(new Limit(worksheet.Rows[i].Cells[1].Value2, worksheet.Rows[i].Cells[2].Value2, worksheet.Rows[i].Cells[6].Value2));
                }


            }//prolaz kroz excel tabelu



            //zatvaramo excel tabelu
            workbook.Close(0);
            excel.Quit();






            return paketi;
        }



        //Prolaz kroz tabelu gde se nalaze limiti i pravljenje liste tih korisnika
        //potrebno je ubaciti parametar broja redova
        private static List<Limit> PaketiKorisnika2(string tabelaSaPaketima)
        {
            List<Limit> paketi = new List<Limit>();



            XLWorkbook wb = new XLWorkbook(tabelaSaPaketima);

            var ws = wb.Worksheet("mobilni korisnici");

            var rows = ws.RangeUsed().RowsUsed().Skip(1);


            foreach(var row in rows )
            {
                var limitCell = row.Cell("F");

                if (limitCell == null || limitCell.GetValue<string>().Trim().ToUpper() == "NEMA" || limitCell.GetValue<string>().Trim().ToUpper() == string.Empty)
                    continue;

                //if (limitCell.GetValue<string>().Trim().ToUpper() == "NEMA")
                //    continue;

                var brojCell = row.Cell("A");
                string broj = brojCell.GetValue<string>().Trim();
                var imeCell = row.Cell("B");
                string ime = imeCell.GetValue<string>().Trim();

                paketi.Add(new Limit(broj, ime, limitCell.GetValue<double>()));



            }

            




            return paketi;
        }








        //Gasi excel
        public static void UgasiExcel()
        {
            Process[] pokrenutiProcesi = Process.GetProcesses();

            foreach (Process p in pokrenutiProcesi)
            {
                if (p.ProcessName.ToLower() == "excel")
                {
                    p.Kill();
                    Shared.LogUser("\nGasim Excel\n");

                }
            }
        }





    }
}
