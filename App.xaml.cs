using System.Configuration;
using System.Data;
using System.Windows;
using System.Globalization;
using System.Threading;

namespace Balanced_Gaming
{

    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var culture = new CultureInfo("en-GB");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata( 
                    System.Windows.Markup.XmlLanguage.GetLanguage(culture.IetfLanguageTag) 
                    )
                );
            base.OnStartup(e);
        }
    }

}
