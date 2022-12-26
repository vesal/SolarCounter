namespace Teho;

/// @author Vesa Lapplainen
/// @version 26.12.2022
/// 
/// <summary>
/// Ohjelmalla arvoidaan aurinkopaneelien tuottoa suhteessa omaan kulutukseen.
/// Ohjelmaan voidaan määritellä tuinti/kuukausikohtaisesti kuinka moni paneeli
/// on käytettävissä minäkin tuntina.
/// </summary>
public static class Teho
{                                          //   0      1     2     3      4
    private static readonly double[] Tehot = { 8600, 8820, 10100, 20000, 6000 }; // tähän tehoja joille lasketaan
    private const int TehoIndex = 2; // tähän mitä tehoa käytetään
    private const double LaskettuTeho = 8600; // tähän millä teholle tiedostot tehty.

    private static readonly double  TehoKerroin = Tehot[TehoIndex] / LaskettuTeho; 
#if true  // true:lla oetuksen kaikki päivät ja tunnit
    private const int Alkuh = 0;
    private const int Loppuh = 23;
    private const int Alkukk = 1;
    private const int Loppukk = 12;
    private const int Alkupp = 1;
    private const int Loppupp = 32; // jos kuun mukaan, laita yli 31
    private static readonly bool PrintHh = 1 <= 0; // printataanko tunnit erikseen
    private static readonly bool PrintPp = 1 <= 0; // printataanko päivät erikseen
#else // falsella valitut osat   
    private const int alkuh = 10;
    private const int loppuh = 14;  
    private const int alkukk = 6;
    private const int loppukk = 6;        // jos kuun mukaan, laita yli 12
    private const int alkupp = 1;
    private const int loppupp = 32;
    private static readonly bool PrintHH = 1 <= 0;  // printataanko tunnit erikseen
    private static readonly bool PrintPP = 1 <= 0;  // printataanko päivät erikseen
#endif    

    // Varjokerroin 1 = käytetään aina laskennallsita tehoa varjoista välittämättä
    // Varjokerroin 0 = käytetään aina varjotaulukoiden mukaista kerrointa tunnille
    // Varjokerroin 0.5 = jos tunnin teho > 0.5*max ko tunnilta, niin käytetään
    //                    varjotaulukoiden mukaista kerrointa tunnille, muuten
    //                    tehoa sellaisenaan (koska ei tule varjoja)
    private const double Varjokerroin = 0.3;
    private const int KertoimetIndex = 1; // 0=pohjoinen, 1=etäläinen, mitä alempana olevaa paneeliluetteloa käytetään

    // Tehotiedosto on sivulta https://re.jrc.ec.europa.eu/pvg_tools/en/tools.html
    // valinnalla "Hourly data" tuotettu csv-tiedosto
    private const string TehoTiedosto = @"E:\oma\talo\talo\varjot\Timeseries_62.300_25.733_SA_8kWp_crystSi_14_22deg_54deg_2005_2016.csv";

    // private static readonly string tehoTiedosto = @"E:\oma\talo\talo\varjot\Timeseries_62.300_25.733_SA_8kWp_crystSi_14_22deg_54deg_2005_2016_horizont.csv";
    // private static readonly string tehoTiedosto = @"E:\oma\talo\talo\varjot\Timeseries_62.300_25.733_SA_8kWp_crystSi_14_22deg_0deg_2005_2016.csv";
    
    // Kulutiedoston on esimerkiksi fingridistä ottamalla DataHubista (https://oma.datahub.fi/)
    //  Energiaraportointi ja sieltä lataa tiedot.  Tiedoston pitäisi olla muotoa:
    //   2019-02-28T22:00:00Z	2.58
    //   2019-02-28T23:00:00Z	2.24
    private const string KuluTiedosto = @"E:\oma\talo\talo\varjot\aurinko\fingrid.csv";

