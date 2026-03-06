namespace Api.Repositories;

internal sealed class SiteStateRecord
{
    public int Id { get; set; } = 1;
    public int Count { get; set; }
    public string KnownAidsJson { get; set; } = "[]";
    public string KnownDogsJson { get; set; } = "{}";
    public DateTimeOffset Updated { get; set; }
}
