namespace SingleInstance.Services;

using System.Collections.Immutable;

public interface ISingleInstanceService
{
    IObservable<ImmutableArray<string>> Activated { get; }

    void OnActivated(string? data);
}