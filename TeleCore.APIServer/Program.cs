using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using TeleCore.APIServer.Hubs;
using TeleCore.Application.Common;
using TeleCore.Application.Services;
using TeleCore.Infrastructure.Hubs;
using TeleCore.Infrastructure.Persistence;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// 1. تسجيل الـ DbContext (لازم يكون الأول)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. ربط الـ Interface بالـ Context (ده الجسر اللي ناقص وكان مسبب الإيرور)
builder.Services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>());

// 3. تسجيل خدمات الـ Application
builder.Services.AddScoped<ICommissionService, CommissionService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IShiftService, ShiftService>();


builder.Services.AddRazorPages();
builder.Services.AddSignalR(); // إضافة الخدمة

// إعدادات الـ OpenAPI الجديدة في .NET 10
builder.Services.AddOpenApi();



// في ملف Program.cs (الخاص بالسيرفر)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed((host) => true) // 👈 ده بيسمح للموبايل يكلم السيرفر
              .AllowCredentials(); // 👈 مهم جداً للـ SignalR
    });
});

var app = builder.Build();

// 1️⃣ لازم Routing يكون أول حاجة عشان السيرفر يفهم الروابط
app.UseRouting();

// 2️⃣ الـ CORS يجي فوراً بعد الـ Routing وقبل أي حاجة تانية
app.UseCors();

// لو الـ Environment مش Development، الاستضافة غالباً هي اللي بتجبر الـ HTTPS
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();

// في ملف Program.cs بالسيرفر - السطر الأخير تقريباً
// غير transactionHub إلى transferHub ليتطابق مع الموبايل
app.MapHub<TransactionHub>("/TransactionHub");

// إذا كنت لا تستخدم NodeHub حالياً، يمكنك حذفه أو تركه، 
// لكن الأهم هو السطر أعلاه الخاص بالـ TransactionHub
app.MapHub<NodeHub>("/nodeHub");
app.Run();