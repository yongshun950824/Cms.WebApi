using System.Text.Json.Serialization;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<CmsDatabaseContext>(options =>
    options.UseInMemoryDatabase("CmsDatabase"));
builder.Services.AddAutoMapper(typeof(CmsMapper));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/courses", async (CmsDatabaseContext db) =>
{
    try
    {
        var result = await db.Courses.ToListAsync();
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapPost("/courses", async ([FromBody] CourseDto courseDto, [FromServices] CmsDatabaseContext db, [FromServices] IMapper mapper) =>
{
    try
    {
        var course = mapper.Map<Course>(courseDto);

        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var result = mapper.Map<CourseDto>(course);
        return Results.Created($"/courses/{result.CourseId}", result);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapGet("/courses/{courseId}", async (int courseId, CmsDatabaseContext db, IMapper mapper) => 
{
    var course = await db.Courses.FindAsync(courseId);
    if (course == null)
        return Results.NotFound();

    var result = mapper.Map<CourseDto>(course);
    return Results.Ok(result);
});

app.MapPut("/courses/{courseId}", async (int courseId, CourseDto courseDto, CmsDatabaseContext db, IMapper mapper) => 
{
    var course = await db.Courses.FindAsync(courseId);
    if (course == null)
        return Results.NotFound();

    course.CourseName = courseDto.CourseName;
    course.CourseDuration = courseDto.CourseDuration;
    course.CourseType = (int)courseDto.CourseType;

    await db.SaveChangesAsync();

    var result = mapper.Map<CourseDto>(course);
    return Results.Ok(result);
});

app.MapDelete("/courses/{courseId}", async (int courseId, CmsDatabaseContext db, IMapper mapper) => 
{
    var course = await db.Courses.FindAsync(courseId);
    if (course == null)
        return Results.NotFound();

    db.Courses.Remove(course);
    await db.SaveChangesAsync();

    return Results.Ok();
});

app.Run();

public class CmsMapper : Profile
{
    public CmsMapper()
    {
        CreateMap<Course, CourseDto>();
        CreateMap<CourseDto, Course>();
    }
}

public class Course
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = String.Empty;
    public int CourseDuration { get; set; }
    public int CourseType { get; set; }
}

public class CourseDto
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = String.Empty;
    public int CourseDuration { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public COURSE_TYPE CourseType { get; set; }
}

public enum COURSE_TYPE
{
    ENGINEERING = 1,
    MEDICAL,
    MANAGEMENT
}

public class CmsDatabaseContext : DbContext
{
    public CmsDatabaseContext(DbContextOptions options) : base(options)
    {

    }

    public DbSet<Course> Courses => Set<Course>();
}