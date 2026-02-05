namespace Cobryx.Api.Outcomes;

public static class ProductOutcomes
{
    private const string Prefix = "PRODUCT";

    public const string Created = $"{Prefix}.CREATED";
    public const string Updated = $"{Prefix}.UPDATED";
    public const string Deleted = $"{Prefix}.DELETED";
}
