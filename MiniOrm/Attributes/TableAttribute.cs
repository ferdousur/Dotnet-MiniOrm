
namespace MiniOrm.Attr; 

[AttributeUsage(AttributeTargets.Class)]
public class TableAttr : Attribute
{
    public string Name {get;}

    public TableAttr(string name)
    {
        Name= name; 
    }
}