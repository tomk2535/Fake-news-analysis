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
    class TwitterHandler
    {
        /* The timer to handle scrolling */
        private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

        int scrollInterval = 2 * 1000;

        // Force scrape if page does not update after 5 minutes
        int timeoutTime;

        private bool scrapeEnded = true;

        private Form1 mainForm;
        ChromiumWebBrowser browser;

        public TwitterHandler(Form1 mainForm, ChromiumWebBrowser browser)
        {
            this.mainForm = mainForm;
            this.browser = browser;

            timer.Interval = scrollInterval;
            timer.Tick += new EventHandler(Scroll_Tick);
        }

        public void HandlePage()
        {
            timeoutTime = 3 * 60 * 1000; // Set timeout time
            scrapeEnded = false;
            timer.Start();
        }

        /* Snap view to last footer */
        public void ScrollBrowser()
        {
            string temp =
                "var footers = document.getElementsByClassName('permalink-footer')\n" +
                "footers[footers.length-1].scrollIntoView();";
            browser.ExecuteScriptAsync(temp);
        }

        /* Expand all buttons with class 'ThreadedConversation-moreRepliesLink' */
        public void ExpandAll()
        {
            string temp = @"
            var elements = document.getElementsByClassName('ThreadedConversation-moreRepliesLink');
            for(var i=0; i<elements.length; i++) {
                elements[i].click();
            }";
            browser.ExecuteScriptAsync(temp);
        }
        
        private void Scroll_Tick(object sender, EventArgs e)
        {
            /* Force scrape page after 3 minutes of waiting */
            timeoutTime -= scrollInterval;
            if(timeoutTime < 0)
            {
                if (!scrapeEnded) EndScraping();
                return;
            }

            ScrollBrowser();

            browser.GetSourceAsync().ContinueWith(taskHtml =>
            {
                /* Open html in HtmlAgilityPack */
                HtmlAgilityPack.HtmlDocument htmlDocument = new HtmlAgilityPack.HtmlDocument();
                htmlDocument.LoadHtml(taskHtml.Result);

                var descendants = htmlDocument.GetElementbyId("descendants");

                /* If no descendants  */
                if (descendants == null)
                {
                    if (!scrapeEnded) EndScraping();
                    return;
                }

                var steamContainer = descendants.SelectSingleNode("./div");

                /* If no steam container exists */
                if (steamContainer == null)
                {
                    if (!scrapeEnded) EndScraping();
                    return;
                }

                var attribute = steamContainer.Attributes["data-min-position"];

                /* If reached end (Data-min-position flag removed) */
                if (attribute == null || attribute.Value.Length == 0)
                {
                    if (!scrapeEnded) EndScraping();
                }
            });
        }

        private void EndScraping()
        {
            scrapeEnded = true;

            timer.Stop();

            /* Give last scroll 3 sec */
            Thread.Sleep(3000);

            ExpandAll();

            /* Sleep 7 sec to give time for expansion */
            Thread.Sleep(7000);

            mainForm.ScrapePage();
        }
    }
}
