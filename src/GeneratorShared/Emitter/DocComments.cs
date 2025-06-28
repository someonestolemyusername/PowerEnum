using PowerEnum.SourceGenerator.Models;
using System.CodeDom.Compiler;
using System.Security;

namespace PowerEnum.SourceGenerator.Emitter
{
    internal static class DocComments
    {
        public static void PartialConstructor(IndentedTextWriter iw, in EnumDefinition e)
        {
            iw.WriteLine($"/// <summary>Constructs a new instance of <see cref=\"{e.EnumClass.GlobalQualifiedName}\" />.</summary>");
            iw.WriteLine($"/// <remarks>This constructor must only be called during the static initialization of <see cref=\"{e.EnumClass.GlobalQualifiedName}\" />. This is because all items must be accounted for at application build time. New items cannot be dynamically created.<para>For an example of how to create a PowerEnum, see <seealso cref=\"global::PowerEnum.PowerEnumAttribute\" />.</para></remarks>");
            iw.WriteLine($"/// <exception cref=\"global::System.InvalidOperationException\">The constructor was called after static initialization of <see cref=\"{e.EnumClass.GlobalQualifiedName}\" /> had already occurred.</exception>");

            foreach (ref readonly var prop in e.Properties.AsReadOnlySpan())
            {
                var paramName = SecurityElement.Escape(prop.PartialConstructorParameterName);
                var propName = SecurityElement.Escape(prop.PropertyName);

                if (prop.ParamDescription == null)
                {
                    iw.WriteLine($"/// <param name=\"{paramName}\">The value for the <see cref=\"{propName}\" /> property.</param>");
                }
                else
                {
                    // Found that I have to include it here also because some tools assume
                    // all XML doc comments will be together in one place.
                    iw.WriteLine($"/// <param name=\"{paramName}\">{SecurityElement.Escape(prop.ParamDescription)}</param>");
                }
            }
        }

        public static void ValueOperator(IndentedTextWriter iw, string powerEnumClassName, string operatorAsString)
        {
            var escapedOperator = SecurityElement.Escape(operatorAsString);

            iw.WriteLine($"/// <summary>Compares two <see cref=\"{powerEnumClassName}\" /> values using the '{escapedOperator}' operator.</summary>");
            iw.WriteLine("/// <param name=\"a\">The first item to be compared.</param>");
            iw.WriteLine("/// <param name=\"b\">The second item to be compared.</param>");
            iw.WriteLine($"/// <returns>Returns a boolean value representing the result of the expression: <c><paramref name=\"a\" /> {escapedOperator} <paramref name=\"b\" /></c>.</returns>");
            iw.WriteLine("/// <exception cref=\"global::System.ArgumentNullException\">Any argument was null.</exception>");
        }

        public static void Items(IndentedTextWriter iw, string readOnlyListType)
        {
            iw.WriteLine("/// <summary>Retrieves a list of all available items in this PowerEnum.</summary>");
            iw.WriteLine($"/// <value>A shared instance of <see cref=\"{readOnlyListType}\" /> containing an ordered list of all items in this PowerEnum. The items are ordered in ascending order by value.</value>");
        }

        public static void FromName_name(IndentedTextWriter iw)
        {
            iw.WriteLine("/// <summary>Retrieves the item with the given name.</summary>");
            iw.WriteLine("/// <param name=\"name\">The name. Treated as case-sensitive.</param>");
            iw.WriteLine("/// <returns>The item that has the given name.</returns>");
            iw.WriteLine("/// <exception cref=\"global::System.ArgumentException\">There is no item in this PowerEnum matching the value of <paramref name=\"name\" />.</exception>");
        }

        public static void TryFromName_name_item(IndentedTextWriter iw)
        {
            iw.WriteLine("/// <summary>Retrieves the item with the given name.</summary>");
            iw.WriteLine("/// <param name=\"name\">The name. Treated as case-sensitive.</param>");
            iw.WriteLine("/// <param name=\"item\">When the method returns true, contains a valid item with a name matching the value of <paramref name=\"name\" />. When the method returns false, contains null.</param>");
            iw.WriteLine("/// <returns>True if a matching item was found; otherwise false.</returns>");
        }

        public static void FromValue_value(IndentedTextWriter iw)
        {
            iw.WriteLine("/// <summary>Retrieves the item with the given value.</summary>");
            iw.WriteLine("/// <param name=\"value\">The value, a number greater than or equal to zero.</param>");
            iw.WriteLine("/// <returns>The item that has the given value.</returns>");
            iw.WriteLine("/// <exception cref=\"global::System.ArgumentException\">There is no item in this PowerEnum matching the value of <paramref name=\"value\" />.</exception>");
        }

        public static void TryFromValue_value_item(IndentedTextWriter iw)
        {
            iw.WriteLine("/// <summary>Retrieves the item with the given value.</summary>");
            iw.WriteLine("/// <param name=\"value\">The value, a number greater than or equal to zero.</param>");
            iw.WriteLine("/// <param name=\"item\">When the method returns true, contains a valid item with a value matching the value of <paramref name=\"value\" />. When the method returns false, contains null.</param>");
            iw.WriteLine("/// <returns>True if a matching item was found; otherwise false.</returns>");
        }
    }
}
