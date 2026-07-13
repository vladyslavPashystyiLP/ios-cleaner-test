using IosCleanerTest.Services;
using Microsoft.Extensions.Logging;

namespace IosCleanerTest
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if IOS
            builder.Services.AddSingleton<IPhotoCleanerService, PhotoCleanerService>();
            builder.Services.AddSingleton<ITestDataSeeder, TestDataSeeder>();
#else
            builder.Services.AddSingleton<IPhotoCleanerService, UnsupportedPhotoCleanerService>();
            builder.Services.AddSingleton<ITestDataSeeder, UnsupportedTestDataSeeder>();
#endif
            builder.Services.AddTransient<MainPage>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
