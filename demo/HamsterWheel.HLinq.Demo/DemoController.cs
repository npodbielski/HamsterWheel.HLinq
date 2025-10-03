using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.Request;
using Microsoft.AspNetCore.Mvc;

namespace HamsterWheel.HLinq.Demo;

[Route("demo/controller")]
public class DemoController : ControllerBase
{
    [HttpGet("memory")]
    public IActionResult QueryMemory(HLinqQuery<Superhero> query, CancellationToken cancellationToken) =>
        Ok(query.ApplyTo(Superhero.Superheroes.AsQueryable(), cancellationToken));
}