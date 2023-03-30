using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Core;
using System.Threading.Tasks;
using X.PagedList;

namespace Moonglade.Web.Pages
{
    public class CategoryListModel : PageModel
    {
        private readonly IPostQueryService _postQueryService;
        private readonly ICategoryService _categoryService;
        private readonly IBlogConfig _blogConfig;
        private readonly IBlogCache _cache;

        [BindProperty(SupportsGet = true)]
        public int P { get; set; }
        public StaticPagedList<PostDigest> Posts { get; set; }

        public CategoryListModel(
            ICategoryService categoryService,
            IBlogConfig blogConfig,
            IBlogCache cache,
            IPostQueryService postQueryService)
        {
            _categoryService = categoryService;
            _blogConfig = blogConfig;
            _cache = cache;
            _postQueryService = postQueryService;

            P = 1;
        }

        public async Task<IActionResult> OnGetAsync(string routeName)
        {
            if (string.IsNullOrWhiteSpace(routeName)) return NotFound();

            var pageSize = _blogConfig.ContentSettings.PostListPageSize;
            var cat = await _categoryService.GetAsync(routeName);

            if (cat is null) return NotFound();

            ViewData["CategoryDisplayName"] = cat.DisplayName;
            ViewData["CategoryRouteName"] = cat.RouteName;
            ViewData["CategoryDescription"] = cat.Note;

            var postCount = _cache.GetOrCreate(CacheDivision.PostCountCategory, cat.Id.ToString(),
                _ => _postQueryService.CountByCategory(cat.Id));

            var postList = await _postQueryService.ListAsync(pageSize, P, cat.Id);

            Posts = new(postList, P, pageSize, postCount);
            return Page();
        }
    }
}
