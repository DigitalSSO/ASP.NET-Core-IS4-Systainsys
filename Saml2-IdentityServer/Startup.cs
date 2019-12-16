using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using IdentityServer4;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Saml2;
using SampleIdentityServer4;
using Sustainsys.Saml2;
using Sustainsys.Saml2.AspNetCore2;
using Sustainsys.Saml2.Metadata;
using Sustainsys.Saml2.WebSso;

namespace Saml2_IdentityServer
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
            //services.Configure<CookiePolicyOptions>(options =>
            //{
            //    // This lambda determines whether user consent for non-essential cookies is needed for a given request.
            //    options.CheckConsentNeeded = context => true;
            //    options.MinimumSameSitePolicy = SameSiteMode.None;
            //});
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            services.AddIdentityServer()
                .AddDeveloperSigningCredential()
                .AddInMemoryPersistedGrants()
                .AddInMemoryIdentityResources(Config.GetIdentityResources())
                .AddInMemoryApiResources(Config.GetApiResources())
                .AddInMemoryClients(Config.GetClients())
                .AddTestUsers(Config.GetUsers());

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = Saml2Defaults.Scheme;
                x.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            }).AddSaml2(options =>
            {
                options.SPOptions.EntityId = new EntityId("https://localhost:44322/saml2");

                var idp = new IdentityProvider(new EntityId("http://adfs.groupyfy.com/adfs/services/trust"), options.SPOptions)
                {
                    MetadataLocation = "https://adfs.groupyfy.com/FederationMetadata/2007-06/FederationMetadata.xml"
                };

                //idp.SigningKeys.AddConfiguredKey(new X509Certificate2(certificate));
                options.IdentityProviders.Add(idp);
            }).AddOpenIdConnect("demoidsrv", "IdentityServer", options =>
            {
                options.Authority = "https://adfs.groupyfy.com/adfs";
                options.ClientId = "12345-1234-1234-1234-12345";
                options.ClientSecret = "HXzULIiN1pa755-stppe848mJrrst8f1Wk3xcTng";
                options.ResponseType = "id_token";
                options.SaveTokens = true;
                options.CallbackPath = new PathString("/signin-idsrv");
                options.SignedOutCallbackPath = new PathString("/signout-callback-idsrv");
                options.RemoteSignOutPath = new PathString("/signout-idsrv");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
            }).AddCookie(); 

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseIdentityServer();
           // app.UseCookiePolicy();
           //  app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
