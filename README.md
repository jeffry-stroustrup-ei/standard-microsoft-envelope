# Standard Microsoft Envelope

**Enterprise-grade value correspondence for .NET.**

Values should never be passed directly. They should be properly addressed.

Standard Microsoft Envelope formalizes the process of placing a value inside an envelope, obtaining the necessary approval, and allowing another department to discover the value at an appropriate time.

```csharp
using StandardEnvelope;

var envelope = Envelope.Create(42)
    .AddressTo("Calculation Department")
    .MarkAsConfidential()
    .RequireApproval()
    .Seal()
    .Approve("Management");

int answer = envelope.Open(
    recipient: "Calculation Department",
    clearance: Clearance.SeniorDeveloper);
```

The answer is still 42. The procedure is now compliant.

## Getting started

Requires the .NET 8 SDK.

```sh
git clone https://github.com/jeffry-stroustrup-ei/standard-microsoft-envelope.git
cd standard-microsoft-envelope
dotnet build src/StandardEnvelope
dotnet pack src/StandardEnvelope --configuration Release --output artifacts
```

Packages are built locally. Nothing has been published to NuGet.

## Correspondence lifecycle

```text
Draft ── Seal() ──> Sealed ── Open() ──> Opened
                      │
                      └── ReturnToSender() ──> Returned
```

Addressing, confidentiality and approval requirements are configured in Draft. A recipient is mandatory. Approval can be recorded after sealing. Opening checks the exact recipient string, required clearance and approval; failed checks preserve the sealed envelope. A successful opening delivers once. Returned correspondence cannot be opened.

Recipient names and clearance are caller-supplied workflow metadata. This in-process library does not authenticate users or encrypt values. Snapshot means capturing a `T`, with normal C# reference semantics; objects are not deep-cloned. Instances are not thread-safe.

## Optional: Intent-Aware Correspondence (via BorowAmpersand)

Some envelopes contain a value. Others contain what the sender meant.

`StandardEnvelope.Borow` integrates with the real [BorowAmpersand runtime](https://github.com/jeffry-stroustrup-ei/borow-ampersand), pinned as a Git submodule. It uses `BorowRef<T>` and exposes the sender's concept metadata. The core library has no dependency on Borow.

To enable this extension, install the pinned BorowAmpersand dependency from the repository root:

```sh
git submodule update --init --recursive
dotnet build StandardEnvelope.sln
dotnet run --project demo/StandardEnvelope.Demo
dotnet run --project tests/StandardEnvelope.Tests
```

The demo and integration tests use this extension. The test project is a dependency-free executable: use `dotnet run`, not `dotnet test`.

```csharp
using BorowAmpersand.Runtime;
using StandardEnvelope.Borow;

int salary = 300_000;
var reference = Borow.Ampersand(ref salary);

var envelope = BorowEnvelope.WithIntent(reference, intendedValue: 300_000);
envelope.AddressTo("HR");
envelope.Seal();

reference.Value = 150_000;
envelope.Open("HR");
// ENV409: Delivered value conflicts with sender's declared intent.
// Value: 150000. Intended value: 300000. Resolution: contact HR.
```

| Factory | Delivered value | Correspondence policy |
| --- | --- | --- |
| `BorowEnvelope.Snapshot(reference)` | Value captured at creation | Subsequent Borow mutations do not replace the captured value. |
| `BorowEnvelope.Live(reference)` | `reference.Value` at opening | Sealing does not prevent the sender from changing their mind. |
| `BorowEnvelope.WithIntent(reference, intendedValue)` | Current value, if equal to declared intent | Disagreement raises `ENV409`; the envelope stays sealed. |

Intent is explicitly provided and compared using `EqualityComparer<T>.Default`. It is not inferred from source code or `FutureUsage`. Correct the reference through `reference.Value` and retry, or return the envelope.

Borow's concept store synchronizes values when reading `BorowRef<T>.Value`. Mutate through that property; direct assignment to the original variable can be overwritten by the upstream runtime. The demo demonstrates the supported mutation path.

`BorowEnvelope<T>` is a `ref struct`, just like `BorowRef<T>`: keep it in synchronous local scope. It cannot be boxed or stored in a class. Its configuration methods return `void`; use separate statements. Copies share the delivery state, so copying an envelope does not authorize a second delivery.

The example uses the runtime API directly. The custom `&borow` syntax belongs to the upstream transpiler and is not enabled by this project.

## Diagnostics

| Code | Explanation |
| --- | --- |
| `ENV400` | An envelope must be addressed before it can be sealed. |
| `ENV403` | Your position does not permit opening this envelope. |
| `ENV409` | Delivered value conflicts with sender's declared intent. Contact HR. |
| `ENV410` | The envelope is not available for delivery in its current state. |
| `ENV423` | Correspondence procedures cannot be amended after sealing. |
| `ENV428` | Management has not yet approved the existence of this value. |

## Frequently asked questions

**Why not just return the value?**  
Direct value transfer bypasses established correspondence procedures.

**Can an envelope contain another envelope?**  
Yes. This is the recommended architecture when two departments are involved.

**Is it thread-safe?**  
No. Please arrange correspondence within one thread. A registry office is not included.

**Does sealing preserve the sender's original intention?**  
Please specify whether this is a technical or an HR question.

**Is the standard complete?**  
The working group has approved the envelope containing the draft standard.

## Acknowledgements

Special thanks to the **Standard Envelope Working Group** for approving the procedure for approving envelope procedures.

The committee has not reviewed the contents. Opening the envelope would require a separate committee.

## Specification and provenance

See [SME-0001](docs/SME-0001.md) for the correspondence standard.

The optional dependency remains in its own repository with its own provenance. No license grant for upstream BorowAmpersand code is implied by this project.

Original project code is available under the [MIT license](LICENSE).
