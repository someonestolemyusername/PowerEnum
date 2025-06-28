using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using PowerEnumShared;
using System.Collections;
using System.Text;

namespace PowerEnum.SourceGenerator.Models;

internal static class SymbolDisplayFormats
{
    internal readonly static SymbolDisplayFormat GlobalQualifiedFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    internal readonly static SymbolDisplayFormat LocalQualifiedFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    internal readonly static SymbolDisplayFormat NameOnlyFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
}

internal static class StringUtils
{
    public static string ConvertToFieldName(string str)
    {
        if (str.Length < 1)
        {
            return str;
        }

        if (str.Length == 1)
        {
            return str.ToLower();
        }

        return char.ToLower(str[0]) + str[1..];
    }

    public static string ConvertToPropertyName(string str)
    {
        if (str.Length < 1)
        {
            return str;
        }

        if (str.Length == 1)
        {
            return str.ToLower();
        }

        return char.ToUpper(str[0]) + str[1..];
    }
}

internal readonly record struct EnumClassTypeInfo(string? LocalNamespace, string NameOnly, string Visibility, string GlobalQualifiedName, EquatableArray<ParentType> Parents)
{
    public string ParentsAsDottedString
    {
        get
        {
            if (Parents.Length == 0)
            {
                return "";
            }

            var sb = new StringBuilder();
            foreach (ref readonly var p in Parents.AsReadOnlySpan())
            {
                sb.Append(p.Name);
                sb.Append(".");
            }
            return sb.ToString();
        }
    }

    public string LocalNamespaceAndParentsAndName => LocalNamespace is null
        ? $"{ParentsAsDottedString}{NameOnly}"
        : $"{LocalNamespace}.{ParentsAsDottedString}{NameOnly}";

    public string GeneratedTypesNamespace => "PowerEnum.Generated." + LocalNamespaceAndParentsAndName;

    internal static EnumClassTypeInfo Create(ITypeSymbol typeSymbol, EquatableArray<ParentType> parents)
    {
        IStructuralEquatable array = new string[2];

        var ns = typeSymbol.ContainingNamespace?.ToDisplayString(SymbolDisplayFormats.LocalQualifiedFormat);
        var nameOnly = typeSymbol.ToDisplayString(SymbolDisplayFormats.NameOnlyFormat);
        var globalQualifiedName = typeSymbol.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat);

        var visibility = SyntaxFacts.GetText(typeSymbol.DeclaredAccessibility);

        return new(ns, nameOnly, visibility, globalQualifiedName, parents);
    }
}

internal readonly record struct EnumItemInfo(
    string Name,
    bool ShouldGenerateProperty);

internal readonly record struct PropertyInfo(
        string PropertyName,
        string PropertyNameForField,
        string? PrimaryConstructorParameterName,
        string? PartialConstructorParameterName,
        string GlobalQualifiedPropertyType,
        string? ParamDescription);

internal readonly record struct ParentType(
    string Name,
    string Keyword,
    string TypeConstraints);

internal readonly record struct EnumDefinition(
    EnumClassTypeInfo EnumClass,
    EquatableArray<EnumItemInfo> ItemNames,
    EquatableArray<PropertyInfo> Properties,
    string? PartialConstructorVisibility,
    ulong SourceFileHash)
{
    public readonly string GlobalQualifiedValueType => "int";

    public readonly string InternalSharedMemberName => $"{Constants.PowerEnumInternalLowerPrefix}_shared_{PrivateMemberUtil.Hash(SourceFileHash, PrivateMemberUtil.MemberType.SharedMember)}";

    public readonly string InternalStructMemberName => $"{Constants.PowerEnumInternalLowerPrefix}_{PrivateMemberUtil.Hash(SourceFileHash, PrivateMemberUtil.MemberType.StructMember)}";

    public readonly string InternalSharedTypeName => $"{Constants.PowerEnumInternalUpperPrefix}_Shared_{PrivateMemberUtil.Hash(SourceFileHash, PrivateMemberUtil.MemberType.SharedType)}";

    public readonly string InternalStructTypeName => $"{Constants.PowerEnumInternalUpperPrefix}_{PrivateMemberUtil.Hash(SourceFileHash, PrivateMemberUtil.MemberType.StructType)}";
}
