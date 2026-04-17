using FluentAssertions;
using MediatR;
using Moq;
using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Abstractions.Security;
using SecondHandShop.Application.UseCases.Admin.ChangeInitialPassword;
using SecondHandShop.Domain.Common;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Application.UnitTests.UseCases.Admin;

public class ChangeAdminInitialPasswordCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCompleteForcedPasswordChange_WhenRequestIsValid()
    {
        var user = AdminUser.CreateWithCredentials("lord", "Lord", "old-hash", mustChangePassword: true);
        var repository = new Mock<IAdminUserRepository>();
        repository
            .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher
            .Setup(x => x.Verify("old-hash", "oldpass1"))
            .Returns(true);
        passwordHasher
            .Setup(x => x.Hash("newpass1"))
            .Returns("new-hash");

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new ChangeAdminInitialPasswordCommandHandler(
            repository.Object,
            passwordHasher.Object,
            unitOfWork.Object);

        var result = await sut.Handle(
            new ChangeAdminInitialPasswordCommand(user.Id, "oldpass1", "newpass1", "newpass1"),
            CancellationToken.None);

        result.Should().Be(Unit.Value);
        user.MustChangePassword.Should().BeFalse();
        user.TokenVersion.Should().Be(1);
        user.PasswordHash.Should().Be("new-hash");
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenConfirmationDoesNotMatch()
    {
        var sut = new ChangeAdminInitialPasswordCommandHandler(
            Mock.Of<IAdminUserRepository>(),
            Mock.Of<IPasswordHasher>(),
            Mock.Of<IUnitOfWork>());

        var act = () => sut.Handle(
            new ChangeAdminInitialPasswordCommand(Guid.NewGuid(), "oldpass1", "newpass1", "different1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("New password and confirmation do not match.");
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorized_WhenCurrentPasswordIsWrong()
    {
        var user = AdminUser.CreateWithCredentials("lord", "Lord", "old-hash", mustChangePassword: true);
        var repository = new Mock<IAdminUserRepository>();
        repository
            .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher
            .Setup(x => x.Verify("old-hash", "wrongpass1"))
            .Returns(false);

        var sut = new ChangeAdminInitialPasswordCommandHandler(
            repository.Object,
            passwordHasher.Object,
            Mock.Of<IUnitOfWork>());

        var act = () => sut.Handle(
            new ChangeAdminInitialPasswordCommand(user.Id, "wrongpass1", "newpass1", "newpass1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainRuleViolation_WhenAccountDoesNotRequireForcedChange()
    {
        var user = AdminUser.CreateWithCredentials("lord", "Lord", "old-hash", mustChangePassword: false);
        var repository = new Mock<IAdminUserRepository>();
        repository
            .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher
            .Setup(x => x.Verify("old-hash", "oldpass1"))
            .Returns(true);

        var sut = new ChangeAdminInitialPasswordCommandHandler(
            repository.Object,
            passwordHasher.Object,
            Mock.Of<IUnitOfWork>());

        var act = () => sut.Handle(
            new ChangeAdminInitialPasswordCommand(user.Id, "oldpass1", "newpass1", "newpass1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainRuleViolationException>()
            .WithMessage("This operation is not available for your account.");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenNewPasswordMatchesCurrentPassword()
    {
        var user = AdminUser.CreateWithCredentials("lord", "Lord", "old-hash", mustChangePassword: true);
        var repository = new Mock<IAdminUserRepository>();
        repository
            .Setup(x => x.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher
            .Setup(x => x.Verify("old-hash", "samepass1"))
            .Returns(true);

        var sut = new ChangeAdminInitialPasswordCommandHandler(
            repository.Object,
            passwordHasher.Object,
            Mock.Of<IUnitOfWork>());

        var act = () => sut.Handle(
            new ChangeAdminInitialPasswordCommand(user.Id, "samepass1", "samepass1", "samepass1"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("New password must be different from the current password.");
    }
}
