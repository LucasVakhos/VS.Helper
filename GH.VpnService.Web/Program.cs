using GH.VpnService.Infrastructure.Persistence;
using GH.VpnService.Infrastructure.WireGuard;
using GH.VpnService.Web.Components;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<VpnDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("VpnDb");

    options.UseSqlite(connectionString);
});

// API
builder.Services.AddControllers();
builder.Services.AddScoped<IWireGuardKeyGenerator, WireGuardKeyGenerator>();
builder.Services.AddScoped<IWireGuardPeerManager, WireGuardPeerManager>();

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// HTTP pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapControllers();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VpnDbContext>();

    await db.Database.MigrateAsync();
    await VpnDbSeeder.SeedAsync(db);
}

app.Run();