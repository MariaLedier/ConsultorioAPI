using ConsultorioAPI.Repositories;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Consultório API",
        Version = "v1",
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

builder.Services.AddScoped<MedicoRepository>();
builder.Services.AddScoped<PacienteRepository>();
builder.Services.AddScoped<ConsultaRepository>();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Consultório API v1");
    c.RoutePrefix = "swagger";   // Swagger agora fica em /swagger
    c.DocumentTitle = "Consultório API";
    c.DefaultModelsExpandDepth(-1);
    c.DisplayRequestDuration();
});

app.UseCors();

// Serve os arquivos estáticos da pasta wwwroot (index.html, etc.)
app.UseDefaultFiles();   // serve index.html automaticamente em /
app.UseStaticFiles();    // serve qualquer arquivo da pasta wwwroot


app.UseAuthorization();
app.MapControllers();
app.Run();