# Contributing to ControlAgentNet.Agents

This repository contains the engine-agnostic base for ControlAgentNet agents.

## Principles

- keep the base runtime independent of any concrete engine;
- avoid reintroducing LLM-specific dependencies into this repo;
- keep `ControlAgentNet.Core` focused on contracts and common models;
- keep `ControlAgentNet.Runtime` focused on orchestration and middleware;
- keep product-facing convenience inside `ControlAgentNet.Agents`.

## Build

```bash
dotnet restore ControlAgentNet.Agents.slnx
dotnet build ControlAgentNet.Agents.slnx -c Release
```
