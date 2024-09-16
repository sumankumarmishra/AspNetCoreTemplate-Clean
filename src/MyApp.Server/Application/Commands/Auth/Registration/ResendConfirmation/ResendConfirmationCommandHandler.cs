﻿using MediatR;
using Microsoft.EntityFrameworkCore;
using MyApp.Server.Domain.Auth.UserConfirmation;
using MyApp.Server.Domain.Auth.UserConfirmation.Failures;
using MyApp.Server.Infrastructure.Database;
using MyApp.Server.Infrastructure.Messaging;

namespace MyApp.Server.Application.Commands.Auth.Registration.ResendConfirmation;

public record ResendConfirmationRequest(string Email) : IRequest;

public class ResendConfirmationCommandHandler(IScopedDbContext dbContext, IMessageProducer messageProducer) : IRequestHandler<ResendConfirmationRequest>
{
    public async Task Handle(ResendConfirmationRequest request, CancellationToken cancellationToken)
    {
        var data = await dbContext.Set<UserConfirmationEntity>()
            .IgnoreQueryFilters()
            .Where(uc => uc.User.Email == request.Email && uc.User.IsEmailConfirmed == false)
            .Select(uc => new
            {
                UserConfirmation = uc,
                uc.User.Username,
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw ResendConfirmationInvalidFailure.Exception();

        dbContext.Remove(data.UserConfirmation);
        var newConfirmation = UserConfirmationEntity.Create(data.UserConfirmation.UserId);
        dbContext.Add(newConfirmation);

        await dbContext.WrapInTransaction(async () =>
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await messageProducer.Send(new SendUserConfirmationMessage(data.Username, request.Email, newConfirmation.Code), cancellationToken);
        }, cancellationToken);
    }
}
