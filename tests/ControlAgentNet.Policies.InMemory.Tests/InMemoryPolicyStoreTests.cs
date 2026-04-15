using ControlAgentNet.Core.Models;
using Xunit;

namespace ControlAgentNet.Policies.InMemory.Tests;

public class InMemoryPolicyStoreTests
{
    [Fact]
    public async Task ResolveToolPolicyAsync_prefers_most_specific_scope()
    {
        var store = new InMemoryPolicyStore();

        await store.SetToolPolicyAsync("send_email", PolicyValue.Disabled);
        await store.SetToolPolicyAsync("send_email", PolicyValue.Enabled, new PolicyContext(TenantId: "acme"));
        await store.SetToolPolicyAsync("send_email", PolicyValue.ApprovalRequired, new PolicyContext(TenantId: "acme", AgentId: "agent-1"));

        var resolved = await store.ResolveToolPolicyAsync("send_email", new PolicyContext(TenantId: "acme", AgentId: "agent-1"));

        Assert.Equal(PolicyValue.ApprovalRequired, resolved);
    }

    [Fact]
    public async Task ListToolPolicyHistory_returns_latest_first()
    {
        var store = new InMemoryPolicyStore();

        await store.SetToolPolicyAsync("send_email", PolicyValue.Enabled);
        await store.SetToolPolicyAsync("send_email", PolicyValue.Disabled);
        await store.SetToolPolicyAsync("send_email", PolicyValue.ApprovalRequired);

        var history = store.ListToolPolicyHistory("send_email");

        Assert.Equal([PolicyValue.ApprovalRequired, PolicyValue.Disabled, PolicyValue.Enabled], history.Select(x => x.PolicyValue).ToArray());
    }

    [Fact]
    public async Task AddEntryAsync_records_audit_entries()
    {
        var store = new InMemoryPolicyStore();
        var entry = new PolicyAuditEntry(
            ScopeType: "Tool",
            ScopeId: "send_email",
            IsEnabled: true,
            ChangedAt: DateTimeOffset.UtcNow,
            Source: "test",
            PolicyValue: PolicyValue.Enabled.ToString(),
            TenantId: "acme");

        await store.AddEntryAsync(entry);

        var recent = store.GetRecentEntries();
        Assert.Single(recent);
        Assert.Equal("send_email", recent[0].ScopeId);
    }
}