    private const int Paneeleja = 20;
    // Montako paneelia käytössä 20:sta minäkin kellonaikana kunkin kuun 1. päivä
    private static readonly string[] SkertoimetP = { // pohjoinen sijoittelu
        // 0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17 18 19 20 21 22 23
        "  0  0  0  0  0  0  0  0 20 20 10 17  7 20 20 20 16  0  0  0  0  0  0  0 ", // 1.
        "  0  0  0  0  0  0  0  0 20 20 20 17  7 20 20 20 16  0  0  0  0  0  0  0 ", // 2.
        "  0  0  0  0  0  0  0  0 20 20 20 17  7 20 20 20 16  0  0  0  0  0  0  0 ", // 3.
        "  0  0  0  0  0  0  0  0 20 20 20 17 17 20 20 20 14  0  0  0  0  0  0  0 ", // 4.
        "  0  0  0  0  0  0  0  0 20 20 20 20 20 20 20 20 12  1  0  0  0  0  0  0 ", // 5.
        "  0  0  0  0  0  0  0  0 20 20 20 20 20 20 20 18 10  0  0  0  0  0  0  0 ", // 6.
        "  0  0  0  0  0  0  0  0 20 20 20 20 20 20 20 18 12  0  0  0  0  0  0  0 ", // 7.
        "  0  0  0  0  0  0  0  0 20 20 20 20 20 20 20 18 12  1  0  0  0  0  0  0 ", // 8.
        "  0  0  0  0  0  0  0  0 20 20 20 20 16 20 20 19 12  2  0  0  0  0  0  0 ", // 9.
        "  0  0  0  0  0  0  0  0 20 20 20  8 16 20 20 20 12  0  0  0  0  0  0  0 ", //10.
        "  0  0  0  0  0  0  0  0 20 20 20  8 16 20 20 20 12  0  0  0  0  0  0  0 ", //11.
        "  0  0  0  0  0  0  0  0 20 20 20  8 16 20 20 20 12  0  0  0  0  0  0  0 "  //12.
    };

    private static readonly string[] SkertoimetE = { // eteläinen sijoittelu
        // 0  1  2  3  4  5  6  7  8  9 10 11 12 13 14 15 16 17 18 19 20 21 22 23
        "  0  0  0  0  0  0  0  0 20 20 10 20 10  7 17 20 20 14  0  0  0  0  0  0 ", // 1.
        "  0  0  0  0  0  0  0  0 20 20 20 20 10  7 17 20 20 14  0  0  0  0  0  0 ", // 2.
        "  0  0  0  0  0  0  0  0 20 20 20 20 10  7 17 20 20 14  0  0  0  0  0  0 ", // 3.
        "  0  0  0  0  0  0  0  0 20 20 20 20 14 15 20 20 20 13  0  0  0  0  0  0 ", // 4.
        "  0  0  0  0  0  0  0  0 20 20 20 20 15 14 20 20 20 20  5  0  0  0  0  0 ", // 5.
        "  0  0  0  0  0  0  0  0 20 20 20 20 18 18 20 20 20 18  2  0  0  0  0  0 ", // 6.
        "  0  0  0  0  0  0  0  0 20 20 20 20 19 19 19 20 20 20  2  0  0  0  0  0 ", // 7.
        "  0  0  0  0  0  0  0  0 20 20 20 20 18 17 20 20 20 20  5  0  0  0  0  0 ", // 8.
        "  0  0  0  0  0  0  0  0 20 20 20 20  7 11 20 20 20 14  0  0  0  0  0  0 ", // 9.
        "  0  0  0  0  0  0  0  0 20 20 20 19  5 12 20 20 20 14  0  0  0  0  0  0 ", //10.
        "  0  0  0  0  0  0  0  0 10 10 10 16  4 11 18 20 20 14  0  0  0  0  0  0 ", //11.
        "  0  0  0  0  0  0  0  0 10 10 10 16  4  9 18 20 20 13  0  0  0  0  0  0 "  //12.
    };

    private static readonly string[][] Skertoimet = {SkertoimetP, SkertoimetE};
    private static readonly double[,] Kertoimet = new double[12, 24];

    
    /// <summary>
    /// Tulostetaan tehoja, tuottoja ja kulutuksia
    /// </summary>
    public static void Main()
    {
        if (PrintHh) Tulos.Jakaja = 1;
        for (var r = 0; r < 12; r++)  // käydään kaikki kerroitaulukon rivit läpi
        { // ja muutetaan merkkijono taulukon riviksi
            var s = Skertoimet[KertoimetIndex][r];
            var palat = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < 24; i++) Kertoimet[r, i] = int.Parse(palat[i]);
        }

