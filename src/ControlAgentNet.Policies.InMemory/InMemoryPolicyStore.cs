using System.Collections.Concurrent;
using ControlAgentNet.Core.Models;

namespace ControlAgentNet.Policies.InMemory;

public sealed class InMemoryPolicyStore : IToolPolicyStore, IChannelPolicyStore, IPolicyAuditStore
{
    private readonly ConcurrentDictionary<string, ConfiguredPolicyRecord> _toolOverrides = new();
    private readonly ConcurrentDictionary<string, ConfiguredPolicyRecord> _channelOverrides = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<ConfiguredPolicyRecord>> _toolHistory = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<ConfiguredPolicyRecord>> _channelHistory = new();
    private readonly ConcurrentQueue<PolicyAuditEntry> _auditLog = new();

    public PolicyValue? GetToolPolicy(string toolId, PolicyContext? context = null)
        => _toolOverrides.TryGetValue(BuildKey(toolId, context), out var val) ? val.PolicyValue : null;

    public Task SetToolPolicyAsync(string toolId, PolicyValue value, PolicyContext? context = null, CancellationToken cancellationToken = default)
    {
        context ??= new PolicyContext();
        var key = BuildKey(toolId, context);
        var version = _toolOverrides.TryGetValue(key, out var existing) ? existing.Version + 1 : 1;
        var record = new ConfiguredPolicyRecord("Tool", toolId, value, context, DateTimeOffset.UtcNow, version);
        _toolOverrides[key] = record;
        _toolHistory.GetOrAdd(key, _ => new ConcurrentQueue<ConfiguredPolicyRecord>()).Enqueue(record);
        return Task.CompletedTask;
    }

    public IReadOnlyList<ConfiguredPolicyRecord> ListToolPolicies(string? toolId = null, PolicyContext? context = null)
        => _toolOverrides.Values
            .Where(r => r.PolicyValue != PolicyValue.Inherit)
            .Where(r => toolId is null || r.SubjectId.Equals(toolId, StringComparison.OrdinalIgnoreCase))
            .Where(r => MatchesContext(r.Context, context))
            .OrderBy(r => r.SubjectId)
            .ToList();

    public Task<PolicyValue> ResolveToolPolicyAsync(string toolId, PolicyContext context, CancellationToken cancellationToken = default)
    {
        var resolved = ResolvePolicy(toolId, context, _toolOverrides);
        return Task.FromResult(resolved ?? PolicyValue.Inherit);
    }

    private static PolicyValue? ResolvePolicy(string subjectId, PolicyContext context, ConcurrentDictionary<string, ConfiguredPolicyRecord> overrides)
    {
        var candidates = overrides.Values
            .Where(r => r.SubjectId.Equals(subjectId, StringComparison.OrdinalIgnoreCase))
            .Where(r => MatchesContext(r.Context, context))
            .OrderByDescending(r => GetSpecificityScore(r.Context))
            .ThenByDescending(r => r.Version)
            .Take(1)
            .ToList();

        return candidates.FirstOrDefault()?.PolicyValue;
    }

    private static int GetSpecificityScore(PolicyContext? ctx)
    {
        ctx ??= new PolicyContext();
        var score = 0;
        if (!string.IsNullOrEmpty(ctx.UserId)) score += 8;
        if (!string.IsNullOrEmpty(ctx.ChannelId)) score += 4;
        if (!string.IsNullOrEmpty(ctx.AgentId)) score += 2;
        if (!string.IsNullOrEmpty(ctx.TenantId)) score += 1;
        return score;
    }

    private static bool MatchesContext(PolicyContext? policyCtx, PolicyContext? queryCtx)
    {
        if (policyCtx is null) return true;
        queryCtx ??= new PolicyContext();
        return (string.IsNullOrEmpty(policyCtx.TenantId) || policyCtx.TenantId == queryCtx.TenantId)
            && (string.IsNullOrEmpty(policyCtx.AgentId) || policyCtx.AgentId == queryCtx.AgentId)
            && (string.IsNullOrEmpty(policyCtx.ChannelId) || policyCtx.ChannelId == queryCtx.ChannelId)
            && (string.IsNullOrEmpty(policyCtx.UserId) || policyCtx.UserId == queryCtx.UserId);
    }

