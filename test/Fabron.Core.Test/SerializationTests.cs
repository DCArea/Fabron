using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Fabron.Events;
using Xunit;

namespace Fabron.Core.Test
{
    public class SerializationTests
    {
        [Fact]
        public void EnvelopRoundTrip()
        {
            var envelop = new FabronEventEnvelop("test", DateTimeOffset.Now, "{}", new Dictionary<string, string> { { "abc", "qwe" } });
            var json = JsonSerializer.Serialize(envelop);
            var s = JsonSerializer.Deserialize<FabronEventEnvelop>(json);
            Assert.Equal("qwe", (string?)s!.Extensions["abc"]);
        }
    }
}