        var rivit = File.ReadAllLines(TehoTiedosto);
        var rivitKulu = File.ReadAllLines(KuluTiedosto);
        Console.WriteLine(rivit.Length);

        var tunnit = (from rivi in rivit where rivi.StartsWith("20") select new Tunti(rivi)).ToList();
        var kulutus = rivitKulu.Select(rivi => new Kulutus(rivi)).ToList();

        Console.WriteLine(tunnit.Count);
        var avg = tunnit.Average(t => t.P);
        var max = tunnit.Max(t => t.P);
        var avgG = tunnit.Average(t => t.G);
        var maxG = tunnit.Max(t => t.G);
        var minK = kulutus.Min(k => k.P);
        var maxK = kulutus.Max(k => k.P);
        var vuosia = tunnit.Count(t => t.Pp == 1 && t.Kk == 1 && t.Hh == 1);
        Console.WriteLine($"{vuosia} {avg,4:#.##} {max} G: {avgG,4:#.##} {maxG} K: {minK} {maxK}");

        var avg01 = tunnit.Where(t => t.Kk == 1).Average(t => t.P);
        var max01 = tunnit.Where(t => t.Kk == 1).Max(t => t.P);
        Console.WriteLine($"01: {avg01,4:#.##} {max01}");
        Console.WriteLine($"t: {tunnit.Sum(t => t.P) / 1000.0 / vuosia,4:#}");
        /*
        for (int kk = 1; kk <= 12; kk++)
            Console.WriteLine($"{kk,2} {tunnit.Where(t => t.Kk == kk).Sum(t => t.P)/1000.0/vuosia,4:#}");
        */
        var vt = from t in tunnit group t by t.Vv;
        var vuodet = vt.ToList();
        for (var kk = 1; kk <= 12; kk++)
        {
            Console.Write($"{kk,2} ");
            foreach (var v in vuodet)
            {
                Console.Write($"{v.Where(t => t.Kk == kk).Sum(t => t.P) / 1000.0,4:#} ");
            }

            Console.WriteLine();
        }


        // for (int kk = 1; kk <= 12; kk++)
        for (var kk = 6; kk <= 6; kk++)
        {
            TulostaTuntiTehot(tunnit, kk, 22, 2);
            TulostaTuntiTehot(kulutus, kk, 22, 0);
        }

        TulostaTuotot(tunnit, kulutus, 2);

