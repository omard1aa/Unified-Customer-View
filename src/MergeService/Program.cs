using MergeService.Clients;
using MergeService.Interfaces;
using MergeService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<SystemAClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["APIs:SystemA:BaseUrl"]!));

builder.Services.AddHttpClient<SystemBClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["APIs:SystemB:BaseUrl"]!);
    c.Timeout = TimeSpan.FromSeconds(3);
});

builder.Services.AddScoped<ICustomerService, CustomersService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.UseRouting();

app.Run();
