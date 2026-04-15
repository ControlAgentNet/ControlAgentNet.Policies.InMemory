# ControlAgentNet.Policies.InMemory

<p align="center">
  <img src="https://img.shields.io/github/license/ControlAgentNet/ControlAgentNet.Policies.InMemory" alt="License">
  <img src="https://img.shields.io/github/actions/workflow/status/ControlAgentNet/ControlAgentNet.Policies.InMemory/ci.yml?branch=main" alt="CI">
  <img src="https://img.shields.io/nuget/v/ControlAgentNet.Policies.InMemory" alt="NuGet Version">
</p>

> In-memory implementation of the ControlAgentNet policy stores.

## What This Repository Contains

This repository publishes the `ControlAgentNet.Policies.InMemory` package and includes a small demo application.

## What It Does

This package provides an in-memory implementation of:

- `IToolPolicyStore`
- `IChannelPolicyStore`
- `IPolicyAuditStore`

Use it when you want:

- development or local testing without external storage
- deterministic behavior in tests
- a reference implementation of the policy contracts

## Installation

```bash
dotnet add package ControlAgentNet.Policies
dotnet add package ControlAgentNet.Policies.InMemory
```

## Usage

```csharp
using ControlAgentNet.Policies.InMemory;

builder.Services.AddInMemoryPolicyStore();
```

## Limitations

- data is lost on restart
- not suitable for production multi-instance deployments
- use SQLite or another persistent backend for durable storage

## Build

```bash
dotnet restore ControlAgentNet.Policies.InMemory.slnx
dotnet build ControlAgentNet.Policies.InMemory.slnx -c Release
dotnet test ControlAgentNet.Policies.InMemory.slnx -c Release --no-build
dotnet pack ControlAgentNet.Policies.InMemory.slnx -c Release -o artifacts/nuget
```

## Sample

The repository includes `samples/InMemoryDemo` to demonstrate scoped policy configuration and resolution with the in-memory store.

## Versioning

- local builds: `0.1.1-dev`
- pull requests: `0.1.1-preview.<run_number>`
- pushes to `main`: `0.1.1-alpha.<run_number>`
- tags like `v0.1.1`: exact stable package version `0.1.1`

See `VERSIONING.md` for the release flow.
