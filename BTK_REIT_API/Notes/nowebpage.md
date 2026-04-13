# 404 No Webpage Found

In .NET Web APIs, if you go to `http://localhost:5039/`, you will always get a 404 (Page Not Found). Unlike a website, an API doesn't have a "Home Page" unless you tell it to.

To see your data, you must go to the specific route we defined in the Controller:
👉 `http://localhost:5039/api/properties`

## Solution in this Project: MapControllers was Missing
In .NET 10, your `Program.cs` needs an explicit command to look for the `[Route]` attributes in your Controllers folder. Without this, the app starts but has no "map" of where your code lives.

Open `Program.cs` and ensure you have these two lines:
```
// 1. Tell the builder to find controllers
builder.Services.AddControllers(); 

var app = builder.Build();

// further down before app.Run()

// 2. Tell the app to use the attribute routes
app.MapControllers(); 

app.Run();
```