        Console.WriteLine("\nMontako tuntia vuodessa tietty W/m2:");
        for (var w = -100; w < 1000; w += 100)
        {
            var yli = tunnit.Count(t => w < t.G && t.G <= w + 100);
            Console.WriteLine($"{w,4} -{w + 100,4}: {yli / vuosia,5:0}");
        }

    }

    
    /// <summary>
    /// Tuostetaan tietyn päivän tuntien tehot 
    /// </summary>
    /// <param name="tunnit">lista tehoista kaikilta päiväiltä</param>
    /// <param name="kk">käsiteltävä kuukausi</param>
    /// <param name="pp">käsiteltävä päivä</param>
    /// <param name="dh">paljonko tunnit-listan aika heittää kulutuksen tunneista</param>
    /// <typeparam name="T">Tunti tai Kulutus</typeparam>
    private static void TulostaTuntiTehot<T>(IReadOnlyList<T> tunnit, int kk, int pp, int dh) where T : ITehoTunti
    {
        Console.WriteLine($"\n{pp,2:00}.{kk,2:00}");
        var kulukkpp = tunnit.Where(t => t.Kk == kk && t.Pp == pp).ToList();

        Console.Write($"{"h:",3} ");
        foreach (var t in kulukkpp.Where(t => t.Hh == 0))
            Console.Write($"{t.Vv,5}  ");
        Console.WriteLine($"{"Avg",8}");
        
        if (tunnit[0] is Tunti)  // jos käsitellään tehotunteja
        {
            Console.Write($"{" ",4}");
            foreach (var t in kulukkpp.Where(t => t.Hh == 10))
                Console.Write($"{((t as Tunti)!).G,5:0}  ");
            var ka = kulukkpp.Where(t => t.Hh == 10).Average(t => ((t as Tunti)!).G);
            Console.WriteLine($"{ka,8:0} {"W/m2",4} klo 12");
        }

        for (var h = 0; h < 24; h++)
        {
            Console.Write($"{h + dh,2:00}: ");
            foreach (var t in kulukkpp.Where(t => t.Hh == h))
                Console.Write($"{t.P,5:0}  ");

            var ka = kulukkpp.Where(t => t.Hh == h).Average(t => t.P);
            Console.WriteLine($"{ka,8:0}");
        }

    }


    /// <summary>
    /// Lasketaan kuinka paljon tehosta saadaan itse kulutettua.  Kunkin tunnin
    /// kohdalla käydään läpi kaikki tunnit-listan tunnit eri vuosilta mitä löytyy.
    /// Eri vuosien ko tuntien maksimia käyettään arvioimaan milloin on pilvinen
    /// päivä jolloin ei tarvitse huomioida varjoja. Varjoisuuden määrää varjokerroin (1=ei varjoja).
    /// Kun ko tunnin teho ko vuodelta on saatu selville, lasketaan oma tuotto suhteessa
    /// oman käytön ko tunnin keskiarvoon. 
    /// </summary>
    /// <param name="tunnit">lista tuntitehoista</param>
    /// <param name="kulutus">lista omasta kulutuksesta</param>
    /// <param name="kk">käsiteltävä kuukausi</param>
    /// <param name="pp">käsiteltävä päivä</param>
    /// <param name="dh">paljonko tunnit-listan aika heittää kulutuksen tunneista</param>
    /// <param name="printHh">tuostetaanko tutnidataa</param>
    /// <param name="tuntiTuotot">taulukko jonne tallennetaan tuntisummia</param>
    /// <returns>päivän tuotto halutulta kk ja pp</returns>
    private static Tulos LaskeTuottoKertoimilla(IEnumerable<Tunti> tunnit, IEnumerable<Kulutus> kulutus, int kk, int pp, int dh, bool printHh,  Tulos[] tuntiTuotot)
    {
        if (printHh) Console.WriteLine($"\n{pp,2:00}.{kk,2:00}");
        var tunnitkkpp = tunnit.Where(t => t.Kk == kk && t.Pp == pp /*&& t.Vv == 2006*/).ToList();
        var kulukkpp = kulutus.Where(t => t.Kk == kk && t.Pp == pp).ToList();

        Tulos result = default;
        for (var h = Alkuh; h <= Loppuh; h++)
        {
            var ka = kulukkpp.Where(t => t.Hh == h).Average(t => t.P);
            result.Kulutus += ka;
            tuntiTuotot[h].Kulutus += ka;
            if (h - dh < 0) // jos ei ole näitä tunteja
            {
                result.Ali += ka;
                tuntiTuotot[h].Ali += ka;
                continue;
            }
            
            var lkm = 0;
            Tulos sr = default;
            var tunnitkkpphh = tunnitkkpp.Where(t => t.Hh == h-dh).ToList();
            var max = tunnitkkpphh.Max(t => t.P);
            if (printHh) Console.Write($"{h,2:00}: {ka, 4:0} {max * Varjokerroin,4:0}, ");
            var riveja = 0;
            foreach (var t in tunnitkkpphh)
            {
                lkm++;
                var p = t.P;
                if (p > max * Varjokerroin) p *= 1.0 * Kertoimet[kk - 1, h] / Paneeleja;
                p *= TehoKerroin;

                // ReSharper disable once InconsistentNaming
                var p_ka = p - ka;
                Tulos r = default;
                r.P = p;
                if (p_ka > 0)
                {
                    r.Kaytto = ka;
                    r.Yli = p_ka;
                }
                else
                {
                    r.Kaytto = p;
                    r.Ali = -p_ka;
                }

                sr += r;
                if (!printHh) continue;
                
                if (riveja % 4 == 0  && riveja > 0) Console.Write($"\n {" ",14}");
                Console.Write($"{t.P,4:0} => {p,4:0} {r.Kaytto, 4:0} {r.Yli, 4:0},  ");
                riveja++;
            }

            var tr = sr / lkm;
            result += tr;
            
            if (printHh) Console.WriteLine($" = {tr.P ,4:0} {tr.Kaytto, 4:0} {tr.Yli, 4:0}");
            
            tuntiTuotot[h] += tr;
        }

        return result;
    }

    
    /// <summary>
    /// Lasketaan keskiarvoilla kuinka paljon tehosta saadaan itse kulutettua
    /// </summary>
    /// <param name="tunnit">lista tuntitehoista</param>
    /// <param name="kulutus">lista omasta kulutuksesta</param>
    /// <param name="kk">käsiteltävä kuukausi</param>
    /// <param name="pp">käsiteltävä päivä</param>
    /// <param name="dh">paljonko tunnit-listan aika heittää kulutuksen tunneista</param>
    /// <param name="printHh">tuostetaanko tutnidataa</param>
    /// <param name="tuntiTuotot">taulukko jonne tallennetaan tuntisummia</param>
    /// <returns>päivän tuotto halutulta kk ja pp</returns>
    private static Tulos LaskeTuotto(IEnumerable<Tunti> tunnit, IEnumerable<Kulutus> kulutus, int kk, int pp, int dh, bool printHh, IList<Tulos> tuntiTuotot)
    {
        if (printHh) Console.WriteLine($"\n{pp,2:00}.{kk,2:00}");
        var tunnitkkpp = tunnit.Where(t => t.Kk == kk && t.Pp == pp /*&& t.Vv == 2006*/).ToList();
        var kulukkpp = kulutus.Where(t => t.Kk == kk && t.Pp == pp).ToList();

        Tulos result = default;
        for (var h = Alkuh; h <= Loppuh; h++)
        {
            var pa = h-dh < 0 ? 0: tunnitkkpp.Where(t => t.Hh == h-dh).Average(t => t.P * TehoKerroin);
            var ka = kulukkpp.Where(t => t.Hh == h).Average(t => t.P);
            if (printHh) Console.WriteLine($"{h,2:00}: {pa,8:0} {ka,8:0}");

            Tulos tr = default;

            tr.P = pa;
            tr.Kulutus = ka;
            if (pa > ka)
            {
                tr.Kaytto = ka;
                tr.Yli = (pa - ka);
            }
            else
            {
                tr.Kaytto = pa;
                tr.Ali = (ka - pa);
            }
            result += tr;
            tuntiTuotot[h] += tr;
        }

        return result;
    }

    
    private static readonly int[,] Kuupaivat = {
        { 0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 },
        { 0, 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 }
    };

    
    /// <summary>
    /// Tulostetaan kuukaisutuotit ja tuntituotot 
    /// </summary>
    /// <param name="tunnit">lista tuntitehoista</param>
    /// <param name="kulutus">lista omasta kulutuksesta</param>
    /// <param name="dh">paljonko tunnit-listan aika heittää kulutuksen tunneista</param>
    private static void TulostaTuotot(List<Tunti> tunnit, List<Kulutus> kulutus, int dh)
    {
        Tulos vvTulos = default;
        Tulos vvTulosk = default;
        var tuntiTuotot = new Tulos[24];
        var tuntiTuototk = new Tulos[24];
        Console.WriteLine($"{Tehot[TehoIndex]} Wp, varjokerroin: {Varjokerroin}");
        Console.WriteLine($"{"",2}" + Tulos.Otsikko(false) + "" + Tulos.Otsikko());
        for (var kk = Alkukk; kk <= Loppukk; kk++)
        {
            Tulos kktulos = default; 
            Tulos kktulosk = default;
            var loppup = Math.Min(Kuupaivat[0, kk], Loppupp);
            
            for (var pp = Alkupp; pp <=  loppup; pp++)
            {
                var r = LaskeTuotto(tunnit, kulutus, kk, pp, dh, PrintHh, tuntiTuotot);
                var rk = LaskeTuottoKertoimilla(tunnit, kulutus, kk, pp, dh, PrintHh, tuntiTuototk);
                if (PrintPp) Console.WriteLine($"{pp,2} " + r + " " + rk);
                kktulos += r;
                kktulosk += rk;
            }
            Console.WriteLine($"{kk,3}" + kktulos.ToStr() + " " + kktulosk);
            vvTulos += kktulos;
            vvTulosk += kktulosk;
        }
        Console.WriteLine($"{"yht",3}" + vvTulos.ToStr() + " " + vvTulosk);
        Console.WriteLine();
        vvTulos = default;
        vvTulosk = default;
        for (var h = 0; h < tuntiTuotot.Length; h++)
        {
            Console.WriteLine($"{h,2} " + tuntiTuotot[h].ToStr() + " " + tuntiTuototk[h]);
            vvTulos += tuntiTuotot[h];
            vvTulosk += tuntiTuototk[h];
        }
        Console.WriteLine($"{"yht",3}" + vvTulos.ToStr() + " " + vvTulosk);
    }
    
}


