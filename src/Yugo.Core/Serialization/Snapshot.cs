using System.Reflection;
using Microsoft.Xna.Framework;

namespace Yugo.Core.Serialization;

/// <summary>
/// Interface for objects that can be serialized to and deserialized from a snapshot representation.
/// </summary>
public interface ISnapshotSerializable
{
    /// <summary>
    /// Serializes the object into a dictionary of string key-value pairs.
    /// The keys and values should be chosen to uniquely represent the state of the object.
    /// </summary>
    /// <param name="data">The dictionary to populate with serialized data.</param>
    void Serialize(Dictionary<string, string> data);

    /// <summary>
    /// Deserializes the object from a dictionary of string key-value pairs.
    /// The dictionary will contain the same keys and values that were produced by the Serialize method.
    /// </summary>
    /// <param name="data">The dictionary containing serialized data.</param>
    void Deserialize(Dictionary<string, string> data);
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TypeIdAttribute(string id, string name) : Attribute
{
    public readonly string Id = id;
    public readonly string Name = name;

    public static string GetId(Type type)
    {
        var attr = type.GetCustomAttribute<TypeIdAttribute>();
        if (attr == null)
            throw new InvalidOperationException($"Missing TypeIdAttribute on '{type.Name}'.");
        return attr.Id;
    }
}

/// <summary>
/// A snapshot representation of an object, consisting of a type identifier and serialized data.
/// </summary>
/// <param name="TypeId">The type identifier of the object.</param>
/// <param name="Data">The serialized data of the object.</param>
public sealed record Snapshot(string TypeId, Dictionary<string, string> Data);

/// <summary>
/// Factory for creating objects from their snapshot representations.
/// </summary>
public static class SnapshotFactory
{
    private static readonly Dictionary<string, HashSet<Type>> Registry = new();
    private static bool _initialized;

    /// <summary>
    /// Ensures that all types with the TypeIdAttribute in the given assembly are registered.
    /// </summary>
    /// <param name="assembly"></param>
    public static void EnsureRegistered(Assembly assembly)
    {
        if (_initialized)
            return;

        foreach (var type in assembly.GetTypes())
        {
            var attr = type.GetCustomAttribute<TypeIdAttribute>();
            if (attr == null)
                continue;

            if (!Registry.ContainsKey(attr.Id))
                Registry[attr.Id] = new HashSet<Type>();

            Registry[attr.Id].Add(type);
        }

        _initialized = true;
    }

    /// <summary>
    /// Gets a mapping of type identifiers to type names for all types that implement the specified interface T.
    /// </summary>
    /// <typeparam name="T">The base interface or class to filter types.</typeparam>
    /// <returns>A dictionary mapping type identifiers to type names.</returns>
    public static Dictionary<string, string> GetTypeNames<T>()
        where T : ISnapshotSerializable
    {
        var result = new Dictionary<string, string>();
        foreach (var kvp in Registry)
        {
            foreach (var type in kvp.Value)
            {
                if (!typeof(T).IsAssignableFrom(type))
                    continue;

                var attr = type.GetCustomAttribute<TypeIdAttribute>();
                if (attr != null)
                    result[kvp.Key] = attr.Name;
            }
        }
        return result;
    }

    /// <summary>
    /// Creates an instance of the specified type T from the given snapshot, using the provided constructor arguments.
    /// </summary>
    /// <typeparam name="T">The type of object to create.</typeparam>
    /// <param name="snapshot">The snapshot containing the serialized data.</param>
    /// <param name="args">Constructor arguments for creating the object.</param>
    /// <returns>The deserialized object of type T.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static T Create<T>(Snapshot snapshot, params object[] args)
        where T : class, ISnapshotSerializable
    {
        if (!Registry.TryGetValue(snapshot.TypeId, out var types))
            throw new InvalidOperationException($"Unknown type id '{snapshot.TypeId}'.");

        foreach (var type in types)
        {
            if (!typeof(T).IsAssignableFrom(type))
                continue;

            var obj =
                Activator.CreateInstance(type, args) as T
                ?? throw new InvalidOperationException(
                    $"Failed to create instance of '{type.Name}'."
                );

            obj.Deserialize(snapshot.Data);
            return obj;
        }

        throw new InvalidOperationException($"No suitable type found for id '{snapshot.TypeId}'.");
    }
}

/// <summary>
/// Helper methods for serializing and deserializing collections of Point objects.
/// </summary>
public static class SnapshotHelpers
{
    public static string SerializeCells(IEnumerable<Point> cells)
    {
        return string.Join(';', cells.Select(p => $"{p.X},{p.Y}"));
    }

    public static List<Point> DeserializeCells(string raw)
    {
        var result = new List<Point>();
        if (string.IsNullOrWhiteSpace(raw))
            return result;

        var tokens = raw.Split(
            [';', '|'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        foreach (var token in tokens)
        {
            var parts = token.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );
            if (parts.Length != 2)
                throw new InvalidOperationException($"Invalid cell format '{token}'.");
            if (!int.TryParse(parts[0], out var x) || !int.TryParse(parts[1], out var y))
                throw new InvalidOperationException($"Invalid cell coordinates '{token}'.");
            result.Add(new Point(x, y));
        }

        return result;
    }
}
