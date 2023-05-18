using ClosedXML.Excel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static LillyApp_TEST.MessageBoxCustom;

namespace LillyApp_TEST
{
    /// <summary>
    /// Interaction logic for FakturisanjePage.xaml
    /// </summary>
    public partial class FakturisanjePage : Page
    {
        public FakturisanjePage()
        {
            InitializeComponent();
        }

        private async void Generisibtn(object sender, RoutedEventArgs e)
        {
            if(txtBruto.Text == string.Empty || txtResurs.Text == string.Empty)
            {
                Shared.LogUser("Nije popunjeno neko polje");
                new MessageBoxCustom("Potrebno je popuniti i polje Resurs i Bruto.").ShowDialog();
            }
            else
            {
                bool resursParse = int.TryParse(txtResurs.Text, out int resurs);

                bool brutoParse = float.TryParse(txtBruto.Text, NumberStyles.Number, CultureInfo.InvariantCulture ,out float bruto);

                if (!resursParse)
                {
                    Shared.LogUser($"Nije uspelo parsiranje unetog resursa: {txtResurs.Text}");
                    new MessageBoxCustom("Resurs mora da bude broj").ShowDialog();
                }
                else
                {

                    if (!brutoParse)
                    {
                        Shared.LogUser($"Nije uspelo parsiranje unetog bruto iznosa: {txtBruto.Text}");
                        new MessageBoxCustom("Bruto mora da bude broj (mogu decimale)").ShowDialog();
                    }
                    else
                    {
                        string brutoS = txtBruto.Text;
                        if (brutoS.Contains(','))
                        {
                            brutoS = txtBruto.Text.Replace(',','.');
                        }
                        


                        string query = "drop table if exists #radi " +
                            "create table #radi (RESURS int, BRUTO FLOAT, TM int)  " +
                            "DECLARE @posl int = 276400 " +
                            "declare c cursor for " +
                            "select posl from bi_posl where tip = 'M' and AKTIVNA = 'D' and OSNOVNA_POSL is NULL " +
                            "open c  " +
                            "fetch next from c into @posl " +
                            "while @@FETCH_STATUS = 0 " +
                            "begin " +
                            "insert into #radi " +
                            $"select {resurs}, {brutoS}, t.POSL " +
                            "FROM " +
                            "( " +
                            "select prom.POSL , CONCAT(posl.IME, ' ', posl.IME1) as NAZIV, count(*) as BR_RACUNA " +
                            "from BIP_PROM prom inner join BI_POSL posl on prom.POSL = posl.POSL " +
                            "where KODA = 'BP' " +
                            "and DATUM between DATEADD(MONTH, DATEDIFF(MONTH, 0, GETDATE())-1, 0) and DATEADD(MONTH, DATEDIFF(MONTH, -1, GETDATE())-1, -1) " +
                            "and prom.POSL = @posl " +
                            "GROUP BY prom.POSL, posl.IME, posl.IME1 " +
                            ") t " +
                            "fetch next from c into @posl " +
                            "END " +
                            "close c " +
                            "DEALLOCATE c " +
                            "select RESURS, (BRUTO / (select count(*) from #radi r)) as NETO, TM " +
                            "from #radi r";



                        DataTable dataTable = new DataTable();

                        dataTable = await GetDataAsync(query);


                        SaveFileDialog save = new SaveFileDialog();

                        save.FileName = "IzvestajZaFakturisanje";
                        save.DefaultExt = ".xlsx";
                        save.Filter = "Excel Worksheets|*.xlsx";

                        bool? result = save.ShowDialog();

                        if(result == true)
                        {


                            XLWorkbook wb = new XLWorkbook();

                            wb.Worksheets.Add(dataTable, "Izvestaj");

                            wb.SaveAs(save.FileName);



                            new MessageBoxCustom("Zavrsen proces").ShowDialog();
                        }




                    }//provera parsiranja bruto iznosa

                }//provera parsiranja resursa







            }//provera da li su uneti i resurs i bruto


            





        }




        //Runs the Query that is passed through and returns data in DataTable
        public async Task<DataTable> GetDataAsync(string query)
        {
            DataTable nnTable = new();
            try
            {


                SqlConnection sqlConnection = await Task.Run(() => new SqlConnection(Shared.prodViewConnString));

                SqlCommand cmd = await Task.Run(() => new SqlCommand(query, sqlConnection));

                //If the query doesnt get excecuted in 2 min Timout Exception will be thrown out
                cmd.CommandTimeout = 120;


                SqlDataAdapter sqlDataAdapter = await Task.Run(() => new SqlDataAdapter(cmd));

                nnTable = new DataTable();
                await Task.Run(() =>
                {

                    using (sqlDataAdapter)
                    {


                        sqlDataAdapter.Fill(nnTable);






                    }

                });







            }
            catch (Exception ex)
            {
                Shared.LogUser($"\n{ex}\n");
                new MessageBoxCustom(ex.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();


            }



            return nnTable;
        }





        private void EnterUpResurs(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Return)
            {
                txtBruto.Focus();
            }
        }

        private void EnterUpBruto(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Return)
            {
                Generisibtn(sender, e);

            }
        }
    }
}
