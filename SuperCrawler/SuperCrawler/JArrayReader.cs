using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperCrawler
{
    class JArrayReader
    {
        public static SourceData[] ReadJArray(string path)
        {
            /* Set of unique IDs */
            HashSet<string> uniqueIDs = new HashSet<string>();

            List<SourceData> sourceData = new List<SourceData>();

            JArray entries;

            if (!File.Exists(path))
            {
                throw new FileNotFoundException();
            }

            /* Get JArray from file */
            using (StreamReader file = File.OpenText(path))
            {
                entries = JArray.Parse(file.ReadToEnd());
            }

            foreach (JToken entry in entries)
            {
                /* Get refrence array */
                JArray refrences = entry["Refrences"] as JArray;
                foreach (JToken refrence in refrences)
                {
                    JArray links = refrence["SourceLinks"] as JArray;
                    foreach (JToken link in links)
                    {
                        string ID = link["ID"].ToString();
                        string href = link["Link"].ToString();

                        /* Continue if not a proper hyperlink */
                        if (!Uri.IsWellFormedUriString(href, UriKind.Absolute)) continue;

                        /* Add if ID is unique */
                        if (uniqueIDs.Contains(ID)) continue;
                        uniqueIDs.Add(ID);

                        sourceData.Add(new SourceData()
                        {
                            ID = ID,
                            link = href
                        });
                    }
                }
            }

            return sourceData.ToArray();
        }
    }

    public class SourceData
    {
        public string ID;
        public string link;

        public override string ToString()
        {
            return $"ID: {ID}, Link: {link}";
        }
    }
}
