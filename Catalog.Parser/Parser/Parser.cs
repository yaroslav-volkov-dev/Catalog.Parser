using Catalog.Parser.Models;
using HtmlAgilityPack;

namespace Catalog.Parser.Parser;

public class Parser
{
    private readonly HtmlWeb _web = new();
    private const int MaxConcurrency = 6;
    private const string BaseUrl = "https://www.jcrew.com/pl/plp/womens/categories/clothing";
    private const string ProductTileClassname = "c-product-tile";
    private const string ProductPriceClassname = "is-price";

    public async Task RunAsync()
    {
        List<string> urls = Enumerable.Range(1, 10)
            .Select(i => $"{BaseUrl}?Npge={i}")
            .ToList();
        
        SemaphoreSlim semaphore = new (MaxConcurrency);
        List<Task<HtmlDocument>> tasks = [];
        
        foreach (string url in urls)
        {
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    Console.WriteLine($"Loading: {url}");
                    return await _web.LoadFromWebAsync(url);
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }
        
        HtmlDocument[] htmlPages = await Task.WhenAll(tasks);

        List<Product> resultProducts = [];

        foreach (HtmlDocument htmlDocument in htmlPages)
        {
            resultProducts.AddRange(ParseHtml(htmlDocument));
        }
        
        string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "parsed");
        Directory.CreateDirectory(directoryPath);
        
        string filePath = Path.Combine(directoryPath, "products.txt");
        await File.WriteAllLinesAsync(filePath, resultProducts.Select(p => p.ToString()));
        Console.WriteLine("Saved");
    }
    
    private List<Product> ParseHtml(HtmlDocument htmlDocument)
    {

        HtmlNodeCollection? nodes = htmlDocument.DocumentNode.SelectNodes($"//li[contains(@class, {ProductTileClassname})]");
        if (nodes == null) return [];

        List<Product> products = [];
        
        foreach (HtmlNode node in nodes)
        {
            string name = node.SelectSingleNode(".//h2")?.InnerText.Trim() ?? "N/A";
            string href = node.SelectSingleNode(".//a")?.GetAttributeValue("href", "N/A") ?? "N/A";
            string price = ParsePrice(node.SelectSingleNode($".//span[contains(@class, {ProductPriceClassname})]"));

            products.Add(new Product
            {
                Name = name,
                Href = $"https://www.jcrew.com{href}",
                Price = price,
            });
        }

        return products;
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