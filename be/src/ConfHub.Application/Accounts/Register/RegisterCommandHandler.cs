using ConfHub.Application.Accounts.Specifications;
using ConfHub.Application.Common.Exceptions;
using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Domain.Accounts;
using MediatR;

namespace ConfHub.Application.Accounts.Register;

public sealed class RegisterCommandHandler(
    IRepository<User> userRepository,
    IReadRepository<Role> roleRepository,
    IPasswordHasher passwordHasher) : IRequestHandler<RegisterCommand, RegisterResponse>
{
    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var email = User.NormalizeEmail(request.Email);
        var existingUser = await userRepository.FirstOrDefaultAsync(
            new UserByEmailSpec(email),
            cancellationToken);
        if (existingUser is not null)
        {
            throw new ConflictException(AccountErrorCodes.EmailTaken, "Email is already in use.");
        }

        // Validator đã chặn mã vai trò lạ; 5 vai trò luôn có sẵn nhờ seed trong migration.
        var role = await roleRepository.FirstOrDefaultAsync(
            new RoleByCodeSpec(request.Role),
            cancellationToken) ?? throw new NotFoundException(nameof(Role), request.Role);

        // Tạo user mới
        var passwordHash = passwordHasher.Hash(request.Password);
        var user = User.Register(email, passwordHash, request.FullName, role, request.Organization);

        // Lưu User + message gửi email xác thực vào outbox trong cùng 1 transaction.
        await userRepository.AddAsync(user, cancellationToken);
        return new RegisterResponse(user.Email);
    }
}
