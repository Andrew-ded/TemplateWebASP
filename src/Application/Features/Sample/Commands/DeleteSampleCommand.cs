using Domain.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Sample.Commands;

public record DeleteSampleCommand(int Id) : IRequest;

public class DeleteSampleHandler(IRepository<SampleEntity> repository)
    : IRequestHandler<DeleteSampleCommand>
{
    public async Task Handle(DeleteSampleCommand request, CancellationToken cancellationToken)
    {
        await repository.DeleteAsync(request.Id, cancellationToken);
    }
}
