using System.Text;
using GH.VpnService.Contracts.Vpn;
using GH.VpnService.Infrastructure.Persistence;
using GH.VpnService.Infrastructure.WireGuard;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GH.VpnService.Web.Controllers;

[ApiController]
[Route("api/vpn")]
public sealed class VpnController(VpnDbContext db, IWireGuardPeerManager peerManager) : ControllerBase
{

    [HttpGet("bootstrap")]
    public async Task<ActionResult<VpnBootstrapResponse>> Bootstrap(CancellationToken cancellationToken)
    {
        var users = await db.Users
            .OrderBy(x => x.Login)
            .Select(x => new VpnUserListItem
            {
                Id = x.Id,
                Login = x.Login,
                Email = x.Email,
                IsActive = x.IsActive
            })
            .ToListAsync(cancellationToken);

        var servers = await db.VpnServers
            .OrderBy(x => x.Country)
            .ThenBy(x => x.Name)
            .Select(x => new VpnServerListItem
            {
                Id = x.Id,
                Name = x.Name,
                Host = x.Host,
                Country = x.Country,
                EndpointPort = x.EndpointPort,
                IsOnline = x.IsOnline
            })
            .ToListAsync(cancellationToken);

        return Ok(new VpnBootstrapResponse
        {
            Users = users,
            Servers = servers
        });
    }

    [HttpGet("clients")]
    public async Task<ActionResult<IReadOnlyList<VpnClientListItem>>> GetClients(CancellationToken cancellationToken)
    {
        var clients = await db.VpnClients
            .Include(x => x.User)
            .Include(x => x.Server)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new VpnClientListItem
            {
                Id = x.Id,
                Name = x.Name,
                UserLogin = x.User == null ? string.Empty : x.User.Login,
                ServerName = x.Server == null ? string.Empty : x.Server.Name,
                AssignedIp = x.AssignedIp,
                ExpireAt = x.ExpireAt,
                IsEnabled = x.IsEnabled,
                RxBytes = x.RxBytes,
                TxBytes = x.TxBytes
            })
            .ToListAsync(cancellationToken);

        return Ok(clients);
    }

    [HttpPost("create")]
    public async Task<ActionResult<VpnClientResponse>> Create([FromBody] CreateVpnRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var client = await peerManager.CreateClientAsync(
                request.UserId,
                request.ServerId,
                request.Name,
                request.Days,
                cancellationToken);

            var config = await peerManager.BuildClientConfigAsync(client.Id, cancellationToken) ?? string.Empty;

            return Ok(new VpnClientResponse
            {
                ClientId = client.Id,
                UserId = client.UserId,
                ServerId = client.ServerId,
                Name = client.Name,
                AssignedIp = client.AssignedIp,
                ExpireAt = client.ExpireAt,
                IsEnabled = client.IsEnabled,
                Config = config
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("config/{clientId:guid}")]
    public async Task<IActionResult> GetConfig(Guid clientId, CancellationToken cancellationToken)
    {
        var config = await peerManager.BuildClientConfigAsync(clientId, cancellationToken);
        if (config is null)
        {
            return NotFound();
        }

        return Content(config, "text/plain", Encoding.UTF8);
    }

    [HttpGet("config/{clientId:guid}/download")]
    public async Task<IActionResult> DownloadConfig(Guid clientId, CancellationToken cancellationToken)
    {
        var config = await peerManager.BuildClientConfigAsync(clientId, cancellationToken);
        if (config is null)
        {
            return NotFound();
        }

        var bytes = Encoding.UTF8.GetBytes(config);
        return File(bytes, "application/octet-stream", $"wg-{clientId:N}.conf");
    }

    [HttpGet("config/{clientId:guid}/qr-text")]
    public async Task<IActionResult> GetQrText(Guid clientId, CancellationToken cancellationToken)
    {
        var config = await peerManager.BuildClientConfigAsync(clientId, cancellationToken);
        if (config is null)
        {
            return NotFound();
        }

        // Пока возвращаем текст для QR. На следующем шаге подключим настоящий PNG/SVG QR-генератор.
        return Ok(new { text = config });
    }

    [HttpPost("disable/{clientId:guid}")]
    public async Task<IActionResult> Disable(Guid clientId, CancellationToken cancellationToken)
    {
        var ok = await peerManager.DisableClientAsync(clientId, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{clientId:guid}")]
    public async Task<IActionResult> Delete(Guid clientId, CancellationToken cancellationToken)
    {
        var ok = await peerManager.DeleteClientAsync(clientId, cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}
