using FreeSql.DataAnnotations;

namespace Demo01.Tables;

public class Orders
{
    [Column(IsPrimary = true)] public int Id { get; set; }
    [Column] public string Name { get; set; } = string.Empty;

    [Column] public DateTime CreateTime { get; set; }
}