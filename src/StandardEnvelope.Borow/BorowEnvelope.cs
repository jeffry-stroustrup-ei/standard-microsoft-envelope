using BorowAmpersand.Runtime;

namespace StandardEnvelope.Borow;

public enum CorrespondenceMode { Snapshot, LiveReference, OriginalIntent }

public static class BorowEnvelope
{
    public static BorowEnvelope<T> Snapshot<T>(BorowRef<T> reference)
        => new(reference, CorrespondenceMode.Snapshot, reference.Value);

    public static BorowEnvelope<T> Live<T>(BorowRef<T> reference)
        => new(reference, CorrespondenceMode.LiveReference, reference.Value);

    public static BorowEnvelope<T> WithIntent<T>(BorowRef<T> reference, T intendedValue)
        => new(reference, CorrespondenceMode.OriginalIntent, intendedValue);
}

/// <summary>Stack-bound correspondence. Intent is declared, never inferred.</summary>
public ref struct BorowEnvelope<T>
{
    private BorowRef<T> _reference;
    private readonly T _capturedValue;
    private readonly Envelope<byte> _routing;
    private readonly CorrespondenceMode _mode;

    internal BorowEnvelope(BorowRef<T> reference, CorrespondenceMode mode, T capturedValue)
    {
        _reference = reference;
        _mode = mode;
        _capturedValue = capturedValue;
        _routing = Envelope.Create((byte)0);
    }

    public CorrespondenceMode Mode => _mode;
    public EnvelopeState State => _routing.State;
    public BorowMeta SenderMetadata => _reference.Meta;

    // Void mutators avoid accidentally chaining copies of a ref struct.
    public void AddressTo(string recipient) => _routing.AddressTo(recipient);
    public void MarkAsConfidential(Clearance required = Clearance.SeniorDeveloper)
        => _routing.MarkAsConfidential(required);
    public void RequireApproval() => _routing.RequireApproval();
    public void Approve(string approvedBy) => _routing.Approve(approvedBy);
    public void Seal() => _routing.Seal();
    public void ReturnToSender() => _routing.ReturnToSender();

    public T Open(string recipient, Clearance clearance = Clearance.Developer)
    {
        _routing.ValidateOpening(recipient, clearance);
        T value = _mode == CorrespondenceMode.Snapshot ? _capturedValue : _reference.Value;
        if (_mode == CorrespondenceMode.OriginalIntent &&
            !EqualityComparer<T>.Default.Equals(value, _capturedValue))
            throw new EnvelopeException("ENV409",
                $"Delivered value conflicts with sender's declared intent. Value: {value}. Intended value: {_capturedValue}. Resolution: contact HR.");
        _routing.Open(recipient, clearance);
        return value;
    }
}
