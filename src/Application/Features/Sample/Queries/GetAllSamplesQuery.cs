using Domain.Entities;
using Domain.Interfaces;
using MediatR;

namespace Application.Features.Sample.Queries;

public record GetAllSamplesQuery : IRequest<IReadOnlyList<SampleEntity>>;

public class GetAllSamplesHandler(IRepository<SampleEntity> repository)
    : IRequestHandler<GetAllSamplesQuery, IReadOnlyList<SampleEntity>>
{
    public async Task<IReadOnlyList<SampleEntity>> Handle(
        GetAllSamplesQuery request,
        CancellationToken cancellationToken)
    {
        return await repository.GetAllAsync(cancellationToken);
    }
}
