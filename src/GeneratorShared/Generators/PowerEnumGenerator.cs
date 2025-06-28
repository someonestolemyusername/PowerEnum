using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PowerEnum.SourceGenerator.Models;
using PowerEnumShared;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace PowerEnum.SourceGenerator.Generator;

[Generator(LanguageNames.CSharp)]
internal class PowerEnumGenerator : IIncrementalGenerator
{
    public static class TrackingNames
    {
        public const string GetCompilationInfo = nameof(GetCompilationInfo);
        public const string GetPowerEnums = nameof(GetPowerEnums);
        public const string FilterOutNulls = nameof(FilterOutNulls);
        public const string CombinedInfo = nameof(CombinedInfo);
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var compilationInfo = context.CompilationProvider
            .Select(static (compilation, ct) =>
            {
                var hasImmutableArray = !compilation.GetTypesByMetadataName(
                        "System.Collections.Immutable.ImmutableArray")
                    .IsEmpty;

                var hasFrozenDictionary = !compilation.GetTypesByMetadataName(
                        "System.Collections.Frozen.FrozenDictionary")
                    .IsEmpty;

                var hasNotNullWhen = CheckForHasNotNullWhen(compilation);

                var hasSystemTextJsonConverters = CheckForSystemTextJsonConverters(compilation);
                var hasNewtonsoftJsonConverters = CheckForNewtonsoftJson(compilation);

                return new CompilationInfo(
                    HasImmutableArray: hasImmutableArray,
                    HasFrozenDictionary: hasFrozenDictionary,
                    HasNullableAnnotations: compilation.Options.NullableContextOptions.AnnotationsEnabled(),
                    HasNotNullWhen: hasNotNullWhen,
                    HasSystemTextJsonConverters: hasSystemTextJsonConverters,
                    HasNewtonsoftJsonConverters: hasNewtonsoftJsonConverters,
                    LanguageVersion: ((CSharpCompilation)compilation).LanguageVersion);
            })
            .WithTrackingName(TrackingNames.GetCompilationInfo);

        var powerEnums = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                Constants.PowerEnumAttributeFullyQualifiedMetadataName,
                predicate: static (n, _) => n is ClassDeclarationSyntax,
                transform: AnalyseSyntax)
            .WithTrackingName(TrackingNames.GetPowerEnums)
            .Where(static x => x is not null)
            .WithTrackingName(TrackingNames.FilterOutNulls);

        var combined = powerEnums.Combine(compilationInfo)
            .WithTrackingName(TrackingNames.CombinedInfo);

