using Microsoft.CodeAnalysis;
using PowerEnumShared;
using System.Linq;

namespace PowerEnum.Analyzers.Utils
{
    internal static class AnalyzerUtils
    {
        internal static bool HasPowerEnumAttribute(INamedTypeSymbol symbol)
        {
            return symbol.GetAttributes().Any(x =>
            {
                var name = x.AttributeClass?.MetadataName;
                var ns = x.AttributeClass?.ContainingNamespace?.MetadataName;

                var nameMatches = name == Constants.PowerEnumAttributeMetadataName;
                var nameSpaceMatches = ns == Constants.PowerEnumAttributeContainingNamespaceName;

                return nameMatches && nameSpaceMatches;
            });
        }
    }
}
