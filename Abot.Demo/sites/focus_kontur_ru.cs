using Abot.Crawler;
using Abot.Poco;
using System;
using Abot.Core;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;

namespace Abot.Demo
{
    class focus_kontur_ru
    {
        public static void Crawl ()
        {
            IWebCrawler crawler;

            //Uncomment only one of the following to see that instance in action
            //crawler = GetDefaultWebCrawler();
            //crawler = GetManuallyConfiguredWebCrawler();
            crawler = GetCustomBehaviorUsingLambdaWebCrawler ();

            //Subscribe to any of these asynchronous events, there are also sychronous versions of each.
            //This is where you process data about specific events of the crawl
            crawler.PageCrawlStartingAsync += crawler_ProcessPageCrawlStarting;
            crawler.PageCrawlCompletedAsync += crawler_ProcessPageCrawlCompleted;
            crawler.PageCrawlDisallowedAsync += crawler_PageCrawlDisallowed;
            crawler.PageLinksCrawlDisallowedAsync += crawler_PageLinksCrawlDisallowed;

            crawler.Crawl (new Uri ("https://focus.kontur.ru/search?query=%D0%9E%D0%9E%D0%9E&region=77&industry=sI"));
            //CrawlResult result = crawler.Crawl(uriToCrawl);
            //Debug.Assert (result != null);
        }

        static readonly string queryStartSearch = @"https://focus.kontur.ru/search\?query=(.*)&region=77&industry=sI";
        static readonly string queryRegexText = @"https://focus.kontur.ru/entity\?query=(.*)$";

        private static IWebCrawler GetCustomBehaviorUsingLambdaWebCrawler ()
        {
            IWebCrawler crawler = CrawlHelpers.GetDefaultWebCrawler ();

            //Register a lambda expression that will make Abot not crawl any url that has the word "ghost" in it.
            //For example http://a.com/ghost, would not get crawled if the link were found during the crawl.
            //If you set the log4net log level to "DEBUG" you will see a log message when any page is not allowed to be crawled.
            //NOTE: This is lambda is run after the regular ICrawlDecsionMaker.ShouldCrawlPage method is run.
            crawler.ShouldCrawlPage ((pageToCrawl, crawlContext) => {
                var uri = pageToCrawl.Uri.AbsoluteUri;
                var expr1 = new Regex (queryStartSearch, RegexOptions.Multiline);
                var match1 = expr1.Match (uri);
                if (match1.Success) {
                    Trace.WriteLine (match1.Groups [1].Value);
                    return new CrawlDecision { Allow = true };
                }

                var expr = new Regex (queryRegexText, RegexOptions.Multiline);
                var match = expr.Match (uri);
                if (match.Success) {
                    Trace.WriteLine (match.Groups [1].Value);
                    return new CrawlDecision { Allow = true };
                }

                return new CrawlDecision { Allow = false, Reason = "Useless content" };
            });

            //Register a lambda expression that will tell Abot to not download the page content for any page after 5th.
            //Abot will still make the http request but will not read the raw content from the stream
            //NOTE: This lambda is run after the regular ICrawlDecsionMaker.ShouldDownloadPageContent method is run
            crawler.ShouldDownloadPageContent ((crawledPage, crawlContext) => {
                string url = crawledPage.Uri.AbsoluteUri;
                var expr = new Regex (queryRegexText, RegexOptions.Multiline);
                var match = expr.Match (url);
                if (match.Success) {
                    Trace.WriteLine (match.Groups [1].Value);
                }
                return new CrawlDecision { Allow = true };
            });

            //Register a lambda expression that will tell Abot to not crawl links on any page that is not internal to the root uri.
            //NOTE: This lambda is run after the regular ICrawlDecsionMaker.ShouldCrawlPageLinks method is run
            crawler.ShouldCrawlPageLinks ((crawledPage, crawlContext) => {
                if (!crawledPage.IsInternal) {
                    return new CrawlDecision { Allow = false, Reason = "We dont crawl links of external pages" };
                }

                var expr = new Regex (queryRegexText, RegexOptions.Multiline);
                var match = expr.Match (crawledPage.Uri.AbsoluteUri);
                if (match.Success) {
                    string id = match.Groups [1].Value;
                    string filename = GetFilename (id);
                    filename = new FileInfo (filename).FullName;
                    if (File.Exists (filename)) {
                        return new CrawlDecision { Allow = false, Reason = "Already downloaded" };
                    }

                }

                return new CrawlDecision { Allow = true };
            });

            return crawler;
        }

        static void crawler_ProcessPageCrawlStarting (object sender, PageCrawlStartingArgs e)
        {
            //Process data
        }

        static void crawler_ProcessPageCrawlCompleted (object sender, PageCrawlCompletedArgs e)
        {
            string uri = e.CrawledPage.Uri.AbsoluteUri;
            var expr = new Regex (queryRegexText, RegexOptions.Multiline);
            var match = expr.Match (uri);
            if (match.Success) {
                string id = match.Groups [1].Value;
                CrawlHelpers.CreateFile (GetFilename (id), e.CrawledPage.Content.Bytes);
            }
        }

        static void crawler_PageLinksCrawlDisallowed (object sender, PageLinksCrawlDisallowedArgs e)
        {
            //Process data
        }

        static void crawler_PageCrawlDisallowed (object sender, PageCrawlDisallowedArgs e)
        {
            //Process data
        }

        static string GetFilename (string id)
        {
            var di = new DirectoryInfo ("focus.kontur.ru");
            if (!di.Exists) {
                di.Create ();
            }
            return di.FullName + Path.DirectorySeparatorChar + id + ".htm";
        }
    }
}

