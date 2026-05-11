namespace MiniOrm.Data; 

public static class TypeMapper
{
    public static string ToPostgresType(Type type)
    {
        var underlying=Nullable.GetUnderlyingType(type) ?? type; 

        return underlying switch
        {
            _ when underlying == typeof(int)      => "INTEGER",
            _ when underlying == typeof(long)     => "BIGINT",
            _ when underlying == typeof(decimal)  => "NUMERIC",
            _ when underlying == typeof(double)   => "DOUBLE PRECISION",
            _ when underlying == typeof(float)    => "REAL",
            _ when underlying == typeof(bool)     => "BOOLEAN",
            _ when underlying == typeof(string)   => "TEXT",
            _ when underlying == typeof(DateTime) => "TIMESTAMP",
            _ when underlying == typeof(Guid)     => "UUID",
            _ => throw new Exception($"Unsupported type: {underlying.Name}")

        };
    }
}