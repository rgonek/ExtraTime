namespace ExtraTime.IntegrationTests.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class TestCategoryAttribute : Attribute
{
    public string Category { get; }

    public TestCategoryAttribute(string category)
    {
        Category = category;
    }
}

public static class TestCategories
{
    public const string Significant = "Significant";
    public const string Extended = "Extended";
    public const string Critical = "Critical";
}
