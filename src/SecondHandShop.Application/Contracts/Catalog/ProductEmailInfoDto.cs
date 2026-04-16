namespace SecondHandShop.Application.Contracts.Catalog;

/// <summary>
/// Lightweight projection used by email dispatch — carries only the fields
/// needed to compose an inquiry notification, avoiding full entity loading.
/// </summary>
public sealed record ProductEmailInfoDto(Guid Id, string Title, string Slug);
