using Microsoft.Azure.Functions.Worker;

namespace InnovationGameTests;

public class MockBindingContext : BindingContext
{
    public MockBindingContext(Dictionary<string, object?> bindingData)
    {
        BindingData = bindingData;
    }
    public override IReadOnlyDictionary<string, object?> BindingData { get;  }
}
