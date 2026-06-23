using Domain.Entities;
using Domain.Interfaces;
using MediatR;

namespace Application.Features.Sample.Queries;

public record GetSampleByIdQuery(int Id) : IRequest<SampleEntity?>;

public class GetSampleByIdHandler(IRepository<SampleEntity> repository)
    : IRequestHandler<GetSampleByIdQuery, SampleEntity?>
{
    public async Task<SampleEntity?> Handle(
        GetSampleByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await repository.GetByIdAsync(request.Id, cancellationToken);
    }
}
