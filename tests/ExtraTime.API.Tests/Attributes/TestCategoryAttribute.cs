using TUnit.Core;

namespace ExtraTime.API.Tests.Attributes;

public sealed class TestCategoryAttribute(string category) : PropertyAttribute("Category", category);

public static class TestCategories
{
    public const string Significant = "Significant";
    public const string Extended = "Extended";
    public const string Critical = "Critical";
}
