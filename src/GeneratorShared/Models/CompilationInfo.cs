using Microsoft.CodeAnalysis.CSharp;

namespace PowerEnum.SourceGenerator.Models;

internal readonly record struct CompilationInfo(
    bool HasImmutableArray,
    bool HasFrozenDictionary,
    bool HasNullableAnnotations,
    bool HasNotNullWhen,
    bool HasSystemTextJsonConverters,
    bool HasNewtonsoftJsonConverters,
    LanguageVersion LanguageVersion)
{
    public string NullQ => HasNullableAnnotations ? "?" : "";
    public string NullBang => HasNullableAnnotations ? "!" : "";

    public bool HasReadonlyAutoImplementedProperties => LanguageVersion >= LanguageVersion.CSharp6;
    public bool HasExpressionBodyPropsMethods => LanguageVersion >= LanguageVersion.CSharp6;
    public bool HasOutVariableDeclaration => LanguageVersion >= LanguageVersion.CSharp7;
    public bool HasFileModifier => LanguageVersion >= (LanguageVersion)1100;
}
