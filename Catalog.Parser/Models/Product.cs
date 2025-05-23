namespace Catalog.Parser.Models;

public class Product
{
    public string Name { get; set; } = "";
    public string Href { get; set; } = "";
    public string Price { get; set; } = "";

    public override string ToString()
    {
        return $"{Name} | {Href} | {Price}";
    }
}