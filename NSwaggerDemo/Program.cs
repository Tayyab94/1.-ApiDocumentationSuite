var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    //app.UseSwaggerUi(options =>
    //{
    //    options.DocumentTitle = "Swagger UI - NSwaggerDemo";
    //    options.DocumentPath= "/openapi/v1.json";
    //});

    app.UseReDoc(options =>
    {
        options.DocumentTitle = "Rec UI - NSwaggerDemo";
        options.DocumentPath = "/openapi/v1.json";
    });

}

app.UseHttpsRedirection();


List<Person> persons = new List<Person>
{
    new Person("John", "Doe", 25),
    new Person("Jane", "Dam", 20),
    new Person("John", "Smith", 25),
    new Person("Ali","Ahmad",14),
    new Person("Jane", "Smith", 25),
};

app.MapGet("/persons", () => persons);
app.MapGet("/persons/{id}", (int id) => persons[id]);
app.MapPost("/persons", (Person model) =>
{
    persons.Add(model);
    return persons;
});


app.MapPut("/persons/{id}", (int id, Person model) =>
{
    persons[id] = model;
    return persons;
});


app.MapDelete("/persons/{id}", (int id) =>
{
    persons.RemoveAt(id);
});


app.Run();


public record Person(string FirstName, string LastName, int age);