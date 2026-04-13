using MediatR;
using SecondHandShop.Application.Contracts.Admin;

namespace SecondHandShop.Application.UseCases.Admin.Login;

public sealed record LoginAdminCommand(string UserName, string Password, string? SourceIpAddress)
    : IRequest<LoginAdminResponse>;
