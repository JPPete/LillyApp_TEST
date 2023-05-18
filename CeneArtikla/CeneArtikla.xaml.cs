using System;
using System.Data.SqlClient;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Net.NetworkInformation;
using System.Net;
using System.Threading;
using System.Net.Http;
using Newtonsoft.Json;
using static LillyApp_TEST.MessageBoxCustom;

namespace LillyApp_TEST
{
    /// <summary>
    /// Interaction logic for CeneArtikla.xaml
    /// </summary>
    public partial class CeneArtikla : Window
    {

        //SqlConnection sqlConnection;

        //Konekcija na SQl server preko serverskog korisnika
        public const string connectionString = Shared.prodViewConnString;


        public static HttpClient client = new HttpClient();
        public static HttpClientHandler handler = new HttpClientHandler();

        public CeneArtikla()
        {
            InitializeComponent();

            txtPosl.Focus();

        }



        #region "Enter UP"

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void EnterUpPoslovnica(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Return)
            {
                dtDatum.Focus();
            }
        }

        private void DatumEnter(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Return)
            {
                txtIdArtikla.Focus();
            }
        }

        private void EnterUpIdArtikla(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Return)
            {
                FindPriceArticleBtn(sender, e);
            }
        }

        private void EnterUpEanArtikla(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Return)
            {
                FindPriceArticleBtn(sender, e);
            }
        }

        #endregion





        #region "BUTTONS"

        private async void FindPriceArticleBtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser("Pritisnuo dugme Pronadji cenu - FindPriceArticleBtn");

            //Resetujem podatke za sledece pretrage
            txtTrenutnaCena.Text = null;
            txtNormalnaCena.Text = null;
            txtNazivAkcije.Text = null;
            txtDatumDoAkcije.Text = null;
            txtDatumOdAkcije.Text = null;
            txtIdAkcije.Text = null;
            txtModelAkcije.Text = null;
            dgSpecialPrices.ItemsSource = null;

            //Provera da li je uneta poslovnica
            if(!EmptyFieldValidation(txtPosl.Text))
            {
                Shared.LogUser("Nije unet broj poslovnice");
                new MessageBoxCustom("Morate da unesete broj poslovnice").ShowDialog();
            }
            else
            {
                //Provera da li je unet datum i da nije u buducnosti
                //|| dtDatum.SelectedDate > DateTime.Now
                if (dtDatum.SelectedDate == null )
                {
                    Shared.LogUser($"Datum mora da bude unet - {dtDatum.SelectedDate}");
                    new MessageBoxCustom("Datum mora da bude unet").ShowDialog();
                }
                else
                {
                    //Provera da li je unet ili ID artikla ili EAN artikla
                    if(!(EmptyFieldValidation(txtIdArtikla.Text) || EmptyFieldValidation(txtEanArtikla.Text)))
                    {
                        Shared.LogUser($"Nije unet ni ID artikla ni EAN: ID-{txtIdArtikla.Text} | EAN-{txtEanArtikla.Text}");
                        new MessageBoxCustom("Morate uneti ili ID artikla ili EAN").ShowDialog();
                    }
                    else
                    {
                        //format datuma za API call
                        string datum = dtDatum.SelectedDate.Value.ToString("yyyy-MM-dd");

                        //Provera da li vidimo server
                        if (!PingServer("192.168.1.46"))
                        {
                            Shared.LogUser($"Ne pingujem server 192.168.1.46");
                            new MessageBoxCustom("Nemam pristup serveru 192.168.1.46", MessageType.Error).ShowDialog();
                        }
                        else
                        {
                            //Provera da li postoji poslovnica u bazi
                            if (!ProveraPosl())
                            {
                                Shared.LogUser($"Ne postoji ili nije aktivna poslovnica u bazi. Posl: {txtPosl.Text}");
                                new MessageBoxCustom("Uneta poslovnica ne postoji ili nije aktivna u bazi", MessageType.Error).ShowDialog();
                            }
                            else
                            {

                                //Ako je unet ID artikla
                                if (EmptyFieldValidation(txtIdArtikla.Text))
                                {
                                    //Provera da li postoji artikal u bazi
                                    if (!ProveraArtikla())
                                    {
                                        Shared.LogUser($"Uneti artikal ne postoji u bazi. Artikal: {txtIdArtikla.Text}");
                                        new MessageBoxCustom("Unet ID artikla ne postoji u bazi", MessageType.Error).ShowDialog();
                                    }
                                    else
                                    {
                                        //Pozivamo API da dobijemo cenu
                                        await ApiPostMethod(datum, txtPosl.Text, txtIdArtikla.Text);

                                        string? zaliha = await GetZaliha(txtIdArtikla.Text);

                                        if (zaliha != null)
                                        {
                                            txtModelAkcije.Text = zaliha;
                                        }


                                        string queryAkcije = "Select p.posl as POSL, r.r_cena as CENA,r.r_popust as POPUST,pr.stevec_a as ID_AKCIJE, pr.opis as NAZIV, pr.datum_od as DATUM_OD, pr.datum_do as DATUM_DO, br.naziv as TIP_AKCIJE " +
                                            "from bip_akc_prom pr " +
                                            "Inner join bip_akc_ppror r on r.stevec_a = pr.stevec_a " +
                                            "Inner join bi_akc_model m on m.model = pr.model " +
                                            "Inner join bi_akc_formula f on f.formula = m.formula " +
                                            "Inner join bip_akc_posl p on p.stevec_a = pr.stevec_a " +
                                            "Left outer join bip_Skupine_Art s on s.id_skupine = r.r_skupina " +
                                            "INNER JOIN bi_artik ba ON ba.artikel = IsNull(r.r_artikel, ba.artikel) AND IsNull(ba.si_art_dob,' ') = IsNull(r.r_si_art_dob, IsNull(ba.si_art_dob,' ')) AND IsNull(ba.blag_sk,' ') = IsNull(r.r_blag_sk, IsNull(ba.blag_sk,' ')) AND IsNull(ba.blag_znamka,' ') = IsNull(r.r_blag_znamka, IsNull(ba.blag_znamka,' ')) AND IsNull(ba.sezona,' ') = IsNull(r.r_sezona, IsNull(ba.sezona,' ')) AND IsNull(ba.artikel,' ') = IsNull(s.artikel, IsNull(ba.artikel,' ')) " +
                                            "LEFT OUTER JOIN Bi_Razlog br on pr.razlog = br.razlog " +
                                            $"WHERE ba.artikel = '{txtIdArtikla.Text}' " +
                                            "and (f.op_formula = 'Art, DS - cena' or f.op_formula = 'Art, DS - popust' or f.op_formula = 'Art, DS - vred. popust' or f.op_formula = 'BS, BZ - POPUST' OR f.op_formula = 'DISK. ZNESEK - POPUST' ) " +
                                            "and f.aktiven = 'D' " +
                                            "and pr.status = '2' " +
                                            $"and p.posl = {txtPosl.Text} " +
                                            $"and {{d '{datum}'}} between pr.datum_od and pr.datum_do " +
                                            "and not exists(select 1 from bip_akc_pprop where stevec_a = pr.stevec_a and p_artikel = r.r_artikel and izloci = 'D') " +
                                            $"and not exists(select 1 from bip_akc_pprop where stevec_a = pr.stevec_a and p_artikel = '{txtIdArtikla.Text}' and izloci = 'D') " +
                                            "ORDER BY pr.datum_od ";

                                        //Pozivamo query da dobijemo akcije
                                        await GetDataAsync(queryAkcije);


                                    }//Else od provere da li postoji artikal



                                }//Ako je pretraga po ID-u
                                else//AKO JE UNET EAN
                                {

                                    if (!ProveraEan())
                                    {
                                        Shared.LogUser($"Uneti EAN ne postoji u bazi. EAN: {txtEanArtikla.Text}");
                                        new MessageBoxCustom("Uneti barkod ne posotji u bazi", MessageType.Error).ShowDialog();
                                    }
                                    else
                                    {

                                        string? idArtikla = await GetIdFromEan();

                                        //Provera da li je program uspeo da nadje ID artikla preko barkoda
                                        if( idArtikla == null)
                                        {
                                            Shared. LogUser($"Neuspesno pronalazenje id artikla preko ean-a. Ean: {txtEanArtikla.Text}");
                                            new MessageBoxCustom("Neuspesno pronalazenje ID artikla preko unetog barkoda", MessageType.Error).ShowDialog();
                                        }
                                        else
                                        {
                                            //Pozivamo API da dobijemo cenu
                                            await ApiPostMethod(datum, txtPosl.Text, idArtikla);


                                            string? zaliha = await GetZaliha(idArtikla);

                                            if(zaliha != null)
                                            {
                                                txtModelAkcije.Text = zaliha;
                                            }


                                            string queryAkcije = "Select p.posl as POSL, r.r_cena as CENA,r.r_popust as POPUST,pr.stevec_a as ID_AKCIJE, pr.opis as NAZIV, pr.datum_od as DATUM_OD, pr.datum_do as DATUM_DO, br.naziv as TIP_AKCIJE " +
                                                "from bip_akc_prom pr " +
                                                "Inner join bip_akc_ppror r on r.stevec_a = pr.stevec_a " +
                                                "Inner join bi_akc_model m on m.model = pr.model " +
                                                "Inner join bi_akc_formula f on f.formula = m.formula " +
                                                "Inner join bip_akc_posl p on p.stevec_a = pr.stevec_a " +
                                                "Left outer join bip_Skupine_Art s on s.id_skupine = r.r_skupina " +
                                                "INNER JOIN bi_artik ba ON ba.artikel = IsNull(r.r_artikel, ba.artikel) AND IsNull(ba.si_art_dob,' ') = IsNull(r.r_si_art_dob, IsNull(ba.si_art_dob,' ')) AND IsNull(ba.blag_sk,' ') = IsNull(r.r_blag_sk, IsNull(ba.blag_sk,' ')) AND IsNull(ba.blag_znamka,' ') = IsNull(r.r_blag_znamka, IsNull(ba.blag_znamka,' ')) AND IsNull(ba.sezona,' ') = IsNull(r.r_sezona, IsNull(ba.sezona,' ')) AND IsNull(ba.artikel,' ') = IsNull(s.artikel, IsNull(ba.artikel,' ')) " +
                                                "LEFT OUTER JOIN Bi_Razlog br on pr.razlog = br.razlog " +
                                                $"WHERE ba.artikel = '{idArtikla}' " +
                                                "and (f.op_formula = 'Art, DS - cena' or f.op_formula = 'Art, DS - popust' or f.op_formula = 'Art, DS - vred. popust' or f.op_formula = 'BS, BZ - POPUST' OR f.op_formula = 'DISK. ZNESEK - POPUST' ) " +
                                                "and f.aktiven = 'D' " +
                                                "and pr.status = '2' " +
                                                $"and p.posl = {txtPosl.Text} " +
                                                $"and {{d '{datum}'}} between pr.datum_od and pr.datum_do " +
                                                "and not exists(select 1 from bip_akc_pprop where stevec_a = pr.stevec_a and p_artikel = r.r_artikel and izloci = 'D') " +
                                                $"and not exists(select 1 from bip_akc_pprop where stevec_a = pr.stevec_a and p_artikel = '{idArtikla}' and izloci = 'D') " +
                                                "ORDER BY pr.datum_od ";

                                            //Pozivamo query da dobijemo akcije
                                            await GetDataAsync(queryAkcije);


                                        }//else od provere pronalazenja ID-a preko EAN-a

                                    }//else od provere barkoda

                                }//Else ako je pretraga po EAN

                            }//Else od provera poslovnice

                        }//Else od pinga servera

                    }//Else od provere da li je unet ID artikla ili EAN

                }//else od provere da li je datum unet i da nije u buducnosti

            }//else od provere da li je uneta poslovnica







        }//FindPriceArticleBtn




        private void FindArticleBtn(object sender, RoutedEventArgs e)
        {
            Shared.LogUser("----------- POKRENUT PROGRAM PRONADJI ARTIKAL -----------");

            PronadjiArtikal prArtical = new();
            prArtical.Show();
        }



        #endregion




        #region "Metods"

        //Metoda za proveru da je unet broj poslovnice u dobrom formatu
        //vraca true ako jeste, false ako nije
        private bool CheckPoslInput(string s)
        {
            Regex regex = new Regex(@"2\d\d\d00");
            return regex.IsMatch(s);

        }

        //Metoda koja proverava da li postoji uneta poslovnica u bazi.
        //Ako postoji vraca true ako ne postoji vraca false
        private bool ProveraPosl()
        {
            //ako je null ne postoji posl
            string? posl = null;

            try
            {

                string query = $"select POSL FROM BI_POSL WHERE POSL = {txtPosl.Text} and AKTIVNA = 'D'";

                SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();
                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    posl = reader.GetValue(0).ToString();

                }
                else
                {
                    posl = null;

                }

                connection.Close();
            }
            catch (Exception e)
            {
                posl = null;
                Shared.LogUser("\n" + e.ToString() + "\n");
                new MessageBoxCustom(e.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }

            if (posl != null)
                return true;
            else
                return false;

        }


        //Method for checking if the field is empty
        //Returns true if its not empty, false if it is empty
        private bool EmptyFieldValidation(string input)
        {
            if (input == null || input == string.Empty)
                return false;
            return true;
        }


        //Gets an ip adress in string form ("192.162.1.46") 
        //returns true or false based on weather it can see it
        private bool PingServer(string s)
        {
            //Za proveru da li imamo konekciju ka serveru
            Ping x = new Ping();
            PingReply reply = x.Send(IPAddress.Parse(s));

            Thread.Sleep(500);

            if (reply.Status == IPStatus.Success)
                return true;
            else
                return false;

        }



        //Metoda koja proverava da li postoji unet artikal u bazi.
        //Ako postoji vraca true ako ne postoji vraca false
        private bool ProveraArtikla()
        {
            //ako je null ne postoji posl
            string? artikal = null;

            try
            {

                string query = $"select ARTIKEL from BI_ARTIK WHERE ARTIKEL = '{txtIdArtikla.Text}'";

                SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();
                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    artikal = reader.GetValue(0).ToString();

                }
                else
                {
                    artikal = null;

                }

                connection.Close();
            }
            catch (Exception ex)
            {
                artikal = null;
                Shared.LogUser("\n" + ex.ToString() +"\n");
                new MessageBoxCustom(ex.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }

            if (artikal != null)
                return true;
            else
                return false;

        }



        //Metoda koja proverava da li postoji unet EAN u bazi.
        //Ako postoji vraca true ako ne postoji vraca false
        private bool ProveraEan()
        {
            //ako je null ne postoji posl
            string? ean = null;

            try
            {

                string query = $"select EAN_KODA from BI_KODE WHERE EAN_KODA = '{txtEanArtikla.Text}'";

                SqlConnection connection = new SqlConnection(connectionString);
                connection.Open();
                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    ean = reader.GetValue(0).ToString();

                }
                else
                {
                    ean = null;

                }

                connection.Close();
            }
            catch (Exception ex)
            {
                ean = null;
                Shared.LogUser("\n" + ex.ToString() + "\n");
                new MessageBoxCustom(ex.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }

            if (ean != null)
                return true;
            else
                return false;
        }



        //Metoda koja vraca ID artikla po unetom EAN-u
        //Ako ne uspe da pronadje ID onda vraca NULL
        private async Task<string?> GetIdFromEan()
        {
            string? id = null;

            try
            {
                string query = $"select ARTIKEL from BI_KODE WHERE EAN_KODA = '{txtEanArtikla.Text}'";

                SqlConnection connection = await Task.Run(() => new SqlConnection(connectionString));
                connection.Open();
                SqlCommand cmd = await Task.Run(() => new SqlCommand(query, connection));
                SqlDataReader reader = await Task.Run(() => cmd.ExecuteReader());

                if (reader.Read())
                {
                    id = reader.GetValue(0).ToString();

                }
                else
                {
                    id = null;

                }

                connection.Close();
            }
            catch (Exception ex)
            {
                Shared.LogUser($"\n{ex}\n");
                new MessageBoxCustom(ex.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();

            }


            return id;

        }



        //Poziv za POST metodu na API 192.168.1.43:8585
        //Prosledjuje se datum u formatu 2023-03-19
        //Poslovnica 273600
        //Id artikla 95741
        private async Task ApiPostMethod(string datum, string posl, string idArtikla)
        {

            string uri = "http://192.168.1.43:8585/api/price-lists?price_date=" + datum + "&store_id=" + posl + "&includeInsurances=false&includeAllPriceTypes=false&includeCurrentPurchasePrice=false&includePromotions=true&includePromotionInfo=false";

            string body = "[" + $"\"{idArtikla}\"" + "]";

            

            try
            {

                dynamic array;

                //HttpClient client = new HttpClient();

                using (client = new(handler,false))
                {
                    var endpoing = new Uri(uri);


                    var content = new StringContent($"{body}", Encoding.UTF8, "application/json");


                    var response = await Task.Run(() => client.PostAsync(endpoing, content).Result);

                    //Ako smo dobili status kod 200 OK od API-a
                    if(((int)response.StatusCode) == 200)
                    {

                        var result = response.Content.ReadAsStringAsync().Result;



#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                        array = JsonConvert.DeserializeObject(result);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

                        if (array == null)
                            throw new Exception();

                        foreach (var item in array)
                        {
                            //Prikazujemo trenutnu cenu
                            txtTrenutnaCena.Text = item.regular_price.retail_price;
                            //Prikazujemo Normalnu cenu
                            txtNormalnaCena.Text = item.default_price.retail_price;



                            //Proveravamo da li neka akcija snizava cenu
                            if (item.promotion_prices != null)
                            {
                                //Prolazimo kroz akciju
                                var promo = item.promotion_prices;

                                //Prolaz kroz uvod akcije
                                foreach (var p in promo)
                                {
                                    txtDatumOdAkcije.Text = p.price_valid_from.ToString().Split(' ')[0];

                                    txtDatumDoAkcije.Text = p.price_valid_to.ToString().Split(' ')[0];

                                   
                                    //prolaz kroz detalje akcije
                                    var def = p.promotion_definitions;

                                    foreach (var v in def)
                                    {
                                        txtNazivAkcije.Text = v.label;

                                        txtIdAkcije.Text = v.id;

                                        //prolaz kroz model
                                        //var model = v.model;

                                        //foreach(var m in model)
                                        //{
                                        //    txtModelAkcije.Text = m.ToString().Split(':')[1].Replace("\"", "").Trim();
                                        //    break;
                                        //}//prolaz kroz model

                                        break;
                                    }//prolaz kroz detalje akcije

                                    break;

                                    //BREAKOVI SU DODATI DA MI VRATI PRVU AKCIJU JER BI TA TREBALA DA JE ONA KOJA SNIZAVA

                                }//prolaz kroz akciju



                            }//provera da li ima akcija koja snizava cenu


                        }//prolaz kroz rezultat response-a


                    }
                    else//NISMO DOBILI STATUS CODE 200
                    {
                        Shared.LogUser($"NISMO DOBILI STATUS CODE 200!!!\t\tSTATUS CODE: {(int)response.StatusCode}");
                        new MessageBoxCustom($"Nismo uspeli da pristupimo API-u.\nStatus Code: {(int)response.StatusCode}", MessageType.Error).ShowDialog();
                    }


                    //client.Dispose();
                }


                



            }
            catch (Exception ex)
            {

                Shared.LogUser("\n" + ex.ToString() + "\n");
                new MessageBoxCustom(ex.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }



        }



        //Runs the Query that is passed through and shows data in DataGrid
        public async Task GetDataAsync(string query)
        {

            try
            {

                SqlConnection sqlConnection = await Task.Run(() => new SqlConnection(connectionString));

                SqlCommand cmd = await Task.Run(() => new SqlCommand(query, sqlConnection));

                cmd.CommandTimeout = 90;


                SqlDataAdapter adapter = await Task.Run(() => new SqlDataAdapter(cmd));

                DataTable dataTable = new DataTable();

                await Task.Run(() =>
                {
                    using (adapter)
                    {
                        adapter.Fill(dataTable);
                    }
                });

                dgSpecialPrices.ItemsSource = dataTable.DefaultView;

            }
            catch (Exception ex)
            {

                Shared.LogUser($"\n{ex}\n");
                new MessageBoxCustom(ex.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }
        }



        //Prima prosledjen ID artikla i vraca prodajnu zalihu za taj artikal
        //ako vrati null to znaci da nije uspeo da dobije vrednost
        private async Task<string?> GetZaliha(string idArtikla)
        {
            string poslZal = txtPosl.Text.Substring(0, txtPosl.Text.Length -2) + "10";

            string query = $"select KO_PROD_ZAL from BI_POSAR where posl = {poslZal} and artikel = '{idArtikla}'";

            string? zaliha = null;


            try
            {

                SqlConnection conn = await Task.Run(() => new SqlConnection(connectionString));

                conn.Open();

                SqlCommand cmd = await Task.Run(() => new SqlCommand(query, conn));

                SqlDataReader reader = await Task.Run(() =>  cmd.ExecuteReader());

                if(reader.Read())
                {
                    zaliha = reader.GetValue(0).ToString();
                }
                else
                {
                    throw new Exception("Didnt Get data");
                }

            }
            catch (Exception ex)
            {

                Shared.LogUser("\n" + ex.ToString());
                new MessageBoxCustom(ex.ToString(), MessageType.Error, MessageButtons.Ok, "GRESKA", 650, 450, 10).ShowDialog();
            }

            return zaliha;
        }









        #endregion







    }//Class
}
