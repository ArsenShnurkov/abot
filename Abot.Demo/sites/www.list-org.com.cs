using Abot.Crawler;
using Abot.Poco;
using System;
using Abot.Core;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Abot.Demo
{
    class www_list_org_com
    {
        static readonly string okato = "45263576";

        static  readonly int startPage = 190;
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

            crawler.Crawl (new Uri ("http://www.list-org.com/list.php?okato=" + okato + "&page="+startPage.ToString()));
            //CrawlResult result = crawler.Crawl(uriToCrawl);
            //Debug.Assert (result != null);
        }

        static readonly string queryListPagesRequest = @"http://www.list-org.com/list.php\?okato="+okato+"(&page=(?<pageid>.*))?$";
        static readonly string queryListItemRequest = @"http://www.list-org.com/company/(.*)$";

        private static IWebCrawler GetCustomBehaviorUsingLambdaWebCrawler ()
        {
            IWebCrawler crawler = CrawlHelpers.GetDefaultWebCrawler ();

            //If you set the log4net log level to "DEBUG" you will see a log message when any page is not allowed to be crawled.
            //NOTE: This is lambda is run after the regular ICrawlDecsionMaker.ShouldCrawlPage method is run.
            crawler.ShouldCrawlPage ((pageToCrawl, crawlContext) => {
                var uri = pageToCrawl.Uri.AbsoluteUri;

                // Check for list
                var expr1 = new Regex (queryListPagesRequest, RegexOptions.Multiline);
                var match1 = expr1.Match (uri);
                if (match1.Success) {
                    string pageid = match1.Groups ["pageid"].Value;
                    Trace.WriteLine (pageid);
                    if (!string.IsNullOrWhiteSpace(pageid))
                    {
                            if (int.Parse(pageid) >= startPage)
                            {
                                return new CrawlDecision { Allow = true };
                            }
                    }
                }


                var expr = new Regex (queryListItemRequest, RegexOptions.Multiline);
                var match = expr.Match (uri);
                if (match.Success) {
                    Trace.WriteLine (match.Groups [1].Value);
                    // check for items
                    var d = CheckIsAlreadyDownloaded (uri); if (d!= null) return d;

                    return new CrawlDecision { Allow = true };
                }

                return new CrawlDecision { Allow = false, Reason = "Useless content" };
                //return new CrawlDecision { Allow = true };
            });

            //Abot will still make the http request but will not read the raw content from the stream
            //NOTE: This lambda is run after the regular ICrawlDecsionMaker.ShouldDownloadPageContent method is run
            crawler.ShouldDownloadPageContent ((crawledPage, crawlContext) => {
                string uri = crawledPage.Uri.AbsoluteUri;

                var d = CheckIsAlreadyDownloaded (uri); if (d!= null) return d;

                return new CrawlDecision { Allow = true };
            });

            //Register a lambda expression that will tell Abot to not crawl links on any page that is not internal to the root uri.
            //NOTE: This lambda is run after the regular ICrawlDecsionMaker.ShouldCrawlPageLinks method is run
            crawler.ShouldCrawlPageLinks ((crawledPage, crawlContext) => {
                var uri = crawledPage.Uri.AbsoluteUri;
                var expr1 = new Regex (queryListPagesRequest, RegexOptions.Multiline);
                var match1 = expr1.Match (uri);
                if (match1.Success) {
                    Trace.WriteLine (match1.Groups ["pageid"].Value);
                    return new CrawlDecision { Allow = true };
                }
                return new CrawlDecision { Allow = false, Reason = "We crawl only links from pages" };
                /*
                if (!crawledPage.IsInternal) {
                    return new CrawlDecision { Allow = false, Reason = "We dont crawl links of external pages" };
                }

                //var d = CheckIsAlreadyDownloaded (uri); if (d!= null) return d;

                var expr = new Regex (queryListItemRequest, RegexOptions.Multiline);
                var match = expr.Match (uri);
                if (match.Success) {
                    Trace.WriteLine (match.Groups [1].Value);
                    return new CrawlDecision { Allow = false, Reason = "We dont crawl links from company to company" };
                }
                return new CrawlDecision { Allow = true };
                */
            });

            return crawler;
        }

        static CrawlDecision CheckIsAlreadyDownloaded (string uri)
        {
            var expr = new Regex (queryListItemRequest, RegexOptions.Multiline);
            var match = expr.Match (uri);
            if (match.Success) {
                string id = match.Groups [1].Value;
                string filename = GetFilename (id);
                filename = new FileInfo (filename).FullName;
                if (File.Exists (filename)) {
                    return new CrawlDecision {
                        Allow = false,
                        Reason = "Already downloaded"
                    };
                }
            }
            return null;
        }

        static void crawler_ProcessPageCrawlStarting (object sender, PageCrawlStartingArgs e)
        {
            //Process data
        }

        static void crawler_ProcessPageCrawlCompleted (object sender, PageCrawlCompletedArgs e)
        {
            if (e.CrawledPage.Content.Text.Contains ("мы хотим убедиться, что вы не робот")) {
                //throw new ApplicationException ("Нас принимают за робота");
                //e.CrawlContext.IsCrawlStopRequested = true;
                /*Process ExternalProcess = new Process();
                ExternalProcess.StartInfo.FileName = "/usr/bin/firefox";
                ExternalProcess.StartInfo.Arguments = "-new-instance http://www.list-org.com/bot.php";
                ExternalProcess.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
                ExternalProcess.Start();
                Thread.Sleep (15000);*/
                //ExternalProcess.WaitForExit();
                return;
            }

            if (e.CrawledPage.Content.Bytes == null) {
                var statusCode = e.CrawledPage.HttpWebResponse.StatusCode;
                Debug.WriteLine (statusCode);
                if (statusCode == System.Net.HttpStatusCode.RedirectKeepVerb) {
                    // 307 code
                    //string location = e.CrawledPage.HttpWebResponse.GetResponseHeader("Location");
                    /*Debug.WriteLine (location);
                    Process ExternalProcess = new Process();
                    ExternalProcess.StartInfo.FileName = "/usr/bin/firefox";
                    ExternalProcess.StartInfo.Arguments = "-new-instance -browser " + location;
                    ExternalProcess.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
                    ExternalProcess.Start();
                    Thread.Sleep (15000);*/
                    e.CrawlContext.IsCrawlStopRequested = true;
                    return;
                }
                return;
            }

            string uri = e.CrawledPage.Uri.AbsoluteUri;
            var expr = new Regex (queryListItemRequest, RegexOptions.Multiline);
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
            var di = new DirectoryInfo ("www.list-org.com");
            if (!di.Exists) {
                di.Create ();
            }
            return di.FullName + Path.DirectorySeparatorChar + id + ".htm";
        }
    }
}

