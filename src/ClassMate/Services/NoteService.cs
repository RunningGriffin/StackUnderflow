
using System.Runtime.CompilerServices;
using System.Xml;
using ClassMate.Data;
using Microsoft.EntityFrameworkCore;

namespace ClassMate.Services;


public class NoteService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public NoteService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<List<Note>> GetNotesAsync(string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        return await db.Notes
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n=> n.Id)
            .ToListAsync();
    }

    public async Task<bool> SaveNoteAsync(string userId, Note input, int? editingNoteId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(input.Title))
        {
            return false;
        }
        
        bool ownsCourse = await db.Courses.AnyAsync(c => c.Id == input.CourseId && c.UserId == userId);

        
        if (!ownsCourse)
        {
            return false;
        }


        if (editingNoteId.HasValue)
        {
            var existing = await db.Notes
                .FirstOrDefaultAsync(n =>
                    n.Id == editingNoteId.Value &&
                    n.UserId == userId);

            if (existing is null)
                return false;


            existing.Title = input.Title.Trim();
            existing.Content = input.Content;
            existing.CourseId = input.CourseId;
        }
        else
        {
            var note = new Note
            {
                Title = input.Title.Trim(),
                Content = input.Content,
                CourseId = input.CourseId,
                UserId = userId
            };

            db.Notes.Add(note);
        }
        await db.SaveChangesAsync();
        return true;
    }

        // Delete a course 
    public async Task<bool> DeleteNoteAsync(int noteId, string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var note = await db.Notes
            .FirstOrDefaultAsync(n =>
                n.Id == noteId &&
                n.UserId == userId);

        if (note is null)
            return false;

        db.Notes.Remove(note);


        await db.SaveChangesAsync();
        return true;
    }

    
}
