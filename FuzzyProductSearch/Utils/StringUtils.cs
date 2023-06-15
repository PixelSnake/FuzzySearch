using System;
using System.Collections.Generic;
using System.Text;

namespace FuzzyProductSearch.Utils
{
    public class StringUtils
    {
        public static string? FindString(string content, int startPos, out int lineBreaks, out int column, out int end)
        {
            var escapeNext = false;
            var result = "";
            lineBreaks = 0;
            column = 0;
            end = -1;

            var prefix = "";
            var stringBegin = content.IndexOf("\"", startPos);
            if (stringBegin - startPos > 0)
            {
                prefix = content.Substring(startPos, stringBegin - startPos);
            }

            for (int i = stringBegin + 1; i < content.Length; ++i)
            {
                var c = content[i];

                switch (c)
                {
                    case '"':
                        if (!escapeNext)
                        {
                            end = i + 1;
                            return prefix + "\"" + result + "\"";
                        }
                        escapeNext = false;
                        break;

                    case '\\':
                        if (!escapeNext)
                        {
                            escapeNext = true;
                            continue;
                        }
                        break;

                    case '\n':
                        lineBreaks++;
                        column = 0;
                        break;
                }

                column++;
                result += c;
            }

            return null;
        }
    }
}
