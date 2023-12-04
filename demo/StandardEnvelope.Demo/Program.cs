using BorowAmpersand.Runtime;
using StandardEnvelope;
using StandardEnvelope.Borow;

Console.WriteLine("STANDARD MICROSOFT ENVELOPE / Correspondence demonstration");
var standard = Envelope.Create(42)
    .AddressTo("Calculation Department")
    .MarkAsConfidential()
    .RequireApproval()
    .Seal()
    .Approve("Management");
Console.WriteLine($"Approved answer: {standard.Open("Calculation Department", Clearance.SeniorDeveloper)}");

int salary = 300_000;
var reference = Borow.Ampersand(ref salary);
var snapshot = BorowEnvelope.Snapshot(reference);
var live = BorowEnvelope.Live(reference);
var intention = BorowEnvelope.WithIntent(reference, intendedValue: 300_000);
snapshot.AddressTo("HR"); snapshot.Seal();
live.AddressTo("HR"); live.Seal();
intention.AddressTo("HR"); intention.Seal();

// Mutate through Borow so its concept store and physical slot agree.
reference.Value = 150_000;
Console.WriteLine($"Snapshot: {snapshot.Open("HR")}");
Console.WriteLine($"Live reference: {live.Open("HR")}");
try { intention.Open("HR"); }
catch (EnvelopeException error) { Console.WriteLine(error.Message); }
Console.WriteLine($"Sender's concept: {reference.Meta.VariableName}");
Console.WriteLine("The value has been delivered. The meaning requires further discussion.");
