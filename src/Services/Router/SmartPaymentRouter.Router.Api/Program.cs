using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SmartPaymentRouter.Router.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.Configure(o =>
{
	o.ActivityTrackingOptions =
		ActivityTrackingOptions.TraceId |
		ActivityTrackingOptions.SpanId |
		ActivityTrackingOptions.ParentId |
		ActivityTrackingOptions.Baggage;
});

builder.Services.AddHealthChecks();

var cs = builder.Configuration.GetConnectionString("routerdb")
		 ?? throw new InvalidOperationException("Connection string 'routerdb' não encontrada.");

builder.Services.AddDbContext<RouterDbContext>(opt => opt.UseNpgsql(cs));

builder.Services.AddOpenTelemetry()
	.ConfigureResource(r => r.AddService("router"))
	.WithTracing(t =>
	{
		t.AddAspNetCoreInstrumentation()
		 .AddHttpClientInstrumentation()
		 .AddOtlpExporter();
	})
	.WithMetrics(m =>
	{
		m.AddAspNetCoreInstrumentation()
		 .AddHttpClientInstrumentation()
		 .AddRuntimeInstrumentation()
		 .AddOtlpExporter();
	});

var app = builder.Build();

app.MapHealthChecks("/health");

app.MapGet("/dbcheck", async (RouterDbContext db) =>
{
	await db.Database.EnsureCreatedAsync();
	var count = await db.RouteConfigs.CountAsync();
	return Results.Ok(new { ok = true, routeConfigs = count });
});

app.Run();