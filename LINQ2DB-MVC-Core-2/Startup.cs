using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using LINQ2DB_MVC_Core_2.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using LINQ2DB_MVC_Core_2.Models;
using LINQ2DB_MVC_Core_2.Data;
using LinqToDB.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Diagnostics;

namespace LINQ2DB_MVC_Core_2
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // allow any class with D.I. constructor expecting IConfiguration
            //  to access the appsettings.json file.
            services.AddSingleton(Configuration);

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
            });
            services.Configure<SecurityStampValidatorOptions>(options =>
            {
                // enables immediate logout, after updating the user's stat.
                options.ValidationInterval = TimeSpan.Zero;
            });

            services.AddTransient<IEmailSender, SendGridEmailSender>();

            // This leans heavily on understanding the Microsoft DotNet Core Identity code (and also the Linq2DB Identity code): -
            //      (see: https://github.com/dotnet/aspnetcore/tree/2.1.3)
            //      (see also: https://github.com/linq2db/LinqToDB.Identity)
            // Set up Linq2DB connection
            LinqToDB.Data.DataConnection.DefaultSettings = new Linq2dbSettings(Configuration);

            // configure app to use Linq2DB for the identity provider: BEGIN
            services.AddTransient<IUserStore<AspNetUsers>, AspNetUsersStore>();
            services.AddTransient<LinqToDB.Identity.IdentityRole<string>, AspNetRoles>();
            services.AddTransient<LinqToDB.Identity.IdentityUserClaim<string>, AspNetUserClaims>();
            services.AddTransient<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>, AspNetUserClaims>();
            services.AddTransient<LinqToDB.Identity.IdentityUserRole<string>, AspNetUserRoles>();
            services.AddTransient<LinqToDB.Identity.IdentityUserLogin<string>, AspNetUserLogins>();
            services.AddTransient<LinqToDB.Identity.IdentityUserToken<string>, AspNetUserTokens>();
            services.AddTransient<LinqToDB.Identity.IdentityRoleClaim<string>, AspNetRoleClaims>();

            services.AddIdentity<AspNetUsers, AspNetRoles>(options =>
            {
                //options.SignIn.RequireConfirmedEmail = true;
            })
                .AddLinqToDBStores(
                    new DefaultConnectionFactory(),
                    typeof(string),
                    typeof(AspNetUserClaims),
                    typeof(AspNetUserRoles),
                    typeof(AspNetUserLogins),
                    typeof(AspNetUserTokens),
                    typeof(AspNetRoleClaims)
                    )
                .AddUserStore<AspNetUsersStore>()
                .AddDefaultTokenProviders();

            services.AddAuthentication()
                .AddCookie(options =>
                {
                    options.Cookie.HttpOnly = false;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                });
            // configure app to use Linq2DB for the identity provider: END
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "PithToken";
                // TicketStore persistes the sessions to AspNetUserTokens...
                //  ...this allows you to clear the AspNetUserTokens table if you need to force-logout everyone.
                options.SessionStore = new TicketStore(new DefaultConnectionFactory());
            });

            // set up to use MVC
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            // Notes for SSO: -
            //  1. For Google, FaceBook and Microsoft... ...All the coding has been done for you.
            //  2. you need to set the ID and Secret for each button in appsettings.json
            //  3. If you're not sure how to get an ID or a secret - then look at the instructions at
            //      https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/?view=aspnetcore-3.1&tabs=visual-studio
            //      (select the type of account in the left menu frame, and then look for the part about setting up the app)
            //  4. If you change the order of the code blocks below (put Facebook first for example) - then you will 
            //      change the order of the buttons on the logni screen.
            //
            // Finally - for keeping secrets on deploy...
            //  here's a good article if you don't know anything yet: -
            //  https://medium.com/google-cloud/adding-social-login-to-your-asp-net-core-2-1-google-cloud-platform-application-1baae89f1dc8

            // Set up Google SSO (add the ClientId and the Secret to the "Authentication" section of appsettings.json
            var oGoogleSSO = new SSOConfigModel(Configuration, "GoogleSSO", "ClientId", "Secret");
            if (oGoogleSSO.HasSettings)
            {
                services.AddAuthentication().AddGoogle(options =>
                    {
                        options.ClientId = oGoogleSSO.ID;
                        options.ClientSecret = oGoogleSSO.Secret;
                    });
            }

            // Set up Microsoft SSO (add the ClientId and the ClientSecret to the "Authentication" section of appsettings.json
            var oMicrosoftSSO = new SSOConfigModel(Configuration, "Microsoft", "ClientId", "ClientSecret");
            if (oMicrosoftSSO.HasSettings)
            {
                services.AddAuthentication().AddMicrosoftAccount(options =>
                {
                    options.ClientId = oMicrosoftSSO.ID;
                    options.ClientSecret = oMicrosoftSSO.Secret;
                });
            }

            // Set up Facebook SSO (add the AppId and the AppSecret to the "Authentication" section of appsettings.json
            var oFacebookSSO = new SSOConfigModel(Configuration, "Facebook", "AppId", "AppSecret");
            if (oFacebookSSO.HasSettings)
            {
                services.AddAuthentication().AddFacebook(options =>
                {
                    options.ClientId = oFacebookSSO.ID;
                    options.ClientSecret = oFacebookSSO.Secret;
                });
            }

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = new PathString("/Identity/Account/Login");
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
                //options.DataProtectionProvider =
                //    DataProtectionProvider.Create(new DirectoryInfo("C:\\Projects\\ProofOfConcept\\Identity\\artifacts"));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // Create basic tables required for Auth (if they don't exist yet).
            using (var _db = new Services.DataConnection())
            {
                try
                {
                    _db.GetTable<AspNetUsers>().FirstOrDefault();
                }
                catch (Exception poFirstErr)
                {
                    if (poFirstErr.Message.ToLowerInvariant().Contains("cannot open database"))
                    {
                        throw new Exception("Please run the SQL Script \"20200406 Create MVC Linq2DB Template Tables.sql\" on your server to create the sample database.");
                    }
                    else
                    {
                        throw poFirstErr;
                    }
                }

                // For DEMO Purpose only: -
                //      Just in case the user created the database without the tables - make sure that the basic tables are in place.
                _db.TryCreateTable<AspNetUsers>();
                _db.TryCreateTable<AspNetRoles>();
                _db.TryCreateTable<AspNetUserClaims>();
                _db.TryCreateTable<AspNetRoleClaims>();
                _db.TryCreateTable<AspNetUserLogins>();
                _db.TryCreateTable<AspNetUserRoles>();
                _db.TryCreateTable<AspNetUserTokens>();

            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
