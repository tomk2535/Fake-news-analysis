using CefSharp;
using CefSharp.WinForms;
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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SuperCrawler
{
    public partial class Form1 : Form
    {
        ChromiumWebBrowser browser = new ChromiumWebBrowser();

        /* Array of sources */
        SourceData[] sources;

        /* Current scraping website */
        SourceData current;

        /* Which html source is next */
        int pageIndex = 0;

        int scrapeAmount = -1; // Amount of pages to scrape. -1 for all

        /* Handlers for twitter and youtube */
        YoutubeHandler youtubeHandler;
        TwitterHandler twitterHandler;

        private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private int timeoutTime = 2 * 60 * 1000; // One min to timeout page load

        public Form1()
        {
            InitializeComponent();

            InitializeChromium();

            timer.Interval = timeoutTime;
            timer.Tick += new EventHandler(Timeout_Tick);

            /* Get list of unique sources */
            sources = JArrayReader.ReadJArray("FakeNews.json");

            /* Create folders */
            if (!Directory.Exists("Scraped")) Directory.CreateDirectory("Scraped");
            if (!Directory.Exists("Scraped/TwitterScrape")) Directory.CreateDirectory("Scraped/TwitterScrape");
            if (!Directory.Exists("Scraped/YoutubeScrape")) Directory.CreateDirectory("Scraped/YoutubeScrape");
            if (!Directory.Exists("Scraped/GeneralScrape")) Directory.CreateDirectory("Scraped/GeneralScrape");

            /* Set scrape amount */
            scrapeAmount = scrapeAmount == -1 ? sources.Length : scrapeAmount;

            /* Set size of progress bar */
            progressbar.Maximum = scrapeAmount;

            /* Give browser and form1 for callback */
            youtubeHandler = new YoutubeHandler(this, browser);
            twitterHandler = new TwitterHandler(this, browser);

            /* Perform first scrape step */
            ScrapeStep();
        }

        /* When timer timeout */
        private void Timeout_Tick(object sender, EventArgs e)
        {
            timer.Stop();

            /* Write empty */
            File.WriteAllText($"Scraped/{GetFolder()}/{current.ID}.txt", "");

            // Invoke new scrape on main
            Invoke(new Action(() => ScrapeStep()));
        }

        void InitializeChromium()
        {
            CefSettings settings = new CefSettings();

            Cef.Initialize(settings);

            this.Controls.Add(browser);

            browser.Dock = DockStyle.Fill;

            browser.FrameLoadEnd += new EventHandler<FrameLoadEndEventArgs>(FrameLoadEnded);
        }

        void ScrapeStep()
        {
            ScrapeStepStart:;

            /* Don't perform scraping */
            if (pageIndex >= scrapeAmount) return;

            current = sources[pageIndex];
            pageIndex++;

            /* Have a page already been scraped for that ID? */
            bool fileExists = File.Exists($"Scraped/{GetFolder()}/{current.ID}.txt");

            /* Return to start if file exists */
            if (fileExists)
            {
                UpdateProgressBar(pageIndex);
                goto ScrapeStepStart; // Return to start of function
            }

            string link = current.link;
            awaitingPageLoad = true;
            SetTextBox($"Working on loading page number {pageIndex}:\nLink: {link}\nID: {current.ID}");
            timer.Start();
            browser.Load(link);
        }

        public void SetTextBox(string value)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(SetTextBox), new object[] { value });
                return;
            }
            textboxOutput.Text = value;
        }

        public void AppendTextBox(string value)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(AppendTextBox), new object[] { value });
                return;
            }
            textboxOutput.Text += value;
        }

        public void UpdateProgressBar(int value)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int>(UpdateProgressBar), new object[] { value });
                return;
            }
            progressbar.Value = value;
        }

        bool awaitingPageLoad = false;
        private void FrameLoadEnded(object sender, FrameLoadEndEventArgs e)
        {
            if (!e.Frame.IsMain || !awaitingPageLoad) return;
            awaitingPageLoad = false;

            timer.Stop();

            UpdateProgressBar(pageIndex);

            DomainType domain = GetDomain();

            switch (GetDomain())
            {
                case DomainType.Twitter:
                    twitterHandler.HandlePage();
                    break;
                case DomainType.Youtube:
                    youtubeHandler.HandlePage();
                    break;
                case DomainType.General:
                    ScrapePage();
                    break;
            }
        }

        /* Scrape all visible information */
        public void ScrapePage()
        {
            browser.GetSourceAsync().ContinueWith(taskHtml =>
            {
                /* Open html in HtmlAgilityPack */
                HtmlAgilityPack.HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
                htmlDocument.LoadHtml(taskHtml.Result);

                string compressed = CompressDocument(htmlDocument);

                File.WriteAllText($"Scraped/{GetFolder()}/{current.ID}.txt", compressed);

                SetTextBox($"Done scraping!");

                // Invoke new scrape on main
                Invoke(new Action(() => ScrapeStep()));
            });
        }

        /* Remove unnecessary parts of the html */
        private string CompressDocument(HtmlAgilityPack.HtmlDocument htmlDocument)
        {
            /* Remove <script>, <noscript>, <style> and <iframe> tags. Remove link with rel="stylesheet". Remove <path> tag. Also remove comments */
            /* Removes <paper-button> and <ytd-watch-next-secondary-results-renderer> from youtube */
            htmlDocument.DocumentNode.Descendants()
                            .Where(n => n.Name == "script" || n.Name == "paper-button" || n.Name == "noscript" || n.Name == "style" || n.Name == "iframe" || n.Name == "select" || n.Name == "ytd-watch-next-secondary-results-renderer" ||
                                (n.Name == "link" && n.Attributes["rel"] != null && n.Attributes["rel"].Value == "stylesheet") || n.Name == "path" || n.NodeType == HtmlNodeType.Comment)
                            .ToList()
                            .ForEach(n => n.Remove());

            /* Remove onclick and style attributes */
            foreach (var node in htmlDocument.DocumentNode.Descendants())
            {
                var attribute = node.Attributes["onclick"];
                if (attribute != null) node.Attributes.Remove(attribute);
                attribute = node.Attributes["style"];
                if (attribute != null) node.Attributes.Remove(attribute);

                /* Remove entire images stored in attribute */
                if (node.Name == "img")
                {
                    attribute = node.Attributes["src"];
                    if (attribute != null && attribute.Value.StartsWith("data:image")) node.Attributes.Remove(attribute);
                }
            }

            string html = htmlDocument.DocumentNode.OuterHtml;
            html = Regex.Replace(html, @"\s*(<[^>]+>)\s*", "$1", RegexOptions.Singleline); // remove space between tags
            html = Regex.Replace(html, @"\s+", " "); // Replace multiple spaces with one

            return html;
        }

        /* Get working folder given domain */
        private string GetFolder()
        {
            switch (GetDomain())
            {
                case DomainType.Twitter:
                    return "TwitterScrape";
                case DomainType.Youtube:
                    return "YoutubeScrape";
                default:
                    return "GeneralScrape";
            }
        }

        /* Available domain types */
        public enum DomainType { Twitter, Youtube, General }

        string[] twitterDomains = new string[] { "https://www.twitter.com", "https://twitter.com" };
        string[] youtubeDomains = new string[] { "https://www.youtube.com", "https://youtube.com", "https://www.youtu.be", "https://youtu.be" };
        public DomainType GetDomain()
        {
            Uri uri = new Uri(current.link);
            string domain = uri.GetLeftPart(UriPartial.Authority);

            /* Return appropriate domain type */
            if (twitterDomains.Contains(domain)) return DomainType.Twitter;
            else if (youtubeDomains.Contains(domain)) return DomainType.Youtube;
            else return DomainType.General;
        }
    }
}
