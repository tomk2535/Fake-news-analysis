using CefSharp;
using CefSharp.WinForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SuperCrawler
{
    class YoutubeHandler
    {
        /* The timer to handle scrolling */
        private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

        int scrollInterval = 2 * 1000;
        int timeoutTime = 10 * 1000;

        /* Maximum scroll for 2 minute */
        int maxScrollTime = 120 * 1000;
        int scrollTime = 0;

        int timeLeft;

        private bool scrapeEnded = true;

        private Form1 mainForm;
        ChromiumWebBrowser browser;

        public YoutubeHandler(Form1 mainForm, ChromiumWebBrowser browser)
        {
            this.mainForm = mainForm;
            this.browser = browser;

            timer.Interval = scrollInterval;
            timer.Tick += new EventHandler(Scroll_Tick);
        }

        public void HandlePage()
        {
            scrollTime = 0;
            timeLeft = timeoutTime;
            scrapeEnded = false;
            timer.Start();
        }

        /* Scroll browser to maximal long value */
        public void ScrollBrowser()
        {
            browser.ExecuteScriptAsync($"window.scrollTo(0, {long.MaxValue});");
        }

        /* Expand all buttons with class 'more-button' */
        public void ExpandAll()
        {
            string temp = @"
            var elements = document.getElementsByClassName('more-button');
            for(var i=0; i<elements.length; i++) {
                elements[i].click();
            }";
            browser.ExecuteScriptAsync(temp);
        }

        static string previousSections = "";
        private void Scroll_Tick(object sender, EventArgs e)
        {
            scrollTime += scrollInterval;

            ScrollBrowser();
            ExpandAll();

            browser.GetSourceAsync().ContinueWith(taskHtml =>
            {
                /* Open html in HtmlAgilityPack */
                HtmlAgilityPack.HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
                htmlDocument.LoadHtml(taskHtml.Result);

                /* If program have scrolled for two minutes */
                if(scrollTime > maxScrollTime)
                {
                    if (!scrapeEnded) EndScraping();
                    return;
                }

                /* Get the node containing all the comments */
                var sectionNode = htmlDocument.GetElementbyId("sections");

                if(sectionNode == null)
                {
                    if(!scrapeEnded) EndScraping();
                    return;
                }

                /* If nothing changed */
                if (sectionNode.OuterHtml == previousSections)
                {
                    timeLeft -= scrollInterval;
                    if (timeLeft <= 0)
                    {
                        if (!scrapeEnded) EndScraping();
                        return;
                    }
                }
                else // If new comments loaded
                {
                    previousSections = sectionNode.OuterHtml;
                    timeLeft = timeoutTime; // Reset timer
                }
            });
        }

        private void EndScraping()
        {
            scrapeEnded = true;

            timer.Stop();

            /* Sleep 10 sec to give time for expansion */
            Thread.Sleep(10000);

            mainForm.ScrapePage();
        }
    }
}
