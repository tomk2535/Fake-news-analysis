using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PolitifactCrawler
{
    public partial class Form1 : Form
    {
        string baseHref = "https://www.politifact.com/truth-o-meter/statements/";
        HtmlWeb web = new HtmlWeb();

        int foundSourceLinks = 0;

        int duplicateSources = 0;

        /* Length if IDs */
        const int IDLength = 6;
        HashSet<string> generatedIDs = new HashSet<string>(); // Lookup hashset for generated IDs
        Dictionary<string, string> SourceIDs = new Dictionary<string, string>(); // Store IDs. Key = SourceHref, Value = ID
        Random random = new Random();

        /* The JArray for storing the data */
        JArray jArray = new JArray();

        public Form1()
        {
            InitializeComponent();

            Stopwatch sw = Stopwatch.StartNew();
            for(int pageNr = 1; pageNr <= 1; pageNr++)
            {
                HandlePage(pageNr);
            }

            /* Save crawled data */
            File.WriteAllText("FakeNews.json", jArray.ToString());

            sw.Stop();
            richTextBox1.Text = $"Amount of fake news found: {jArray.Count}\nSource links found: {foundSourceLinks}\nTime taken: {sw.Elapsed.ToString()} s!";
            richTextBox1.Text += "\n\nCollisions: " + duplicateSources;
        }

        /* Handle one page */
        void HandlePage(int pageNr)
        {
            var htmlDoc = web.Load(baseHref + $"?page={pageNr}");

            /* Get all scoretables */
            var scoreTables = htmlDoc.DocumentNode.SelectNodes(".//main/section/div");

            foreach (var table in scoreTables)
            {

                /* The linknode cotains a href to more info */
                var linkNode = table.SelectSingleNode(".//a[@class='link']");

                string href = linkNode.Attributes["href"].Value;

                /* Check if entry has twitter as main source for crawling */
                if (HasSources(href, out JArray refrences, out JObject article))
                {
                    /* Who came with the statement? */
                    string source = table.SelectSingleNode(".//div[@class='statement__source']/a").InnerText.Trim();

                    /* The true/false-meter */
                    var meter = table.SelectSingleNode(".//div[@class='meter']");

                    /* What was the score */
                    string score = meter.SelectSingleNode(".//a/img").Attributes["alt"].Value.Trim();

                    /* The author quote */
                    string quote = HtmlEntity.DeEntitize(meter.SelectSingleNode(".//p").InnerText);

                    /* Statement */
                    string statement = HtmlEntity.DeEntitize(linkNode.InnerHtml).Replace("\n", "").Trim();

                    /* Subgroup of politifacts that made the entry */
                    string subGroup = table.SelectSingleNode(".//p[@class='statement__edition']/a").InnerText.Replace("&mdash; ", "").Trim();

                    /* Create new JObject */
                    JObject jObject = new JObject(
                        new JProperty("Link", href),
                        new JProperty("Source", source),
                        new JProperty("Score", score),
                        new JProperty("Quote", quote),
                        new JProperty("Statement", statement),
                        new JProperty("SubGroup", subGroup),
                        new JProperty("Article", article),
                        new JProperty("Refrences", refrences)
                    );

                    jArray.Add(jObject);
                }
            }
        }

        string[] politifactsDomains = new string[] { "https://www.politifact.com", "https://politifact.com" };

        /* Check if scoreboard has sources.
         * Also returns the information of interest from the page. */
        bool HasSources(string href, out JArray sources, out JObject article)
        {
            /* The article data */
            article = new JObject();

            /* An array containing arrays of sources. Index in array is somewhat relative to relevance */
            sources = new JArray();

            try
            {
                var htmlDoc = web.Load("https://www.politifact.com" + href);

                /* Get the right side widget container */
                var widgetContainer = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='widget__content']");

                /* Get all source points */
                var sourcePoints = widgetContainer.SelectNodes(".//div/p");

                /* Get all sources */
                foreach(var point in sourcePoints)
                {
                    JArray subSources = new JArray();
                    var sourceA = point.SelectNodes(".//a");
                    if (sourceA == null) continue;
                    foreach(var a in sourceA)
                    {
                        var sourceAttribute = a.Attributes["href"];
                        if (sourceAttribute == null) continue;
                        string sourceHref = sourceAttribute.Value;

                        /* Avoid links pointing back to politifacts */
                        Uri uri = new Uri(sourceHref);
                        string domain = uri.GetLeftPart(UriPartial.Authority);
                        if (politifactsDomains.Contains(domain)) continue;

                        subSources.Add(new JObject()
                        {
                            new JProperty("ID", GetSourceID(sourceHref)), // Also generate ID for source
                            new JProperty("Link", sourceHref)
                        });
                        foundSourceLinks++; /* Increment sources found */
                    }
                    /* If source point have valid refrences */
                    if (subSources.Count > 0)
                    {
                        sources.Add(new JObject(
                            new JProperty("SourceText", HtmlEntity.DeEntitize(point.InnerText)),
                            new JProperty("SourceLinks", subSources)
                        ));
                    }
                }

                /* Get more data from the widget */
                var researchers = GetInnerHyperlinkTexts(widgetContainer.SelectSingleNode(".//p[2]"));
                var editors = GetInnerHyperlinkTexts(widgetContainer.SelectSingleNode(".//p[3]"));
                var subjects = GetInnerHyperlinkTexts(widgetContainer.SelectSingleNode(".//p[4]"));

                /* Get data from the main node */
                var mainNode = htmlDoc.DocumentNode.SelectSingleNode("//main");

                /* Find article date */
                string articleTitle = HtmlEntity.DeEntitize(mainNode.SelectSingleNode("//h1[@class='article__title']").InnerText);

                /* Getting article text */
                var articleTextNode = mainNode.SelectSingleNode("//div[@class='article__text']");

                /* Get first child */
                var node = articleTextNode.SelectSingleNode(".//p");


                //
                // Get article text subheaders
                //
                JObject subText = new JObject(
                    new JProperty("SubHeader", "")    
                );
                string tempText = "";
                JArray articleArray = new JArray();
                while (true)
                {
                    if (node == null) break;

                    bool newTitle = false;

                    if (node.Name == "p")
                    {
                        string nodeHtml = node.InnerHtml.ToLower();
                        /* Start new subtitle if <strong>...</strong> */
                        if(nodeHtml.StartsWith("<strong>") && nodeHtml.EndsWith("</strong>")) newTitle = true;
                        else
                        {
                            string t = HtmlEntity.DeEntitize(node.InnerText);

                            /* Fixes a problem where one article had an entire div inside <p>...</p> */
                            if (!t.Contains("artembed")) tempText += t + "\n";
                        }
                    }
                    else if(node.Name == "div")
                    {
                        /* Break if node does not have the correct class field */
                        var classAttribute = node.Attributes["class"];
                        if (classAttribute != null && classAttribute.Value.ToString().Equals("pf_subheadline")) newTitle = true;
                    }

                    if (newTitle)
                    {
                        subText.Add(new JProperty("Text", tempText));
                        articleArray.Add(subText);

                        /* Prepare for next subheader section */
                        subText = new JObject(
                            new JProperty("SubHeader", HtmlEntity.DeEntitize(node.InnerText))
                        );
                        tempText = "";
                    }

                    /* Iterate to next sibling */
                    node = node.NextSibling;
                }
                /* Add last node */
                subText.Add(new JProperty("Text", tempText));
                articleArray.Add(subText);



                /* Find article date */
                string articleDate = mainNode.SelectSingleNode("//span[@class='article__meta']").InnerText;

                /* Construct full article JSON */
                article = new JObject(
                    new JProperty("Title", articleTitle),
                    new JProperty("Researchers", researchers),
                    new JProperty("Editors", editors),
                    new JProperty("Subjects", subjects),
                    new JProperty("Date", GetArticleDateFromText(articleDate)),
                    new JProperty("ArticleBody", articleArray)
                );
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        /* Return an array of innerTexts from a basenode */
        JArray GetInnerHyperlinkTexts(HtmlNode baseNode)
        {
            JArray output = new JArray();
            var aNodes = baseNode.SelectNodes(".//a");
            if(aNodes.Count == 0)
            {
                output.Add("Unknown");
                return output;
            }

            foreach(var a in aNodes)
            {
                output.Add(HtmlEntity.DeEntitize(a.InnerText));
            }

            return output;
        }

        string GetArticleDateFromText(string text)
        {
            string temp = text;
            temp = HtmlEntity.DeEntitize(temp).ToLower();
            temp = temp.Replace("on ", "").Replace("at ", "").Replace(",", "");
            /* Example format at this point: wednesday september 19th 2018 3:10 p.m. */

            string[] parts = temp.Split(' ');

            int day = 0, month = 0, year = 0, hour = 0, min = 0;

            try
            {
                /* Day without two last letters -th, -st */
                day = int.Parse(parts[2].Substring(0, parts[2].Length - 2));

                /* Month */
                month = ResolveMonth(parts[1]);

                /* Year */
                year = int.Parse(parts[3]);

                string[] timeParts = parts[4].Split(':');
                hour = int.Parse(timeParts[0]);

                /* Convert hour to military time */
                bool am = parts[5] == "a.m.";
                hour = am ? hour : (hour % 12) + 12;

                min = int.Parse(timeParts[1]);
            }
            catch
            {
                return "";
            }
            
            return $"{year}.{month}.{day} {hour}:{min}";
        }


        /* Generate and return an available ID */
        char[] availableChars = "abcdefghijklmnopqrstuvwxyz1234567890".ToCharArray();
        string GetSourceID(string href)
        {
            /* Return same ID for duplicates */
            if (SourceIDs.ContainsKey(href))
            {
                duplicateSources++;
                return SourceIDs[href];
            }

            while (true)
            {
                string ID = "";
                for (int i = 0; i < IDLength; i++) ID += availableChars[random.Next(0, availableChars.Length)];
                if (!generatedIDs.Contains(ID))
                {
                    SourceIDs.Add(href, ID); // Register source
                    generatedIDs.Add(ID); // Register generated ID
                    return ID; // Return ID
                }
            }
        }

        /* Resolves month */
        string[] months = new string[] { "january", "february", "march", "april", "may", "june", "july", "august", "september", "october", "november", "december" };
        public int ResolveMonth(string month)
        {
            for(int i = 0; i < months.Length; i++)
            {
                if (months[i].StartsWith(month)) return i + 1;
            }

            return -1;
        }
    }
}
