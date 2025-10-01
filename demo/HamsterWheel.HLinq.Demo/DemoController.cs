using HamsterWheel.HLinq.Appliers;
using HamsterWheel.HLinq.Request;
using Microsoft.AspNetCore.Mvc;

namespace HamsterWheel.HLinq.Demo;

[Route("demo/controller")]
public class DemoController(IHLinqQueryApplier applier) : ControllerBase
{
    [HttpGet("memory")]
    public IActionResult QueryMemory(HLinqQuery<Superhero> query, CancellationToken cancellationToken) =>
        Ok(applier.Apply(Superhero.Superheroes.AsQueryable(), query, cancellationToken));
}