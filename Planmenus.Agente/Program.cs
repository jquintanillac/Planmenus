using Planmenus.Agente;

// Agente de impresion de Planmenus: recibe los tickets del sistema web y los imprime
// en la impresora de Windows configurada (RAW / ESC/POS). Puede correr como consola o como servicio.
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(o => o.ServiceName = "Planmenus Agente de impresion");
builder.Services.Configure<OpcionesAgente>(builder.Configuration.GetSection("Agente"));
builder.Services.AddHttpClient<ClienteServidor>();
builder.Services.AddHostedService<AgenteWorker>();

var host = builder.Build();
host.Run();
