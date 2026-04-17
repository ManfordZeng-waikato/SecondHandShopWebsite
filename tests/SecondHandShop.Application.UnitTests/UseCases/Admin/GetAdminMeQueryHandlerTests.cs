using FluentAssertions;
using Moq;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Contracts.Admin;
using SecondHandShop.Application.UseCases.Admin.Me;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Application.UnitTests.UseCases.Admin;

public class GetAdminMeQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnResponse_WhenAdminIsActive()
    {
        var user = AdminUser.CreateWithCredentials("lord", "Lord", "hash", mustChangePassword: true);
        var repository = new Mock<IAdminUserRepository>();
        repository
            .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = new GetAdminMeQueryHandler(repository.Object);

        var result = await sut.Handle(new GetAdminMeQuery(user.Id), CancellationToken.None);

        result.Should().BeEquivalentTo(new AdminMeResponse(
            true,
            user.Id,
            user.UserName,
            user.Email,
            user.Role,
            true));
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenAdminDoesNotExist()
    {
        var repository = new Mock<IAdminUserRepository>();
        repository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminUser?)null);

        var sut = new GetAdminMeQueryHandler(repository.Object);

        var result = await sut.Handle(new GetAdminMeQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenAdminIsInactive()
    {
        var user = AdminUser.CreateWithCredentials("lord", "Lord", "hash");
        user.SetActive(false);

        var repository = new Mock<IAdminUserRepository>();
        repository
            .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var sut = new GetAdminMeQueryHandler(repository.Object);

        var result = await sut.Handle(new GetAdminMeQuery(user.Id), CancellationToken.None);

        result.Should().BeNull();
    }
}
