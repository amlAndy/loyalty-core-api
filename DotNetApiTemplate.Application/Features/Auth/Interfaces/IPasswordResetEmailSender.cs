using DotNetApiTemplate.Application.Features.Auth.Dtos;

namespace DotNetApiTemplate.Application.Features.Auth.Interfaces;

public interface IPasswordResetEmailSender
{
    Task SendAsync(PasswordResetEmailModel model, CancellationToken cancellationToken = default);
}
