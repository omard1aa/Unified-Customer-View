using MergeService.Clients;
using MergeService.Interfaces;
using MergeService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<ISystemAClient, SystemAClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["APIs:SystemA:BaseUrl"]!));

builder.Services.AddHttpClient<ISystemBClient, SystemBClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["APIs:SystemB:BaseUrl"]!);
    c.Timeout = TimeSpan.FromSeconds(3);
});

builder.Services.AddScoped<ICustomerService, CustomersService>();
builder.Services.AddMemoryCache();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.MapControllers();

app.Run();
