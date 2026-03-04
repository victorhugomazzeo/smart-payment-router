using Aspire.Hosting.Yarp.Transforms;
using SmartPaymentRouter.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
	.WithDataVolume();

var routerDb = postgres.AddDatabase("routerdb");

var mongo = builder.AddMongoDB("mongodb")
	.WithDataVolume();

var transactionsDb = mongo.AddDatabase("transactionsdb");

// Projetos
var router = builder.AddProject<Projects.SmartPaymentRouter_Router_Api>(ServiceNames.Router)
	.WithReference(routerDb);

var transactions = builder.AddProject<Projects.SmartPaymentRouter_Transactions_Api>(ServiceNames.Transactions)
	.WithReference(transactionsDb);

// Gateway YARP
builder.AddYarp(ServiceNames.Gateway)
	.WithHostPort(8080)
	.WithConfiguration(yarp =>
	{
		yarp.AddRoute("/router/{**catch-all}", router)
			.WithTransformPathRemovePrefix("/router");

		yarp.AddRoute("/transactions/{**catch-all}", transactions)
			.WithTransformPathRemovePrefix("/transactions");
	});

builder.Build().Run();