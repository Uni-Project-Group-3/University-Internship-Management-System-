using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace University_Internship_Management_System.Shared
{
    internal static class FirebaseDatabaseVersion
    {
        internal static string ReadETag(HttpResponseMessage response)
        {
            IEnumerable<string> tags;
            string etag = response.Headers.TryGetValues("ETag", out tags) ? tags.SingleOrDefault() : null;
            if (string.IsNullOrWhiteSpace(etag))
                throw new InvalidOperationException("Firebase did not return a database version header.");
            return etag;
        }
    }
}