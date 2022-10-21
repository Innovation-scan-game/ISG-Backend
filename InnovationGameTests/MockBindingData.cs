using Microsoft.Azure.Functions.Worker;

namespace InnovationGameTests;

public class MockBindingMetadata : BindingMetadata
{
    public MockBindingMetadata(string type, BindingDirection direction)
    {
        Type = type;
        Direction = direction;
    }

    public override string Name { get; }
    public override string Type { get; }

    public override BindingDirection Direction { get; }
}
