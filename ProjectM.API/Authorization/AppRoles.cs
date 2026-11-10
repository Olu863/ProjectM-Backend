using System.Security.Claims;

namespace ProjectM.API.Authorization
{
    /// <summary>
    /// Role names for use in [Authorize(Roles = ...)] attributes (these must be compile-time constants).
    /// </summary>
    public static class AppRoles
    {
        public const string Admin = "Admin";

        /// <summary>
        /// Everyone who works for the service company. Anyone NOT in one of these roles (i.e. a Client)
        /// is treated as an external customer who may only see and change their own records.
        /// </summary>
        public const string Staff = "Admin,Manager,Technician,Operations";
    }

    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// The signed-in user's ID from the JWT, or 0 when the token has no valid ID claim
        /// (0 never matches a real user, so ownership checks fail safely).
        /// </summary>
        public static int GetUserId(this ClaimsPrincipal user)
        {
            return int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;
        }

        /// <summary>
        /// True for company staff. Unknown or missing roles are NOT staff, so they get the restricted view.
        /// </summary>
        public static bool IsStaff(this ClaimsPrincipal user)
        {
            return user.IsInRole("Admin")
                || user.IsInRole("Manager")
                || user.IsInRole("Technician")
                || user.IsInRole("Operations");
        }
    }
}
