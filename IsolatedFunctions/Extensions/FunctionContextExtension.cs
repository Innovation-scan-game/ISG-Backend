using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;

namespace IsolatedFunctions.Extensions;

public static class FunctionContextExtension
{
    public static ClaimsPrincipal? GetUser(this FunctionContext context)
    {
        if (context.Items.TryGetValue("User", out object? user))
        {
            return (ClaimsPrincipal) user;
        }

        return null;
    }


    public static bool IsLoggedIn(this FunctionContext context)
    {
        return context.GetUser() != null;
    }

    public static bool IsAdmin(this FunctionContext context)
    {
        var user = context.GetUser();
        if (user != null && user.IsInRole("Admin"))
        {
            return true;
        }

        return false;
    }
}
