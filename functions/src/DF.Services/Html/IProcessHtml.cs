using DF.Services.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace DF.Services.Html
{
    public interface IProcessHtml
    {
        Task<List<Deal>> ProcessAsync(string keywords);
    }
}
