// Copyright © 2023, Gary Gocek, www.gocek.org
// See app.config for usage instructions.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Net;
using System.ServiceModel.Syndication;
using System.Reflection;
using System.Xml.Linq;
using System.Xml;
using System.IO;

namespace mp3scraper
{
    /// <summary>
    /// Get HTML markup from feed, scrape it into dataset, search for MP3 links, write RSS podcast file.
    /// </summary>
    public class Program
    {
        public static mp3scrApi.Mp3scrApi mp3scr = new mp3scrApi.Mp3scrApi();

        public static void Main(string[] args)
        {
            mp3scr.ScrapeExtensionFromObj(".mp3"); // This program looks for MP3 links

            object o = null;
            SyndicationFeed sf = null;
            SyndicationLink mpSl = null;
            DateTimeOffset latestItemLastModDt = new DateTime(1900, 1, 1);
            string testIndexVal = string.Empty;

            try
            {
                // Get some filtering guidance from the config file
                o = ConfigurationManager.AppSettings["testIndex"];
                testIndexVal = (o == null) ? string.Empty : o.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Main (filtering guidance): " + ex.Message);
                Environment.Exit(-2);
            }

            try
            {
                // The ServicePoint class provides connection management for HTTP connections.
                // This resolves occasional issues with some SSL sites
                // https://stackoverflow.com/questions/2859790/the-request-was-aborted-could-not-create-ssl-tls-secure-channel
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.DefaultConnectionLimit = 9999;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls13;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Main (ServicePoint): " + ex.Message);
                Environment.Exit(-3);
            }

            try
            {
                // Stuff for RSS generator property
                string verVal = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                string apiVal = mp3scr.Ver;
                string genVal = "mp3scraper v" + verVal + ", mp3scrApi v" + apiVal + ", by Gary Gocek, http://www.gocek.org/software/";
                Console.WriteLine(genVal);

                for (int iii = 1; iii <= 9999; iii++)
                {

                    // If the test index is set, only process the matching index
                    if (!string.IsNullOrEmpty(testIndexVal) && (testIndexVal != iii.ToString("d4")))
                    {
                        Console.WriteLine(iii.ToString("d4"));
                        Console.WriteLine("Skipping feed " + iii.ToString("d4") + " because testIndex is set to " + testIndexVal);
                        continue;
                    }

                    o = ConfigurationManager.AppSettings["channelTitle" + iii.ToString("d4")];
                    mp3scr.ChannelTitleFromObj(o);
                    o = ConfigurationManager.AppSettings["enabled" + iii.ToString("d4")];
                    mp3scr.EnabledFromObj(o);
                    if (!mp3scr.Enabled)
                    {
                        // If the channel title is empty, then there is no configuration for the index.
                        if (!string.IsNullOrEmpty(mp3scr.ChannelTitle))
                        {
                            Console.WriteLine(iii.ToString("d4"));
                            Console.WriteLine("Skipping disabled feed " + mp3scr.ChannelTitle);
                        }
                        continue;
                    }

                    o = ConfigurationManager.AppSettings["url" + iii.ToString("d4")];
                    mp3scr.UrlFromObj(o);
                    if (string.IsNullOrEmpty(mp3scr.Url))
                        continue;
                    // Get the set of chars to look backward for, before the .mp3, usually just "=" but not always
                    o = ConfigurationManager.AppSettings["urlPrefix" + iii.ToString("d4")];
                    mp3scr.UrlPrefixFromObj(o);
                    // Get some filtering guidance from the config file
                    o = ConfigurationManager.AppSettings["mp3Filter" + iii.ToString("d4")];
                    mp3scr.Mp3FilterFromObj(o);
                    o = ConfigurationManager.AppSettings["indirectFilter" + iii.ToString("d4")];
                    mp3scr.IndirectFilterFromObj(o);
                    // Determine if the current file to be scraped contains MP3 links (direct)
                    // or links to files containing MP3 links.
                    o = ConfigurationManager.AppSettings["indirect" + iii.ToString("d4")];
                    mp3scr.IndirectFromObj(o);
                    // The config file specifies the expected date order of the URLs.
                    o = ConfigurationManager.AppSettings["sortDescending" + iii.ToString("d4")];
                    mp3scr.SortDescendingFromObj(o);
                    // Feed file naming
                    o = ConfigurationManager.AppSettings["existingFeedFolder" + iii.ToString("d4")];
                    mp3scr.ExistingFeedFolderFromObj(o);
                    o = ConfigurationManager.AppSettings["destBase" + iii.ToString("d4")];
                    mp3scr.DestBaseFromObj(o);
                    o = ConfigurationManager.AppSettings["destFolderName" + iii.ToString("d4")];
                    mp3scr.DestFolderNameFromObj(o);
                    // Channel and item value helpers
                    o = ConfigurationManager.AppSettings["guidPrefix" + iii.ToString("d4")];
                    mp3scr.GuidPrefixFromObj(o);
                    // The config file specifies whether an MP3 link should be retained in an existing feed
                    // if the scraped web page no longer contains the link.
                    o = ConfigurationManager.AppSettings["retainOrphans" + iii.ToString("d4")];
                    mp3scr.RetainOrphansFromObj(o);
                    // For use when prepending a relative name
                    o = ConfigurationManager.AppSettings["stripBaseName" + iii.ToString("d4")];
                    mp3scr.StripBaseNameFromObj(o);
                    // Prepend relative URLs with this
                    o = ConfigurationManager.AppSettings["prependRelative" + iii.ToString("d4")];
                    mp3scr.PrependRelativeFromObj(o);
                    // Number of days to wait to reprocess a feed
                    o = ConfigurationManager.AppSettings["refreshDays" + iii.ToString("d4")];
                    mp3scr.RefreshDaysFromObj(o);
                    o = ConfigurationManager.AppSettings["ftpPath" + iii.ToString("d4")];
                    mp3scr.FtpPathFromObj(o);
                    o = ConfigurationManager.AppSettings["ftpUserName" + iii.ToString("d4")];
                    mp3scr.FtpUserNameFromObj(o);
                    o = ConfigurationManager.AppSettings["ftpPassword" + iii.ToString("d4")];
                    mp3scr.FtpPasswordFromObj(o);
                    o = ConfigurationManager.AppSettings["permissionRevoked" + iii.ToString("d4")];
                    mp3scr.PermissionRevokedFromObj(o);
                    o = ConfigurationManager.AppSettings["channelNotes" + iii.ToString("d4")];
                    mp3scr.ChannelNotesFromObj(o);
                    // The config file specifies whether MP3 links should be forced to HTTP.
                    o = ConfigurationManager.AppSettings["allowHttps" + iii.ToString("d4")];
                    mp3scr.AllowHttpsFromObj(o);
                    // The config file specifies whether a formula should be used to wrap around to the beginning of a non-changing source feed
                    o = ConfigurationManager.AppSettings["wraparound" + iii.ToString("d4")];
                    mp3scr.WraparoundFromObj(o);
                    // Max items to test remotely and place in RSS
                    o = ConfigurationManager.AppSettings["maxWebRequests" + iii.ToString("d4")];
                    mp3scr.MaxWebRequestsFromObj(o);


                    // Get any existing RSS file, for merging with the latest MP3s and checking the refresh date.
                    Console.WriteLine(iii.ToString("d4"));
                    Console.WriteLine("Looking for RSS for " + mp3scr.ChannelTitle + ": " + mp3scr.Url);
                    sf = null;
                    if (!string.IsNullOrEmpty(mp3scr.ExistingFeedFolder) && !string.IsNullOrEmpty(mp3scr.DestBase))
                    {
                        string existingRssUrl = System.IO.Path.Combine(mp3scr.ExistingFeedFolder, mp3scr.DestBase);
                        try
                        {
                            sf = mp3scr.RssFromUrl(existingRssUrl);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                            // Ignore exceptions from RssFromUrl - the remote RSS probably doesn't exist
                        }
                    }
                    // If testIndex is set, ignore refreshDays.
                    // If refreshDays is not set, ignore refreshDays.
                    // If there is no existing feed, ignore refreshDays.
                    if (string.IsNullOrEmpty(testIndexVal) && (sf != null) && (mp3scr.RefreshDays > 0))
                    {
                        // There is an existing feed and the number of days to refresh has been set.
                        // Determine the most recent item in the feed.
                        if (sf.Items.Count() > 0)
                        {
                            latestItemLastModDt = (from feedItem in sf.Items
                                                   select feedItem.LastUpdatedTime).Max();
                            // Determine if enough time has passed to refresh the feed.
                            if (DateTime.UtcNow < latestItemLastModDt.Add(new TimeSpan(mp3scr.RefreshDays, 0, 0, 0)))
                            {
                                // The current datetime is NOT refreshDays after the datetime of the most recent
                                // existing feed item. It's too soon to refresh the feed.
                                Console.WriteLine("Next refresh " + latestItemLastModDt.Add(new TimeSpan(mp3scr.RefreshDays, 0, 0, 0)).ToString() + ", skipping " + mp3scr.ChannelTitle + " (" + mp3scr.Url + ")");
                                continue;
                            }
                            else
                            {
                                Console.WriteLine("Latest item " + latestItemLastModDt.ToString() + ", scraping " + mp3scr.ChannelTitle + " (" + mp3scr.Url + ")");
                            }
                        }
                        else
                        {
                            Console.WriteLine("RefreshDays ignored, no previous items, scraping " + mp3scr.ChannelTitle + " (" + mp3scr.Url + ")");
                        }
                    }
                    else
                    {
                        Console.WriteLine("RefreshDays " + mp3scr.RefreshDays.ToString() + ", TestIndex '" + testIndexVal.ToString() + "', scraping " + mp3scr.ChannelTitle + " (" + mp3scr.Url + ")");
                    }

                    // Get the full markup of the web page
                    string curMarkup = string.Empty;
                    try
                    {
                        curMarkup = mp3scr.Mp3PageMarkup(mp3scr.Url);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        continue;
                    }

                    List<string> curAddrs = new List<string>();
                    // Read the current file and get the links in that file.
                    try
                    {
                        if (!mp3scr.Indirect)
                        {
                            // These addresses will contain MP3 links
                            curAddrs = mp3scr.Mp3PageScrape(curMarkup, mp3scr.Mp3Filter, mp3scr.PrependRelative, mp3scr.StripBaseName, mp3scr.UrlPrefix);
                        }
                        else
                        {
                            // Each of these addresses will contain links to files containing links to MP3 files.
                            List<string> iAddrs = mp3scr.IndirectPageScrape(curMarkup, mp3scr.IndirectFilter);
                            foreach (string iMarkup in iAddrs)
                            {
                                // These addresses will contain MP3 links
                                curAddrs = mp3scr.Mp3PageScrape(curMarkup, mp3scr.Mp3Filter, mp3scr.PrependRelative, mp3scr.StripBaseName, mp3scr.UrlPrefix);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        continue;
                    }

                    // Remove the duplicates from the list of addresses
                    List<string> noDupsCurAddrs = curAddrs.Distinct().ToList();
                    // Ensure that the addresses are sorted in reverse chronological order.
                    // If SortDescending is false, reverse the list now.
                    if (!mp3scr.SortDescending)
                        noDupsCurAddrs.Reverse();

                    // RSS items from an existing feed to retain
                    List<SyndicationItem> itemsToKeep = new List<SyndicationItem>();
                    // RSS items created from scraped MP3 links
                    List<SyndicationItem> itemsToAdd = new List<SyndicationItem>();
                    int webRequestCnt = 0;

                    // In general, the scraped MP3 links are used to create new RSS items with new
                    // sizes and mod dates. Theoretically, an MP3 link could be saved in an RSS file
                    // but later removed from the host web page, but the file might still exist on
                    // the back end. If RetainOrphans is true, add RSS items to itemsToKeep if
                    // the RSS item's link is not found in noDupsCurAddrs.
                    // The host removed the MP3 link for a reason, so this could be considered discourteous.
                    // The links are tested for existence.
                    // If RetainOrphans is false, the RSS items are not retained and the links will be lost.
                    // The number of RSS links could be the maximum, i.e., MaxWebRequests. If RetainOrphans
                    // is true, the item check will count towards the maximum and there won't be any room
                    // left for new items. So, if RetainOrphans is true then MaxWebRequests should be 0 and
                    // you'll need to hope the host does not throttle the number of web requests.
                    if ((sf != null) && mp3scr.RetainOrphans)
                    {
                        foreach (SyndicationItem si in sf.Items)
                        {
                            string myErrorMsg = string.Empty;
                            // Get the MP3 link.
                            // There are usually two link objects, one for the item's parent feed, and one for the MP3 file.
                            // If there are multiple MP3 links in one item, this will not work right.
                            mpSl = (from sil in si.Links
                                    where sil.Uri.AbsoluteUri.EndsWith(mp3scr.ScrapeExtension, StringComparison.InvariantCultureIgnoreCase)
                                    select sil).FirstOrDefault();
                            // If the RSS item's MP3 link is not in the list of scraped links, create an RSS item.
                            // The file will either be found with its current size and mod date,
                            // or the file will no longer exist and the RSS item will not be created.
                            if (!noDupsCurAddrs.Contains(mpSl.Uri.AbsoluteUri, StringComparer.InvariantCultureIgnoreCase) &&
                                ((webRequestCnt < mp3scr.MaxWebRequests) || (mp3scr.MaxWebRequests <= 0)))
                            {
                                webRequestCnt++;
                                Console.WriteLine("RSS item " + webRequestCnt.ToString() + ": " + mpSl.Uri.AbsoluteUri);
                                try
                                {
                                    SyndicationItem newSi =
                                        mp3scr.ItemForMp3(mpSl.Uri.AbsoluteUri, mp3scr.ChannelTitle, mp3scr.GuidPrefix, mp3scr.Url, mp3scr.AllowHttps);
                                    if (newSi != null)
                                    {
                                        itemsToKeep.Add(newSi);
                                    }
                                    else
                                    {
                                        Console.WriteLine("While retaining orphans, ItemForMp3 returned null for " + mpSl.Uri.AbsoluteUri);
                                    }
                                }
                                catch (WebException webEx)
                                {
                                    Console.WriteLine("While retaining orphans, ItemForMp3 returned message for " + mpSl.Uri.AbsoluteUri +
                                        " | " + webEx.Message + " | " + (webEx.InnerException != null ? webEx.InnerException.Message : ""));
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("While retaining orphans, ItemForMp3 returned message for " + mpSl.Uri.AbsoluteUri +
                                        " | " + ex.Message + " | " + (ex.InnerException != null ? ex.InnerException.Message : ""));
                                }
                            }
                            else if (!noDupsCurAddrs.Contains(mpSl.Uri.AbsoluteUri, StringComparer.InvariantCultureIgnoreCase) &&
                                (webRequestCnt >= mp3scr.MaxWebRequests) && (mp3scr.MaxWebRequests > 0))
                            {
                                Console.WriteLine("RSS item " + webRequestCnt.ToString() + " not added because it would exceed MaxWebRequests (" +
                                    mp3scr.MaxWebRequests.ToString() + "): " + mpSl.Uri.AbsoluteUri);
                                webRequestCnt++;
                            }
                        }
                    }

                    // Create a new RSS item for each scraped MP3 link.
                    // itemsToKeep will not contain any of these (because they were tested above).
                    // Previously generated RSS link items are discarded and recreated.
                    foreach (string mp3Addr in noDupsCurAddrs)
                    {
                        webRequestCnt++;
                        if ((webRequestCnt > mp3scr.MaxWebRequests) && (mp3scr.MaxWebRequests > 0))
                        {
                            Console.WriteLine("RSS item " + webRequestCnt.ToString() + " not added because it would exceed MaxWebRequests (" +
                                mp3scr.MaxWebRequests.ToString() + "): " + System.Web.HttpUtility.UrlDecode(mp3Addr, System.Text.Encoding.UTF8));
                            continue;
                        }
                        // The count so far is within the max or the mex is unlimited.
                        Console.WriteLine("RSS item " + webRequestCnt.ToString() + ": " + System.Web.HttpUtility.UrlDecode(mp3Addr, System.Text.Encoding.UTF8));
                        try
                        {
                            SyndicationItem newSi = mp3scr.ItemForMp3(System.Web.HttpUtility.UrlDecode(mp3Addr, System.Text.Encoding.UTF8), mp3scr.ChannelTitle, mp3scr.GuidPrefix, mp3scr.Url, mp3scr.AllowHttps);
                            if (newSi != null)
                            {
                                itemsToAdd.Add(newSi);
                            }
                            else
                            {
                                Console.WriteLine("While creating RSS items, ItemForMp3 returned null for " + System.Web.HttpUtility.UrlDecode(mp3Addr, System.Text.Encoding.UTF8));
                            }
                        }
                        catch (WebException webEx)
                        {
                            Console.WriteLine("While creating RSS items, ItemForMp3 returned message for " + System.Web.HttpUtility.UrlDecode(mp3Addr, System.Text.Encoding.UTF8) +
                                " | " + webEx.Message + " | " + (webEx.InnerException != null ? webEx.InnerException.Message : ""));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("While creating RSS items, ItemForMp3 returned message for " + System.Web.HttpUtility.UrlDecode(mp3Addr, System.Text.Encoding.UTF8) +
                                " | " + ex.Message + " | " + (ex.InnerException != null ? ex.InnerException.Message : ""));
                        }
                    }

                    Console.WriteLine("Creating RSS feed...");
                    // Create the new RSS feed and add all the items
                    try
                    {
                        sf = mp3scr.GenerateFeed(mp3scr.ChannelTitle, mp3scr.Url, "ar", mp3scr.PermissionRevoked, itemsToAdd, itemsToKeep, genVal, mp3scr.ChannelNotes);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        continue;
                    }

                    // Wraparound processing
                    if (mp3scr.Wraparound)
                    {
                        List<SyndicationItem> myItems = mp3scr.WrapAroundItems(sf);
                        if (myItems != null)
                        {
                            sf.Items = myItems;
                        }
                    }

                    // Save the feed
                    string localName = System.IO.Path.Combine(mp3scr.DestFolderName, mp3scr.DestBase);
                    Console.WriteLine("Saving " + localName);
                    sf.LastUpdatedTime = new DateTimeOffset(DateTime.UtcNow, new TimeSpan(0));
                    using (System.Xml.XmlWriter xmlWriter = System.Xml.XmlWriter.Create(localName))
                    {
                        sf.SaveAsRss20(xmlWriter);
                    }

                    // Upload the feed file to the FTP folder
                    try
                    {
                        if (!string.IsNullOrEmpty(mp3scr.FtpPath))
                        {
                            mp3scr.FtpUpload(localName, mp3scr.DestBase, mp3scr.FtpPath, mp3scr.FtpUserName, mp3scr.FtpPassword);
                            Console.WriteLine("FtpUpload: " + mp3scr.FtpPath + mp3scr.DestBase + " uploaded successfully");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        continue;
                    }
                }

                Console.WriteLine("mp3scraper done");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Main: " + ex.Message);
                Environment.Exit(-1);
            }


            XmlDocument doc = new XmlDocument();
            doc.Load(mp3scr.DestFolderName + "\\" + mp3scr.DestBase);


            /*
            XmlElement newElement5 = doc.CreateElement("itunes", "type", "http://your");
            newElement5.InnerText = "serial";

            XmlNode parentNode5 = doc.SelectSingleNode("//channel");
            parentNode5.AppendChild(newElement5);
            */

            XmlNodeList cElement = doc.GetElementsByTagName("channel");
            foreach (XmlNode node in cElement)
            {
                node.SelectSingleNode("description").InnerText = ConfigurationManager.AppSettings["channelDescription0001"];
            }

                XmlNodeList cGeneratorElement = doc.GetElementsByTagName("generator");
            foreach (XmlNode node in cGeneratorElement)
            {

                XmlElement newElement = doc.CreateElement("itunestype");
                XmlElement newElement2 = doc.CreateElement("itunescomplete");
                XmlElement newElement3 = doc.CreateElement("itunesauthor");
                XmlElement newElement4 = doc.CreateElement("itunestitle");
                XmlElement newElement5 = doc.CreateElement("itunesexplicit");
                XmlElement newElement6 = doc.CreateElement("itunescategory");
                XmlElement newElement7 = doc.CreateElement("itunescategory");
                XmlElement newElement8 = doc.CreateElement("itunesimage");

                XmlElement newElement9 = doc.CreateElement("podcastlocked");

                newElement.InnerText = "Serial";
                newElement2.InnerText = "Yes";
                newElement3.InnerText = ConfigurationManager.AppSettings["channelAuthor0001"];
                newElement4.InnerText = node.ParentNode.SelectSingleNode("title")?.InnerText;
                newElement5.InnerText = "False";
                newElement6.SetAttribute("text", "Religion & Spirituality");
                newElement7.SetAttribute("text", "Islam");
                newElement6.AppendChild(newElement7);
                newElement8.SetAttribute("href", "http://www.gocek.org/podcasts/mp3scraper-logo.jpg");

                newElement9.InnerText = "no";

                node.ParentNode.InsertAfter(newElement, node);
                node.ParentNode.InsertAfter(newElement2, node);
                node.ParentNode.InsertAfter(newElement3, node);
                node.ParentNode.InsertAfter(newElement4, node);
                node.ParentNode.InsertAfter(newElement5, node);
                node.ParentNode.InsertAfter(newElement6, node);
                node.ParentNode.InsertAfter(newElement8, node);
                node.ParentNode.InsertAfter(newElement9, node);
            }

            XmlNodeList iElement = doc.GetElementsByTagName("item");
            int ii = int.Parse(ConfigurationManager.AppSettings["episodesBeginning0001"]);
            foreach (XmlNode node in iElement)
            {
               
                XmlElement newElement = doc.CreateElement("itunesduration");
                XmlElement newElement2 = doc.CreateElement("itunesepisode");
                XmlElement newElement3 = doc.CreateElement("itunesexplicit");
                XmlElement newElement4 = doc.CreateElement("itunestitle");

                XmlElement newElement5 = doc.CreateElement("link");

              //  newElement.InnerText = ;
                newElement2.InnerText = ii.ToString();
                newElement3.InnerText = "false";
                newElement4.InnerText = node.SelectSingleNode("title")?.InnerText;
                newElement5.InnerText = ConfigurationManager.AppSettings["channelUrl0001"];

                XmlNode enclosure = node.SelectSingleNode("enclosure");
                string length = enclosure.Attributes["length"].Value;
                 //double dataSize = Double.Parse(length);

                long fileSizeInBits = long.Parse(length) * 8;

                // Calculate bit rate in kbps
                //  long fileSizeInBits = dataSize * 8;
                //  double bitRateKbps = (double)fileSizeInBits / totalSeconds / 1000;

                //Console.WriteLine(dataSize);

                 double seconds = 1 ;

                if (int.Parse(ConfigurationManager.AppSettings["mp3Bitrat0001"]) > 0)
                {

                     seconds = fileSizeInBits / (int.Parse(ConfigurationManager.AppSettings["mp3Bitrat0001"]) * 1000);
                }

                //return TimeSpan.FromSeconds(seconds);

                newElement.InnerText = seconds.ToString();

                node.AppendChild(newElement);
                node.AppendChild(newElement2);
                node.AppendChild(newElement3);
                node.AppendChild(newElement4);
                node.AppendChild(newElement5);

                node.SelectSingleNode("description").InnerText = ConfigurationManager.AppSettings["itemDescription0001"];

                // string title = ConfigurationManager.AppSettings["channelUrl0001"];

                //Console.WriteLine($"Title: {title}");

                string Title = ConfigurationManager.AppSettings["Title0001"].ToString();

                if (Title == "true")
                {
                    ii = ii-- ;
                    node.SelectSingleNode("title").InnerText = ConfigurationManager.AppSettings["eTitle" + ii.ToString("d4")];
                    newElement4.InnerText = ConfigurationManager.AppSettings["eTitle" + ii.ToString("d4")];
                }

                if (ConfigurationManager.AppSettings["mp3Bitrat0001"].ToString() == "0")
                {
                    ii = ii--;
                    newElement.InnerText = ConfigurationManager.AppSettings["duration" + ii.ToString("d4")];
                }

                // Console.WriteLine("Title0001: "  + ii);

                ii++;

            }
            /*
            XmlNodeList childNodes = doc.GetElementsByTagName("item");
            foreach (XmlNode child in childNodes)
            {
               // foreach (XmlNode subChild in child.ChildNodes)
               // {
                    
                    XmlElement newElement4 = doc.CreateElement("itunestitle");
                    newElement4.InnerText = child.SelectSingleNode("title")?.InnerText;
                    child.AppendChild(newElement4);
                  //  Console.WriteLine(subChild.InnerText);
               // }
                Console.WriteLine("ct"+child.SelectSingleNode("title")?.InnerText);
            }
            */


            doc.Save(mp3scr.DestFolderName + "\\" + mp3scr.DestBase);

            XDocument xdoc = XDocument.Load(mp3scr.DestFolderName + "\\" + mp3scr.DestBase);


            // Find the specific tag (e.g., <title>)
            var languageElement = xdoc.Descendants("language").FirstOrDefault();

            var enclosureElement = xdoc.Descendants("enclosure").FirstOrDefault();

            //Console.WriteLine("le" + languageElement);

            /*
            if (titleElement != titleElement)
            {
            // Create the new tag
            XElement newTag = new XElement("itunestype5", "exampleType");

            // Insert the new tag after the found tag
            languageElement.AddAfterSelf(newTag);
            Console.WriteLine("nt" + newTag);
             }
            */

            /*
            XNamespace ns = "http://www.example.com";
            XElement root = new XElement(ns + "Root",
                new XAttribute(XNamespace.Xmlns + "itunes", "http://www.example.com"),
                new XAttribute("Remove", "Yes"),
                new XElement(ns + "type", "Serial"),
                new XElement(ns + "complete", "Yes"),
                new XElement(ns + "author", "Islamic Podcast"),
                new XElement(ns + "title", xdoc.Descendants("title").FirstOrDefault().Value),
                new XElement(ns + "explicit" , "False"
            ));
            Console.WriteLine("root" + root);

            //xdoc.Root.Add(root);

            languageElement.AddAfterSelf(root);
            */

            var elements = xdoc.Descendants("enclosure"); // Adjust the element name as needed

            foreach (var element in elements)
            {
                //enclosureElement.AddAfterSelf(root);
            }



            // XElement elementToRemove = xdoc.Descendants().FirstOrDefault(e => e.Attribute("Remove")?.Value == "Yes");



            xdoc.Save(mp3scr.DestFolderName + "\\" + mp3scr.DestBase);

            
            string filePath = (mp3scr.DestFolderName + "\\" + mp3scr.DestBase).ToString();
            string content = File.ReadAllText(filePath);
            content = content.Replace(ConfigurationManager.AppSettings["url0001"], ConfigurationManager.AppSettings["channelUrl0001"]);
            content = content.Replace("itunes", "itunes:");
            content = content.Replace("podcastlocked", "podcast:locked");
            content = content.Replace("<rss xmlns:a10=\"http://www.w3.org/2005/Atom\" version=\"2.0\">", "<rss xmlns:a10=\"http://www.w3.org/2005/Atom\" xmlns:atom=\"http://www.w3.org/2005/Atom\" xmlns:content=\"http://purl.org/rss/1.0/modules/content/\" xmlns:googleplay=\"http://www.google.com/schemas/play-podcasts/1.0\" xmlns:itunes=\"http://www.itunes.com/dtds/podcast-1.0.dtd\" xmlns:media=\"http://search.yahoo.com/mrss/\" xmlns:podcast=\"https://podcastindex.org/namespace/1.0\" version=\"2.0\">");
            content = content.Replace("http://www.gocek.org/podcasts/mp3scraper-logo.jpg", ConfigurationManager.AppSettings["channelLogo0001"]);
            File.WriteAllText(filePath, content);
            
        }
    }
}
