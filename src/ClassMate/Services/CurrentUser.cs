using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace ClassMate.Services;

/// <summary>
/// Gives any page or service the signed-in user's Id, which is the UserId
/// stored on Courses, Assignments, Notes, StudySessions, StudyLogs and UserSettings.
///
/// Usage in a .razor page:
///     @inject CurrentUser CurrentUser
///     var userId = await CurrentUser.GetUserIdAsync();
///     var courses = await Db.Courses.Where(c => c.UserId == userId).ToListAsync();
/// </summary>
public sealed class CurrentUser(AuthenticationStateProvider authStateProvider)
{
    /// <summary>The signed-in user's Id, or null if nobody is signed in.</summary>
    public async Task<string?> GetUserIdAsync()
    {
        var state = await authStateProvider.GetAuthenticationStateAsync();
        return state.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    /// <summary>
    /// The signed-in user's Id. Throws if nobody is signed in, so use it on
    /// pages marked [Authorize].
    /// </summary>
    public async Task<string> GetRequiredUserIdAsync()
        => await GetUserIdAsync()
           ?? throw new InvalidOperationException("No user is signed in.");

    /// <summary>The signed-in user's email/user name, or null.</summary>
    public async Task<string?> GetUserNameAsync()
    {
        var state = await authStateProvider.GetAuthenticationStateAsync();
        return state.User.Identity?.Name;
    }

    public async Task<bool> IsSignedInAsync()
    {
        var state = await authStateProvider.GetAuthenticationStateAsync();
        return state.User.Identity?.IsAuthenticated == true;
    }
}
