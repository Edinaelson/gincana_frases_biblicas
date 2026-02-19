using System.IO;
using GincanaPassagensBiblicas.Components;
using GincanaPassagensBiblicas.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GincanaPassagensBiblicas
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddServerSideBlazor().AddHubOptions(options =>
            {
                options.EnableDetailedErrors = true;
                options.MaximumReceiveMessageSize = 32 * 1024 * 1024; // 32MB para aguentar imagens base64
                options.HandshakeTimeout = TimeSpan.FromSeconds(30);
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            });

            // Configure SQLite database (frases.db)
            var dbPath = Path.Combine(builder.Environment.ContentRootPath, "frases.db");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));
            // Register AI service (Switch between Gemini and Ollama here)
            // To use Gemini, uncomment the block below and comment out the Ollama block.
            /*
            builder.Services.AddHttpClient<GincanaPassagensBiblicas.Services.IGeminiService, GincanaPassagensBiblicas.Services.GeminiService>(client =>
            {
                client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
            });
            */

            // To use local Llama (via Ollama), use this block:
            builder.Services.AddHttpClient<GincanaPassagensBiblicas.Services.IGeminiService, GincanaPassagensBiblicas.Services.OllamaService>(client =>
            {
                client.BaseAddress = new Uri("http://localhost:11434/");
            });

            var app = builder.Build();

            // Ensure database file and schema are created
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();
                // If the database was created before adding ImagePath, add the column when missing
                var conn = db.Database.GetDbConnection();
                try
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA table_info('Usuarios');";
                        using (var reader = cmd.ExecuteReader())
                        {
                            var hasImagePath = false;
                            while (reader.Read())
                            {
                                if (reader.GetString(1) == "ImagePath")
                                {
                                    hasImagePath = true;
                                    break;
                                }
                            }

                            if (!hasImagePath)
                            {
                                using (var add = conn.CreateCommand())
                                {
                                    add.CommandText = "ALTER TABLE Usuarios ADD COLUMN ImagePath TEXT";
                                    add.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                    // ensure Frases.IsValid column exists
                    using (var cmd2 = conn.CreateCommand())
                    {
                        cmd2.CommandText = "PRAGMA table_info('Frases');";
                        using (var reader2 = cmd2.ExecuteReader())
                        {
                            var hasIsValid = false;
                            while (reader2.Read())
                            {
                                if (reader2.GetString(1) == "IsValid")
                                {
                                    hasIsValid = true;
                                    break;
                                }
                            }

                            if (!hasIsValid)
                            {
                                using (var add2 = conn.CreateCommand())
                                {
                                    add2.CommandText = "ALTER TABLE Frases ADD COLUMN IsValid INTEGER DEFAULT 0";
                                    add2.ExecuteNonQuery();
                                }
                            }
                        }
                    }

                    // ensure Pontuacoes.FraseId column exists
                    using (var cmd3 = conn.CreateCommand())
                    {
                        cmd3.CommandText = "PRAGMA table_info('Pontuacoes');";
                        using (var reader3 = cmd3.ExecuteReader())
                        {
                            var hasFraseId = false;
                            while (reader3.Read())
                            {
                                if (reader3.GetString(1) == "FraseId")
                                {
                                    hasFraseId = true;
                                    break;
                                }
                            }

                            if (!hasFraseId)
                            {
                                using (var add3 = conn.CreateCommand())
                                {
                                    add3.CommandText = "ALTER TABLE Pontuacoes ADD COLUMN FraseId INTEGER";
                                    add3.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                    // ensure Frases.CreatedAt column exists
                    using (var cmd4 = conn.CreateCommand())
                    {
                        cmd4.CommandText = "PRAGMA table_info('Frases');";
                        using (var reader4 = cmd4.ExecuteReader())
                        {
                            var hasCreatedAt = false;
                            while (reader4.Read())
                            {
                                if (reader4.GetString(1) == "CreatedAt")
                                {
                                    hasCreatedAt = true;
                                    break;
                                }
                            }

                            if (!hasCreatedAt)
                            {
                                using (var add4 = conn.CreateCommand())
                                {
                                    // SQLite does not allow non-constant defaults in ALTER TABLE. Add nullable column first,
                                    // then populate existing rows with current timestamp.
                                    add4.CommandText = "ALTER TABLE Frases ADD COLUMN CreatedAt TEXT";
                                    add4.ExecuteNonQuery();
                                }

                                using (var update = conn.CreateCommand())
                                {
                                    // set existing rows to current timestamp
                                    update.CommandText = "UPDATE Frases SET CreatedAt = (datetime('now')) WHERE CreatedAt IS NULL OR CreatedAt = ''";
                                    update.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
                finally
                {
                    try { conn.Close(); } catch { }
                }
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
