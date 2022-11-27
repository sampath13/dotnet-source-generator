namespace CodeEmitter;

public static class Templates
{
    public const string MarkerAttribute = @"
            namespace PatientApp;
            [System.AttributeUsage(AttributeTargets.Class)]
            public sealed class GenerateCompareAndMergeAttribute : System.Attribute
            {
            }";
    
    public const string GeneratedClassWithMergeMethod = @"
/* !! Auto generated code. Please do not modify !! */

namespace PatientApp;
public partial class {0}
{{
    public void CompareAndMergeWith({0} incoming)
    {{
        if(incoming == null!) return;
        {1}
    }}
}}";
    
    /// <summary>
    /// {0} --> Property Name 
    /// </summary>
    public const string RepeatComparision = @"
        if (incoming.{0} != default && incoming.{0} != {0})
        {{
            {0} = incoming.{0};
        }}";
}