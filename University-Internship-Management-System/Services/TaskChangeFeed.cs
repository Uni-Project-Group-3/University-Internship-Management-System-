using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Hosting;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json.Linq;

namespace UIMS.Tasks
{
    // Shared Firebase REST streams carry invalidations only. No business data is relayed or persisted here.
    internal sealed class TaskChangeFeed : IRegisteredObject
    {
        private static readonly Lazy<TaskChangeFeed> Instance = new Lazy<TaskChangeFeed>(() => new TaskChangeFeed());
        private readonly object sync = new object();
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private readonly HttpClient client = new HttpClient(new HttpClientHandler { MaxConnectionsPerServer = 12 }) { Timeout = Timeout.InfiniteTimeSpan };
        private TaskCompletionSource<long> next = NewSignal();
        private long version;
        private int connected;
        private static TaskCompletionSource<long> NewSignal() { return new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously); }
        private TaskChangeFeed()
        {
            HostingEnvironment.RegisterObject(this);
            string taskRoot = WebConfigurationManager.AppSettings["FirebaseTaskRootPath"];
            foreach (string root in new[] { string.IsNullOrWhiteSpace(taskRoot) ? "tasks" : taskRoot.Trim('/'), "taskBoards", "taskExams", "taskExamAttempts" })
                _ = ListenAsync(root, lifetime.Token);
        }
        private void Changed()
        {
            TaskCompletionSource<long> signal; long value;
            lock (sync) { value = ++version; signal = next; next = NewSignal(); }
            signal.TrySetResult(value);
        }
        public static async Task<JObject> WaitAsync(long previous, CancellationToken cancellation)
        {
            var feed = Instance.Value; Task<long> signal;
            lock (feed.sync)
            {
                if (previous != feed.version) return new JObject { ["version"] = feed.version, ["changed"] = true, ["connected"] = feed.connected == 4 };
                signal = feed.next.Task;
            }
            await Task.WhenAny(signal, Task.Delay(TimeSpan.FromSeconds(25), cancellation)).ConfigureAwait(false);
            cancellation.ThrowIfCancellationRequested();
            lock (feed.sync) return new JObject { ["version"] = feed.version, ["changed"] = previous != feed.version, ["connected"] = feed.connected == 4 };
        }
        private async Task ListenAsync(string root, CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                bool active = false;
                try
                {
                    var config = new UIMS.Services.FirebaseConfiguration(); config.EnsureAuthenticationEnabled();
                    var credential = (await GoogleCredential.GetApplicationDefaultAsync().ConfigureAwait(false)).CreateScoped("https://www.googleapis.com/auth/firebase.database", "https://www.googleapis.com/auth/userinfo.email");
                    string token = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync(cancellationToken: cancellation).ConfigureAwait(false);
                    using (var request = new HttpRequestMessage(HttpMethod.Get, config.DatabaseUrl.TrimEnd('/') + "/" + root + ".json"))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                        using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false))
                        {
                            response.EnsureSuccessStatusCode(); Interlocked.Increment(ref connected); active = true;
                            using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                            using (cancellation.Register(() => stream.Dispose()))
                            using (var reader = new StreamReader(stream))
                            {
                                while (!cancellation.IsCancellationRequested)
                                {
                                    string line = await reader.ReadLineAsync().ConfigureAwait(false); if (line == null) break;
                                    // Discard payload lines, including private answers/results. Clients receive only a sequence number.
                                    if (line == "event: put" || line == "event: patch") Changed();
                                    if (line == "event: cancel" || line == "event: auth_revoked") throw new IOException("Firebase change stream requires reconnection.");
                                }
                            }
                        }
                    }
                }
                catch (Exception) when (!cancellation.IsCancellationRequested) { /* Reconnect without logging credentials or Firebase payloads. */ }
                finally { if (active) { Interlocked.Decrement(ref connected); Changed(); } }
                if (!cancellation.IsCancellationRequested) await Task.Delay(2000, cancellation).ConfigureAwait(false);
            }
        }
        public void Stop(bool immediate) { lifetime.Cancel(); client.Dispose(); HostingEnvironment.UnregisterObject(this); }
    }
}
