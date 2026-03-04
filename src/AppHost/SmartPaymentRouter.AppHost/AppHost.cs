using Aspire.Hosting.Yarp.Transforms;
using SmartPaymentRouter.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var router = builder.AddProject<Projects.SmartPaymentRouter_Router_Api>(ServiceNames.Router);
var transactions = builder.AddProject<Projects.SmartPaymentRouter_Transactions_Api>(ServiceNames.Transactions);

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