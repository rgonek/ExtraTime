using TUnit.Core;

namespace ExtraTime.IntegrationTests.Attributes;

public sealed class TestCategoryAttribute(string category) : PropertyAttribute("Category", category);

public static class TestCategories
{
    public const string Significant = "Significant";
    public const string Extended = "Extended";
    public const string Critical = "Critical";
    public const string RequiresDatabase = "RequiresDatabase";
}
