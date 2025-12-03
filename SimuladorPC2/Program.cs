using SimuladorPC2.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton<ISimulator, SimulatorService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
 app.UseExceptionHandler("/Error");
 // The default HSTS value is30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
 app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

// Minimal API endpoint used by the Index.js fetch call
app.MapPost("/Simulador/Executar", (ProgramRequest req, ISimulator sim) =>
{
 if (req == null || string.IsNullOrWhiteSpace(req.Codigo))
 {
 return Results.Json(new { error = "Codigo vazio" });
 }

 var lines = req.Codigo.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
 sim.Reset();
 sim.LoadAssembly(lines);
 sim.Run(10000);

 var log = string.Join("\n", sim.GetLogs());
 var m = sim.GetMetrics();

 return Results.Json(new
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
});

app.Run();

internal record ProgramRequest(string Codigo);
