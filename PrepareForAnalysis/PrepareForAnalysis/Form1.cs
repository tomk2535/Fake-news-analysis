using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PrepareForAnalysis
{
    public partial class Form1 : Form
    {

        string scrapedFolder = "C:/Users/tomk/source/repos/SuperCrawler/SuperCrawler/bin/x86/Debug/Scraped";
        string fakeNews = "C:/Users/tomk/source/repos/SuperCrawler/SuperCrawler/bin/x86/Debug/FakeNews.json";

        int filenr = 1;
        
        int fromEntry = 1;
        int toEntry = 20;

        string[] falseScore = new string[] { "false", "full flop", "half-frue", "mostly false", "pants on fire!" };
        string[] trueScore = new string[] { "true", "mostly true", "half-true" };

        public Form1()
        {
            InitializeComponent();
            string json = File.ReadAllText(fakeNews);

            if (!Directory.Exists("Entries")) Directory.CreateDirectory("Entries");

            /* Iterate over entries */
            foreach (JObject entry in JArray.Parse(json))
            {
                if (filenr < fromEntry)
                {
                    filenr++;
                    continue;
                }

                //if (filenr > toEntry) return;



                JToken scoreObj = entry["Score"];
                if (scoreObj == null) continue;
                string score = scoreObj.ToString().ToLower();

                /* Set scores to simply true or false */
                if (falseScore.Contains(score))
                {
                    entry["Score"].Replace("False");
                }
                else if (trueScore.Contains(score))
                {
                    entry["Score"].Replace("True");
                }
                else continue;



                /* JArray to replace refrences */
                JArray sources = new JArray();

                JArray references = (JArray)entry["Refrences"];

                /* Iterate over refrences */
                foreach (JObject reference in references)
                {
                    /* Iterate over links */
                    foreach (JObject link in (JArray)reference["SourceLinks"])
                    {
                        string domain = GetDomain(link["Link"].ToString());

                        string ID = link["ID"].ToString();

                        bool success = false; object data = null;
                        if (domain == "Twitter") ReadSource("TwitterBeautified", ID, true, out success, out data);
                        // else if (domain == "YouTube") ReadSource("YoutubeScrape", ID, false, out success, out data);
                        // else ReadSource("GeneralScrape", ID, false, out success, out data);

                        if (success)
                        {
                            string dataString = data.ToString();
                            if (dataString.Equals("Not a tweet") || dataString.Equals("Tweet has been removed")) continue;

                            JObject obj = data as JObject;
                            int replies = int.Parse(obj["Replies"].ToString());
                            if (replies < 5) continue;

                            /*JObject newSource = new JObject(
                                new JProperty("Domain", domain),
                                new JProperty("Data", data)
                            );
                            sources.Add(newSource);*/
                            sources.Add(data);
                        }
                    }
                }

                entry.Remove("Refrences");
                entry.Add("Sources", sources);

                if(sources.Count > 0)
                {
                    File.WriteAllText($"Entries/Entry{filenr++}.txt", entry.ToString());
                }
            }

        }
        
        string[] twitterDomains = new string[] { "https://www.twitter.com", "https://twitter.com" };
        string[] youtubeDomains = new string[] { "https://www.youtube.com", "https://youtube.com", "https://www.youtu.be", "https://youtu.be" };
        public string GetDomain(string link)
        {
            Uri uri = new Uri(link);
            string domain = uri.GetLeftPart(UriPartial.Authority);

            /* Return appropriate domain type */
            if (twitterDomains.Contains(domain)) return "Twitter";
            else if (youtubeDomains.Contains(domain)) return "YouTube";
            else return "General";
        }

        public void ReadSource(string folder, string ID, bool isJson, out bool success, out object data)
        {
            success = false;  data = null;
            string path = $"{scrapedFolder}/{folder}/{ID}.txt";

            if (!File.Exists(path)) return;

            string dataText = File.ReadAllText(path);

            if (isJson)
            {
                try
                {
                    data = JObject.Parse(dataText);
                }
                catch
                {
                    data = dataText;
                }
            }
            else data = dataText;

            success = true;
        }
    }
}
