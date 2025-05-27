using Catalog.Parser.Models;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Catalog.Parser.Parser;

public class Parser
{
    private const int MaxConcurrency = 6;
    private const string BaseUrl = "https://www.jcrew.com/pl/plp/womens/categories/clothing";
    private readonly HtmlWeb _web = new();

    public async Task RunAsync()
    {
        List<string> urls = Enumerable.Range(1, 20)
            .Select(pageNumber => $"{BaseUrl}?Npge={pageNumber}")
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
                    Console.WriteLine($"Loading {url}");
                    return await _web.LoadFromWebAsync(url);
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Error {url}: {exception.Message}");
                    return null;
                }
                finally
                {
                    semaphore.Release();
                }
            })!);
        }
         
        HtmlDocument[] htmlDocuments = await Task.WhenAll(tasks);
        List<Product> resultProducts = [];

        foreach (HtmlDocument document in htmlDocuments)
        {
            List<Product> parsed = ParseFromNextDataScript(document);
            resultProducts.AddRange(parsed);
        }
        
        string directoryPath = Path.Combine(Directory.GetCurrentDirectory(), "parsed");
        Directory.CreateDirectory(directoryPath);
        
        string filePath = Path.Combine(directoryPath, "products.csv");
        List<string> csvLines = ["Name,Href,Price"];
        csvLines.AddRange(resultProducts.Select(product =>
            $"\"{product.Name}\",\"{product.Href}\",\"{product.Price}\""
        ));
        await File.WriteAllLinesAsync(filePath, csvLines);
        Console.WriteLine("Saved");
    }
    
    private List<Product> ParseFromNextDataScript(HtmlDocument htmlDocument)
    {
        HtmlNode? scriptNode = htmlDocument.DocumentNode.SelectSingleNode("//script[@id='__NEXT_DATA__']");
        if (scriptNode == null)
        {
            Console.WriteLine("Error: __NEXT_DATA__ script tag not found.");
            return [];
        }

        try
        {
            string json = scriptNode.InnerText;
            JObject root = JObject.Parse(json);

            JToken? productsJsonArray = root.SelectToken("props.initialState.array.data.productArray.productList[0].products");

            if (productsJsonArray is not JArray)
            {
                Console.WriteLine("Error: Products array not found in __NEXT_DATA__ JSON.");
                return [];
            }

            List<Product> products = [];

            foreach (JToken productJson in productsJsonArray)
            {
                string name = productJson["productDescription"]?.ToString() ?? "N/A";
                string href = productJson["url"]?.ToString() ?? "";
                string price = productJson["now"]?["formatted"]?.ToString() ?? "N/A";

                products.Add(new Product
                {
                    Name = name,
                    Href = $"https://www.jcrew.com{href}",
                    Price = price
                });
            }

            return products;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error" + ex.Message);
            return [];
        }
    }
}