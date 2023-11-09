using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SourceGen
{
    [Generator]
    public sealed class CodeEmitter : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(ctx =>
                ctx.AddSource("IncludeInEqualsAttribute.g.cs", 
                    SnippetTemplates.MarkerAttribute)
            );
            
            var classDeclarations = context
                .SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: (s, _) =>IsClassTargetedForGeneration(s), 
                    transform: (ctx, _) => GetTargetClassForGeneration(ctx))
                .Where(m => m is not null); // filter out null values
            
                // Combine the selected classes with the `Compilation`
            IncrementalValueProvider<(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> ClassDeclarationSyntaxes)> compilationAndClasses
                = context.CompilationProvider.Combine(classDeclarations.Collect())!;
            
            context.RegisterSourceOutput(compilationAndClasses, 
                (spc, cnc) => Generate(cnc.compilation, cnc.ClassDeclarationSyntaxes, spc));
        }

        private void Generate(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classDeclarationSyntaxes,
            SourceProductionContext sourceProductionContext)
        {
            var codeBuilder = new StringBuilder();
            var hashBuilder = new StringBuilder();
            var includeInEqualsAttributeTypeSymbol =
                compilation.GetTypeByMetadataName($"ConsoleClient.IncludeInEqualsAttribute")!;

            foreach (var classDeclarationSyntax in classDeclarationSyntaxes)
            {
                var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
                var namedTypeSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax);
                if (namedTypeSymbol is null) continue;

                var properties = namedTypeSymbol.GetMembers();
                foreach (var property in properties)
                {
                    if (property is not IPropertySymbol) continue;

                    var propertyAttributes = property.GetAttributes();
                    if (CanIncludeInEquals(propertyAttributes, includeInEqualsAttributeTypeSymbol))
                    {
                        codeBuilder.Append($"this.{property.Name} == input.{property.Name} && ");
                        hashBuilder.Append($"{property.Name},");
                    }
                }

                var generated = codeBuilder.ToString();
                generated = generated.Trim(' ').Trim('&').Trim('&').TrimEnd();
                var hashingProps = hashBuilder.ToString().TrimEnd(',')!;
                var generatedPartial = string.Format(SnippetTemplates.EqualsMethod, namedTypeSymbol.Name,
                    generated, hashingProps);
                sourceProductionContext.AddSource($"{namedTypeSymbol.Name}.g.cs",
                    SourceText.From(generatedPartial, Encoding.UTF8));
                
                codeBuilder.Clear();
                hashBuilder.Clear();
            }
        }
        
        private static bool CanIncludeInEquals(ImmutableArray<AttributeData> attributes,
            ISymbol skipEnrichmentAttributeTypeSymbol)
            => attributes.Any(at =>
                at
                    .AttributeClass?
                    .Equals(skipEnrichmentAttributeTypeSymbol, SymbolEqualityComparer.Default) 
                ?? false);

        private ClassDeclarationSyntax? GetTargetClassForGeneration(GeneratorSyntaxContext context)
        {
            // Try cast the node to ClassDeclarationSyntax
            if (context.Node is not ClassDeclarationSyntax declarationSyntax)
            {
                return null;
            }

            // Read properties of the class
            var propertyMembers = declarationSyntax
                .Members
                .Where(m => m.IsKind(SyntaxKind.PropertyDeclaration));
            
            // Read all the attributes of the properties
            var attributes = propertyMembers.SelectMany(p => p.AttributeLists)
                .SelectMany(al => al.Attributes)
                .ToList();
            
            // Use Semantic model of each attribute to know if it matches with our market attribute or not
            foreach (var attribute in attributes)
            {
                if (ModelExtensions.GetSymbolInfo(context.SemanticModel, attribute).Symbol is not IMethodSymbol attributeSymbol)
                {
                    // return null;
                    continue;
                }

                var fullName = attributeSymbol.ContainingSymbol.ToDisplayString();
                // Check if the attribute name matches our marker attribute
                if (fullName == "ConsoleClient.IncludeInEqualsAttribute")
                {
                    return declarationSyntax;
                }
            }

            // No attributes, return null
            return null;
        }

        private bool IsClassTargetedForGeneration(SyntaxNode syntaxNode)
        {
            if (syntaxNode is not ClassDeclarationSyntax classDeclarationSyntax) return false;
            return classDeclarationSyntax.Members.SelectMany(m => m.AttributeLists).Any();
        }
    }
}