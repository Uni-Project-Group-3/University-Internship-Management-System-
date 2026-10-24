using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Shared;

namespace UIMS.Services
{
    // Coordinator-managed dropdown lists, stored in Firebase at
    // systemOptions/{list} = { name, updatedAt, items: { key: { label, order, active } } }.
    // Keys are generated from the names, so callers never invent Firebase IDs.
    public sealed class SystemOptionsService
    {
        private const string Root = "systemOptions";
        private readonly IStudentDatabase db;
        private readonly Func<string, Task<bool>> activeAccount;
        public SystemOptionsService() { var gateway = new StudentFirebaseGateway(); db = gateway; activeAccount = gateway.IsActiveAuthenticationUidAsync; }
        public SystemOptionsService(IStudentDatabase database, Func<string, Task<bool>> account) { db = database; activeAccount = account; }

        private static JObject O(JToken value) { return value as JObject ?? new JObject(); }
        private static string T(JToken value, string key) { return (string)value?[key] ?? ""; }

        public static string Slug(string text)
        {
            var builder = new StringBuilder();
            foreach (char c in (text ?? "").Trim().ToLowerInvariant())
                builder.Append(char.IsLetterOrDigit(c) && c < 128 ? c : '-');
            string slug = Regex.Replace(builder.ToString(), "-+", "-").Trim('-');
            if (slug.Length > 60) slug = slug.Substring(0, 60).Trim('-');
            return slug.Length == 0 ? "option" : slug;
        }
        private static string ListKey(string value)
        {
            if (!Regex.IsMatch(value ?? "", "\\A[a-z0-9][a-z0-9-]{0,59}\\z")) throw new FirebaseException(400, "Choose an existing options list.");
            return value;
        }
        private static string Label(string value, string what)
        {
            string label = (value ?? "").Trim();
            if (label.Length == 0) throw new FirebaseException(400, "Enter the " + what + ".");
            if (label.Length > 120) throw new FirebaseException(400, "The " + what + " is limited to 120 characters.");
            return label;
        }
        private static string UniqueKey(JObject existing, string text)
        {
            string key = Slug(text), candidate = key;
            for (int n = 2; existing[candidate] != null; n++) candidate = key + "-" + n;
            return candidate;
        }
        private static JObject Item(JProperty p)
        {
            return new JObject { ["key"] = p.Name, ["label"] = T(p.Value, "label"), ["order"] = (int?)p.Value["order"] ?? 0, ["active"] = (bool?)p.Value["active"] != false };
        }
        private static JArray Sorted(JObject items, bool activeOnly)
        {
            return new JArray(items.Properties().Where(p => !activeOnly || (bool?)p.Value["active"] != false)
                .OrderBy(p => (int?)p.Value["order"] ?? 0).ThenBy(p => T(p.Value, "label"), StringComparer.CurrentCultureIgnoreCase).Select(Item));
        }

        private async Task<string> Authorize(string uid, bool coordinator)
        {
            if (string.IsNullOrWhiteSpace(uid) || !await activeAccount(StudentManagementService.Key(uid))) throw new FirebaseException(401, "Sign in with an active account.");
            if (!coordinator) return uid;
            var profile = O(await db.ReadAsync("users/" + uid));
            try { if (FirebaseSignInService.ProfileResult(uid, profile).Role != "coordinator") throw new FirebaseException(403, "Coordinator access is required."); }
            catch (FirebaseLoginException) { throw new FirebaseException(403, "An active, fully configured coordinator profile is required."); }
            return uid;
        }

        // Any signed-in user may read one list's active options to fill a dropdown.
        public async Task<JObject> ListAsync(string uid, string list)
        {
            await Authorize(uid, false);
            var value = O(await db.ReadAsync(Root + "/" + ListKey(list)));
            return new JObject { ["key"] = list, ["name"] = T(value, "name"), ["items"] = Sorted(O(value["items"]), true), ["updatedAt"] = T(value, "updatedAt") };
        }

