using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.Messaging;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Abstractions.Security;
using SecondHandShop.Application.Contracts.Admin;
using SecondHandShop.Application.UseCases.Admin.Login;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Application.UnitTests.UseCases.Admin;

public class LoginAdminCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_ShouldIncrementFailedLoginCount_WhenPasswordIsInvalid()
    {
        var user = AdminUser.CreateWithCredentials("lord", "Lord", "hashed-password");
        var repository = new Mock<IAdminUserRepository>();
        repository
            .Setup(x => x.GetByUserNameAsync("lord", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher
            .Setup(x => x.Verify("hashed-password", "wrong-password"))
            .Returns(false);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new LoginAdminCommandHandler(
            new StubClock(UtcNow),
            Mock.Of<IAdminLoginNotificationQueue>(),
            repository.Object,
            passwordHasher.Object,
            Mock.Of<IJwtTokenService>(),
            unitOfWork.Object,
            NullLogger<LoginAdminCommandHandler>.Instance);

        var act = () => sut.Handle(new LoginAdminCommand("lord", "wrong-password", "127.0.0.1"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials");
        user.FailedLoginCount.Should().Be(1);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnToken_AndQueueNotification_WhenCredentialsAreValid()
    {
        var user = AdminUser.CreateWithCredentials("lord", "Lord", "hashed-password", mustChangePassword: true);
        var repository = new Mock<IAdminUserRepository>();
        repository
            .Setup(x => x.GetByUserNameAsync("lord", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher
            .Setup(x => x.Verify("hashed-password", "correct-password"))
            .Returns(true);

        var jwtTokenService = new Mock<IJwtTokenService>();
        jwtTokenService
            .Setup(x => x.CreateToken(user))
            .Returns(("jwt-token", new DateTimeOffset(UtcNow.AddMinutes(20))));

        var notificationQueue = new Mock<IAdminLoginNotificationQueue>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new LoginAdminCommandHandler(
            new StubClock(UtcNow),
            notificationQueue.Object,
            repository.Object,
            passwordHasher.Object,
            jwtTokenService.Object,
            unitOfWork.Object,
            NullLogger<LoginAdminCommandHandler>.Instance);

        var result = await sut.Handle(
            new LoginAdminCommand("lord", "correct-password", "127.0.0.1"),
            CancellationToken.None);

        result.Should().BeEquivalentTo(new LoginAdminResponse("jwt-token", new DateTimeOffset(UtcNow.AddMinutes(20)), true));
        user.LastSuccessfulLoginAtUtc.Should().Be(UtcNow);
        user.LastSuccessfulLoginIp.Should().Be("127.0.0.1");
        notificationQueue.Verify(x => x.Enqueue(It.IsAny<AdminLoginNotificationMessage>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldExtendLockout_WhenLockedUserKeepsFailing()
    {
        var user = AdminUser.CreateWithCredentials("lord", "Lord", "hashed-password");
        user.RegisterFailedLogin(5, TimeSpan.FromMinutes(15), UtcNow.AddMinutes(-1));
        user.RegisterFailedLogin(5, TimeSpan.FromMinutes(15), UtcNow.AddMinutes(-1));
        user.RegisterFailedLogin(5, TimeSpan.FromMinutes(15), UtcNow.AddMinutes(-1));
        user.RegisterFailedLogin(5, TimeSpan.FromMinutes(15), UtcNow.AddMinutes(-1));
        user.RegisterFailedLogin(5, TimeSpan.FromMinutes(15), UtcNow.AddMinutes(-1));

        var repository = new Mock<IAdminUserRepository>();
        repository
            .Setup(x => x.GetByUserNameAsync("lord", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher
            .Setup(x => x.Verify("hashed-password", "wrong-password"))
            .Returns(false);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new LoginAdminCommandHandler(
            new StubClock(UtcNow),
            Mock.Of<IAdminLoginNotificationQueue>(),
            repository.Object,
            passwordHasher.Object,
            Mock.Of<IJwtTokenService>(),
            unitOfWork.Object,
            NullLogger<LoginAdminCommandHandler>.Instance);

        var act = () => sut.Handle(new LoginAdminCommand("lord", "wrong-password", "127.0.0.1"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        user.LockedUntilUtc.Should().Be(UtcNow.AddMinutes(15));
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class StubClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
