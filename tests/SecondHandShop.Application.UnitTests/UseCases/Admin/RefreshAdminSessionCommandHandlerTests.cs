using FluentAssertions;
using Moq;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Abstractions.Security;
using SecondHandShop.Application.Contracts.Admin;
using SecondHandShop.Application.UseCases.Admin.RefreshSession;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Application.UnitTests.UseCases.Admin;

public class RefreshAdminSessionCommandHandlerTests
{
    private static readonly DateTimeOffset ExpiresAt = new(new DateTime(2026, 4, 16, 2, 0, 0, DateTimeKind.Utc));

    [Fact]
    public async Task Handle_ShouldReturnFreshToken_WhenAdminIsActive()
    {
        var user = AdminUser.CreateWithCredentials("lord", "Lord", "hashed-password", mustChangePassword: true);

        var repository = new Mock<IAdminUserRepository>();
        repository
            .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var jwtTokenService = new Mock<IJwtTokenService>();
        jwtTokenService
            .Setup(x => x.CreateToken(user))
            .Returns(("fresh-token", ExpiresAt));

        var sut = new RefreshAdminSessionCommandHandler(repository.Object, jwtTokenService.Object);

        var result = await sut.Handle(new RefreshAdminSessionCommand(user.Id), CancellationToken.None);

        result.Should().BeEquivalentTo(new LoginAdminResponse("fresh-token", ExpiresAt, true));
        jwtTokenService.Verify(x => x.CreateToken(user), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRejectInactiveAdmin()
    {
        var user = AdminUser.CreateWithCredentials("lord", "Lord", "hashed-password");
        user.SetActive(false);

        var repository = new Mock<IAdminUserRepository>();
        repository
            .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var jwtTokenService = new Mock<IJwtTokenService>();
        var sut = new RefreshAdminSessionCommandHandler(repository.Object, jwtTokenService.Object);

        var act = () => sut.Handle(new RefreshAdminSessionCommand(user.Id), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        jwtTokenService.Verify(x => x.CreateToken(It.IsAny<AdminUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRejectUnknownAdmin()
    {
        var repository = new Mock<IAdminUserRepository>();
        repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminUser?)null);

        var jwtTokenService = new Mock<IJwtTokenService>();
        var sut = new RefreshAdminSessionCommandHandler(repository.Object, jwtTokenService.Object);

        var act = () => sut.Handle(new RefreshAdminSessionCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        jwtTokenService.Verify(x => x.CreateToken(It.IsAny<AdminUser>()), Times.Never);
    }
}
