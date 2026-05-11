using System.ComponentModel.DataAnnotations.Schema;
using MiniOrm.Attr;

namespace MiniOrm.Data; 

public class ColumnInfo
{
    public string PropertyName {get;set;}= string.Empty; 
    public string ColumnName {get;set;}=string.Empty; 
    public Type PropertyType {get;set;}=typeof(object); 

    public bool IsPrimaryKey {get;set;}
}


public class EntityMedatada<T>
{
    public string? TableName {get;}

    public List<ColumnInfo>? Columns {get;}

    public ColumnInfo? PrimaryKey{get;}


    public EntityMedatada()
    {
        var type= typeof(T); 

        var TableAttr=(TableAttr?)Attribute.GetCustomAttribute(type, typeof(TableAttr));

        TableName=TableAttr?.Name ?? type.Name.ToLower();

        Columns=new List<ColumnInfo>(); 

        foreach(var properties in type.GetProperties())
        {
            var ColumnAttr=(ColumnAttr?)Attribute.GetCustomAttribute(properties, typeof(ColumnAttr)); 
            var columnName=ColumnAttr?.Name ?? properties.Name.ToLower(); 

            var isPK=Attribute.IsDefined(properties, typeof(PrimaryKeyAttr)); 

            Columns.Add( new ColumnInfo
            {
                PropertyName=properties.Name,
                ColumnName=columnName, 
                PropertyType=properties.PropertyType,
                IsPrimaryKey=isPK
            }); 
        }

        PrimaryKey= Columns.FirstOrDefault(c=> c.IsPrimaryKey);
         

    }
}