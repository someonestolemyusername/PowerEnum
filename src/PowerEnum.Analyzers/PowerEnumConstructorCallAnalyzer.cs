using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using PowerEnum.Analyzers.Utils;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace PowerEnum.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PowerEnumConstructorCallAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor RuleConstructorMustNotBeCalledOutsideOfClass = new(
            id: "PWE005",
            title: "PowerEnum item constructor must not be called from outside the class",
            messageFormat: "PowerEnums have a fixed set of items. New items cannot be dynamically created. It is a runtime error to attempt to construct any new items. Instead, use the static members of '{0}' to locate existing items.",
            category: "PowerEnum.Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(RuleConstructorMustNotBeCalledOutsideOfClass);

        public override void Initialize(AnalysisContext context)
        {
            const bool EnableDebugger = false;

            if (EnableDebugger && !Debugger.IsAttached)
            {
                Debugger.Launch();
            }

            // We want to analyse generated code because this analyzer is raising
            // diagnostics for a situation which will cause a runtime error if not fixed.
            context.ConfigureGeneratedCodeAnalysis(
                GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.EnableConcurrentExecution();

            // We run this analysis in a start action so we can cache symbols for speed.
            context.RegisterCompilationStartAction(AnalyseCompilation);
        }

        private void AnalyseCompilation(CompilationStartAnalysisContext context)
        {
            var analysis = new CompilationAnalyzer();

            context.RegisterSyntaxNodeAction(
                analysis.AnalyseObjectCreationExpression,
                SyntaxKind.ObjectCreationExpression,
                SyntaxKind.ImplicitObjectCreationExpression);
        }

        private class CompilationAnalyzer
        {
            // Cache of symbols we have checked to be PowerEnum classes.
            // The values are the class with the [PowerEnum] attribute.
            private ConcurrentDictionary<ITypeSymbol, ITypeSymbol?> _powerEnumSymbols
                = new ConcurrentDictionary<ITypeSymbol, ITypeSymbol?>(SymbolEqualityComparer.Default);

            // Recursively checks a type symbol and its base types.
            // If any type is annotated with [PowerEnum], returns true.
            // The out param will contain the first ancestor type with the [PowerEnum] attribute.
            private bool TryGetBasePowerEnumSymbol(
                ITypeSymbol? symbol,
                [NotNullWhen(true)] out ITypeSymbol? symbolResult)
            {
                symbolResult = null;

                if (symbol == null)
                {
                    return false;
                }

                if (_powerEnumSymbols.TryGetValue(symbol, out ITypeSymbol? cachedResult))
                {
                    symbolResult = cachedResult;
                    return symbolResult != null;
                }

                if (symbol is INamedTypeSymbol nts)
                {
                    if (AnalyzerUtils.HasPowerEnumAttribute(nts))
                    {
                        symbolResult = symbol;
                    }
                }

                if (symbolResult == null)
                {
                    TryGetBasePowerEnumSymbol(symbol.BaseType, out symbolResult);
                }

                _powerEnumSymbols[symbol] = symbolResult;
                return symbolResult != null;
            }

            internal void AnalyseObjectCreationExpression(SyntaxNodeAnalysisContext context)
            {
                // Get the constructor method symbol.
                var constructor = context.SemanticModel.GetSymbolInfo(context.Node).Symbol as IMethodSymbol;
                if (constructor == null)
                {
                    return;
                }

                // Now we have to check if the constructor is a PowerEnum.
                // We go through all containing types, and if any of them
                // has the [PowerEnum] attribute, then we proceed.
                if (!TryGetBasePowerEnumSymbol(constructor.ContainingType, out var powerEnumClass))
                {
                    return;
                }

                // We have a constructor call for a PowerEnum type.
                // If the constructor call is located outside the PowerEnum class definition,
                // it is invalid and we raise a diagnostic.
                foreach (var syntaxReference in powerEnumClass.DeclaringSyntaxReferences)
                {
                    var powerEnumSyntax = syntaxReference.GetSyntax(context.CancellationToken);
                    if (powerEnumSyntax.Contains(context.Node))
                    {
                        // Constructor call is in a valid location,
                        // because it is inside a PowerEnum class definition.
                        return;
                    }
                }

                // We have a constructor call for a PowerEnum type
                // and the call was not located inside a PowerEnum class definition.
                // Raise a diagnostic.
                var diagnostic = Diagnostic.Create(
                    RuleConstructorMustNotBeCalledOutsideOfClass,
                    context.Node.GetLocation(),
                    powerEnumClass.Name);

                //_diagnostics.Add(diagnostic);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
