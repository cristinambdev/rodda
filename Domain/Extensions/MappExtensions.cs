using System.Reflection;

namespace Domain.Extensions;

// Extension methods for mapping objects from one type to another.
public static class MapExtensions
{
    public static TDestination MapTo<TDestination>(this object source)
    {
        // Null check for the source object.
        ArgumentNullException.ThrowIfNull(source, nameof(source));

        // Create a new instance of the destination type.
        TDestination destination = Activator.CreateInstance<TDestination>();
        source.MapOnto(destination);
        return destination;
    }

    // Copies matching properties onto an existing instance (e.g. form data onto a tracked entity).
    public static void MapOnto<TDestination>(this object source, TDestination destination)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(destination, nameof(destination));

        //  Retrieve all public instance properties for both source and destination types.
        var sourceProperties = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var destinationProperties = typeof(TDestination).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Iterate through the destination properties to find matches.
        foreach (var destinationProperty in destinationProperties)
        {
            // The mapper only copies data if the source and destination properties have the exact same name and exact same data type.
            var sourceProperty = sourceProperties.FirstOrDefault(x =>
                x.Name == destinationProperty.Name &&
                x.PropertyType == destinationProperty.PropertyType);

            //  If a match is found and the destination property can be written to, map the value.
            if (sourceProperty != null && destinationProperty.CanWrite)
            {
                var value = sourceProperty.GetValue(source);
                destinationProperty.SetValue(destination, value);
            }
        }
    }
}
