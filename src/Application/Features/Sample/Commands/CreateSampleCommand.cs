using Domain.Entities;
using Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace Application.Features.Sample.Commands;

public record CreateSampleCommand(string Name, string? Description) : IRequest<SampleEntity>;

public class CreateSampleValidator : AbstractValidator<CreateSampleCommand>
{
    public CreateSampleValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");
    }
}

public class CreateSampleHandler(IRepository<SampleEntity> repository)
    : IRequestHandler<CreateSampleCommand, SampleEntity>
{
    public async Task<SampleEntity> Handle(
        CreateSampleCommand request,
        CancellationToken cancellationToken)
    {
        var entity = new SampleEntity
        {
            Name = request.Name,
            Description = request.Description
        };

        await repository.AddAsync(entity, cancellationToken);
        return entity;
    }
}
