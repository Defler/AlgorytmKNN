using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace MIW2
{
    public class AustralianConfig
    {
        public string nazwa { get; set; }
        public int iloscKolumn { get; set; }
        public int iloscWierszy { get; set; }
        public string[] rodzajKolumny { get; set; }
        public char separator { get; set; }
        public string[] klasyDecyzyjne { get; set; }
        public string[] tablicaMin { get; set; }
        public string[] tablicaMax { get; set; }
        public string[] tablicaZnakow { get; set; }
    }

    public class IndeksIDystans : IComparable<IndeksIDystans>
    {
        public int indeks; 
        public double dystans;  

        public int CompareTo(IndeksIDystans other)
        {
            if (this.dystans < other.dystans) return -1;
            else if (this.dystans > other.dystans) return +1;
            else return 0;
        }
    }
    class Program
    {
        public static void UstawienieDataSetu(DataTable dane, string[] rodzajKolumny, int iloscKolumn, int iloscWierszy, bool czySuroweDane)
        {
            DataColumn column;
            DataRow row;

            for (int i = 0; i < iloscKolumn - 1; i++)
            {
                column = new DataColumn();
                if (czySuroweDane)
                {
                    if (rodzajKolumny[i] == "liczba")
                        column.DataType = System.Type.GetType("System.Double");
                    else
                        column.DataType = System.Type.GetType("System.String");
                }
                else
                {
                    column.DataType = System.Type.GetType("System.Double");
                }

                column.ColumnName = $"kol{i}";
                dane.Columns.Add(column);
            }
            //ostatnia kolumna jest zawsze stringiem, bo to klasa dec
            column = new DataColumn();
            column.DataType = System.Type.GetType("System.String");
            column.ColumnName = $"kol{iloscKolumn - 1}";
            dane.Columns.Add(column);

            for (int i = 0; i < iloscWierszy; i++)
            {
                row = dane.NewRow();
                dane.Rows.Add(row);
            }
        }

        public static double DystansEuklides(double[] probkaTestowa, DataRow dane)
        {
            double sum = 0.0;
            for (int i = 0; i < probkaTestowa.Length; ++i)
                sum += (probkaTestowa[i] - dane.Field<double>(i)) * (probkaTestowa[i] - dane.Field<double>(i));
            return Math.Sqrt(sum);
        }

        public static double DystansManhattan(double[] probkaTestowa, DataRow dane)
        {
            double sum = 0.0;
            for (int i = 0; i < probkaTestowa.Length; ++i)
                sum += Math.Abs(probkaTestowa[i] - dane.Field<double>(i));
            return sum;
        }

        public static double DystansCzernobyl(double[] probkaTestowa, DataRow dane)
        {
            double max = 0.0;
            for(int i=0; i<probkaTestowa.Length; ++i)
            {
                double tmp = Math.Abs(probkaTestowa[i] - dane.Field<double>(i));
                if (tmp > max)
                    max = tmp;
            }
            return max;
        }

        public static double DystansLogarytm(double[] probkaTestowa, DataRow dane)
        {
            double sum = 0.0;
            for (int i = 0; i < probkaTestowa.Length; ++i)
                sum += Math.Abs(Math.Log(probkaTestowa[i]) - Math.Log(dane.Field<double>(i)));
            return sum;
        }

        public static double DystansMinkowsky(double[] probkaTestowa, DataRow dane)
        {
            int p = 2;
            double sum = 0.0;
            for (int i = 0; i < probkaTestowa.Length; ++i)
                sum += Math.Pow((Math.Abs(probkaTestowa[i] - dane.Field<double>(i))),p);
            return Math.Pow(sum, 1/p);

        }

        public static string KNN2(string[] klasyDecyzyjne, DataTable daneLiczbowe, IndeksIDystans[] tabDystanse, int k)
        {
            double[] tabSumKlasDec = new double[klasyDecyzyjne.Length];

            for (int i = 0; i < tabSumKlasDec.Length; i++)
            {
                int licznik = 0;
                int indeks = 0;
                while (licznik < k)
                {
                    if (daneLiczbowe.Rows[tabDystanse[indeks].indeks].Field<string>(daneLiczbowe.Columns.Count - 1) == klasyDecyzyjne[i])
                    {
                        tabSumKlasDec[i] += tabDystanse[indeks].dystans;
                        licznik++;
                    }
                    indeks++;
                }
            }

            int indeksNajmKlas = 0;
            for (int i = 0; i < tabSumKlasDec.Length; i++)
            {
                if (tabSumKlasDec[i] < tabSumKlasDec[indeksNajmKlas])
                    indeksNajmKlas = i;
            }

            string decyzja = klasyDecyzyjne[indeksNajmKlas];
            //Console.WriteLine($"\nOceniam te probke jako {decyzja}!!!");
            return decyzja;
        }

        public static string KNN1(string[] klasyDecyzyjne, IndeksIDystans[] tabDystanse, DataTable daneLiczbowe, int k)
        {
            //tworzenie tablicy tablicy z licznikami wystapien klas decyzyjnych, z indekoswaniem zgodnym z tablica z configa
            int indeksBadany;
            string klasaBadana;
            int[] licznikiKlasDec = new int[klasyDecyzyjne.Length];
            for (int i = 0; i < licznikiKlasDec.Length; i++)
                licznikiKlasDec[i] = 0;
            for (int i = 0; i < k; i++)
            {
                indeksBadany = tabDystanse[i].indeks;
                klasaBadana = daneLiczbowe.Rows[indeksBadany].Field<string>(daneLiczbowe.Columns.Count - 1);
                for (int j = 0; j < licznikiKlasDec.Length; j++)
                    if (klasaBadana == klasyDecyzyjne[j])
                        licznikiKlasDec[j]++;
            }

            //okreslanie indeksu z wynikiem
            int indeksNajwKlas = 0;
            for (int i = 0; i < licznikiKlasDec.Length; i++)
                if (licznikiKlasDec[indeksNajwKlas] < licznikiKlasDec[i])
                    indeksNajwKlas = i;
            string decyzja = klasyDecyzyjne[indeksNajwKlas];


            //safecheck - jesli w tablicy istnieja 2 wartosci maksymalne to jest blad
            for (int i = 0; i < licznikiKlasDec.Length; i++)
                if ((indeksNajwKlas != i) && (licznikiKlasDec[indeksNajwKlas] == licznikiKlasDec[i]))
                    decyzja = "blad - wiecej niz jedna klasa ma taka sama sume!";
            //Console.WriteLine($"\nOceniam te probke jako {decyzja}!");
            //Console.WriteLine($"\nLiczba wystapien najczestszej klasy: {licznikiKlasDec[indeksNajwKlas]}");
            return decyzja;
        }

        public static string KNN1normalne(string[] klasyDecyzyjne, DataTable daneLiczbowe, double[] probkaTestowa, int k, char metryka)
        {
            //tworzenie tablicy dystansow
            IndeksIDystans[] tabDystanse = TworzenieTablicyDystansow(daneLiczbowe, probkaTestowa, metryka);

            if (k > daneLiczbowe.Rows.Count)
            {
                Console.WriteLine("\nPodana wartosc k wykracza poza zakres! ");
                Console.WriteLine($"Ustawiono wartosc k na maksymalna mozliwa, czyli {daneLiczbowe.Rows.Count}.");
                k = daneLiczbowe.Rows.Count;
            }
            string decyzja = KNN1(klasyDecyzyjne, tabDystanse, daneLiczbowe, k);

            return decyzja;
        }

        public static string KNN2normalne(string[] klasyDecyzyjne, DataTable daneLiczbowe, double[] probkaTestowa, int k, char metryka)
        {
            IndeksIDystans[] tabDystanse = TworzenieTablicyDystansow(daneLiczbowe, probkaTestowa, metryka);

            int liczNajmniejszejProbki;
            int indeksNajmKlasDec;

            int[] liczKlasDec = new int[klasyDecyzyjne.Length];
            for (int i = 0; i < liczKlasDec.Length; i++)
            {
                liczKlasDec[i] = 0;
                for (int j = 0; j < daneLiczbowe.Rows.Count; j++)
                {
                    if (klasyDecyzyjne[i] == daneLiczbowe.Rows[j].Field<string>(daneLiczbowe.Columns.Count - 1))
                        liczKlasDec[i]++;
                }
            }

            indeksNajmKlasDec = 0;
            liczNajmniejszejProbki = liczKlasDec[0];

            for (int i = 1; i < liczKlasDec.Length; i++)
                if (liczNajmniejszejProbki > liczKlasDec[i])
                {
                    liczNajmniejszejProbki = liczKlasDec[i];
                    indeksNajmKlasDec = i;
                }

            Console.WriteLine($"\n\nNajmniejsza probka to {klasyDecyzyjne[indeksNajmKlasDec]}, wystapila {liczNajmniejszejProbki} razy.");

            if (k > liczNajmniejszejProbki)
            {
                Console.WriteLine("\nPodana wartosc k wykracza poza zakres! ");
                Console.WriteLine($"Ustawiono wartosc k na maksymalna mozliwa, czyli {liczNajmniejszejProbki}.");
                k = liczNajmniejszejProbki;
            }
            string decyzja = KNN2(klasyDecyzyjne, daneLiczbowe, tabDystanse, k);

            return decyzja;
        }

        public static string KNN1JKR(string[] klasyDecyzyjne, DataTable daneLiczbowe, double[] wierszBadany, int k, char metryka, int i)
        {
            IndeksIDystans[] tabDystansow = TworzenieTabDystJKR(daneLiczbowe, wierszBadany, metryka, i);
            string decyzja = KNN1(klasyDecyzyjne, tabDystansow, daneLiczbowe, k);

            return decyzja;
        }

        public static string KNN2JKR(string[] klasyDecyzyjne, DataTable daneLiczbowe, double[] wierszBadany, int k, char metryka, int liczNajmniejszejProbki, int i)
        {
            IndeksIDystans[] tabDystansow = TworzenieTabDystJKR(daneLiczbowe, wierszBadany, metryka, i);
            string decyzja = KNN2(klasyDecyzyjne, daneLiczbowe, tabDystansow, k);
            return decyzja;
        }

        public static IndeksIDystans[] TworzenieTablicyDystansow(DataTable daneLiczbowe, double[] probkaTestowa, char metryka)
        {
            //tworzenie tablicy dystansow
            IndeksIDystans[] tabDystanse = new IndeksIDystans[daneLiczbowe.Rows.Count];
            for (int i = 0; i < tabDystanse.Length; i++)
            {
                IndeksIDystans nowy = new IndeksIDystans();
                if (metryka == '1')
                    nowy.dystans = DystansEuklides(probkaTestowa, daneLiczbowe.Rows[i]);
                else if (metryka == '2')
                    nowy.dystans = DystansManhattan(probkaTestowa, daneLiczbowe.Rows[i]);
                else if (metryka == '3')
                    nowy.dystans = DystansCzernobyl(probkaTestowa, daneLiczbowe.Rows[i]);
                else if (metryka == '4')
                    nowy.dystans = DystansLogarytm(probkaTestowa, daneLiczbowe.Rows[i]);
                else if (metryka == '5')
                    nowy.dystans = DystansMinkowsky(probkaTestowa, daneLiczbowe.Rows[i]);

                nowy.indeks = i;
                tabDystanse[i] = nowy;
            }
            Array.Sort(tabDystanse);
            return tabDystanse;
        }

        public static IndeksIDystans[] TworzenieTabDystJKR(DataTable daneLiczbowe, double[] probkaTestowa, char metryka, int indeksProbki)
        {
            int indeks = 0;
            IndeksIDystans[] tabDystanse = new IndeksIDystans[daneLiczbowe.Rows.Count - 1];
            for(int i=0; i<daneLiczbowe.Rows.Count; i++)
            {
                if (i == indeksProbki)
                    continue;
                IndeksIDystans nowy = new IndeksIDystans();
                if (metryka == '1')
                    nowy.dystans = DystansEuklides(probkaTestowa, daneLiczbowe.Rows[i]);
                else if (metryka == '2')
                    nowy.dystans = DystansManhattan(probkaTestowa, daneLiczbowe.Rows[i]);
                else if (metryka == '3')
                    nowy.dystans = DystansCzernobyl(probkaTestowa, daneLiczbowe.Rows[i]);
                else if (metryka == '4')
                    nowy.dystans = DystansLogarytm(probkaTestowa, daneLiczbowe.Rows[i]);
                else if (metryka == '5')
                    nowy.dystans = DystansMinkowsky(probkaTestowa, daneLiczbowe.Rows[i]);

                nowy.indeks = indeks;
                tabDystanse[indeks] = nowy;
                indeks++;
            }
            Array.Sort(tabDystanse);
            return tabDystanse;
        }


        static int Main(string[] args)
        {
            JsonSerializer serializer = new JsonSerializer();

            //wczytanie configa z zamiana znakow na liczby
            StreamReader fileZnaki = File.OpenText("conZnakiNaLiczby.json");
            Dictionary<string, double> tabZnakiNaLiczby = (Dictionary<string, double>)serializer.Deserialize(fileZnaki, typeof(Dictionary<string, double>));

            DirectoryInfo d = new DirectoryInfo("./");
            FileInfo[] tabDataSety = d.GetFiles("*.data"); //tablica wszystkich plikow .data w folderze


            char nrSetu;
            Console.WriteLine("Wybierz dane na ktorych chcesz pracowac: ");
            for (int i = 0; i < tabDataSety.Length; i++)
                Console.Write($"{tabDataSety[i].Name} [{i}]      ");
            Console.WriteLine();
            nrSetu = Console.ReadKey().KeyChar;
            string nazwaSetu = tabDataSety[Int32.Parse(nrSetu.ToString())].Name; //wyciaganie nazwy wybranego setu
            Console.WriteLine($"\n\nWybrano set: {nazwaSetu}");

            //wyszukiwanie configa zawierajacego nazwe data setu
            string nazwaConfigu = "";
            FileInfo[] tabConfigi = d.GetFiles("con*.json");
            for (int i = 0; i < tabConfigi.Length; i++)
                if (tabConfigi[i].Name.Contains(nazwaSetu.Replace(".data", "")))
                    nazwaConfigu = tabConfigi[i].Name;
            if (nazwaConfigu == "")
            {
                Console.WriteLine("Nie istnieje config dla danego setu");
                return 0;
            }

            Console.WriteLine($"\nWczytano config: {nazwaConfigu}\n");



            //wczytanie configa z danymi o secie
            StreamReader fileDane = File.OpenText(nazwaConfigu);
            AustralianConfig config = (AustralianConfig)serializer.Deserialize(fileDane, typeof(AustralianConfig));

            //tworzenie 3 dataTabli: jeden z surowymi danymi (jak w secie), drugi z zamienionymi znakami na liczby, trzeci ze znormalizowanymi danymi
            System.Data.DataTable dane = new DataTable("TablicaDanych");
            System.Data.DataTable daneLiczbowe = new DataTable("TablicaDanychLiczbowych");
            //System.Data.DataTable daneZnormalizowane = new DataTable("TablicaDanychZnormalizowanych");

            List<int> indeksyBledow = new List<int>();
            int indeksBledu = 0;

            UstawienieDataSetu(dane, config.rodzajKolumny, config.iloscKolumn, config.iloscWierszy, true);

            //zapisywanie danych do dataTable
            string[] load = System.IO.File.ReadAllLines(nazwaSetu);
            int nrWiersza = 0;
            string[] wiersz;

            foreach (string line in load)
            {
                //jezeli jest '?' to linia jest pomijana a jej indeks trafia do listy z bledami
                if (line.Contains('?'))
                {
                    indeksyBledow.Add(indeksBledu);
                    indeksBledu++;
                    continue;
                }

                wiersz = line.Split(config.separator);
                for (int i = 0; i < wiersz.Length; i++)
                {
                    wiersz[i] = wiersz[i].Replace('.', ',');
                    if (config.rodzajKolumny[i] == "znak")
                        dane.Rows[nrWiersza][i] = wiersz[i];
                    else
                        dane.Rows[nrWiersza][i] = Convert.ToDouble(wiersz[i]);
                }
                nrWiersza++;
                indeksBledu++;
            }

            //ustawianie wartosci w danychLiczbowych - ewentualne zamienianie znakow na liczby
            UstawienieDataSetu(daneLiczbowe, config.rodzajKolumny, config.iloscKolumn, config.iloscWierszy - indeksyBledow.Count, false);
            for (int i = 0; i < config.iloscKolumn; i++)
            {
                for (int j = 0; j < config.iloscWierszy - indeksyBledow.Count; j++)
                {
                    if (config.rodzajKolumny[i] == "znak")
                        if (i == config.iloscKolumn - 1)
                            daneLiczbowe.Rows[j][i] = dane.Rows[j][i];
                        else
                            daneLiczbowe.Rows[j][i] = tabZnakiNaLiczby[dane.Rows[j].Field<string>(i)];
                    else
                        daneLiczbowe.Rows[j][i] = dane.Rows[j][i];
                }
            }

            //wyswietlanie danych na konsoli
            for(int i=0; i<daneLiczbowe.Rows.Count; i++)
            {
                for (int j = 0; j < dane.Columns.Count; j++)
                    Console.Write($"{dane.Rows[i][j]} ");
                Console.WriteLine();
            }

            //wyszukiwanie min max wartosci dla kolumn
            string[] tabMin = new string[config.iloscKolumn - 1];
            string[] tabMax = new string[config.iloscKolumn - 1];
            double min;
            double max;

            for(int i=0; i<config.iloscKolumn - 1; i++)
            {
                if(config.rodzajKolumny[i] == "znak")
                {
                    tabMin[i] = "znak";
                    tabMax[i] = "znak";
                }
                else
                {
                    min = daneLiczbowe.Rows[0].Field<double>(i);
                    max = daneLiczbowe.Rows[0].Field<double>(i);
                    for(int j=1; j<daneLiczbowe.Rows.Count; j++)
                    {
                        if (daneLiczbowe.Rows[j].Field<double>(i) < min)
                            min = daneLiczbowe.Rows[j].Field<double>(i);
                        if (daneLiczbowe.Rows[j].Field<double>(i) > max)
                            max = daneLiczbowe.Rows[j].Field<double>(i);
                    }
                    tabMin[i] = min.ToString();
                    tabMax[i] = max.ToString();
                }
            }

            //wyswietlanie min maxow na konsoli
            Console.WriteLine("\nWartosci minimalne dla kolejnych kolumn:");
            for (int i = 0; i < tabMin.Length; i++)
                Console.Write($"{tabMin[i]} ");
            Console.WriteLine("\n\nWartosci maksymalne dla kolejnych kolumn:");
            for (int i = 0; i < tabMax.Length; i++)
                Console.Write($"{tabMax[i]} ");


            
                

            Console.WriteLine("\n\nWbybierz metryke: ");
            Console.WriteLine("1-Euklides, 2-Manhattan, 3-Czernobyl, 4-Logarytm, 5-Minkowsky");
            char metryka = Console.ReadKey().KeyChar;

            bool czyKnn;
            Console.WriteLine("\n\nCzy chcesz uruchomic KNN? Jesli nie, to uruchomi sie JedenKontraResztaSwiata. [y-tak]");
            if (Console.ReadKey().KeyChar == 'y')
                czyKnn = true;
            else
                czyKnn = false;

            if (czyKnn)
            {
                //pobieranie i ustawianie probki testowej (probkaTestowaS to oryginalna probka, a probkaTestowa to probka double po zamianie znakow na liczby
                string[] probkaTestowaS;
                Console.WriteLine($"\nPodaj {config.iloscKolumn-1} wartosci dla probki testowej (jako separatora uzyj spacji, jako separatora dziesietnego przecinka): ");
                probkaTestowaS = Console.ReadLine().Split(' ');
                //double[] probkaTestowa = Array.ConvertAll(probkaTestowaS, Double.Parse);
                double[] probkaTestowa = new double[probkaTestowaS.Length];
                for (int i = 0; i < probkaTestowa.Length; i++)
                {
                    if (config.rodzajKolumny[i] == "znak")
                        probkaTestowa[i] = tabZnakiNaLiczby[probkaTestowaS[i]];
                    else
                        probkaTestowa[i] = Double.Parse(probkaTestowaS[i]);
                }

                Console.WriteLine("\nWybierz wersje KNN: [1-KNNver1.1(alpha) / 2-KNNver1.2(pre-alpha)]");
                char wersjaKNN = Console.ReadKey().KeyChar;

                //pobieranie i ustawianie k
                int k;
                Console.WriteLine("\nPodaj wartosc k: ");
                k = Int32.Parse(Console.ReadLine());

                if (wersjaKNN == '1')
                {
                    string decyzja = KNN1normalne(config.klasyDecyzyjne, daneLiczbowe, probkaTestowa, k, metryka);
                    Console.WriteLine($"\nOceniam probke jako {decyzja}!");
                }
                else
                {
                    string decyzja = KNN2normalne(config.klasyDecyzyjne, daneLiczbowe, probkaTestowa, k, metryka);
                    Console.WriteLine($"\nOceniam probke jako: {decyzja}!!");
                }
            }
            else
            {
                int liczbaKlasyfikacji = 0;
                int liczbaUdanychKlasyfikacji = 0;
                int liczbaNieudanychKlasyfikacji = 0;
                int liczbaBlednychKlasyfikacji = 0;
                double[] wierszBadany = new double[daneLiczbowe.Columns.Count - 1];

                int liczNajmniejszejProbki;
                int indeksNajmKlasDec;

                int[] liczKlasDec = new int[config.klasyDecyzyjne.Length];
                for (int i = 0; i < liczKlasDec.Length; i++)
                {
                    liczKlasDec[i] = 0;
                    for (int j = 0; j < daneLiczbowe.Rows.Count; j++)
                    {
                        if (config.klasyDecyzyjne[i] == daneLiczbowe.Rows[j].Field<string>(daneLiczbowe.Columns.Count - 1))
                            liczKlasDec[i]++;
                    }
                }

                indeksNajmKlasDec = 0;
                liczNajmniejszejProbki = liczKlasDec[0];

                for (int i = 1; i < liczKlasDec.Length; i++)
                    if (liczNajmniejszejProbki > liczKlasDec[i])
                    {
                        liczNajmniejszejProbki = liczKlasDec[i];
                        indeksNajmKlasDec = i;
                    }

                Console.WriteLine($"\n\nNajmniejsza probka to {config.klasyDecyzyjne[indeksNajmKlasDec]}, wystapila {liczNajmniejszejProbki} razy.");


                Console.WriteLine("\nWybierz wersje KNN: [1-KNNver1.1(alpha) / 2-KNNver1.2(pre-alpha)]");
                char wersjaKNN = Console.ReadKey().KeyChar;

                //pobieranie i ustawianie k
                int k;
                Console.WriteLine("\nPodaj wartosc k: ");
                k = Int32.Parse(Console.ReadLine());

                if (wersjaKNN == '1')
                {

                    if (k > liczNajmniejszejProbki)
                    {
                        Console.WriteLine("\nPodana wartosc k wykracza poza zakres! ");
                        Console.WriteLine($"Ustawiono wartosc k na maksymalna mozliwa, czyli {liczNajmniejszejProbki}.");
                        k = liczNajmniejszejProbki;
                    }

                    for (int i=0; i<daneLiczbowe.Rows.Count; i++)
                    {
                        for(int j=0; j<daneLiczbowe.Columns.Count-1; j++)
                        {
                            wierszBadany[j] = daneLiczbowe.Rows[i].Field<double>(j);  //ustawianie wartosci w probce testowej
                        }
                        string decyzja = KNN1JKR(config.klasyDecyzyjne, daneLiczbowe, wierszBadany, k, metryka, i);
                        if (decyzja == "blad - wiecej niz jedna klasa ma taka sama sume!")
                            liczbaNieudanychKlasyfikacji++;
                        else
                        {
                            liczbaKlasyfikacji++;
                            if (decyzja == daneLiczbowe.Rows[i].Field<string>(daneLiczbowe.Columns.Count - 1))
                                liczbaUdanychKlasyfikacji++;
                            else
                                liczbaBlednychKlasyfikacji++;
                        }
                    }
                    Console.WriteLine($"\nLiczba klasyfikacji to {liczbaKlasyfikacji}");
                    Console.WriteLine($"Pokrycie to {(liczbaKlasyfikacji * 100 / daneLiczbowe.Rows.Count)}");
                    Console.WriteLine($"Liczba blednych klasyfikacji to {liczbaBlednychKlasyfikacji}");
                    Console.WriteLine($"Skutecznosc klasyfikacji to {(liczbaUdanychKlasyfikacji * 100 / liczbaKlasyfikacji)}");
                }
                else
                {
                    if (k > liczNajmniejszejProbki-1)
                    {
                        Console.WriteLine("\nPodana wartosc k wykracza poza zakres! ");
                        Console.WriteLine($"Ustawiono wartosc k na maksymalna mozliwa, czyli {liczNajmniejszejProbki-1}.");
                        k = liczNajmniejszejProbki-1;
                    }
                    for (int i = 0; i < daneLiczbowe.Rows.Count; i++)
                    {
                        for (int j = 0; j < daneLiczbowe.Columns.Count - 1; j++)
                        {
                            wierszBadany[j] = daneLiczbowe.Rows[i].Field<double>(j);
                        }

                        string decyzja = KNN2JKR(config.klasyDecyzyjne, daneLiczbowe, wierszBadany, k, metryka, liczNajmniejszejProbki, i);
                        if (decyzja == "blad - wiecej niz jedna klasa ma taka sama sume!")
                            liczbaNieudanychKlasyfikacji++;
                        else
                        {
                            liczbaKlasyfikacji++;
                            if (decyzja == daneLiczbowe.Rows[i].Field<string>(daneLiczbowe.Columns.Count - 1))
                                liczbaUdanychKlasyfikacji++;
                            else
                                liczbaBlednychKlasyfikacji++;
                        }
                    }
                    Console.WriteLine($"\nLiczba klasyfikacji to {liczbaKlasyfikacji}");
                    Console.WriteLine($"Pokrycie to {(liczbaKlasyfikacji * 100 / daneLiczbowe.Rows.Count)}");
                    Console.WriteLine($"Liczba blednych klasyfikacji to {liczbaBlednychKlasyfikacji}");
                    Console.WriteLine($"Skutecznosc klasyfikacji to {(liczbaUdanychKlasyfikacji * 100 / liczbaKlasyfikacji)}");
                }
            }
            
            
            Console.WriteLine("\nKoniec przedstawienia!");
            return 0;
        }
    }
}
