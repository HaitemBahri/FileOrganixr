using System;

namespace FileOrganixr.Core.Runtime.ActionRequests;
public sealed record ActionStatusEntry(
    ActionRequestStatus Status,
    DateTimeOffset TimestampUtc,
    string? Note
);
