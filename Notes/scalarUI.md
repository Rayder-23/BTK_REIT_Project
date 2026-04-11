# 1. Install NuGet Packages:

### This generates the JSON document that describes your API
`dotnet add package Microsoft.AspNetCore.OpenApi`

### This provides the beautiful Scalar interface to view that JSON
`dotnet add package Scalar.AspNetCore`

# 2. Configure `Program.cs`

You need to tell your app to generate the API metadata and then map the Scalar endpoint. Open your `Program.cs` and add these specific lines:
```
// 1. Add to the top of the file
using Scalar.AspNetCore;

// 2. Add the service to the builder
builder.Services.AddOpenApi();

var app = builder.Build();

// 3. Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Generates the /openapi/v1.json file
    app.MapOpenApi(); 
    
    // Generates the UI at /scalar/v1
    app.MapScalarApiReference(); 
}

// Ensure your other logic is still there
app.MapControllers();
app.Run();
```

# 3. Verify the Setup
Run your project: `dotnet run`.

Open your browser to: http://localhost:5039/scalar/v1