﻿using Microsoft.Azure.Functions.Worker;

namespace InnovationGameTests;

public class MockFunctionContext : FunctionContext, IDisposable
{
    private readonly FunctionInvocation _invocation;

    public MockFunctionContext(string? token)
        : this(new MockFunctionDefinition(), new MockFunctionInvocation())
    {
        if (token != null)
        {
            Dictionary<string, object?> bindings = new()
            {
                {"Headers", "{\"Authorization\":\"Bearer " + token + "\"}"}
            };

            BindingContext = new MockBindingContext(bindings);
        }
    }

    public MockFunctionContext(FunctionDefinition functionDefinition, FunctionInvocation invocation)
    {
        FunctionDefinition = functionDefinition;
        _invocation = invocation;
    }

    public bool IsDisposed { get; private set; }

    public override IServiceProvider? InstanceServices { get; set; }

    public override FunctionDefinition FunctionDefinition { get; }

    public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();

    public override IInvocationFeatures? Features { get; } /*= new InvocationFeatures(Enumerable.Empty<IInvocationFeatureProvider>());*/

    public override string InvocationId => _invocation.Id;

    public override string FunctionId => _invocation.FunctionId;

    public override TraceContext TraceContext => _invocation.TraceContext;

    public override BindingContext BindingContext { get; }

    public override RetryContext? RetryContext { get; }

    public void Dispose()
    {
        IsDisposed = true;
    }
}
