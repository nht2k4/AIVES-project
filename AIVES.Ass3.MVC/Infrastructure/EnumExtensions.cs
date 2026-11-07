using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace AIVES.Ass3.MVC.Infrastructure;

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        return member?.GetCustomAttribute<DisplayAttribute>()?.Name ?? value.ToString();
    }
}
