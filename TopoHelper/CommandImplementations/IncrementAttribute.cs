using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Text.RegularExpressions;

namespace TopoHelper.CommandImplementations
{
    internal class IncrementAttribute
    {
        #region Private Fields

        private const double Tolerance = Commands.Tolerance;

        #endregion

        #region Public Methods

        /// <summary>
        /// This command is used by user to align an inserted blocks angle
        /// property to a selected polyline.
        /// </summary>
        public static void ExecuteIncrementsByPatern(Transaction transaction, ObjectId blockId, ref string patern, string attributeName)
        {
            using (var sourceBlockReference = transaction.GetObject(blockId, OpenMode.ForWrite) as BlockReference)
            {
                if (sourceBlockReference.AttributeCollection.Count == 0)
                    throw new Exception("This is not a block containing attributes.");

                foreach (ObjectId property in sourceBlockReference.AttributeCollection)
                {
                    using (var att = transaction.GetObject(property, OpenMode.ForRead) as AttributeReference)
                        if (att.Tag == attributeName && !att.IsConstant)
                        {
                            att.UpgradeOpen();
                            var currentValue = att.TextString;
                            att.TextString = ExecuteIncrementsByPatern(ref patern);
                            break;
                        }
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// We look in the string for occurences of
        /// [value][paternSwitch][increment value] where the first value, is the
        /// actual value of the number, and the second value is the value to
        /// increment the number by.
        /// ea: 12[paternSwitch]2 the number twelve here needs to be incremented
        /// by the number two. Or as example: input --&gt; This is an example
        /// 12/20[paternSwitch]2. output --&gt; This is an example 12/22.
        /// </summary>
        /// <param name="paternString">
        /// Passed by reference, so we can actualy update the string accordingly.
        /// </param>
        /// <returns> The new incremented string </returns>
        private static string ExecuteIncrementsByPatern(ref string paternString)
        {
            // this is our unchanged original string, we will be using to create
            // out new paternstring. (when function has completed)
            var workPaternString = paternString;
            const string paternSwitch = "~";
            string resultString = "";

            // do not regex if there is no paternSwitch in string
            if (!workPaternString.Contains(paternSwitch))
                return workPaternString;

            // create reg expr
            var expression =
              new Regex("[0-9]{1,9}" + paternSwitch + "{1}[0-9]{1,9}", RegexOptions.None);

            // get first match
            Match wMatch = expression.Match(workPaternString);

            // execute patern for all matches
            while (wMatch.Success)
            {
                string backOfBase = null, frontOfBase = null;
                int leftInt, rightInt, start, size;
                // get the value where we need to work against
                var arrayInput = wMatch.Value.Split(paternSwitch.ToCharArray(),
                                   StringSplitOptions.RemoveEmptyEntries);

                if (!int.TryParse(arrayInput[0], out leftInt))
                    throw new Exception("ExecuteIncrementsByPatern: parsing error");
                if (!int.TryParse(arrayInput[1], out rightInt))
                    throw new Exception("ExecuteIncrementsByPatern: parsing error");

                // get front of string
                frontOfBase = workPaternString.Substring(0, wMatch.Index);

                // are there any characters behind the matching patern?
                if (wMatch.Index + wMatch.Length < workPaternString.Length)
                {
                    start = wMatch.Index + wMatch.Length;
                    size = workPaternString.Length - frontOfBase.Length - wMatch.Length;
                    backOfBase = workPaternString.Substring(start, size);
                }

                // create actual return value
                var newIntValue = (leftInt + rightInt);
                if (string.IsNullOrEmpty(frontOfBase))
                    frontOfBase = "";
                if (string.IsNullOrEmpty(backOfBase))
                    backOfBase = "";

                resultString = frontOfBase + newIntValue + backOfBase;

                // update patern and find new match
                workPaternString = frontOfBase + newIntValue + backOfBase;
                wMatch = expression.Match(workPaternString);
            }

            var match = expression.Match(paternString);
            var tempString = paternString;
            var offset = 0;
            while (match.Success)
            {
                int leftInt, rightInt;
                // get the value where we need to work against
                var arrayInput = match.Value.Split(paternSwitch.ToCharArray(),
                                   StringSplitOptions.RemoveEmptyEntries);

                if (!int.TryParse(arrayInput[0], out leftInt))
                    throw new Exception("ExecuteIncrementsByPatern: parsing error");
                if (!int.TryParse(arrayInput[1], out rightInt))
                    throw new Exception("ExecuteIncrementsByPatern: parsing error");

                var leftNewString = (leftInt + rightInt).ToString();
                var replacement = leftNewString + paternSwitch + rightInt;
                tempString = ReplaceAt(tempString, match.Index + offset, match.Length, replacement);

                if (match.Index + match.Length + offset < tempString.Length)
                {
                    var sub = tempString.Substring(match.Index + match.Length + offset, (tempString.Length - offset) - (match.Index + match.Length));
                    offset += match.Index + match.Length;
                    match = expression.Match(sub);
                }
                else
                    break;
            }
            paternString = tempString;
            return resultString;
        }

        /// <summary>
        /// Replaces string in an existing string.
        /// </summary>
        /// <param name="str">     the source string </param>
        /// <param name="index">   the start location to replace at (0-based) </param>
        /// <param name="length"> 
        /// the number of characters to be removed before inserting
        /// </param>
        /// <param name="replace"> the string that is replacing characters </param>
        /// <returns> </returns>
        private static string ReplaceAt(string str, int index, int length, string replace)
        {
            return str.Remove(index, Math.Min(length, str.Length - index))
              .Insert(index, replace);
        }

        #endregion
    }
}