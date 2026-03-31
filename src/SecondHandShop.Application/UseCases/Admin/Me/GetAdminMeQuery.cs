using MediatR;
using SecondHandShop.Application.Contracts.Admin;

namespace SecondHandShop.Application.UseCases.Admin.Me;

public sealed record GetAdminMeQuery(Guid AdminUserId) : IRequest<AdminMeResponse?>;
