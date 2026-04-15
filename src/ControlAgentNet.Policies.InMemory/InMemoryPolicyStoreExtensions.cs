using Microsoft.Extensions.DependencyInjection;

namespace ControlAgentNet.Policies.InMemory;

public static class InMemoryPolicyStoreExtensions
{
    public static IServiceCollection AddInMemoryPolicyStore(this IServiceCollection services)
    {
        var store = new InMemoryPolicyStore();
        services.AddSingleton<IToolPolicyStore>(store);
        services.AddSingleton<IChannelPolicyStore>(store);
        services.AddSingleton<IPolicyAuditStore>(store);
        return services;
    }
}
