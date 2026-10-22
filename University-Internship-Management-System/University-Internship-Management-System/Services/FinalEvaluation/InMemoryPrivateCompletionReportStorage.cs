using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace University_Internship_Management_System.Services.FinalEvaluation
{
    // Explicit test composition only. No URLs, filesystem writes, credentials or Firebase.
    public sealed class InMemoryPrivateCompletionReportStorage : IPrivateCompletionReportStorage
    {
        private readonly object gate = new object();
        private readonly Dictionary<string, byte[]> objects = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        public Task PutNewAsync(string key, byte[] pdf)
        {
            ValidateKey(key); CompletionReportPdfRenderer.ValidatePdf(pdf);
            lock (gate) { if (objects.ContainsKey(key)) throw new ConflictException(); objects.Add(key, (byte[])pdf.Clone()); }
            return Task.CompletedTask;
        }
        public Task<byte[]> ReadAsync(string key)
        {
            ValidateKey(key);
            lock (gate) { byte[] value; if (!objects.TryGetValue(key, out value)) throw new RepositoryException("Private report object is unavailable."); return Task.FromResult((byte[])value.Clone()); }
        }
        public Task DeleteIfHashMatchesAsync(string key, string hash)
        {
            ValidateKey(key);
            lock (gate) { byte[] value; if (objects.TryGetValue(key, out value) && CompletionReportValidation.Hash(value) == hash) objects.Remove(key); }
            return Task.CompletedTask;
        }
        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Invalid private object key.");
            string[] parts = key.Split('/');
            if (parts.Length != 3 || parts[0] != "completion-reports" || !parts[2].EndsWith(".pdf", StringComparison.Ordinal)) throw new ArgumentException("Invalid private object key.");
            string docId = parts[2].Substring(0, parts[2].Length - 4);
            if (key != CompletionReportValidation.ObjectKey(Uri.UnescapeDataString(parts[1]), docId)) throw new ArgumentException("Invalid private object key.");
        }
    }
}
