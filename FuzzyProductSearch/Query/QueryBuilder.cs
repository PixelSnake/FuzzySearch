using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using FuzzyProductSearch.Exceptions;
using FuzzyProductSearch.Utils;

namespace FuzzyProductSearch.Query
{
    internal class QueryBuilder
    {
        public IEnumerable<IQueryPart> BuildQuery(string query)
        {
            query += " ";

            var parts = new List<string>();
            var tokenStart = 0;

            for (int i = 0; i < query.Length; i++)
            {
                var current = query[i];

                if (current == '"')
                {
                    var strLiteral = StringUtils.FindString(query, i, out _, out _, out i);
                    if (strLiteral != null)
                    {
                        parts.Add(strLiteral);
                    }
                    else
                    {
                        throw new QueryException("Malformed string literal");
                    }

                    tokenStart = i;
                }
                else if (current == ' ')
                {
                    parts.Add(query.Substring(tokenStart, i - tokenStart).ToLower().Trim());
                    tokenStart = i + 1;
                }
            }

            for (var i = 0; i < parts.Count; i++)
            {
                switch (parts[i])
                {
                    case "search":
                        if (parts.Count <= i + 1)
                        {
                            throw new QueryException("SEARCH statement expects string, but EOL was given");
                        }

                        yield return new SearchQueryPart
                        {
                            SearchString = parts[i + 1].Substring(1, parts[i + 1].Length - 2)
                        };
                        i++;
                        break;

                    case "limit":
                        if (parts.Count <= i + 1)
                        {
                            throw new QueryException("LIMIT statement expects number, but EOL was given");
                        }

                        if (!int.TryParse(parts[i + 1], out var limit))
                        {
                            throw new QueryException($"LIMIT statement expects number, but \"{parts[i + 1]}\" was given");
                        }

                        yield return new LimitQueryPart
                        {
                            Limit = limit
                        };
                        i++;
                        break;

                    case "offset":
                        if (parts.Count <= i + 1)
                        {
                            throw new QueryException("OFFSET statement expects number, but EOL was given");
                        }

                        if (!int.TryParse(parts[i + 1], out var offset))
                        {
                            throw new QueryException($"OFFSET statement expects number, but \"{parts[i + 1]}\" was given");
                        }

                        yield return new OffsetQueryPart
                        {
                            Offset = offset
                        };
                        i++;
                        break;

                    case "maxdist":
                        if (parts.Count <= i + 1)
                        {
                            throw new QueryException("MAXDIST statement expects number, but EOL was given");
                        }

                        if (!float.TryParse(parts[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var maxdist))
                        {
                            throw new QueryException($"MAXDIST statement expects number, but \"{parts[i + 1]}\" was given");
                        }

                        yield return new MaximumDistanceQueryPart
                        {
                            MaximumDistance = maxdist
                        };
                        i++;
                        break;

                    case "return":
                        if (parts.Count <= i + 1)
                        {
                            throw new QueryException("RETURN statement expects a list of properties, but EOL was given");
                        }

                        var properties = new List<string>();
                        do
                        {
                            var prop = parts[i + 1];
                            if (prop[^1] == ',')
                            {
                                prop = prop.Substring(0, prop.Length - 1);
                            }

                            properties.Add(prop);
                            i++;
                        } while (properties[^1].EndsWith(','));

                        yield return new ReturnQueryPart
                        {
                            Properties = properties.ToArray()
                        };
                        break;

                    default:
                        throw new QueryException($"Unknown statement {parts[i]}");
                }
            }
        }

        public interface IQueryPart
        {
        }

        public class SearchQueryPart : IQueryPart
        {
            public string SearchString;
        }

        public class LimitQueryPart : IQueryPart
        {
            public int Limit;
        }

        public class OffsetQueryPart : IQueryPart
        {
            public int Offset;
        }

        public class MaximumDistanceQueryPart : IQueryPart
        {
            public float MaximumDistance;
        }

        public class ReturnQueryPart : IQueryPart
        {
            public string[] Properties;
        }
    }
}