    public IReadOnlyList<ConfiguredPolicyRecord> ListToolPolicyHistory(string toolId, PolicyContext? context = null, int take = 50)
        => _toolHistory.TryGetValue(BuildKey(toolId, context), out var history)
            ? history.Reverse().Take(take).ToList()
            : [];

    public Task RestoreToolPolicyAsync(string toolId, PolicyContext? context = null, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(toolId, context);
        var lastActive = _toolHistory.TryGetValue(key, out var history)
            ? history.LastOrDefault(r => r.PolicyValue != PolicyValue.Inherit)
            : null;
        return SetToolPolicyAsync(toolId, lastActive?.PolicyValue ?? PolicyValue.Inherit, context, cancellationToken);
    }

    public PolicyValue? GetChannelPolicy(string channelId, PolicyContext? context = null)
        => _channelOverrides.TryGetValue(BuildKey(channelId, context), out var val) ? val.PolicyValue : null;

    public Task SetChannelPolicyAsync(string channelId, PolicyValue value, PolicyContext? context = null, CancellationToken cancellationToken = default)
    {
        context ??= new PolicyContext();
        var key = BuildKey(channelId, context);
        var version = _channelOverrides.TryGetValue(key, out var existing) ? existing.Version + 1 : 1;
        var record = new ConfiguredPolicyRecord("Channel", channelId, value, context, DateTimeOffset.UtcNow, version);
        _channelOverrides[key] = record;
        _channelHistory.GetOrAdd(key, _ => new ConcurrentQueue<ConfiguredPolicyRecord>()).Enqueue(record);
        return Task.CompletedTask;
    }

    public IReadOnlyList<ConfiguredPolicyRecord> ListChannelPolicies(string? channelId = null, PolicyContext? context = null)
        => _channelOverrides.Values
            .Where(r => r.PolicyValue != PolicyValue.Inherit)
            .Where(r => channelId is null || r.SubjectId.Equals(channelId, StringComparison.OrdinalIgnoreCase))
            .Where(r => MatchesContext(r.Context, context))
            .OrderBy(r => r.SubjectId)
            .ToList();

    public Task<PolicyValue> ResolveChannelPolicyAsync(string channelId, PolicyContext context, CancellationToken cancellationToken = default)
    {
        var resolved = ResolvePolicy(channelId, context, _channelOverrides);
        return Task.FromResult(resolved ?? PolicyValue.Inherit);
    }

    public IReadOnlyList<ConfiguredPolicyRecord> ListChannelPolicyHistory(string channelId, PolicyContext? context = null, int take = 50)
        => _channelHistory.TryGetValue(BuildKey(channelId, context), out var history)
            ? history.Reverse().Take(take).ToList()
            : [];

    public Task RestoreChannelPolicyAsync(string channelId, PolicyContext? context = null, CancellationToken cancellationToken = default)
    {
        var key = BuildKey(channelId, context);
        var lastActive = _channelHistory.TryGetValue(key, out var history)
            ? history.LastOrDefault(r => r.PolicyValue != PolicyValue.Inherit)
            : null;
        return SetChannelPolicyAsync(channelId, lastActive?.PolicyValue ?? PolicyValue.Inherit, context, cancellationToken);
    }

    public Task AddEntryAsync(PolicyAuditEntry entry, CancellationToken cancellationToken = default)
    {
        _auditLog.Enqueue(entry);
        return Task.CompletedTask;
    }

    public IReadOnlyList<PolicyAuditEntry> GetRecentEntries(int take = 20)
        => _auditLog.TakeLast(take).Reverse().ToList();

    private static string BuildKey(string id, PolicyContext? context)
    {
        context ??= new PolicyContext();
        return $"{id}|{context.TenantId}|{context.AgentId}|{context.ChannelId}|{context.UserId}";
    }
}
