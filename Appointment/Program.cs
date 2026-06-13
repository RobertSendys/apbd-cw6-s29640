using Appointment.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddScoped<AppointmentQueryRepository>();
builder.Services.AddScoped<AppointmentLookupRepository>();
builder.Services.AddScoped<AppointmentCommandRepository>();
builder.Services.AddScoped<AppointmentService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();


