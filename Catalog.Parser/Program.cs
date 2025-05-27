using Catalog.Parser.Parser;

class Program 
{
    static async Task Main()
    {
       Parser parser = new();
     
       await parser.RunAsync();
    }
}