public interface ITehoTunti
{
    public int Vv { get; set; }
    public int Kk { get; set; }
    public int Pp { get; set; }
    public int Hh { get; set; }
    public double P { get; set; }
}


public class Tunti: ITehoTunti
{
    public int Vv { get; set; }
    public int Kk { get; set; }
    public int Pp { get; set; }
    public int Hh { get; set; }
    public double P { get; set; }
    public double G { get; set; }
    
    
    public Tunti(string s)
    {
        // Vv  KkPp Hh   P      G
        // 20050101:1011,26.06,11.65,4.67,-2.98,3.03,0.0
        var palat = s.Split(",:".ToCharArray());
        Vv = int.Parse(palat[0][..4]);
        Kk = int.Parse(palat[0][4..6]);
        Pp = int.Parse(palat[0][6..8]);    
        Hh = int.Parse(palat[1][..2]);
        P = double.Parse(palat[2]);    
        G = double.Parse(palat[3]);    
    }
}


public class Kulutus: ITehoTunti
{
    public int Vv { get; set; }
    public int Kk { get; set; }
    public int Pp { get; set; }
    public int Hh { get; set; }
    public double P { get; set; }
    
    
    public Kulutus(string s)
    {
        // Vv   Kk Pp Hh          P     
        // 0123456789012
        // 2022-12-07T16:00:00Z	2.64
        var palat = s.Split("\t".ToCharArray());
        Vv = int.Parse(palat[0][..4]);
        Kk = int.Parse(palat[0][5..7]);
        Pp = int.Parse(palat[0][8..10]);    
        Hh = int.Parse(palat[0][11..13]);    
        var dt = DateTime.Parse(palat[0]);
        var dl = dt.ToLocalTime();
        Vv = dl.Year;
        Kk = dl.Month;
        Pp = dl.Day;
        Hh = dl.Hour;
        P = double.Parse(palat[1])*1000;    
    }
    
    
    public override string ToString()
    {
        return $"{Vv}{Kk,2:00}{Pp,2:00}:{Hh,2:00} {P,4:0.00}";
    }
}


