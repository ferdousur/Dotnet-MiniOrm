using MiniOrm.Attr; 
namespace MiniOrm.Models; 


[TableAttr("products")]
public class Products
{
    [PrimaryKeyAttr]
    public int Id {get;set;}

    public string Product_Name {get;set;}

    public decimal Price {get;set;}
}