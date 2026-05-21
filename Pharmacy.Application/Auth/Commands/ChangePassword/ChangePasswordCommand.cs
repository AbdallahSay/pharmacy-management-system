using MediatR;

namespace Pharmacy.Application.Auth.Commands.ChangePassword;

public sealed record ChangePasswordCommand(
    int UserId, 
    string CurrentPassword, 
    string NewPassword) : IRequest<bool>;
