using System;
using System.Collections.Generic;

namespace Hast.Transformer.Vhdl.Models;

/// <summary>
/// Maps class members to generated IDs that the hardware-implemented logic uses. A member access in .NET is thus
/// transferred as a call to a member ID and this member will determine which part of the logic will execute.
/// </summary>
public class MemberIdTable
{
    private readonly Dictionary<string, int> _mappings = new();
    private static MemberIdTable _emptyInstance;

    public IReadOnlyDictionary<string, int> Mappings => _mappings;

    public static MemberIdTable Empty
    {
        get
        {
            _emptyInstance ??= new MemberIdTable();
            return _emptyInstance;
        }
    }

    public int MaxId { get; private set; }

    public void SetMapping(string memberFullName, int id)
    {
        if (id > MaxId) MaxId = id;
        _mappings[memberFullName] = id;
    }

    public int LookupMemberId(string memberFullName)
    {
        if (_mappings.TryGetValue(memberFullName, out int id)) return id;
        throw new InvalidOperationException("No member ID mapping found for the given member name: " + memberFullName);
    }
}
