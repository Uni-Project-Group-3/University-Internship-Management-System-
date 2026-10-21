using System;
using System.IO;

namespace University_Internship_Management_System.Services
{
    public static class FirebaseConfiguration
    {
        public const string ProjectId = "intership-managment-system";
        public const string DatabaseUrl = "https://intership-managment-system-default-rtdb.europe-west1.firebasedatabase.app";
        // Firebase Web configuration is public application metadata, never an administrator credential.
        public static string WebApiKey => Environment.GetEnvironmentVariable("FIREBASE_API_KEY") ?? "AIzaSyAwjWNcfHHB43ktxN6R2qglfL76C6p1Q90";
        public const string UsersPath = "users";
        public const string UidMappingsPath = "usersByUid";
        public static object WebConfiguration => new
        {
            apiKey = WebApiKey, projectId = ProjectId, databaseURL = DatabaseUrl,
            authDomain = ProjectId + ".firebaseapp.com",
            storageBucket = ProjectId + ".firebasestorage.app", messagingSenderId = "122376636032",
            appId = "1:122376636032:web:d90d30837aacc38c754865", measurementId = "G-TKPYNNR3X9"
        };

        public static string Required(string name)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("Firebase server configuration is incomplete.");
            return value.Trim();
        }

        public static void ValidateCredentials()
        {
            string file = Path.GetFullPath(Required("GOOGLE_APPLICATION_CREDENTIALS"));
            string site = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            bool inRepository = false;
            for (var directory = Directory.GetParent(file); directory != null; directory = directory.Parent)
                if (Directory.Exists(Path.Combine(directory.FullName, ".git")) || File.Exists(Path.Combine(directory.FullName, ".git"))) { inRepository = true; break; }
            if (!File.Exists(file) || inRepository || file.StartsWith(site, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Configure protected Application Default Credentials outside the application.");
        }
    }
}
