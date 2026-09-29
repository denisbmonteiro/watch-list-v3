using WatchList.Shared.Guards;

namespace WatchList.Shared.Results;

public sealed record Error(string Code, string Message)
{
    public string Code { get; } = Guard.NotNullOrWhiteSpace(Code);

    public string Message { get; } = Guard.NotNullOrWhiteSpace(Message);

    public override string ToString() => $"{Code}: {Message}";
}
