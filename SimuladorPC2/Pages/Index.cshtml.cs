using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SimuladorPC2.Services;

namespace SimuladorPC2.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ISimulator _sim;

        public IndexModel(ILogger<IndexModel> logger, ISimulator sim)
        {
            _logger = logger;
            _sim = sim;
        }

        [BindProperty]
        public string Codigo { get; set; }

        public void OnGet()
        {

        }

        // Endpoint used by JS to execute program
        public IActionResult OnPostExecutar([FromBody] ProgramRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Codigo))
            {
                return new JsonResult(new { error = "Codigo vazio" });
            }

            var lines = req.Codigo.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            _sim.Reset();
            _sim.LoadAssembly(lines);
            _sim.Run(10000);

            var log = string.Join("\n", _sim.GetLogs());
            var m = _sim.GetMetrics();

            return new JsonResult(new
            {
                log,
                metricas = new
                {
                    ciclos = m.Cycles,
                    ipc = m.Ipc,
                    hits = m.CacheHits,
                    misses = m.CacheMisses
                }
            });
        }

        public class ProgramRequest
        {
            public string Codigo { get; set; }
        }
    }
}
