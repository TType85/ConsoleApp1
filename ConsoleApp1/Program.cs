using System;
using System.Reflection;
using Newtonsoft.Json;

public record Foo([property: JsonProperty("barasdf")] string Bar);

public class Program
{
    public static void Main()
    {
        Type type = typeof(Foo);

        // Get all properties of the record
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            // Get the JsonProperty attribute
            var jsonPropertyAttribute = property.GetCustomAttribute<JsonPropertyAttribute>();

            if (jsonPropertyAttribute != null)
            {
                string jsonPropertyName = jsonPropertyAttribute.PropertyName;
                Console.WriteLine($"JsonProperty value for {property.Name}: {jsonPropertyName}");
            }
            else
            {
                Console.WriteLine($"No JsonProperty attribute found for {property.Name}.");
            }
        }
    }
}