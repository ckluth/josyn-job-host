# josyn-job-host

Job execution runtime — a NuGet library linked by every JOSYN job executable.

When a job process starts, it calls `Core.Run(args)` and the library handles all
protocol communication, argument deserialization, reflection dispatch, and result routing.

---

## Role in the platform

`josyn-job-host` is a **decoupled consumer** of the JOSYN protocol.
It speaks `IJosynApplicationProtocol` but is not part of `josyn-jap`.
Job executables are first-class external participants, not internal scheduler components.

Every job executable references this package and delegates its `Main` entirely to `Core.Run`:

```csharp
private static async Task<int> Main(string[] args) => await Core.Run(args);
```

---

## Package

| Item | Value |
|------|-------|
| NuGet package | `JOSYN.JobHost` |
| Version | `1.0.0-preview01` |
| Target | `net10.0` |
| Namespace | `JOSYN.JobHost` |

---

## Further reading

- `JOSYN.JobHost/README.md` — component internals: `Core`, `JobInvoker`, `JAPClient`, attributes, test coverage
- `josyn-platform/repos/josyn-job-host.md` — full repo summary with structure, dispatch flow, and sanity notes