        public async Task<JObject> AllAsync(string uid)
        {
            await Authorize(uid, true);
            var root = O(await db.ReadAsync(Root));
            var lists = new JArray(root.Properties().OrderBy(p => T(p.Value, "name"), StringComparer.CurrentCultureIgnoreCase)
                .Select(p => new JObject { ["key"] = p.Name, ["name"] = T(p.Value, "name").Length > 0 ? T(p.Value, "name") : p.Name, ["items"] = Sorted(O(p.Value["items"]), false), ["updatedAt"] = T(p.Value, "updatedAt") }));
            return new JObject { ["lists"] = lists };
        }

        public async Task<JObject> SaveListAsync(string uid, string list, string name)
        {
            await Authorize(uid, true);
            name = Label(name, "list name");
            var root = O(await db.ReadAsync(Root));
            if (root.Properties().Any(p => p.Name != list && string.Equals(T(p.Value, "name"), name, StringComparison.CurrentCultureIgnoreCase)))
                throw new FirebaseException(409, "A list with this name already exists.");
            string key = string.IsNullOrEmpty(list) ? UniqueKey(root, name) : ListKey(list);
            if (!string.IsNullOrEmpty(list) && root[key] == null) throw new FirebaseException(404, "This list no longer exists. Refresh the page.");
            string now = DateTimeOffset.UtcNow.ToString("o");
            await db.PatchAsync(Root + "/" + key, new JObject { ["name"] = name, ["updatedAt"] = now, ["updatedBy"] = uid });
            return new JObject { ["saved"] = true, ["key"] = key };
        }

        public async Task<JObject> DeleteListAsync(string uid, string list)
        {
            await Authorize(uid, true);
            await db.DeleteAsync(Root + "/" + ListKey(list));
            return new JObject { ["deleted"] = true, ["key"] = list };
        }

        public async Task<JObject> SaveOptionAsync(string uid, string list, string key, string label, int? order, bool active)
        {
            await Authorize(uid, true);
            label = Label(label, "option label");
            var value = await db.ReadAsync(Root + "/" + ListKey(list)) as JObject;
            if (value == null) throw new FirebaseException(404, "This list no longer exists. Refresh the page.");
            var items = O(value["items"]);
            if (items.Properties().Any(p => p.Name != key && string.Equals(T(p.Value, "label"), label, StringComparison.CurrentCultureIgnoreCase)))
                throw new FirebaseException(409, "This list already has an option with this label.");
            if (!string.IsNullOrEmpty(key) && items[key] == null) throw new FirebaseException(404, "This option no longer exists. Refresh the page.");
            string itemKey = string.IsNullOrEmpty(key) ? UniqueKey(items, label) : key;
            int position = order ?? (string.IsNullOrEmpty(key) ? (items.Properties().Select(p => (int?)p.Value["order"] ?? 0).DefaultIfEmpty(0).Max() + 1) : ((int?)items[key]["order"] ?? 0));
            string now = DateTimeOffset.UtcNow.ToString("o");
            // One atomic multi-location update: the option and the list's change marker.
            await db.PatchAsync(Root + "/" + list, new JObject
            {
                ["items/" + itemKey + "/label"] = label, ["items/" + itemKey + "/order"] = position, ["items/" + itemKey + "/active"] = active,
                ["items/" + itemKey + "/updatedAt"] = now, ["items/" + itemKey + "/updatedBy"] = uid, ["updatedAt"] = now
            });
            return new JObject { ["saved"] = true, ["key"] = itemKey };
        }

        public async Task<JObject> DeleteOptionAsync(string uid, string list, string key)
        {
            await Authorize(uid, true);
            if (string.IsNullOrEmpty(key) || key.Length > 80 || key.Any(c => ".#$[]/\\".Contains(c) || char.IsControl(c))) throw new FirebaseException(400, "Choose an existing option.");
            await db.PatchAsync(Root + "/" + ListKey(list), new JObject { ["items/" + key] = JValue.CreateNull(), ["updatedAt"] = DateTimeOffset.UtcNow.ToString("o") });
            return new JObject { ["deleted"] = true, ["key"] = key };
        }
    }
}
