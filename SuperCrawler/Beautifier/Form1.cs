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

using Microsoft.WindowsAPICodePack.Dialogs;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace Beautifier
{
    public partial class Form1 : Form
    {
        HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();

        public Form1()
        {
            InitializeComponent();
            LoadPath();
        }

        /* Set scrape folder */
        private void buttonScrapeFolder_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = "C:\\Users";
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                SavePath(dialog.FileName);
            }
        }

        /* Save path to file */
        private void SavePath(string path)
        {
            File.WriteAllText("Path.txt", path);
            textboxScrapeFolder.Text = path;
        }

        /* Load path from file */
        private void LoadPath()
        {
            if (File.Exists("Path.txt"))
            {
                textboxScrapeFolder.Text = File.ReadAllText("Path.txt");
            }
        }

        /* Beautifies text from HTML to structured JSON */
        private void buttonBeautify_Click(object sender, EventArgs e)
        {
            string folder = textboxScrapeFolder.Text;

            /* Read folder */
            if (folder == "")
            {
                textboxOutput.Text = "No folder selected";
                return;
            }

            /* Get twitter folder */
            string TwitterFolder = folder + "\\TwitterScrape";
            if (!Directory.Exists(TwitterFolder))
            {
                textboxOutput.Text = "No Twitter folder found";
                return;
            }

            /* Get all twitter scrapes */
            string[] twitterScrapes = Directory.GetFiles(TwitterFolder);
            textboxOutput.Text = $"{twitterScrapes.Count()} scrapes found!";

            /* Beautified twitter folder and create if needed */
            string TwitterBeautified = folder + "\\TwitterBeautified";
            if (!Directory.Exists(TwitterBeautified))
            {
                Directory.CreateDirectory(TwitterBeautified);
            }

            int n = 0;

            /* Scrape each page */
            foreach(string scrape in twitterScrapes)
            {
                //if (n++ >= 5) break;

                string filename = Path.GetFileNameWithoutExtension(scrape);
                string path = $"{folder}\\TwitterBeautified\\{filename}.txt";

                string doc = File.ReadAllText(scrape);

                /* Load document */
                htmlDoc.LoadHtml(doc);

                BeautifyPage(htmlDoc, path);
            }
        }

        /* Scrape page when fully loaded */
        public void BeautifyPage(HtmlAgilityPack.HtmlDocument htmlDoc, string path)
        {
            var main = htmlDoc.DocumentNode.SelectSingleNode(".//div[@role='main']");

            /* If main is not found, tweet have been removed */
            if(main == null)
            {
                File.WriteAllText(path, "Tweet has been removed");
                return;
            }

            // >>>>>>>>>>>>>>>>>>>>>>
            //   Scrape main tweet
            // <<<<<<<<<<<<<<<<<<<<<<
            var mainTweet = main.SelectSingleNode(".//div[contains(@class, 'permalink-tweet-container')]/div");

            /* If main found, but no maintweet, this is not a tweet. Could be profile page */
            if (mainTweet == null)
            {
                File.WriteAllText(path, "Not a tweet");
                return;
            }

            string mainTweetID = mainTweet.Attributes["data-tweet-id"].Value;
            string mainUserName = mainTweet.Attributes["data-name"].Value;
            string mainScreenName = mainTweet.Attributes["data-screen-name"].Value;

            bool mainVerified = mainTweet.SelectSingleNode(".//span[@class='UserBadges']").SelectSingleNode("./span[contains(@class,'Icon--verified')]") != null;

            string mainTime = mainTweet.SelectSingleNode(".//small[@class='time']/a/span").Attributes["data-time-ms"].Value;

            /* Get main tweet text */
            var tweetTextNode = mainTweet.SelectSingleNode(".//div[@class='js-tweet-text-container']/p");
            string mainTweetText = HandleTweetText(tweetTextNode);

            var mainFooter = mainTweet.SelectSingleNode(".//div[@class='stream-item-footer']");
            ReadFooter(mainFooter, out string mainReplies, out string mainRetweets, out string mainFavorites);

            JObject root = new JObject(
                new JProperty("ScrapeTimeUTC", DateTime.UtcNow.Subtract(DateTime.MinValue.AddYears(1969)).TotalMilliseconds),
                new JProperty("TweetID", mainTweetID),
                new JProperty("Time", mainTime),
                new JProperty("UserName", mainUserName),
                new JProperty("ScreenName", mainScreenName),
                new JProperty("Verified", mainVerified),
                new JProperty("Text", mainTweetText),
                new JProperty("Replies", mainReplies),
                new JProperty("Retweets", mainRetweets),
                new JProperty("Favorites", mainFavorites)
            );


            // >>>>>>>>>>>>>>>>>>>>>>
            //    Scrape comments
            // <<<<<<<<<<<<<<<<<<<<<<
            JArray replyArray = new JArray();
            var threadContainer = htmlDoc.GetElementbyId("stream-items-id");
            var threads = threadContainer.SelectNodes("./li");

            /* Total amounts of replies */
            int replyCount = 0;

            /* If there are any comments */
            if(threads != null)
            {
                foreach (var thread in threads)
                {
                    /* Approach for tweet threads is slightly different */
                    if (thread.Attributes["class"].Value.Equals("ThreadedConversation"))
                    {
                        JArray streamThread = new JArray();
                        HtmlNodeCollection streamTweets = thread.SelectNodes(".//div[contains(@class,'js-stream-tweet')]");
                        foreach (HtmlNode streamTweet in streamTweets)
                        {
                            streamThread.Add(GetSingleComment(streamTweet));
                            replyCount++;
                        }
                        replyArray.Add(streamThread);
                    }
                    else
                    {
                        JArray singleTweet = new JArray();
                        HtmlNode streamTweet = thread.SelectSingleNode(".//div[contains(@class,'js-stream-tweet')]");
                        singleTweet.Add(GetSingleComment(streamTweet));
                        replyArray.Add(singleTweet);
                        replyCount++;
                    }
                }
            }

            root.Add("FoundReplies", replyCount);

            // Add all comments to root
            root.Add("ReplyArray", replyArray);

            // Save data
            File.WriteAllText(path, root.ToString());
        }

        JObject GetSingleComment(HtmlNode streamTweet)
        {
            string tweetID = streamTweet.Attributes["data-tweet-id"].Value;
            string userName = streamTweet.Attributes["data-name"].Value;
            string screenName = streamTweet.Attributes["data-screen-name"].Value;

            /* Return simple JObject if tweet is unavailable */
            if (streamTweet.Attributes["class"].Value.Contains("withheld-tweet"))
            {
                return new JObject(
                    new JProperty("TweetID", tweetID),
                    new JProperty("UserName", userName),
                    new JProperty("ScreenName", screenName),
                    new JProperty("Available", false)
                );
            }

            bool verified = streamTweet.SelectSingleNode(".//span[@class='UserBadges']").SelectSingleNode("./span[contains(@class,'Icon--verified')]") != null;

            string time = streamTweet.SelectSingleNode(".//small[@class='time']/a/span").Attributes["data-time-ms"].Value;


            /* Get tweet text */
            var textContainer = streamTweet.SelectSingleNode(".//div[@class='js-tweet-text-container']/p");
            string tweetText = HandleTweetText(textContainer);

            /* Footer node */
            var footer = streamTweet.SelectSingleNode(".//div[@class='stream-item-footer']");
            ReadFooter(footer, out string replies, out string retweets, out string favorites);

            return new JObject(
                new JProperty("TweetID", tweetID),
                new JProperty("UserName", userName),
                new JProperty("ScreenName", screenName),
                new JProperty("Available", true),
                new JProperty("Time", time),
                new JProperty("Verified", verified),
                new JProperty("Text", tweetText),
                new JProperty("Replies", replies),
                new JProperty("Retweets", retweets),
                new JProperty("Favorites", favorites)
            );
        }

        /* Transform the tweets into much better text representations */
        private string HandleTweetText(HtmlNode textNode)
        {
            /* Is there any links in the html */
            var aNodes = textNode.SelectNodes("./a");
            if (aNodes != null)
            {
                foreach (HtmlNode a in aNodes)
                {
                    /* New text to be injected */
                    string replacement = "";

                    var classes = a.Attributes["class"];
                    /* Is link reply or hashtag? */
                    if (classes.Value.Contains("twitter-atreply") || classes.Value.Contains("twitter-hashtag"))
                    {
                        replacement = $" {HtmlEntity.DeEntitize(a.InnerText)} ";
                    }

                    /* Set new text */
                    var newNode = HtmlNode.CreateNode(replacement);
                    a.ParentNode.ReplaceChild(newNode, a);
                }
            }

            /* Compress uneccesary whitespaces and returns */
            string tweetText = HtmlEntity.DeEntitize(textNode.InnerText).Trim();
            tweetText = Regex.Replace(tweetText, @"\s+", " ");
            return tweetText;
        }

        /* Scrape footer */
        public void ReadFooter(HtmlNode footer, out string replies, out string retweets, out string favorites)
        {
            replies = footer.SelectSingleNode("./div/span/span").Attributes["data-tweet-stat-count"].Value;
            retweets = footer.SelectSingleNode("./div/span[2]/span").Attributes["data-tweet-stat-count"].Value;
            favorites = footer.SelectSingleNode("./div/span[3]/span").Attributes["data-tweet-stat-count"].Value;
        }
    }
}
