using DF.Services.Hash;
using DF.Services.Models;
using DF.Services.State;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DF.Services.Html
{

    public class ProcessTechBargains : IProcessHtml
    {
        private const string ClassName = "deal";
        private const string DealSiteURI = "https://www.techbargains.com/";
        private const string Domain = "www.techbargains.com";

        public StateService StateService { get; set; }

        public async Task<List<Deal>> ProcessAsync(string keywords)
        {
            var tempDealList = new List<Deal>();
            var web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync(DealSiteURI);

            var nodes = doc.DocumentNode.Descendants().Where(c => c.HasClass(ClassName)).ToList();
            var words = keywords.Split(",");


            string fullDescription = string.Empty;
            string shortDescription = string.Empty;
            string dealLink = string.Empty;

            List<HtmlNode> childElement = null;

            foreach (var node in nodes)
            {
                try
                {
                    fullDescription = node.InnerText;
                    shortDescription = fullDescription;

                    if (fullDescription.Length > 200)
                        shortDescription = fullDescription.Substring(0, 200) + " ...";

                    var hash = HashService.GetStringSha256Hash(shortDescription);

                    dealLink = string.Empty;
                    childElement = node.Descendants("a").Where(c => !string.IsNullOrEmpty(c.GetAttributeValue("href", ""))).ToList();
                    if (childElement.Count > 1)
                    {
                        dealLink = childElement[1].GetAttributeValue("href", "");
                    }

                    foreach (var word in words)
                    {
                        if (fullDescription.ToLower().IndexOf(word.ToLower()) >= 0)
                        {
                            var deal = new Deal
                            {
                                Site = DealSiteURI,
                                Domain = Domain,
                                Keyword = word,
                                Description = shortDescription.Replace("\n", " "),
                                Price = string.Empty,
                                Vendor = string.IsNullOrEmpty(shortDescription) ? string.Empty : shortDescription.Split(" ")[0],
                                Hash = hash,
                                Link = dealLink
                            };
                            tempDealList.Add(deal);
                            break;
                        }
                    }
                }
                catch (Exception) { }
            }

            var finalListOfDeals = new List<Deal>();

            if (tempDealList.Any())
            {
                foreach (var deal in tempDealList)
                {
                    if (!await StateService.FindAsync(deal.Hash))
                    {
                        // Add to state
                        await StateService.SaveAsync(deal.Hash);

                        // Add to final list
                        finalListOfDeals.Add(deal);
                    }
                }
            }

            return finalListOfDeals;
        }
    }
}
