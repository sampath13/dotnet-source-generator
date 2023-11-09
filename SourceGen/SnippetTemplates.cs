namespace SourceGen
{
    public static class SnippetTemplates
    {
        public const string MarkerAttribute = @"
namespace ConsoleClient;
[System.AttributeUsage(AttributeTargets.Property)]
public sealed class IncludeInEqualsAttribute : System.Attribute
{
}
        ";

        public const string EqualsMethod = @"
namespace ConsoleClient;
public partial class {0} 
{{
    public override bool Equals(object? obj)
    {{
        if(obj == null) return false;
        if(obj is not {0} input) return false;

        return {1};
    }}

    public override int GetHashCode()
    {{
        return HashCode.Combine({2});
    }}
}}
";
    }
}