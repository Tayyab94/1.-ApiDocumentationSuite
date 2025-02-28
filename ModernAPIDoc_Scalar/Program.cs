using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ModernAPIDoc_Scalar.Models;
using ModernAPIDoc_Scalar.Repos;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ✅ 1. Read JWT Configuration
var jwtSettings = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme= JwtBearerDefaults.AuthenticationScheme;
        }) 
         //.AddJwtBearer(options =>
         //{
         //    options.Authority = builder.Configuration["Jwt:Authority"];
         //    options.Audience = builder.Configuration["Jwt:Audience"];
         //    options.RequireHttpsMetadata = true;
         //    options.TokenValidationParameters =
         //        new Microsoft.IdentityModel.Tokens.TokenValidationParameters
         //        {
         //            ValidateIssuer = true,
         //            ValidIssuer = builder.Configuration["Jwt:Authority"],
         //        };
         //});

         .AddJwtBearer(options =>
         {
             options.TokenValidationParameters = new TokenValidationParameters
             {
                 ValidateIssuer = true,
                 ValidateAudience = true,
                 ValidateLifetime = true,  // The JWT middleware no longer rejects expired tokens. we will set false, if we want to test expired token
                 ValidateIssuerSigningKey = true,
                 ValidIssuer = jwtSettings["ValidIssuer"],
                 ValidAudience = jwtSettings["ValidAudience"],
                 IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]))
             };
         });


builder.Services.AddAuthorization();

// Configure OpenAPI
builder.Services.AddOpenApi(opt =>
{
    //opt.AddDocumentTransformer((document, context, _) =>
    //{
    //    document.Info = new()
    //    {
    //        Title = "Product Catalog API",
    //        Version = "v1",
    //        Description = """
    //            Modern API for managing product catalogs.
    //            Supports JSON and XML responses.
    //            Rate limited to 1000 requests per hour.
    //            """,
    //        Contact = new()
    //        {
    //            Name = "API Support",
    //            Email = "api@example.com",
    //            Url = new Uri("https://api.example.com/support")
    //        }
    //    };
    //    return Task.CompletedTask;
    //});


    opt.AddDocumentTransformer<BearerSecuritySchemeTransformer>();

});


builder.Services.AddScoped<ITokenService, TokenService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if(app.Environment.IsDevelopment())
{
    // enable OpenAPI &Scalar
    app.MapOpenApi().CacheOutput();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();



// Redirect root to Scalar UI
app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();

app.MapPost("/users", (UserLoginModel model, ITokenService _tokenService) =>
{
    if (model.UserName == "admin" && model.Password == "password") // Replace with actual validation
    {
        var token = _tokenService.GenerateJwtToken(model.UserName); 
        return Results.Ok(new { token });
    }
    return Results.Unauthorized();
}).Produces<string>(200).Produces(400).WithName("Login").WithSummary("User Login");


app.MapGet("/product/ListOfProducts",(int? pageSize, int? page) =>
{
    var products = Enumerable.Range(1, 100).Select(index =>
        new Product(index, $"Product {index}", index * 10)).ToArray();
    return products;

}).RequireAuthorization().Produces<List<Product>>(200)
.Produces(400)
.WithName("GetProducts")
.WithTags("products").WithSummary("Retrieve a list of products").WithDescription("""
    Returns a paginated list of products.
    Default page size is 10.
    Use page parameter for pagination.
    """);

app.MapPost("/product",(Product product) =>
{
    return product;
}).Produces<Product>(201).Produces(400).WithName("CreateProduct")
.WithTags("Products")
.WithSummary("Create a new product")
.WithDescription("Add a new product to the catalog.");


app.MapGet("/products/{id}", (int id) =>
{
    var products = Enumerable.Range(1, 100).Select(index =>
           new Product(index, $"Product {index}", index * 10))
           .ToArray().Where(s => s.Id == id);

    return products is null ? Results.NotFound() : Results.Ok(products);
}).Produces<Product>(200).Produces(400).WithOpenApi(operation =>
{
    operation.Responses.Add("404", new OpenApiResponse
    {
        Description = "Product not found"
    });
    return operation;
});

app.Run();



public record Product(int Id, string Name, decimal Price);

internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider)
        :IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
        {
            var requirements = new Dictionary<string, OpenApiSecurityScheme>
            {
                ["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer", // "bearer" refers to the header name here
                    In = ParameterLocation.Header,
                    BearerFormat = "Json Web Token",
                },
            };
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = requirements;
        }
    }
}