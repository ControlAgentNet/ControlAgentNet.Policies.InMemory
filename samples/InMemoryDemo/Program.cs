using Microsoft.Extensions.DependencyInjection;
using ControlAgentNet.Core.Models;
using ControlAgentNet.Policies;
using ControlAgentNet.Policies.InMemory;

Console.WriteLine("=== ControlAgentNet.Policies.InMemory Demo ===\n");

var services = new ServiceCollection();
services.AddInMemoryPolicyStore();
var provider = services.BuildServiceProvider();

var toolStore = provider.GetRequiredService<IToolPolicyStore>();
var channelStore = provider.GetRequiredService<IChannelPolicyStore>();
var auditStore = provider.GetRequiredService<IPolicyAuditStore>();

Console.WriteLine("--- Setting Global Policies ---\n");
await toolStore.SetToolPolicyAsync("greeting", PolicyValue.Enabled);
await toolStore.SetToolPolicyAsync("send_email", PolicyValue.ApprovalRequired);
await toolStore.SetToolPolicyAsync("delete_data", PolicyValue.Disabled);

Console.WriteLine("Global Tool Policies:");
foreach (var policy in toolStore.ListToolPolicies())
{
    Console.WriteLine($"  {policy.SubjectId}: {policy.PolicyValue}");
}

Console.WriteLine("\n--- Setting Scoped Policies (with PolicyContext) ---\n");

await toolStore.SetToolPolicyAsync(
    "send_email", 
    PolicyValue.Enabled, 
    new PolicyContext(TenantId: "acme-corp"));

await toolStore.SetToolPolicyAsync(
    "admin_tool", 
    PolicyValue.Enabled, 
    new PolicyContext(TenantId: "acme-corp"));

await toolStore.SetToolPolicyAsync(
    "send_email", 
    PolicyValue.Disabled, 
    new PolicyContext(TenantId: "partner-inc"));

Console.WriteLine("Policies for 'send_email':");
foreach (var policy in toolStore.ListToolPolicies("send_email"))
{
    Console.WriteLine($"  {policy.SubjectId}: {policy.PolicyValue} (Scope: {policy.Context?.TenantId ?? "global"})");
}

Console.WriteLine("\n--- Channel Policies ---\n");

await channelStore.SetChannelPolicyAsync("telegram", PolicyValue.Enabled);
await channelStore.SetChannelPolicyAsync("web", PolicyValue.Disabled);

Console.WriteLine("Channel Policies:");
foreach (var policy in channelStore.ListChannelPolicies())
{
    Console.WriteLine($"  {policy.SubjectId}: {policy.PolicyValue}");
}

Console.WriteLine("\n--- Policy Resolution ---\n");

var greetingPolicy = toolStore.GetToolPolicy("greeting");
Console.WriteLine($"  greeting (global): {greetingPolicy}");

var sendEmailGlobal = toolStore.GetToolPolicy("send_email");
Console.WriteLine($"  send_email (global): {sendEmailGlobal}");

var sendEmailAcme = toolStore.GetToolPolicy("send_email", new PolicyContext(TenantId: "acme-corp"));
Console.WriteLine($"  send_email (tenant=acme-corp): {sendEmailAcme}");

var sendEmailPartner = toolStore.GetToolPolicy("send_email", new PolicyContext(TenantId: "partner-inc"));
Console.WriteLine($"  send_email (tenant=partner-inc): {sendEmailPartner}");

Console.WriteLine("\n--- Policy History ---\n");

await toolStore.SetToolPolicyAsync("send_email", PolicyValue.ApprovalRequired);
await toolStore.SetToolPolicyAsync("send_email", PolicyValue.Disabled);
await toolStore.SetToolPolicyAsync("send_email", PolicyValue.Enabled);

Console.WriteLine("History for 'send_email':");
foreach (var record in toolStore.ListToolPolicyHistory("send_email").Take(5))
{
    Console.WriteLine($"  v{record.Version}: {record.PolicyValue}");
}

Console.WriteLine("\n--- Restore Previous Policy ---\n");
await toolStore.RestoreToolPolicyAsync("send_email");
var restored = toolStore.GetToolPolicy("send_email");
Console.WriteLine($"  send_email restored to: {restored}");

Console.WriteLine("\n--- Scoped Resolution ---\n");
var resolvedTool = await toolStore.ResolveToolPolicyAsync(
    "send_email",
    new PolicyContext(TenantId: "acme-corp", AgentId: "sales-agent", UserId: "alice"));
Console.WriteLine($"  resolved tool policy for acme/sales-agent/alice: {resolvedTool}");

await channelStore.SetChannelPolicyAsync("telegram", PolicyValue.Disabled, new PolicyContext(TenantId: "acme-corp", UserId: "alice"));
var resolvedChannel = await channelStore.ResolveChannelPolicyAsync(
    "telegram",
    new PolicyContext(TenantId: "acme-corp", UserId: "alice"));
Console.WriteLine($"  resolved channel policy for telegram/acme/alice: {resolvedChannel}");

Console.WriteLine("\n--- Audit Log ---\n");
await auditStore.AddEntryAsync(new PolicyAuditEntry(
    ScopeType: "Tool",
    ScopeId: "send_email",
    IsEnabled: true,
    ChangedAt: DateTimeOffset.UtcNow,
    Source: "demo",
    PolicyValue: PolicyValue.Enabled.ToString(),
    TenantId: "acme-corp",
    UserId: "alice"));

foreach (var entry in auditStore.GetRecentEntries())
{
    Console.WriteLine($"  {entry.ScopeType}:{entry.ScopeId} -> {entry.PolicyValue} ({entry.Source})");
}

Console.WriteLine("\n⚠️  Note: Data is lost on restart (in-memory only)");
Console.WriteLine("    Use ControlAgentNet.Policies.Sqlite for persistence");
Console.WriteLine("\n=== Demo Complete ===");
