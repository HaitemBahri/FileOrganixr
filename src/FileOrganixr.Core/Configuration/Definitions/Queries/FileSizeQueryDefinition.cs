namespace FileOrganixr.Core.Configuration.Definitions.Queries;
public sealed class FileSizeQueryDefinition : QueryDefinition
{
    public FileSizeQueryDefinition() : base("FileSize")
    {
    }

    public decimal MaxSize { get; set; } = decimal.MaxValue;

    public decimal MinSize { get; set; } = 0m;
}
