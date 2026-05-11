

namespace MiniOrm.Attr; 

[AttributeUsage(AttributeTargets.Property)]
public class ColumnAttr : Attribute
{
    public string Name {get;}
    
    public ColumnAttr(string name )
    {
        Name=name; 
    }
}