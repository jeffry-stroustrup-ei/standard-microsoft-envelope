using BorowAmpersand.Runtime;
using StandardEnvelope;
using StandardEnvelope.Borow;

int passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception($"FAIL: {name}");
    Console.WriteLine($"PASS: {name}");
    passed++;
}
void Reject(Action operation, string code)
{
    try { operation(); }
    catch (EnvelopeException error) when (error.Code == code) { passed++; return; }
    throw new Exception($"Expected {code}");
}

Reject(() => Envelope.Create(42).Seal(), "ENV400");
Reject(() => Envelope.Create(42).Open("HR"), "ENV410");
var letter = Envelope.Create(42).AddressTo("HR").MarkAsConfidential().RequireApproval().Seal();
Reject(() => letter.Open("Payroll", Clearance.SeniorDeveloper), "ENV403");
Reject(() => letter.Open("HR"), "ENV403");
Reject(() => letter.Open("HR", Clearance.SeniorDeveloper), "ENV428");
Check(letter.State == EnvelopeState.Sealed, "failed opening preserves sealed state");
Reject(() => letter.AddressTo("Payroll"), "ENV423");
letter.Approve("Director");
Check(letter.Open("HR", Clearance.SeniorDeveloper) == 42, "approved delivery");
Reject(() => letter.Open("HR", Clearance.SeniorDeveloper), "ENV410");
var returned = Envelope.Create(42).AddressTo("HR").Seal();
returned.ReturnToSender();
Reject(() => returned.Open("HR"), "ENV410");
var inner = Envelope.Create("meaning").AddressTo("HR").Seal();
var outer = Envelope.Create(inner).AddressTo("HR").Seal();
Check(outer.Open("HR").Open("HR") == "meaning", "nested correspondence");
Check(Envelope.Create<string?>(null).AddressTo("HR").Seal().Open("HR") is null, "null correspondence");
try { Envelope.Create(42).MarkAsConfidential((Clearance)999); throw new Exception("Invalid clearance accepted"); }
catch (ArgumentOutOfRangeException) { passed++; }

int salary = 300_000;
var reference = Borow.Ampersand(ref salary, conceptId: "tests:salary");
var snapshot = BorowEnvelope.Snapshot(reference);
var live = BorowEnvelope.Live(reference);
var intended = BorowEnvelope.WithIntent(reference, 300_000);
snapshot.AddressTo("HR"); snapshot.Seal();
live.AddressTo("HR"); live.Seal();
intended.AddressTo("HR"); intended.Seal();
reference.Value = 150_000;
Check(snapshot.Open("HR") == 300_000, "snapshot preserves dispatch value");
Check(live.Open("HR") == 150_000, "live correspondence follows Borow mutation");
try { intended.Open("HR"); throw new Exception("Conflicting intent delivered"); }
catch (EnvelopeException error) when (error.Code == "ENV409") { passed++; }
Check(intended.State == EnvelopeState.Sealed, "intent conflict is recoverable");
reference.Value = 300_000;
Check(intended.Open("HR") == 300_000, "corrected intent delivers");
Check(intended.SenderMetadata.ConceptId == "tests:salary", "actual Borow metadata retained");
var copy = intended;
try { copy.Open("HR"); throw new Exception("Copy delivered twice"); }
catch (EnvelopeException error) when (error.Code == "ENV410") { passed++; }
Console.WriteLine($"{passed} correspondence checks passed. Management is satisfied.");
