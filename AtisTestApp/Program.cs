using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace AtisTestApp
{
    internal static class Program
    {
        private static readonly List<string> Icaos = new List<string>();
        private static readonly List<string> Phonetics = new List<string>();
        private static void Main(string[] args)
        {
            PopulateAtisIcaos();
            using WebClient wc = new WebClient();
            string json = wc.DownloadString("https://data.vatsim.net/v3/vatsim-data.json");
            JsonData result = JsonConvert.DeserializeObject<JsonData>(json);
            if (result == null) return;
            Console.WriteLine($"{result.Atis.Length} ATIS's in the Dataserver");
            int liveCount = 0;
            foreach (Ati atis in result.Atis)
            {
                if (atis.TextAtis == null) return;
                int oldCount = liveCount;

                foreach (string line in atis.TextAtis)
                {
                    string upper = new Regex("[ ]{2,}", RegexOptions.None).Replace(line.ToUpper(), " ");
                    if (!upper.Contains("INFORMATION ") && !upper.Contains("INFO ") && !upper.Contains("INFO ") && !upper.Contains("ATIS ")) continue;
                    string[] strings = upper.Split(" ");
                    foreach(string strin in strings)
                    {
                        string clean = strin.Replace(".", string.Empty).Replace(",", string.Empty);
                        if (!Phonetics.Contains(clean) && !Icaos.Contains(clean)) continue;
                        string letter = strin.Substring(0, 1);
                        liveCount++;
                        Console.WriteLine(letter + " - " + upper);
                    }

                    if (oldCount == liveCount)
                    {
                        Console.WriteLine($"no valid line for {atis.Callsign}");
                    }
                    break;
                }
            }
            wc.Dispose();
        }

        private static void PopulateAtisIcaos()
        {
            Icaos.Add("A");
            Icaos.Add("B");
            Icaos.Add("C");
            Icaos.Add("D");
            Icaos.Add("E");
            Icaos.Add("F");
            Icaos.Add("G");
            Icaos.Add("H");
            Icaos.Add("I");
            Icaos.Add("J");
            Icaos.Add("K");
            Icaos.Add("L");
            Icaos.Add("M");
            Icaos.Add("N");
            Icaos.Add("O");
            Icaos.Add("P");
            Icaos.Add("Q");
            Icaos.Add("R");
            Icaos.Add("S");
            Icaos.Add("T");
            Icaos.Add("U");
            Icaos.Add("V");
            Icaos.Add("W");
            Icaos.Add("X");
            Icaos.Add("Y");
            Icaos.Add("Z");
            Phonetics.Add("ALPHA");
            Phonetics.Add("BRAVO");
            Phonetics.Add("CHARLIE");
            Phonetics.Add("DELTA");
            Phonetics.Add("ECHO");
            Phonetics.Add("FOXTROT");
            Phonetics.Add("GOLF");
            Phonetics.Add("HOTEL");
            Phonetics.Add("INDIA");
            Phonetics.Add("JULIET");
            Phonetics.Add("JULIETT");
            Phonetics.Add("KILO");
            Phonetics.Add("LIMA");
            Phonetics.Add("MIKE");
            Phonetics.Add("NOVEMBER");
            Phonetics.Add("OSCAR");
            Phonetics.Add("PAPA");
            Phonetics.Add("QUEBEC");
            Phonetics.Add("ROMEO");
            Phonetics.Add("SIERRA");
            Phonetics.Add("TANGO");
            Phonetics.Add("UNIFORM");
            Phonetics.Add("VICTOR");
            Phonetics.Add("WHISKEY");
            Phonetics.Add("XRAY");
            Phonetics.Add("YANKEE");
            Phonetics.Add("ZULU");
        }
    }
}
