using Microsoft.EntityFrameworkCore;

namespace SmartPaymentRouter.Router.Api
{
	public sealed class RouterDbContext : DbContext
	{
		public RouterDbContext(DbContextOptions<RouterDbContext> options) : base(options)
		{
		}

		public DbSet<RouteConfig> RouteConfigs => Set<RouteConfig>();
	}

	public sealed class RouteConfig
	{
		public int Id { get; set; }
		public string Name { get; set; } = default!;
		public string Target { get; set; } = default!;
	}
}