public struct Tulos
{
    public double P = 0;
    public double Kaytto = 0;
    public double Yli = 0;
    public double Ali = 0;
    public double Kulutus = 0;
    public static int Jakaja = 1000; 

    private Tulos (double p=0, double kaytto=0, double yli=0, double ali=0, double kulutus=0)
    {
        P = p;
        Kaytto = kaytto;
        Yli = yli;
        Ali = ali;
        Kulutus = kulutus;
    }
    
    
    public static Tulos operator +(Tulos t1, Tulos t2)
    {
        return new Tulos(t1.P + t2.P, t1.Kaytto + t2.Kaytto, t1.Yli + t2.Yli, t1.Ali + t2.Ali, t1.Kulutus+t2.Kulutus);
    }

    
    public static Tulos operator /(Tulos t, double div)
    {
        return new Tulos(t.P/div, t.Kaytto/div, t.Yli/div, t.Ali/div, t.Kulutus/div);
    }

    
    public override string ToString()
    {
        return $"{P / Jakaja,5:0} {Kaytto / Jakaja,5:0} {Yli / Jakaja,5:0} {Ali / Jakaja,5:0} {Kulutus/Jakaja,5:0}";
    }

    
    public string ToStr() // ilman kulutusta
    {
        return $"{P / Jakaja,5:0} {Kaytto / Jakaja,5:0} {Yli / Jakaja,5:0} {Ali / Jakaja,5:0}";
    }

    
    public static string Otsikko(bool showKulu = true)
    {
        var result = $"{"p", 5} {"kaytto", 5} {"yli", 5} {"ali", 5}";
        if (showKulu) result += $" {"kulu",5}";
        return result;
    }
}


