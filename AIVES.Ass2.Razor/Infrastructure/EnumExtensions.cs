using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace AIVES.Ass2.Razor.Infrastructure;

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        return member?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? value.ToString();
    }
}
