using HtmlAgilityPack;

namespace Catalog.Parser.Parser;

public class Parser
{
    public void Parse()
    {
        const string url = "https://www.jcrew.com/pl/plp/womens/categories/clothing?Npge=2";
        const string productTileClassname = "c-product-tile";
        const string productPriceClassname = "is-price";

        HtmlWeb web = new ();

        HtmlDocument document = web.Load(url);

        HtmlNodeCollection? productNodes = document.DocumentNode.SelectNodes($"//li[contains(@class, {productTileClassname})]");
        
        if (productNodes == null)
        {
            Console.WriteLine("There's no products on the page.");
            return;
        }
        
        foreach (HtmlNode product in productNodes)
        {
            HtmlNode? hyperlinkNode = product.SelectSingleNode(".//a");
            string href = hyperlinkNode?.GetAttributeValue("href", "N/A") ?? "N/A";
            
            HtmlNode? nameNode = product.SelectSingleNode(".//h2");
            string name = nameNode?.InnerText.Trim() ?? "N/A";
            
            HtmlNode? priceNode = product.SelectSingleNode($".//span[contains(@class, {productPriceClassname})]");
            string price = ParsePrice(priceNode);
            
            Console.WriteLine($"Name: {name}");
            Console.WriteLine($"HREF: https://www.jcrew.com{href}");
            Console.WriteLine($"Price: {price}");
            Console.WriteLine(new string('-', 40));
        }
    }
    
    private string ParsePrice(HtmlNode? node)
    {
        if (node is null)
        {
            return "N/A";
        };
        IEnumerable<string> visibleTexts = node.ChildNodes
            .Where(n => !(n.Name == "span" && n.GetClasses().Contains("is-visually-hidden")))
            .Select(n => n.InnerText.Trim())
            .Where(text => !string.IsNullOrEmpty(text));

        return string.Join(" ", visibleTexts);
    }
}