        context.RegisterSourceOutput(combined, Generate);
    }

    private static bool CheckForHasNotNullWhen(Compilation compilation)
    {
        var notNullWhen = compilation.GetTypesByMetadataName(
            "System.Diagnostics.CodeAnalysis.NotNullWhenAttribute");

        foreach (var item in notNullWhen)
        {
            // If it is not in our assembly, then we have to ensure it is public.
            // This is because some libraries polyfill this attribute with an internal implementation.
            if (!SymbolEqualityComparer.Default.Equals(item.ContainingAssembly, compilation.Assembly))
            {
                if (item.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }
            }

            foreach (var c in item.InstanceConstructors)
            {
                // Basically - if you are not using 'new' .NET, you might have this
                // attribute but the constructor will be private.
                if (c.DeclaredAccessibility == Accessibility.Public
                    && c.Parameters.Length == 1
                    && c.Parameters[0].Type?.SpecialType == SpecialType.System_Boolean)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool CheckForSystemTextJsonConverters(Compilation compilation)
    {
        var jsonConverter = compilation.GetTypesByMetadataName(
            "System.Text.Json.Serialization.JsonConverter`1");
        var jsonConverterAttribute = compilation.GetTypesByMetadataName(
            "System.Text.Json.Serialization.JsonConverterAttribute");

        return !jsonConverter.IsEmpty && !jsonConverterAttribute.IsEmpty;
    }

    private static bool CheckForNewtonsoftJson(Compilation compilation)
    {
        var jsonConverter = compilation.GetTypesByMetadataName(
            "Newtonsoft.Json.JsonConverter`1");
        var jsonConverterAttribute = compilation.GetTypesByMetadataName(
            "Newtonsoft.Json.JsonConverterAttribute");

        return !jsonConverter.IsEmpty && !jsonConverterAttribute.IsEmpty;
    }

    private static void Generate(
        SourceProductionContext context,
        (EnumDefinition? enumDefinition, CompilationInfo compilationInfo) item)
    {
        if (item.enumDefinition.HasValue)
        {
            Emitter.Emit(context, item.enumDefinition.Value, item.compilationInfo);
        }
    }

    private static EnumDefinition? AnalyseSyntax(
        GeneratorAttributeSyntaxContext context,
        CancellationToken ct)
    {
        if (context.TargetNode is not ClassDeclarationSyntax classDeclarationSyntax)
        {
            return null;
        }

        if (context.SemanticModel.GetDeclaredSymbol(context.TargetNode, ct)
            is not INamedTypeSymbol targetClass)
        {
            return null;
        }

        var (itemProperties, partialConstructorVisibility) = ExtractEnumItemProperties(
            context, classDeclarationSyntax, targetClass, ct);

        var items = ExtractItems(classDeclarationSyntax, targetClass, context, ct);

        return new EnumDefinition
        {
            EnumClass = EnumClassTypeInfo.Create(targetClass, GetParents(classDeclarationSyntax)),
            ItemNames = items,
            Properties = itemProperties,
            PartialConstructorVisibility = partialConstructorVisibility,
            SourceFileHash = HashContent(context.TargetNode, ct),
        };
    }

    private static ulong HashContent(SyntaxNode node, CancellationToken ct)
    {
        var tree = node.SyntaxTree;
        var text = tree.GetText(ct);
        var hash = PrivateMemberUtil.Hash(PrivateMemberUtil.InitialHashValue, text.ToString());
        return hash;
    }

    private static EquatableArray<ParentType> GetParents(BaseTypeDeclarationSyntax classDeclarationSyntax)
    {
        List<ParentType> parents = [];

        var parent = classDeclarationSyntax.Parent as TypeDeclarationSyntax;
        while (parent != null)
        {
            string keyword = parent.Keyword.ValueText;

            if (parent is RecordDeclarationSyntax record)
            {
                keyword += " " + record.ClassOrStructKeyword.ValueText;
            }

            parents.Add(new()
            {
                Name = parent.Identifier.ToString(),
                Keyword = keyword,
                TypeConstraints = parent.ConstraintClauses.ToString(),
            });

            parent = parent.Parent as TypeDeclarationSyntax;
        }

        parents.Reverse();

        return new(parents.ToArray());
    }

    private static EquatableArray<EnumItemInfo> ExtractItems(
        ClassDeclarationSyntax classDeclarationSyntax,
        INamedTypeSymbol targetClass,
        in GeneratorAttributeSyntaxContext context,
        CancellationToken ct)
    {
        // To define an item in a PowerEnum, you:
        // - call the constructor, and
        // - assign the result to a readonly field/property.
        //
        // That's it.
        //
        // The name of the item comes from the thing it is being assigned to.
        //
        // You can assign to properties that don't already exist inside the static
        // constructor, and we will generate a property for you:
        //
        //     static Colour() {
        //         Red = new Country("#FF0000");
        //         Green = new("#00FF00");
        //     }
        //
        // Another great way to define an item is to declare a public static get-only property:
        //     public static Colour Blue { get; } = new("#0000FF");
        //
        // Not all C# versions support those, however.
        //
        // Some users might want to assign to public readonly fields.
        //
        // Finally, people might want to set an item to a private backing field and then expose
        // it via a get-only property. In this specific case, we must track down the property
        // that exposes it and use that to get the name for the item.



        ConstructorDeclarationSyntax? staticConstructor = null;

        using var staticConstructors = MemoryPool<IMethodSymbol>
            .Shared
            .Rent(targetClass.StaticConstructors.Length);

        int constructorCount = 0;

        foreach (var constructor in targetClass.StaticConstructors)
        {
            staticConstructors.Memory.Span[constructorCount++] = constructor;
        }

        if (constructorCount == 1)
        {
            // One static constructor found - let's analyse it and get the items.
            var c = staticConstructors.Memory.Span[0];
            if (c.DeclaringSyntaxReferences.Length == 1)
            {
                var declaration = c.DeclaringSyntaxReferences[0].GetSyntax(ct);
                if (declaration.IsKind(SyntaxKind.ConstructorDeclaration))
                {
                    staticConstructor = (ConstructorDeclarationSyntax)declaration;
                }
            }
        }




        // So now we identify all constructor calls in the ClassDeclarationSyntax
        // Any calls to our type or derived type, we ensure it is in an assignment context.
        // TODO: If it is not called in an assignment context, raise a diagnostic.

        var items = new List<EnumItemInfo>();
        var staticConstructorItems = new List<EnumItemInfo>();
        var fieldItems = new HashSet<int>();

        var allNewExpressions = classDeclarationSyntax
            .DescendantNodes()
            .OfType<BaseObjectCreationExpressionSyntax>()
            .ToList();

        foreach (var n in allNewExpressions)
        {
            var ti = context.SemanticModel.GetTypeInfo(n, ct);
            bool inheritsFromTargetClass = IsSubclassOfTarget(targetClass, ti);

            if (n.Parent is AssignmentExpressionSyntax aes)
            {
                // Deal with assignments in the static constructor body first.
                // They must either be the correct type, or an unknown type.
                // If unknown type, we will generate the property ourselves.

                // Assignment expression can only be valid if in a static constructor.
                if (staticConstructor?.Body != null
                    && aes.Parent is ExpressionStatementSyntax ess
                    && ess.Parent == staticConstructor.Body)
                {
                    if (aes.Left is IdentifierNameSyntax ins)
                    {
                        var symbol = context.SemanticModel.GetSymbolInfo(ins, ct);

                        staticConstructorItems.Add(new(ins.Identifier.Text, symbol.Symbol == null));
                        continue;
                    }
                    else if (aes.Left is MemberAccessExpressionSyntax maes
                        && maes.Expression is IdentifierNameSyntax)
                    {
                        var memberSymbol = context.SemanticModel.GetSymbolInfo(maes.Expression, ct);
                        if (SymbolEqualityComparer.Default.Equals(memberSymbol.Symbol, targetClass))
                        {
                            var symbol = context.SemanticModel.GetSymbolInfo(maes.Name, ct);

                            staticConstructorItems.Add(new(maes.Name.Identifier.Text, symbol.Symbol == null));
                            continue;
                        }
                    }
                }
            }

            if (!inheritsFromTargetClass)
            {
                // This object creation expression does not concern us.
                continue;
            }


            // Work out if this is in an assignment context...
            // If it is not, raise a diagnostic
            // Otherwise, it is an item.
            var x = n;

            if (n.Parent is EqualsValueClauseSyntax evcs)
            {
                if (evcs.Parent is VariableDeclaratorSyntax vds)
                {
                    if (vds.Parent is VariableDeclarationSyntax vdns)
                    {
                        if (vdns.Parent is FieldDeclarationSyntax fds)
                        {
                            if (fds.Parent == classDeclarationSyntax
                                && fds.Modifiers.Any(SyntaxKind.StaticKeyword))
                            {
                                // This is a static field declaration in our class that is initialised to a new item.
                                items.Add(new EnumItemInfo(vds.Identifier.Text, false));

                                // We will try and find a property exposing this field later
                                // if we find one, that name is the one we will use.
                                fieldItems.Add(items.Count - 1);

                                continue;
                            }
                        }
                    }
                }
                else if (evcs.Parent is PropertyDeclarationSyntax pds)
                {
                    if (pds.Parent == classDeclarationSyntax
                        && pds.Modifiers.Any(SyntaxKind.StaticKeyword))
                    {
                        // This is a static property declaration syntax in our class that is initialised to a new item.
                        items.Add(new(pds.Identifier.Text, false));

                        continue;
                    }
                }
            }
        }
        // end of loop through constructor invocations


        // Now we need to find all properties that are either:
        // - a simple getter for a field, or
        // - an arrow expression for a field.
        // Any such property will have its name replace the name of the field item.

        // TODO
        var properties = classDeclarationSyntax
            .DescendantNodes()
            .OfType<PropertyDeclarationSyntax>()
            .ToList();

        foreach (var n in properties)
        {
            if (n.ExpressionBody is ArrowExpressionClauseSyntax aecs)
            {
                HandlePropertyExpression(targetClass, context, items, fieldItems, n, aecs.Expression, ct);
            }
            else if (n.AccessorList?.Accessors.Count == 1)
            {
                var accessor = n.AccessorList.Accessors[0];
                if (accessor is AccessorDeclarationSyntax ads && ads.IsKind(SyntaxKind.GetAccessorDeclaration))
                {
                    if (accessor.ExpressionBody is ArrowExpressionClauseSyntax aecs2)
                    {
                        HandlePropertyExpression(targetClass, context, items, fieldItems, n, aecs2.Expression, ct);
                    }
                    else if (accessor.Body is BlockSyntax bs)
                    {
                        if (bs.Statements.Count == 1)
                        {
                            if (bs.Statements[0] is ReturnStatementSyntax rss && rss.Expression != null)
                            {
                                HandlePropertyExpression(targetClass, context, items, fieldItems, n, rss.Expression, ct);
                            }
                        }
                    }
                    else
                    {
                        // Not sure how to handle this accessor.
                    }
                }
            }
        }


        var combinedItems = items.Concat(staticConstructorItems).ToArray();

        return new EquatableArray<EnumItemInfo>(combinedItems);

        static bool IsSubclassOfTarget(INamedTypeSymbol targetClass, TypeInfo ti)
        {
            bool inheritsFromTargetClass = false;

            var testType = ti.Type;
            while (testType != null)
            {
                if (SymbolEqualityComparer.Default.Equals(testType, targetClass))
                {
                    inheritsFromTargetClass = true;
                    break;
                }
                else
                {
                    testType = testType.BaseType;
                }
            }

            return inheritsFromTargetClass;
        }

        //static (SyntaxToken Identifier, ISymbol? Symbol)? GetIdentifierInTargetClass(
        //    SemanticModel semanticModel,
        //    INamedTypeSymbol targetClass,
        //    ExpressionSyntax expression,
        //    CancellationToken ct)
        //{
        //    if (expression is IdentifierNameSyntax ins)
        //    {
        //        var symbolInfo = semanticModel.GetSymbolInfo(ins, ct);

        //        return (ins.Identifier, symbolInfo.Symbol);
        //    }
        //    else if (expression is MemberAccessExpressionSyntax maes
        //        && maes.Expression is IdentifierNameSyntax)
        //    {
        //        var memberSymbolInfo = semanticModel.GetSymbolInfo(maes.Expression, ct);
        //        if (SymbolEqualityComparer.Default.Equals(memberSymbolInfo.Symbol, targetClass))
        //        {
        //            var symbolInfo = semanticModel.GetSymbolInfo(maes.Name, ct);

        //            return (maes.Name.Identifier, symbolInfo.Symbol);
        //        }
        //    }

        //    return default;
        //}

        static void HandlePropertyExpression(INamedTypeSymbol targetClass, GeneratorAttributeSyntaxContext context, List<EnumItemInfo> items, HashSet<int> fieldItems, PropertyDeclarationSyntax n, ExpressionSyntax expression, CancellationToken ct)
        {
            if (expression is IdentifierNameSyntax ins)
            {
                MapExpressionProperty(targetClass, context, items, fieldItems, n, expression, ct);
            }
            else if (expression is MemberAccessExpressionSyntax maes
                && maes.Expression is IdentifierNameSyntax)
            {
                MapExpressionProperty(targetClass, context, items, fieldItems, n, maes.Expression, ct);
            }

            static void MapExpressionProperty(INamedTypeSymbol targetClass, GeneratorAttributeSyntaxContext context, List<EnumItemInfo> items, HashSet<int> fieldItems, PropertyDeclarationSyntax n, ExpressionSyntax expression, CancellationToken ct)
            {
                var ti = context.SemanticModel.GetTypeInfo(expression, ct);
                bool inheritsFromTargetClass = IsSubclassOfTarget(targetClass, ti);

                if (inheritsFromTargetClass)
                {
                    string identifier;
                    if (expression is IdentifierNameSyntax ins)
                    {
                        identifier = ins.Identifier.Text;
                    }
                    else if (expression is MemberAccessExpressionSyntax maes)
                    {
                        identifier = maes.Name.Identifier.Text;
                    }
                    else
                    {
                        return;
                    }


                    // Map this property name to the field.

                    (EnumItemInfo value, int index)? fieldItem = items
                        .Select((value, index) => (value, index))
                        .FirstOrDefault(x => x.value.Name == identifier);

                    if (fieldItem != null)
                    {
                        if (fieldItems.Contains(fieldItem.Value.index))
                        {
                            items[fieldItem.Value.index] = items[fieldItem.Value.index] with
                            {
                                Name = n.Identifier.Text,
                            };

                            fieldItems.Remove(fieldItem.Value.index);
                        }
                        else
                        {
                            // todo: report diagnostic to warn user that
                            // they have exposed the same field twice?
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Try to find a list of properties we will need to generate for each enum item.
    /// There may be no properties, or the user may be implementing the properties manually.
    /// </summary>
    /// <remarks>
    /// - If the class has a primary constructor, we generate a property for each parameter.
    /// - If the class has no constructor, or a non-partial constructor, or more than one constructor,
    ///     the user is doing their own thing and we leave it alone.
    /// - If the class has a single unimplemented partial constructor declared,
    ///     we can implement it and also generate a property for each parameter.
    /// </remarks>
    private static (EquatableArray<PropertyInfo> propertyInfo, string? partialConstructorVisibility)
        ExtractEnumItemProperties(
            in GeneratorAttributeSyntaxContext context,
            ClassDeclarationSyntax classDeclarationSyntax,
            INamedTypeSymbol targetClass,
            CancellationToken ct)
    {
        using var constructors = MemoryPool<IMethodSymbol>.Shared.Rent(targetClass.InstanceConstructors.Length);
        int constructorCount = 0;

        foreach (var constructor in targetClass.InstanceConstructors)
        {
            if (constructor.DeclaredAccessibility == Accessibility.Public)
            {
                constructors.Memory.Span[constructorCount++] = constructor;
            }
        }

        if (constructorCount == 1)
        {
            var constructor = constructors.Memory.Span[0];
            var parameters = constructor.Parameters;

            var dsr = constructor
                .DeclaringSyntaxReferences
                .FirstOrDefault();

            if (dsr != null)
            {
                var s = dsr.GetSyntax(ct);
                if (s.IsKind(SyntaxKind.ClassDeclaration))
                {
                    var cds = (ClassDeclarationSyntax)s;

                    foreach (var node in cds.ChildNodes())
                    {
                        if (node is ParameterListSyntax)
                        {
                            // We have a primary constructor.
                            return (ExtractEnumItemProperties(parameters, ct), null);
                        }
                    }
                }
            }
        }

        // The generator is being written as partial constructors are in C# language preview.
        // The semantic model doesn't seem to have it yet, so instead will look for it in the syntax.
        var childNodes = classDeclarationSyntax.ChildNodes();
        foreach (var node in childNodes)
        {
            if (node.IsKind(SyntaxKind.ConstructorDeclaration))
            {
                var cd = (ConstructorDeclarationSyntax)node;
                if (cd.Modifiers.Any(SyntaxKind.PartialKeyword)
                    && !cd.Modifiers.Any(SyntaxKind.StaticKeyword)
                    && cd.ParameterList.Parameters.Count > 0
                    && cd.Body == null)
                {
                    string accessibility = "";

                    var syntax = context.SemanticModel.GetDeclaredSymbol(cd, ct);
                    if (syntax != null && syntax.DeclaredAccessibility != Accessibility.NotApplicable)
                    {
                        // If the constructor is private, we have to emit blank accessibiltiy
                        // if the user has not used the private keyword - C# defaults to private.
                        if (syntax.DeclaredAccessibility != Accessibility.Private
                            || cd.Modifiers.Any(SyntaxKind.PrivateKeyword))
                        {
                            accessibility = SyntaxFacts.GetText(syntax.DeclaredAccessibility);
                        }
                    }

                    return (
                        ExtractPartialConstructorEnumItemProperties(
                            context,
                            cd,
                            ct),
                        accessibility);
                }
            }
        }

        return new();
    }

    private static EquatableArray<PropertyInfo> ExtractEnumItemProperties(
        in ImmutableArray<IParameterSymbol> parameters,
        CancellationToken ct)
    {
        var properties = new PropertyInfo[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            var propertyName = StringUtils.ConvertToPropertyName(p.Name);
            var fieldName = StringUtils.ConvertToFieldName(propertyName);
            properties[i] = new PropertyInfo
            {
                PropertyName = propertyName,
                PropertyNameForField = fieldName,
                PrimaryConstructorParameterName = p.Name,
                GlobalQualifiedPropertyType = p.Type
                        .ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat),
            };
        }

        return new(properties);
    }

    private static EquatableArray<PropertyInfo> ExtractPartialConstructorEnumItemProperties(
        in GeneratorAttributeSyntaxContext context,
        in ConstructorDeclarationSyntax cds,
        CancellationToken ct)
    {
        var constructorSymbol = context.SemanticModel.GetDeclaredSymbol(cds, ct);
        var xmlDoc = constructorSymbol?.GetDocumentationCommentXml(
            expandIncludes: true,
            cancellationToken: ct);

        XDocument? doc = null;
        if (xmlDoc != null)
        {
            try
            {
                doc = new XDocument(
                    XElement.Parse(xmlDoc));
            }
            catch { }
        }

        var parameters = cds.ParameterList.Parameters;

        var properties = new PropertyInfo[parameters.Count];

        for (var i = 0; i < parameters.Count; i++)
        {
            var p = parameters[i];

            if (p.Type == null)
            {
                continue;
            }

            var type = context.SemanticModel.GetTypeInfo(p.Type, ct).Type;

            if (type == null)
            {
                continue;
            }

            var propertyName = StringUtils.ConvertToPropertyName(p.Identifier.ValueText);
            var fieldName = StringUtils.ConvertToFieldName(p.Identifier.ValueText);

            var paramDescription = doc
                ?.Descendants("param")
                .Where(x => x.Attribute("name")?.Value == p.Identifier.ValueText)
                .FirstOrDefault()
                ?.Value;

            properties[i] = new PropertyInfo
            {
                PropertyName = propertyName,
                PropertyNameForField = fieldName,
                PartialConstructorParameterName = p.Identifier.ValueText,
                GlobalQualifiedPropertyType = type
                        .ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat),
                ParamDescription = paramDescription,
            };
        }

        return new(properties);
    }
}
