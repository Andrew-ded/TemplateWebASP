//#if (useMediatr)
using Application.Features.Sample.Queries;
using MediatR;
//#else
using Domain.Entities;
using Domain.Interfaces;
//#endif
//#if (useJwt)
using Microsoft.AspNetCore.Authorization;
//#endif
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[ApiController]
//#if (useApiVersioning)
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
//#endif
[Route("api/[controller]")]
//#if (useJwt)
[Authorize]
//#endif
//#if (useMediatr)
public class SampleController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var items = await mediator.Send(new GetAllSamplesQuery(), cancellationToken);
        return Ok(items);
    }
}
//#else
public class SampleController(IRepository<SampleEntity> repository) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var items = await repository.GetAllAsync(cancellationToken);
        return Ok(items);
    }
}
//#endif
