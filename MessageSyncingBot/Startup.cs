// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.DependencyInjection;
using MessageSyncingBot.Bots;
using MessageSyncingBot.Dialogs.Root;
using MessageSyncingBot.Helpers;
using MessageSyncingBot.Interfaces;
using MessageSyncingBot.Services;

namespace MessageSyncingBot
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddCors(o => o.AddPolicy("AllowAllOrigins", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            }));

            services.AddMvc().SetCompatibilityVersion(Microsoft.AspNetCore.Mvc.CompatibilityVersion.Version_2_1);

            services.AddSingleton<IBotServices, BotServices>();

            services.AddSingleton<IBotFrameworkHttpAdapter, BotFrameworkHttpAdapter>();

            services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();

            services.AddSingleton<IStorage, MemoryStorage>();

            services.AddSingleton<UserState>();

            services.AddSingleton<ConversationState>();

            services.AddSingleton<RootDialog>();

            services.AddBot<MainBot<RootDialog>>(options =>
            {
      
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseMvc();
        }
    }
}
