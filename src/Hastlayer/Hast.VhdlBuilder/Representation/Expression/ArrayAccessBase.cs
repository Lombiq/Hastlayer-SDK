using Hast.VhdlBuilder.Representation.Declaration;

namespace Hast.VhdlBuilder.Representation.Expression;

public abstract class ArrayAccessBase : DataObjectBase
{
    private IDataObject _arrayReference;

    public IDataObject ArrayReference
    {
        get => _arrayReference;
        set
        {
            _arrayReference = value;
            DataObjectKind = value.DataObjectKind;
            Name = value.Name;
        }
    }

    public override IDataObject ToReference() => this;
}
