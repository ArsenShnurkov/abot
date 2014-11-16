using System;
using System.IO;
using Abot.Crawler;
using Abot.Core;
using System.Diagnostics;

namespace Abot.Demo
{
    public class CrawlHelpers
    {
        public static void CreateFile (string str, byte[] bytes)
        {
            using (var file = new FileStream (str, FileMode.OpenOrCreate)) {
                file.Write (bytes, 0, bytes.Length);
            }
            Debug.WriteLine ("File created: " + str);
        }

        public static IWebCrawler GetDefaultWebCrawler ()
        {
            var config = WebCrawler.GetCrawlConfigurationFromConfigFile ();
            //var pageRequester = new PageRequesterWithCookies (config);
            var pageRequester = new PageRequester (config);

            var crawler = new PoliteWebCrawler (config, null, null, null, pageRequester, null, null, null, null);
            return crawler;
        }

        public static Uri GetSiteToCrawl (string[] args)
        {
            string userInput = "";
            if (args.Length < 1) {
                System.Console.WriteLine ("Please enter ABSOLUTE url to crawl:");
                userInput = System.Console.ReadLine ();
            } else {
                userInput = args [0];
            }

            if (string.IsNullOrWhiteSpace (userInput))
                throw new ApplicationException ("Site url to crawl is as a required parameter");

            return new Uri (userInput);
        }
    }
}

