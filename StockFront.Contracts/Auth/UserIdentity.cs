namespace StockFront.Contracts.Auth;

/// <summary>
/// The safe, public view of a signed-in account — everything a ViewModel may know about
/// the current user. Deliberately carries <b>no</b> password hash or other secrets.
/// </summary>
public sealed record UserIdentity(int Id, string Username, string DisplayName, UserRole Role);
