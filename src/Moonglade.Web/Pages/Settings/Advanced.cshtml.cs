using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Configuration;
using Moonglade.Web.Models.Settings;

namespace Moonglade.Web.Pages.Settings
{
    public class AdvancedModel : PageModel
    {
        private readonly IBlogConfig _blogConfig;
        public AdvancedSettingsViewModel ViewModel { get; set; }

        public AdvancedModel(IBlogConfig blogConfig)
        {
            _blogConfig = blogConfig;
        }

        public void OnGet()
        {
            var settings = _blogConfig.AdvancedSettings;
            ViewModel = new()
            {
                RobotsTxtContent = settings.RobotsTxtContent,
                EnablePingbackSend = settings.EnablePingBackSend,
                EnablePingbackReceive = settings.EnablePingBackReceive,
                EnableOpenGraph = settings.EnableOpenGraph,
                EnableOpenSearch = settings.EnableOpenSearch,
                EnableMetaWeblog = settings.EnableMetaWeblog,
                WarnExternalLink = settings.WarnExternalLink,
                AllowScriptsInPage = settings.AllowScriptsInPage,
                ShowAdminLoginButton = settings.ShowAdminLoginButton
            };
        }
    }
}
