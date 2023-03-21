using Hast.VhdlBuilder.Representation.Declaration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hast.Transformer.Vhdl.Models;

/// <summary>
/// Stores dependency relations between VHDL types, for custom types that need this. E.g. if MyArray is an array type
/// that stores elements of type MyRecord then that will be stored as MyArray depending on MyRecord. This is needed
/// because in VHDL MyArray should come after MyRecord in the code file.
/// </summary>
public class DependentTypesTable
{
    private readonly Dictionary<DataType, HashSet<string>> _dependencies =
        new(new DataTypeEqualityComparer());

    /// <summary>
    /// Declare a dependency between two types.
    /// </summary>
    public void AddDependency(DataType type, string dependencyTypeName)
    {
        if (!_dependencies.TryGetValue(type, out var dependencies))
        {
            dependencies = _dependencies[type] = new HashSet<string>();
        }

        dependencies.Add(dependencyTypeName);
    }

    /// <summary>
    /// Add a type that doesn't depend on any other type but other types depend on it.
    /// </summary>
    public void AddBaseType(DataType type) => AddDependency(type, dependencyTypeName: null);

    /// <summary>
    /// Gets all types stored in this table.
    /// </summary>
    public IEnumerable<DataType> Types => _dependencies.Keys;

    /// <summary>
    /// Fetch the dependencies of the given type.
    /// </summary>
    public IEnumerable<string> GetDependencies(DataType type)
    {
        if (!_dependencies.TryGetValue(type, out var dependencies)) return Enumerable.Empty<string>();
        return dependencies;
    }

    /// <summary>
    /// Adds this table to a list of tables but only if it actually has any dependencies.
    /// </summary>
    public void AddToIfNotEmpty(ICollection<DependentTypesTable> tables)
    {
        if (_dependencies.Count > 0) tables.Add(this);
    }

    private sealed class DataTypeEqualityComparer : IEqualityComparer<DataType>
    {
        public bool Equals(DataType x, DataType y) => x.Name == y.Name;

        public int GetHashCode(DataType obj) => obj.Name.GetHashCode(StringComparison.InvariantCulture);
    }
}
