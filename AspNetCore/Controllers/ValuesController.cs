using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace AspNetCore.Controllers
{
	[Route("api/[controller]")]
	public class ValuesController : Controller
	{
		private IMemoryCache _cache;

		public ValuesController(IMemoryCache memoryCache)
		{
			_cache = memoryCache;
		}

		// GET api/values
		[HttpGet]
		public async Task<IEnumerable<string>> Get()
		{
			var cts = new CancellationTokenSource();
			var token = new CancellationChangeToken(cts.Token);

			ICacheEntry e = null;
			var parent = await _cache.GetOrCreateAsync("Parent", entry =>
			{
				e = entry;
				entry.SlidingExpiration = TimeSpan.FromSeconds(3);
				entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10);

				return Task.FromResult(new[] { DateTime.Now.ToString() });
			});

			var child = await _cache.GetOrCreateAsync("Child", entry =>
			{
				entry.AddExpirationToken(token);
				return Task.FromResult(new[] { "Hi" });
			});

			if (e != null)
			{
				e.RegisterPostEvictionCallback((k, v, reason, state) =>
				{
					cts.Cancel();
					var message = $"Entry was evicted. Reason: {reason}.";
					parent = new[] { message };
				});
			}

			return parent.Concat(child);
		}

		// GET api/values/5
		[HttpGet("{id}")]
		public IActionResult Get(int id)
		{
			var test = new { Name = "MyName", Age = 100500 };
			return Json(test);
		}

		// POST api/values
		[HttpPost]
		public void Post([FromBody]string value)
		{
		}

		// PUT api/values/5
		[HttpPut("{id}")]
		public void Put(int id, [FromBody]string value)
		{
		}

		// DELETE api/values/5
		[HttpDelete("{id}")]
		public void Delete(int id)
		{
		}
	}
}
