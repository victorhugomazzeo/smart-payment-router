using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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

var mongoCs = builder.Configuration.GetConnectionString("transactionsdb")
			  ?? throw new InvalidOperationException("Connection string 'transactionsdb' não encontrada.");

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoCs));

builder.Services.AddSingleton(sp =>
{
	var client = sp.GetRequiredService<IMongoClient>();
	return client.GetDatabase("transactionsdb");
});

builder.Services.AddOpenTelemetry()
	.ConfigureResource(r => r.AddService("transactions"))
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

app.MapGet("/dbcheck", async ([FromServices] IMongoDatabase db) =>
{
	var names = await (await db.ListCollectionNamesAsync()).ToListAsync();
	return Results.Ok(new { ok = true, collections = names });
});

app.Run();