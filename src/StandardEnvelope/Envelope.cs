namespace StandardEnvelope;

public enum EnvelopeState { Draft, Sealed, Opened, Returned }
public enum Clearance { Developer, SeniorDeveloper, PrincipalEnvelopeArchitect }

public sealed class EnvelopeException(string code, string message)
    : InvalidOperationException($"{code}: {message}")
{
    public string Code { get; } = code;
}

public static class Envelope
{
    public static Envelope<T> Create<T>(T value) => new(value);
}

/// <summary>A single-delivery, in-process envelope. Not synchronized.</summary>
public sealed class Envelope<T>
{
    private readonly T _value;
    private string? _recipient;
    private Clearance _requiredClearance;
    private bool _requiresApproval;
    private bool _approved;

    internal Envelope(T value) => _value = value;

    public EnvelopeState State { get; private set; }
    public string? Recipient => _recipient;

    public Envelope<T> AddressTo(string recipient)
    {
        EnsureDraft();
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient);
        _recipient = recipient;
        return this;
    }

    public Envelope<T> MarkAsConfidential(Clearance required = Clearance.SeniorDeveloper)
    {
        EnsureDraft();
        ValidateClearance(required);
        _requiredClearance = required;
        return this;
    }

    public Envelope<T> RequireApproval()
    {
        EnsureDraft();
        _requiresApproval = true;
        return this;
    }

    public Envelope<T> Seal()
    {
        EnsureDraft();
        if (_recipient is null)
            throw new EnvelopeException("ENV400", "An envelope must be addressed before it can be sealed.");
        State = EnvelopeState.Sealed;
        return this;
    }

    public Envelope<T> Approve(string approvedBy)
    {
        EnsureSealed();
        ArgumentException.ThrowIfNullOrWhiteSpace(approvedBy);
        _approved = true;
        ApprovedBy = approvedBy;
        return this;
    }

    public string? ApprovedBy { get; private set; }

    public T Open(string recipient, Clearance clearance = Clearance.Developer)
    {
        ValidateOpening(recipient, clearance);
        State = EnvelopeState.Opened;
        return _value;
    }

    public void ValidateOpening(string recipient, Clearance clearance = Clearance.Developer)
    {
        EnsureSealed();
        ValidateClearance(clearance);
        if (!string.Equals(recipient, _recipient, StringComparison.Ordinal) || clearance < _requiredClearance)
            throw new EnvelopeException("ENV403", "Your position does not permit opening this envelope.");
        if (_requiresApproval && !_approved)
            throw new EnvelopeException("ENV428", "Management has not yet approved the existence of this value.");
    }

    public void ReturnToSender()
    {
        EnsureSealed();
        State = EnvelopeState.Returned;
    }

    private void EnsureDraft()
    {
        if (State != EnvelopeState.Draft)
            throw new EnvelopeException("ENV423", "Correspondence procedures cannot be amended after sealing.");
    }

    private void EnsureSealed()
    {
        if (State != EnvelopeState.Sealed)
            throw new EnvelopeException("ENV410", $"Expected a sealed envelope; received {State}.");
    }

    private static void ValidateClearance(Clearance clearance)
    {
        if (!Enum.IsDefined(clearance)) throw new ArgumentOutOfRangeException(nameof(clearance));
    }
}
