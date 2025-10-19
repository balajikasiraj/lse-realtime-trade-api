using Lse.Application.Services;
using Lse.Domain;
using Lse.Infrastructure;
using Lse.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Lse.Tests
{
    [TestClass]
    public class TradeServiceTests
    {
        private ITradeService _service = null!;
        private LseDbContext _db = null!;

        [TestInitialize]
        public void Init()
        {
            var opts = new DbContextOptionsBuilder<LseDbContext>().UseInMemoryDatabase("TestDb").Options;
            _db = new LseDbContext(opts);
            var repo = new TradeRepository(_db);
            _service = new TradeService(repo);
        }

        [TestMethod]
        public async Task RecordAndGetValue()
        {
            var t = new Trade { Ticker = "VOD", Price = 120.5m, Quantity = 10, BrokerId = "B1" };
            await _service.RecordTradeAsync(t);
            var val = await _service.GetCurrentValueAsync("VOD");
            Assert.IsNotNull(val);
            Assert.AreEqual(120.5m, val.Value);
        }
    }
}
