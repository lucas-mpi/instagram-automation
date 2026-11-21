namespace InstagramStoryBot.Configuration;

public sealed class BotOptions
{
    public string GeminiApiKey { get; init; } = string.Empty;
    public string GeminiModel { get; init; } = "gemini-3.8-flash";

    public string SupabaseUrl { get; init; } = string.Empty;
    public string SupabaseKey { get; init; } = string.Empty;
    public string SupabaseBucket { get; init; } = "stories";

    public string InstagramAccessToken { get; init; } = string.Empty;
    public string InstagramUserId { get; init; } = string.Empty;
    public string InstagramHandle { get; init; } = "@perfil.profissional";

    public bool DryRun { get; init; }
    public int? ForcedTemaId { get; init; }
    public string OutputDirectory { get; init; } = Path.Combine(Directory.GetCurrentDirectory(), "output");

    public bool HasSupabaseConfig =>
        !string.IsNullOrWhiteSpace(SupabaseUrl) &&
        !string.IsNullOrWhiteSpace(SupabaseKey);

    public bool HasInstagramConfig =>
        !string.IsNullOrWhiteSpace(InstagramAccessToken) &&
        !string.IsNullOrWhiteSpace(InstagramUserId);

    public static BotOptions Load(string[] args)
    {
        LoadDotEnvIfExists();

        bool dryRun = args.Any(a => a.Equals("--dry-run", StringComparison.OrdinalIgnoreCase));
        int? forcedId = null;

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals("--tema-id", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(args[i + 1], out int parsedId))
            {
                forcedId = parsedId;
            }
        }

        string rawModel = GetEnv("GEMINI_MODEL", "gemini-3.8-flash");
        if (rawModel.Equals("gemini-2.5-flash", StringComparison.OrdinalIgnoreCase))
        {
            rawModel = "gemini-3.8-flash";
        }

        return new BotOptions
        {
            GeminiApiKey = GetEnv("GEMINI_API_KEY"),
            GeminiModel = rawModel,
            SupabaseUrl = GetEnv("SUPABASE_URL").TrimEnd('/'),
            SupabaseKey = GetEnv("SUPABASE_KEY"),
            SupabaseBucket = GetEnv("SUPABASE_BUCKET", "stories"),
            InstagramAccessToken = GetEnv("INSTAGRAM_ACCESS_TOKEN"),
            InstagramUserId = GetEnv("INSTAGRAM_USER_ID"),
            InstagramHandle = GetEnv("INSTAGRAM_HANDLE", "@perfil.profissional"),
            DryRun = dryRun,
            ForcedTemaId = forcedId
        };
    }

    private static string GetEnv(string key, string defaultValue = "")
    {
        string? val = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(val) ? defaultValue : val.Trim();
    }

    private static void LoadDotEnvIfExists()
    {
        string[] candidatePaths =
        [
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.Combine(AppContext.BaseDirectory, ".env"),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env"))
        ];

        foreach (string path in candidatePaths)
        {
            if (!File.Exists(path))
                continue;

            foreach (string rawLine in File.ReadAllLines(path))
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                    continue;

                int sepIndex = line.IndexOf('=');
                if (sepIndex <= 0)
                    continue;

                string key = line[..sepIndex].Trim();
                string value = line[(sepIndex + 1)..].Trim().Trim('"', '\'');

                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }
            break;
        }
    }
}
