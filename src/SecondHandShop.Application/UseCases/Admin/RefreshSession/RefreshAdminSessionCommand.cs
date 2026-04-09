using MediatR;
using SecondHandShop.Application.Contracts.Admin;

namespace SecondHandShop.Application.UseCases.Admin.RefreshSession;

public sealed record RefreshAdminSessionCommand(Guid AdminUserId) : IRequest<LoginAdminResponse>;
