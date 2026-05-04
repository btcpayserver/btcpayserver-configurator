using BTCPayServerDockerConfigurator.Models;
using Microsoft.AspNetCore.Mvc;

namespace BTCPayServerDockerConfigurator.Controllers;

[Route("api/tunnel/{secret}")]
[ApiController]
public class TunnelController : ControllerBase
{
    private readonly TunnelService _tunnelService;

    public TunnelController(TunnelService tunnelService)
    {
        _tunnelService = tunnelService;
    }

    [HttpGet("agent")]
    public IActionResult GetAgentScript(string secret)
    {
        var session = _tunnelService.GetSession(secret);
        if (session == null)
            return NotFound("Invalid or expired session");

        var baseUrl =
            $"{Request.Scheme}://{Request.Host}{Request.PathBase}/api/tunnel/{secret}";
        var script = $$"""
            #!/bin/bash
            set -e
            BASE_URL="{{baseUrl}}"

            cleanup() { curl -sS -X POST "$BASE_URL/disconnect" 2>/dev/null || true; }
            trap cleanup EXIT INT TERM

            echo "Connected to BTCPay Server Configurator."
            echo "Waiting for commands... (press Ctrl+C to disconnect)"
            echo ""

            while true; do
                resp=$(curl -sS --max-time 30 "$BASE_URL/poll" 2>/dev/null) || { sleep 2; continue; }

                if [ "$resp" = "##WAIT##" ]; then
                    continue
                fi
                if [ "$resp" = "##END##" ]; then
                    echo "Session complete."
                    break
                fi

                output=$(bash -c "$resp" 2>&1) || true
                exitcode=$?
                curl -sS -X POST "$BASE_URL/result" \
                    -H "Content-Type: application/x-www-form-urlencoded" \
                    --data-urlencode "output=$output" \
                    -d "exit=$exitcode" 2>/dev/null
            done
            """;

        return Content(script, "text/plain");
    }

    [HttpGet("poll")]
    public async Task<IActionResult> Poll(string secret)
    {
        var session = _tunnelService.GetSession(secret);
        if (session == null)
            return Ok("##END##");

        if (session.State is TunnelState.Done or TunnelState.Expired)
            return Ok("##END##");

        var command = await session.GetNextCommand(HttpContext.RequestAborted);
        return Ok(command ?? "##WAIT##");
    }

    [HttpPost("result")]
    public async Task<IActionResult> Result(string secret, [FromForm] string output,
        [FromForm] int exit)
    {
        var session = _tunnelService.GetSession(secret);
        if (session == null)
            return NotFound();

        await session.SetResult(output ?? "", exit);
        return Ok();
    }

    [HttpPost("disconnect")]
    public IActionResult Disconnect(string secret)
    {
        var session = _tunnelService.GetSession(secret);
        if (session == null)
            return NotFound();

        session.OnDisconnect();
        return Ok();
    }

    [HttpGet("status")]
    public IActionResult Status(string secret)
    {
        var session = _tunnelService.GetSession(secret);
        if (session == null)
            return Ok(new { state = "expired" });

        return Ok(new
        {
            state = session.State.ToString().ToLowerInvariant(),
            error = session.ErrorMessage
        });
    }
}
