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

    public class ProcessEDealInfo : IProcessHtml
    {
        private const string ClassName = "deal-start";
        private const string DealSiteURI = "https://www.edealinfo.com";
        private const string Domain = "www.edealinfo.com";
        private const string DealImageClassName = "deal-image-div";
        private const string Super_Hot_Deal = "super hot";
        private const string HRefAttribute = "href";

        public StateService StateService { get; set; }

        public async Task<List<Deal>> ProcessAsync(string keywords)
        {
            var tempDeals = new List<Deal>();
            var web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync(DealSiteURI);
            var nodes = doc.DocumentNode.Descendants().Where(c => c.HasClass(ClassName)).ToList();
            var words = keywords.Split(",");
            
            List<HtmlNode> childElement = null;
            string fullDescription = null;
            string description = null;
            string vendor = null;
            string hash = null;
            bool isSuperHot = false;
            var dealLink = string.Empty;

            foreach (var node in nodes)
            {
                try
                {
                    fullDescription = node.InnerText;
                    description = node.ChildNodes[1].ChildNodes[0].ChildNodes[1].ChildNodes[3].InnerText;
                    vendor = node.ChildNodes[1].ChildNodes[1].ChildNodes[0].InnerText;
                    hash = HashService.GetStringSha256Hash(description);
                    isSuperHot = fullDescription.ToLower().IndexOf(Super_Hot_Deal) >= 0;
                    
                    dealLink = string.Empty;
                    childElement = node.Descendants("a").Where(c => !string.IsNullOrEmpty(c.GetAttributeValue("href",""))).ToList();
                    if (childElement.Count>1)
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
                                Description = (isSuperHot ? "<span style='color:red'>Super Hot</span> ": "") + description,
                                Price = string.Empty,
                                Vendor = vendor,
                                Hash = hash,
                                Link = DealSiteURI + dealLink
                            };
                            tempDeals.Add(deal);
                            break;
                        }
                    }
                }
                catch (Exception) { }
            }

            var finalListOfDeals = new List<Deal>();

            if (tempDeals.Any())
            {
                foreach (var deal in tempDeals)
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
