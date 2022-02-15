using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TopoHelper.Model.String
{
    internal static class StringExtensions
    {
        public static string CapitalizeFirstLetter(this string s)
        {
            return char.ToUpper(s.First()) + s.Substring(1).ToLower();
        }

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

        public static string ToFriendlyCommandName(this string s) =>
            Regex.Replace(
             s.Replace("IAMTopo_", "")
            .Trim()
            .Replace("2D", "Tweeedeee").Replace("3D", "Treeedeee")
            .Replace("2", "Two")
            .UnPascalCase().CapitalizeFirstLetter()
            .Replace("Treeedeee", "3D").Replace("Tweeedeee", "2D") //Lower and upercase
            .Replace("treeedeee", "3D").Replace("tweeedeee", "2D") //Lower and upercase
            .Replace("_", " "),
             @"\s+", " "); //remove double spaces
    }
}