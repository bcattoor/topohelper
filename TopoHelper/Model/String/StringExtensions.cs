using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopoHelper.Model.String
{
    internal static class StringExtensions
    {
        public static string UnPascalCase(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";
            var newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++)
            {
                var currentUpper = char.IsUpper(text[i]);
                var prevUpper = char.IsUpper(text[i - 1]);
                var nextUpper = (text.Length > i + 1) ? char.IsUpper(text[i + 1]) || char.IsWhiteSpace(text[i + 1]) : prevUpper;
                var spaceExists = char.IsWhiteSpace(text[i - 1]);
                if (currentUpper && !spaceExists && (!nextUpper || !prevUpper))
                    newText.Append(' ');
                newText.Append(text[i]);
            }

            return newText.ToString();
        }

        public static string ToFriendlyCommandName(this string s) => s.Replace("IAMTopo_", "").Trim()
            .Replace("2D", "Tweeedeee").Replace("3D", "Treeedeee")
            .Replace("2", "Two")
            .UnPascalCase()
            .Replace("Treeedeee", "3D").Replace("Tweeedeee", "2D")
            .Replace("_", " ");
    }
}