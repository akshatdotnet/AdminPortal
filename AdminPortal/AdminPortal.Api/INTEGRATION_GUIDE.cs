// ══════════════════════════════════════════════════════════════════════════════
// HOW TO WIRE UP AdminApiClient IN YOUR EXISTING AdminPortal.Web / Program.cs
// ══════════════════════════════════════════════════════════════════════════════
//
// 1. Add the AdminApiClient.cs file to AdminPortal.Infrastructure/ApiClient/
//
// 2. In AdminPortal.Web/Program.cs, add this BEFORE builder.Build():
//
//    builder.Services.AddHttpClient<AdminApiClient>(client =>
//    {
//        client.BaseAddress = new Uri(
//            builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7001");
//    });
//
// 3. Add to appsettings.json (AdminPortal.Web):
//    "ApiBaseUrl": "https://localhost:7001"
//
// 4. Inject AdminApiClient into any controller:
//
//    public class ProductsController : Controller
//    {
//        private readonly AdminApiClient _api;
//        public ProductsController(AdminApiClient api) => _api = api;
//
//        public async Task<IActionResult> Index(string? search, int page = 1)
//        {
//            var result = await _api.GetProductsAsync(search: search, page: page);
//            // map result.Data to your existing ProductListViewModel
//            return View(viewModel);
//        }
//    }
//
// ── Example: DashboardController replacement ──────────────────────────────────
//
//    public class DashboardController : Controller
//    {
//        private readonly AdminApiClient _api;
//        public DashboardController(AdminApiClient api) => _api = api;
//
//        public async Task<IActionResult> Index()
//        {
//            var dash = await _api.GetDashboardAsync();
//            var vm = new DashboardViewModel
//            {
//                TotalRevenue    = dash?.TotalRevenue    ?? 0,
//                TotalOrders     = dash?.TotalOrders     ?? 0,
//                TotalProducts   = dash?.TotalProducts   ?? 0,
//                TotalCustomers  = dash?.TotalCustomers  ?? 0,
//                RevenueChart    = dash?.RevenueChart.Select(r => new RevenuePoint
//                                      { Label = r.Label, Revenue = r.Revenue }).ToList() ?? new(),
//                TopProducts     = dash?.TopProducts.Select(MapProduct).ToList() ?? new(),
//                RecentOrders    = dash?.RecentOrders.Select(MapOrder).ToList()  ?? new()
//            };
//            return View(vm);
//        }
//    }
//
// ══════════════════════════════════════════════════════════════════════════════
// RUNNING BOTH PROJECTS
// ══════════════════════════════════════════════════════════════════════════════
//
// Terminal 1 — Web API (this project):
//   cd AdminPortal.Api
//   dotnet run
//   → Swagger UI: https://localhost:7001/swagger
//
// Terminal 2 — MVC Web:
//   cd AdminPortal.Web
//   dotnet run
//   → https://localhost:5001
//
// Or use Visual Studio / Rider's "Multiple Startup Projects" feature.
// ══════════════════════════════════════════════════════════════════════════════
