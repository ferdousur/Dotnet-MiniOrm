using MiniOrm.Attr; 
namespace MiniOrm.Models; 

[TableAttr("orders")]
public class Order
{
    [PrimaryKeyAttr]
    public int id {get;set;}

    public int ProductId {get;set;}

     public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow; 
}