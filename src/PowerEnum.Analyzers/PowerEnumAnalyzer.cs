using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using PowerEnum.Analyzers.Utils;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace PowerEnum.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PowerEnumAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor RuleClassNotPartial = new(
            id: "PWE001",
            title: "Class must be partial",
            messageFormat: "The class '{0}' must be marked as partial to allow code generation to provide the implementation of the PowerEnum functionality",
            category: "PowerEnum.Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor RuleContainingTypeNotPartial = new(
            id: "PWE002",
            title: "Containing type must be partial",
            messageFormat: "The containing type '{0}' must be marked as partial to allow code generation to provide the implementation of the PowerEnum functionality",
            category: "PowerEnum.Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor RuleConstructorShouldBePrivateOrProtected = new(
            id: "PWE003",
            title: "Constructors should be private or protected",
            messageFormat: "Any constructors for '{0}' should be made private or protected, as they will result in a runtime error if called outside of the '{0}' class",
            category: "PowerEnum.Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor RuleClassWithPrimaryConstructorShouldBeNonPublic = new(
            id: "PWE004",
            title: "Class with primary constructor should not be public",
            messageFormat: "You are using a primary constructor. C# does not currently have a way to make primary constructors private. Instead, the entire class '{0}' should be made internal. If you want to make this class public, consider using a private partial constructor.",
            category: "PowerEnum.Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                RuleClassNotPartial,
                RuleContainingTypeNotPartial,
                RuleConstructorShouldBePrivateOrProtected,
                RuleClassWithPrimaryConstructorShouldBeNonPublic);

        public override void Initialize(AnalysisContext context)
        {
            const bool EnableDebugger = false;

            if (EnableDebugger && !Debugger.IsAttached)
            {
                Debugger.Launch();
            }

            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSyntaxNodeAction(AnalyseClassDeclaration, SyntaxKind.ClassDeclaration);
            context.RegisterSemanticModelAction(AnalyseSemanticModel);
        }

        private void AnalyseClassDeclaration(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration, context.CancellationToken);

            if (symbol == null || !AnalyzerUtils.HasPowerEnumAttribute(symbol))
            {
                return;
            }

            // If the PowerEnum class is not partial, report diagnostic.
            if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                var diagnostic = Diagnostic.Create(
                    RuleClassNotPartial,
                    GetDeclarationLocation(classDeclaration),
                    classDeclaration.Identifier.ToString());

                context.ReportDiagnostic(diagnostic);
            }

            // If any constructor is not private or protected, report diagnostic.
            var constructors = symbol.InstanceConstructors;
            foreach (var constructor in constructors)
            {
                switch (constructor.DeclaredAccessibility)
                {
                    case Accessibility.NotApplicable:
                    case Accessibility.Private:
                    case Accessibility.ProtectedAndInternal:
                    case Accessibility.Protected:
                        // This is restrictive enough.
                        continue;

                    case Accessibility.Internal:
                    case Accessibility.ProtectedOrInternal:
                    case Accessibility.Public:
                    default:
                        // Too public. Raise a diagnostic.
                        break;
                }

                foreach (var declaringSyntax in constructor.DeclaringSyntaxReferences)
                {
                    var constructorSyntax = declaringSyntax.GetSyntax(context.CancellationToken);

                    // Could be a primary constructor (the class declaration is the constructor)
                    if (TryHandlePrimaryConstructor(context, classDeclaration, constructorSyntax))
                    {
                        continue;
                    }

                    // Otherwise, handle normal constructor.

                    var location = GetDeclarationLocation(constructorSyntax);

                    var diagnostic = Diagnostic.Create(
                        RuleConstructorShouldBePrivateOrProtected,
                        location,
                        classDeclaration.Identifier.ToString());

                    context.ReportDiagnostic(diagnostic);
                }
            }

            static bool TryHandlePrimaryConstructor(
                SyntaxNodeAnalysisContext context,
                ClassDeclarationSyntax classDeclaration,
                SyntaxNode constructorSyntax)
            {
                if (constructorSyntax == classDeclaration)
                {
                    foreach (var node in classDeclaration.ChildNodes())
                    {
                        if (node is ParameterListSyntax)
                        {
                            var location = GetDeclarationLocation(classDeclaration);

                            var diagnostic = Diagnostic.Create(
                                RuleConstructorShouldBePrivateOrProtected,
                                location,
                                classDeclaration.Identifier.ToString());

                            context.ReportDiagnostic(diagnostic);

                            return true;
                        }
                    }
                }

                return false;
            }
        }

        private void AnalyseSemanticModel(SemanticModelAnalysisContext context)
        {
            var root = context.SemanticModel.SyntaxTree.GetRoot(context.CancellationToken);

            foreach (var node in root.DescendantNodes())
            {
                if (node.IsKind(SyntaxKind.ClassDeclaration))
                {
                    var classDeclaration = (ClassDeclarationSyntax)node;
                    var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration, context.CancellationToken);

                    if (symbol == null || !AnalyzerUtils.HasPowerEnumAttribute(symbol))
                    {
                        continue;
                    }

                    // At this point, we have a class declaration for a PowerEnum.
                    AnalysePowerEnumClassDeclaration(context, classDeclaration);
                }
            }
        }

        private void AnalysePowerEnumClassDeclaration(
            SemanticModelAnalysisContext context, ClassDeclarationSyntax classDeclaration)
        {
            // If any parent type of the PowerEnum class is not partial, report diagnostic.
            var parent = classDeclaration.Parent as TypeDeclarationSyntax;
            while (parent != null)
            {
                if (!parent.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    var location = parent.Identifier.GetLocation();
                    var parentIdentifier = parent.Identifier.ToString();

                    var diagnostic = Diagnostic.Create(
                        RuleContainingTypeNotPartial,
                        GetDeclarationLocation(parent),
                        parentIdentifier);

                    // Importantly - this could not be done in AnalyseClassDeclaration
                    // because the diagnostic is about a different syntax node than
                    // we started the analysis with.
                    context.ReportDiagnostic(diagnostic);
                }

                parent = parent.Parent as TypeDeclarationSyntax;
            }
        }

        /// <summary>
        /// Gets a location to describe the 'declaration' of a type or method
        /// i.e. "[Attribute] public partial class ClassName(string data) {}"
        ///                   ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        /// </summary>
        private static Location GetDeclarationLocation(SyntaxNode syntaxNode)
        {
            var firstToken = syntaxNode.GetFirstToken();

            if (syntaxNode is TypeDeclarationSyntax typeDeclaration)
            {
                // Skip over the attributes though
                if (typeDeclaration.AttributeLists.Count > 0)
                {
                    var lastAttributeList = typeDeclaration.AttributeLists[typeDeclaration.AttributeLists.Count - 1];
                    var lastAttributeListToken = lastAttributeList.GetLastToken();

                    // We will return the range starting from the first token after the last attribute list.
                    firstToken = lastAttributeListToken.GetNextToken();
                }
            }

            var lastToken = syntaxNode switch
            {
                BaseTypeDeclarationSyntax btds => btds.Identifier,
                ConstructorDeclarationSyntax cds => cds.Identifier,
                MethodDeclarationSyntax mds => mds.Identifier,
                _ => syntaxNode.GetLastToken()
            };

            // For a method or a primary constructor, extend the location to include the parameter list.
            var firstParameterList = syntaxNode.ChildNodes().OfType<ParameterListSyntax>().FirstOrDefault();
            if (firstParameterList != null)
            {
                lastToken = firstParameterList.GetLastToken();
            }

            var span = TextSpan.FromBounds(firstToken.SpanStart, lastToken.Span.End);

            return Location.Create(syntaxNode.SyntaxTree, span);
        }
    }
}
