using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CodeEmitter
{
    [Generator]
    public sealed class Emitter : IIncrementalGenerator
    {
        
       
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(ctx =>
                ctx.AddSource("GenerateCompareAndMergeAttribute.g.cs", 
                    Templates.MarkerAttribute)
            );

            var classDeclarations = context
                .SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: (s, _) =>IsClassTargetedForGeneration(s), 
                    transform: (ctx, _) => GetTargetClassForGeneration(ctx))
                .Where(m => m is not null); // filter out null values
            
            // Combine the selected classes with the `Compilation`
            IncrementalValueProvider<(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> RecordDeclarationSyntaxes)> compilationAndClasses
                = context.CompilationProvider.Combine(classDeclarations.Collect())!;
            
            context.RegisterSourceOutput(compilationAndClasses, 
                (spc, cnc) => Generate(cnc.compilation, cnc.RecordDeclarationSyntaxes, spc));
        }

        private void Generate(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classDeclarationSyntaxes, SourceProductionContext sourceProductionContext)
        {
            var codeBuilder = new StringBuilder();
            foreach (var classDeclarationSyntax in classDeclarationSyntaxes)
            {
                var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
                var namedTypeSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax) as INamedTypeSymbol;
                if(namedTypeSymbol is null) continue;
                
                var properties = namedTypeSymbol.GetMembers();
                foreach (var property in properties)
                {
                    if (property is not IPropertySymbol propertySymbol) continue;
                    codeBuilder.AppendLine(string.Format(Templates.RepeatComparision, propertySymbol.Name));
                }

                var generatedPartial = string.Format(Templates.GeneratedClassWithMergeMethod, namedTypeSymbol.Name,
                    codeBuilder);
                sourceProductionContext.AddSource($"{namedTypeSymbol.Name}.g.cs", SourceText.From(generatedPartial, Encoding.UTF8));

            }
        }

        private ClassDeclarationSyntax? GetTargetClassForGeneration(GeneratorSyntaxContext context)
        {
            // Try cast the node to ClassDeclarationSyntax
            if (context.Node is not ClassDeclarationSyntax declarationSyntax)
            {
                return null;
            }

            // Read all the attributes of the class
            var attributes = declarationSyntax
                .AttributeLists
                .SelectMany(al => al.Attributes)
                .ToList();
            
            // Use Semantic model of each attribute to know if it matches with our market attribute or not
            foreach (var attribute in attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is not IMethodSymbol attributeSymbol)
                {
                    // return null;
                    continue;
                }

                var fullName = attributeSymbol.ContainingSymbol.ToDisplayString();
                if (fullName == "PatientApp.GenerateCompareAndMergeAttribute")
                {
                    return declarationSyntax;
                }
            }

            // No attributes, return null
            return null;
        }

        private bool IsClassTargetedForGeneration(SyntaxNode syntaxNode)
            => syntaxNode is ClassDeclarationSyntax { AttributeLists.Count: > 0 };

        
    }
}