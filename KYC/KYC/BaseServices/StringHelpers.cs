using System.Collections.Generic;
using System.Linq;

namespace KYC.BaseServices
{
    public class StringHelpers
    {
        public static string MakeUrlWithQuery(string baseUrl,
            IEnumerable<KeyValuePair<string, string>> parameters)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                baseUrl = string.Empty;
            }
            else
            {
                baseUrl = baseUrl.Trim();

                if (baseUrl.ElementAt(baseUrl.Length - 1) != '?')
                {
                    if (baseUrl.IndexOf('?') < 0)
                        baseUrl = string.Concat(baseUrl, "?");
                }
            }

            if (parameters == null || parameters.Count() == 0) return baseUrl;

            return parameters.Aggregate(baseUrl,
                (accumulated, kvp) => string.Format($"{accumulated}{kvp.Key}={kvp.Value}&"));
        }
    }
}